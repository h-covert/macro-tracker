using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Script.Serialization;
namespace MacroTracker {
public sealed class LibraryItem {
 public string Name,Category,Serving,Notes; public double Quantity; public double[] Macros;
 public static LibraryItem From(Dictionary<string,string> r){return new LibraryItem{Name=r["name"],Category=r["category"],Serving=r["serving"],Notes=r["notes"],Quantity=FoodPortion.Quantity(r),Macros=Store.Values(r)};}
}
public sealed partial class Store {
 public void InitializeLibrary(){
 Db.Transaction(delegate{
 Db.Query("CREATE TABLE IF NOT EXISTS library(id INTEGER PRIMARY KEY,name TEXT NOT NULL,kind TEXT NOT NULL,items TEXT NOT NULL,yield REAL NOT NULL,grams REAL NOT NULL)");
 Db.Query("CREATE TABLE IF NOT EXISTS presets(name TEXT PRIMARY KEY,calories REAL,protein REAL,carbs REAL,fat REAL)");
 Db.Query("CREATE TABLE IF NOT EXISTS schedule(weekday INTEGER PRIMARY KEY,preset TEXT NOT NULL)");
 Db.Query("CREATE TABLE IF NOT EXISTS completions(day TEXT PRIMARY KEY)");
 Db.Query("CREATE TABLE IF NOT EXISTS checkins(week TEXT PRIMARY KEY,hunger TEXT,energy TEXT,training TEXT,note TEXT,focus TEXT)");
 Db.Query("CREATE TRIGGER IF NOT EXISTS food_insert_reopen AFTER INSERT ON foods BEGIN DELETE FROM completions WHERE day=NEW.day; END");
 Db.Query("CREATE TRIGGER IF NOT EXISTS food_update_reopen AFTER UPDATE ON foods BEGIN DELETE FROM completions WHERE day=NEW.day OR day=OLD.day; END");
 Db.Query("CREATE TRIGGER IF NOT EXISTS food_delete_reopen AFTER DELETE ON foods BEGIN DELETE FROM completions WHERE day=OLD.day; END");
 }); }
 public static DateTime Sunday(DateTime date){return date.Date.AddDays(-(int)date.DayOfWeek);}
 public double[] ScheduledTargets(DateTime date){var rows=Db.Query("SELECT p.* FROM schedule s JOIN presets p ON p.name=s.preset WHERE s.weekday=?",(int)date.DayOfWeek);return rows.Count==0?Defaults():Values(rows[0]);}
 public void SetDayTargets(DateTime date,double[] values){Validate(values,true);EnsureDay(date);Db.Query("UPDATE days SET calories=?,protein=?,carbs=?,fat=? WHERE day=?",values[0],values[1],values[2],values[3],Day(date));}
 public void SaveLibrary(string id,string name,string kind,List<LibraryItem> items,double yield,double grams){
 if(string.IsNullOrWhiteSpace(name)||name.Length>200||items.Count==0)throw new ArgumentException("Enter a name and select at least one ingredient.");
 if(!new[]{"Meal","Recipe","Snack","Drink"}.Contains(kind))throw new ArgumentException("Select a library type.");
 FoodPortion.ValidateQuantity(yield);if(double.IsNaN(grams)||double.IsInfinity(grams)||grams<0||grams>1000000)throw new ArgumentException("Enter a valid finished batch weight.");
 foreach(var item in items){Validate(item.Macros,false);FoodPortion.ValidateQuantity(item.Quantity);}
 var json=new JavaScriptSerializer().Serialize(items);
 if(id==null)Db.Query("INSERT INTO library(name,kind,items,yield,grams) VALUES(?,?,?,?,?)",name,kind,json,yield,grams);
 else Db.Query("UPDATE library SET name=?,kind=?,items=?,yield=?,grams=? WHERE id=?",name,kind,json,yield,grams,id);
 }
 public void LogLibrary(Dictionary<string,string> record,DateTime date,double amount,bool byGrams,string time,string category){
 FoodPortion.ValidateQuantity(amount);time=FoodPortion.NormalizeTime(time);
 var items=new JavaScriptSerializer().Deserialize<List<LibraryItem>>(record["items"]);
 double basis=byGrams?Num(record["grams"]):Num(record["yield"]);if(basis<=0)throw new ArgumentException("This recipe has no finished batch weight. Use servings instead.");
 double factor=amount/basis;
 // Validate every row before opening the all-or-nothing transaction.
 foreach(var item in items){Validate(item.Macros.Select(x=>x*factor).ToArray(),false);FoodPortion.ValidateQuantity(item.Quantity*factor);}
 Db.Transaction(delegate {EnsureDay(date);foreach(var item in items)Db.Query("INSERT INTO foods(day,time,name,calories,protein,carbs,fat,category,serving,notes,quantity) VALUES(?,?,?,?,?,?,?,?,?,?,?)",Day(date),time,item.Name,item.Macros[0]*factor,item.Macros[1]*factor,item.Macros[2]*factor,item.Macros[3]*factor,category,item.Serving,"From "+record["name"]+" · "+item.Notes,item.Quantity*factor);});
 }
 public bool Complete(DateTime date){return Db.Query("SELECT day FROM completions WHERE day=?",Day(date)).Count>0;}
 public void MarkComplete(DateTime date,bool complete){if(date.Date>DateTime.Today)throw new ArgumentException("Future days cannot be completed.");if(complete){if(Foods(date).Count==0)throw new ArgumentException("Log your food before marking the day complete.");Db.Query("INSERT OR IGNORE INTO completions VALUES(?)",Day(date));}else Db.Query("DELETE FROM completions WHERE day=?",Day(date));}
}
}
