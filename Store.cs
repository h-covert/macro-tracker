using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MacroTracker {
public sealed class Database : IDisposable {
    IntPtr handle;
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_open16([MarshalAs(UnmanagedType.LPWStr)] string path, out IntPtr db);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_close(IntPtr db);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_prepare16_v2(IntPtr db, [MarshalAs(UnmanagedType.LPWStr)] string sql, int bytes, out IntPtr stmt, IntPtr tail);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_bind_text16(IntPtr stmt, int index, [MarshalAs(UnmanagedType.LPWStr)] string value, int bytes, IntPtr destroy);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_step(IntPtr stmt);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_finalize(IntPtr stmt);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_column_count(IntPtr stmt);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern IntPtr sqlite3_column_name16(IntPtr stmt, int col);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern IntPtr sqlite3_column_text16(IntPtr stmt, int col);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern IntPtr sqlite3_errmsg16(IntPtr db);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_busy_timeout(IntPtr db,int ms);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern IntPtr sqlite3_backup_init(IntPtr dest, byte[] destName, IntPtr source, byte[] sourceName);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_backup_step(IntPtr backup,int pages);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_backup_finish(IntPtr backup);
    public Database(string path) { if(sqlite3_open16(path,out handle)!=0) { string msg=Error(); Dispose(); throw new IOException(msg); } sqlite3_busy_timeout(handle,5000); }
    string Error() { return Marshal.PtrToStringUni(sqlite3_errmsg16(handle)); }
    public List<Dictionary<string,string>> Query(string sql, params object[] values) {
        IntPtr stmt; if(sqlite3_prepare16_v2(handle,sql,-1,out stmt,IntPtr.Zero)!=0) throw new IOException(Error());
        try { for(int i=0;i<values.Length;i++) { string s=Convert.ToString(values[i],CultureInfo.InvariantCulture); if(sqlite3_bind_text16(stmt,i+1,s,-1,new IntPtr(-1))!=0) throw new IOException(Error()); }
            var rows=new List<Dictionary<string,string>>(); int code;
            while((code=sqlite3_step(stmt))==100) { var row=new Dictionary<string,string>(); for(int i=0;i<sqlite3_column_count(stmt);i++) row[Marshal.PtrToStringUni(sqlite3_column_name16(stmt,i))]=Marshal.PtrToStringUni(sqlite3_column_text16(stmt,i))??""; rows.Add(row); }
            if(code!=101) throw new IOException(Error()); return rows;
        } finally { sqlite3_finalize(stmt); }
    }
    public void Transaction(Action action) { Query("BEGIN IMMEDIATE"); try { action(); Query("COMMIT"); } catch { Query("ROLLBACK"); throw; } }
    public void CopyTo(Database target) {
        byte[] name=Encoding.ASCII.GetBytes("main\0"); IntPtr b=sqlite3_backup_init(target.handle,name,handle,name); if(b==IntPtr.Zero) throw new IOException(target.Error());
        int result=sqlite3_backup_step(b,-1); int finish=sqlite3_backup_finish(b); if(result!=101 || finish!=0) throw new IOException("Database backup failed. Please try again.");
    }
    public void Dispose() { if(handle!=IntPtr.Zero) { sqlite3_close(handle); handle=IntPtr.Zero; } }
}
public sealed partial class Store : IDisposable {
    public readonly string Path; public Database Db;
    public static readonly string[] MacroNames={"Calories","Protein","Carbs","Fat"};
    public static readonly string[] Categories={"Breakfast","Lunch","Dinner","Snack","Drink","Other"};
    public static string Day(DateTime d) {return d.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);}
    public static double Num(string s) { return double.Parse(s,CultureInfo.InvariantCulture); }
    public static string F(double x) { return x.ToString("0.#",CultureInfo.CurrentCulture); }
    public Store(string path) {
        Path=path; Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)); Db=new Database(path);
        if(Db.Query("SELECT name FROM sqlite_master WHERE type='table' AND name='settings'").Count>0 && Get("schema","")=="1")
            Backup(path+".before-quantity-upgrade.bak");
        if(Db.Query("SELECT name FROM sqlite_master WHERE type='table' AND name='foods'").Count>0 && Db.Query("SELECT name FROM sqlite_master WHERE type='table' AND name='library'").Count==0) Backup(path+".before-library-upgrade.bak");
        Initialize(); InitializeLibrary();
    }
    void Initialize() {
        Db.Query("PRAGMA foreign_keys=ON"); Db.Query("PRAGMA journal_mode=DELETE");
        Db.Transaction(delegate {
            Db.Query("CREATE TABLE IF NOT EXISTS settings (key TEXT PRIMARY KEY,value TEXT NOT NULL)");
            Db.Query("CREATE TABLE IF NOT EXISTS days (day TEXT PRIMARY KEY, calories REAL NOT NULL, protein REAL NOT NULL, carbs REAL NOT NULL, fat REAL NOT NULL)");
            Db.Query("CREATE TABLE IF NOT EXISTS foods (id INTEGER PRIMARY KEY AUTOINCREMENT,day TEXT NOT NULL REFERENCES days(day),time TEXT NOT NULL,name TEXT NOT NULL,calories REAL NOT NULL CHECK(calories>=0),protein REAL NOT NULL CHECK(protein>=0),carbs REAL NOT NULL CHECK(carbs>=0),fat REAL NOT NULL CHECK(fat>=0),category TEXT NOT NULL,serving TEXT NOT NULL,notes TEXT NOT NULL)");
            Db.Query("CREATE TABLE IF NOT EXISTS favorites (id INTEGER PRIMARY KEY AUTOINCREMENT,name TEXT NOT NULL,calories REAL NOT NULL,protein REAL NOT NULL,carbs REAL NOT NULL,fat REAL NOT NULL,category TEXT NOT NULL,serving TEXT NOT NULL,notes TEXT NOT NULL)");
            Db.Query("CREATE TABLE IF NOT EXISTS weights (day TEXT PRIMARY KEY,kg REAL NOT NULL CHECK(kg>0))");
            Db.Query("CREATE TABLE IF NOT EXISTS reminders (id INTEGER PRIMARY KEY AUTOINCREMENT,time TEXT NOT NULL,message TEXT NOT NULL,enabled INTEGER NOT NULL DEFAULT 1,last_day TEXT NOT NULL DEFAULT '',snooze TEXT NOT NULL DEFAULT '')");
            Db.Query("CREATE INDEX IF NOT EXISTS food_day ON foods(day,time)");
            if(Get("schema","")=="") {
                Set("targets","2450|190|240|75"); Set("schema","1"); Set("unit","lb"); Set("tray","1");
                string[] times={"08:00","12:30","15:30","19:00","21:00"};
                string[] messages={"Don't forget to log your breakfast.","Log your lunch and check your protein progress.","Anything to log? Don't forget snacks or drinks.","Log dinner and see where your macros stand.","Final macro check — anything you forgot to log?"};
                for(int i=0;i<times.Length;i++) Db.Query("INSERT INTO reminders(time,message) VALUES(?,?)",times[i],messages[i]);
            }
            foreach(string table in new[]{"foods","favorites"})
                if(!Db.Query("PRAGMA table_info("+table+")").Any(r=>r["name"]=="quantity"))
                    Db.Query("ALTER TABLE "+table+" ADD COLUMN quantity REAL NOT NULL DEFAULT 1 CHECK(quantity>0)");
            Set("schema","2");
        });
    }
    public string Get(string key,string fallback) {var r=Db.Query("SELECT value FROM settings WHERE key=?",key); return r.Count==0?fallback:r[0]["value"];}
    public void Set(string key,string value) {Db.Query("INSERT OR REPLACE INTO settings(key,value) VALUES(?,?)",key,value);}
    public double[] Defaults() {return Get("targets","2450|190|240|75").Split('|').Select(Num).ToArray();}
    public void EnsureDay(DateTime date) {bool fresh=Db.Query("SELECT day FROM days WHERE day=?",Day(date)).Count==0;var t=ScheduledTargets(date); Db.Query("INSERT OR IGNORE INTO days VALUES(?,?,?,?,?)",Day(date),t[0],t[1],t[2],t[3]);if(fresh){var type=Db.Query("SELECT preset FROM schedule WHERE weekday=?",(int)date.DayOfWeek);Set("daytype:"+Day(date),type.Count==0?"Default":type[0]["preset"]);}}
    public double[] Targets(DateTime date) {var r=Db.Query("SELECT * FROM days WHERE day=?",Day(date)); return r.Count==0?ScheduledTargets(date):Values(r[0]);}
    public static double[] Values(Dictionary<string,string> r) {return new[]{Num(r["calories"]),Num(r["protein"]),Num(r["carbs"]),Num(r["fat"])};}
    public double[] Totals(DateTime date) {return Values(Db.Query("SELECT coalesce(sum(calories),0) calories,coalesce(sum(protein),0) protein,coalesce(sum(carbs),0) carbs,coalesce(sum(fat),0) fat FROM foods WHERE day=?",Day(date))[0]);}
    public void SaveTargets(double[] values,bool today) {Validate(values,true); Db.Transaction(delegate {Set("targets",string.Join("|",values.Select(v=>v.ToString(CultureInfo.InvariantCulture)))); if(today) Db.Query("UPDATE days SET calories=?,protein=?,carbs=?,fat=? WHERE day=?",values[0],values[1],values[2],values[3],Day(DateTime.Today));});}
    public static void Validate(double[] values,bool positive) {if(values.Length!=4 || values.Any(v=>double.IsNaN(v)||double.IsInfinity(v)||v<(positive?0.1:0)||v>100000)) throw new ArgumentException("Enter valid, nonnegative macro values (targets must be greater than zero). Maximum: 100,000.");}
    public void SaveFood(string id,DateTime date,string name,double[] v,string category,string serving,string notes,string time,double quantity=1) {
        FoodPortion.ValidateQuantity(quantity); time=FoodPortion.NormalizeTime(time);
        Validate(v,false); if(string.IsNullOrWhiteSpace(name)||name.Length>200) throw new ArgumentException("Enter a food name of 1–200 characters."); if(!Categories.Contains(category)) throw new ArgumentException("Select a meal category.");
        Db.Transaction(delegate {EnsureDay(date); if(id==null) Db.Query("INSERT INTO foods(day,time,name,calories,protein,carbs,fat,category,serving,notes,quantity) VALUES(?,?,?,?,?,?,?,?,?,?,?)",Day(date),time,name.Trim(),v[0],v[1],v[2],v[3],category,serving,notes,quantity); else Db.Query("UPDATE foods SET name=?,calories=?,protein=?,carbs=?,fat=?,category=?,serving=?,notes=?,time=?,quantity=? WHERE id=?",name.Trim(),v[0],v[1],v[2],v[3],category,serving,notes,time,quantity,id);});
    }
    public List<Dictionary<string,string>> Foods(DateTime date) {return Db.Query("SELECT * FROM foods WHERE day=? ORDER BY time DESC,id DESC",Day(date));}
    public void Favorite(Dictionary<string,string> r) {double q=FoodPortion.Quantity(r);if(Db.Query("SELECT id FROM favorites WHERE name=? AND calories=? AND protein=? AND carbs=? AND fat=? AND serving=? AND quantity=?",r["name"],r["calories"],r["protein"],r["carbs"],r["fat"],r["serving"],q).Count>0)return; Db.Query("INSERT INTO favorites(name,calories,protein,carbs,fat,category,serving,notes,quantity) VALUES(?,?,?,?,?,?,?,?,?)",r["name"],r["calories"],r["protein"],r["carbs"],r["fat"],r["category"],r["serving"],r["notes"],q);}
    public List<Dictionary<string,string>> Recent() {return Db.Query("SELECT * FROM foods WHERE id IN (SELECT max(id) FROM foods GROUP BY lower(name),serving) ORDER BY id DESC LIMIT 12");}
    public string Context(string message,DateTime now) {var total=Totals(now); var target=Targets(now); var foods=Foods(now); if(now.Hour>=18 && target[1]-total[1]>=20) return message+" You still have "+F(target[1]-total[1])+"g of protein remaining today."; if(foods.Count==0) return message+" No food logged today yet."; DateTime last=DateTime.Parse(Day(now)+" "+foods[0]["time"],CultureInfo.InvariantCulture); if(now-last>=TimeSpan.FromHours(4))return message+" Your last entry was at "+last.ToString("t")+"."; return message; }
    public List<Dictionary<string,string>> Due(DateTime now) {return Db.Query("SELECT * FROM reminders WHERE enabled=1").Where(r=> {DateTime snooze; if(DateTime.TryParse(r["snooze"],CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out snooze))return now>=snooze; return r["last_day"]!=Day(now)&&r["time"]==now.ToString("HH:mm");}).ToList();}
    public void Fired(string id,DateTime now) {Db.Query("UPDATE reminders SET last_day=?,snooze='' WHERE id=?",Day(now),id);}
    public void Snooze(string id,DateTime now) {Db.Query("UPDATE reminders SET last_day=?,snooze=? WHERE id=?",Day(now),now.AddMinutes(15).ToString("o"),id);}
    public void Backup(string path) {if(System.IO.Path.GetFullPath(path).Equals(System.IO.Path.GetFullPath(Path),StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Choose a different file from the live database."); using(var target=new Database(path)) Db.CopyTo(target);}
    public void Restore(string path) {
        if(System.IO.Path.GetFullPath(path).Equals(System.IO.Path.GetFullPath(Path),StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Choose a backup file, not the live database.");
        using(var source=new Database(path)) {
            if(source.Query("PRAGMA integrity_check")[0].Values.First()!="ok")throw new IOException("This backup failed its integrity check.");
            string schema=source.Query("SELECT value FROM settings WHERE key='schema'")[0]["value"];
            if(schema!="1"&&schema!="2")throw new IOException("Unsupported backup version.");
            if(schema=="2")foreach(string table in new[]{"foods","favorites"}) {
                source.Query("SELECT quantity FROM "+table+" LIMIT 1");
                if(source.Query("SELECT quantity FROM "+table+" WHERE quantity IS NULL OR quantity<=0 OR quantity>100000 LIMIT 1").Count>0)
                    throw new IOException("This backup contains an invalid food quantity.");
            }
            foreach(string table in new[]{"days","foods","favorites","weights","reminders"})source.Query("SELECT * FROM "+table+" LIMIT 1");
            // Read the exact columns used by the app before replacing any live data.
            source.Query("SELECT day,calories,protein,carbs,fat FROM days LIMIT 1");
            source.Query("SELECT id,day,time,name,calories,protein,carbs,fat,category,serving,notes FROM foods LIMIT 1");
            source.Query("SELECT id,name,calories,protein,carbs,fat,category,serving,notes FROM favorites LIMIT 1");
            source.Query("SELECT day,kg FROM weights LIMIT 1"); source.Query("SELECT id,time,message,enabled,last_day,snooze FROM reminders LIMIT 1");
            var targets=source.Query("SELECT value FROM settings WHERE key='targets'"); Validate(targets[0]["value"].Split('|').Select(Num).ToArray(),true);
            if(source.Query("PRAGMA foreign_key_check").Count!=0)throw new IOException("This backup has invalid food history references.");
            Backup(Path+".before-restore.bak"); source.CopyTo(Db);
        }
        if(Db.Query("SELECT name FROM sqlite_master WHERE type='table' AND name='foods'").Count>0 && Db.Query("SELECT name FROM sqlite_master WHERE type='table' AND name='library'").Count==0) Backup(path+".before-library-upgrade.bak");
        Initialize(); InitializeLibrary(); // Also upgrades schema-1 backups while preserving their original totals.
    }
    public void Export(string path,bool weights,string unit) {
        var r=Db.Query(weights?"SELECT day,kg FROM weights ORDER BY day":"SELECT day,time,name,calories,protein,carbs,fat,category,serving,notes,quantity FROM foods ORDER BY day,time");
        string[] cols=weights?new[]{"day","kg"}:new[]{"day","time","name","calories","protein","carbs","fat","category","serving","notes","quantity"};
        using(var w=new StreamWriter(path,false,new UTF8Encoding(true))) {w.WriteLine(string.Join(",",cols)); foreach(var row in r)w.WriteLine(string.Join(",",cols.Select(c=>Csv(row[c]))));}
    }
    static string Csv(string s) {if(s.Length>0 && "=+-@\t\r".Contains(s[0]))s="'"+s; return "\""+s.Replace("\"","\"\"")+"\"";}
    public void Dispose() {Db.Dispose();}
}
}
