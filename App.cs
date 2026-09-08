using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MacroTracker {
public static class Program {
    [STAThread] public static int Main(string[] args) {
        bool test=args.Contains("--self-test"), smoke=args.Contains("--smoke"), reopen=args.Contains("--verify-persistence");
        string data=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MacroTracker","macro-tracker.db");
        int index=Array.IndexOf(args,"--data");if(index>=0 && index+1<args.Length)data=System.IO.Path.GetFullPath(args[index+1]);
        if((test||smoke||reopen||args.Contains("--usda-test")||args.Contains("--usda-ui")||args.Contains("--portion-test")||args.Contains("--portion-ui")||args.Contains("--update-test")||args.Contains("--feature-test")||args.Contains("--feature-ui"))&&index<0) {File.WriteAllText("test-error.txt","Tests require --data with an isolated database path.");return 2;}
        bool created;using(var mutex=new Mutex(true,"Local\\MacroTracker-"+StableHash(data),out created)) {
            if(!created){MessageBox.Show("Macro Tracker is already running. Open it from the system tray.","Macro Tracker");return 0;}
            try {
                if(args.Contains("--feature-test")){FeatureTests.Run(data);return 0;} if(args.Contains("--update-test")){UpdateTests.Run(data);return 0;} if(args.Contains("--portion-test")){PortionTests.Run(data);return 0;} if(args.Contains("--usda-test")){UsdaTests.Run(data,args.Contains("--live"));return 0;} if(test){Tests.Run(data);return 0;}
                if(reopen){Tests.Reopen(data);return 0;}
                using(var store=new Store(data)) {
                    var app=new Application{ShutdownMode=ShutdownMode.OnMainWindowClose};
                    app.DispatcherUnhandledException+=delegate(object sender,DispatcherUnhandledExceptionEventArgs e){File.AppendAllText(data+".errors.log",DateTime.Now+" "+e.Exception+Environment.NewLine);MessageBox.Show("Something went wrong. Your saved data remains on this device.\n\n"+e.Exception.Message,"Macro Tracker");e.Handled=true;};
                    var window=new MainWindow(store,smoke||args.Contains("--feature-ui")||args.Contains("--usda-ui")||args.Contains("--portion-ui"));app.MainWindow=window;
                    window.Loaded+=delegate {if(!args.Contains("--feature-ui"))window.CheckUpdatesOnOpen();if(args.Contains("--feature-ui"))FeatureUiTests.Run(window,store);if(args.Contains("--tray")&&store.Get("setup","")=="1")window.Hide();if(args.Contains("--portion-ui"))PortionTests.Smoke(window,store);if(smoke)Tests.Smoke(window,store);if(args.Contains("--usda-ui"))UsdaTests.Smoke(window,store);};
                    app.Run(window);
                }
                return 0;
            } catch(Exception e) {Directory.CreateDirectory(System.IO.Path.GetDirectoryName(data));File.WriteAllText(data+".errors.log",e.ToString());if(!test&&!smoke&&!args.Contains("--usda-test")&&!args.Contains("--usda-ui")&&!args.Contains("--portion-test")&&!args.Contains("--portion-ui")&&!args.Contains("--update-test")&&!args.Contains("--feature-test"))MessageBox.Show("Macro Tracker couldn't start.\n\n"+e.Message+"\n\nDetails: "+data+".errors.log","Macro Tracker");return 1;}
        }
    }
    static string StableHash(string s) {uint hash=2166136261;foreach(char c in s.ToUpperInvariant()){hash^=c;hash*=16777619;}return hash.ToString("X8");}
}
public static class Tests {
    static int checks;static void Check(bool condition,string name) {if(!condition)throw new Exception("FAIL: "+name);checks++;}
    public static void Reopen(string path) {
        using(var s=new Store(path)) {
            Check(s.Foods(new DateTime(2026,9,3)).Count==1,"prior food after process restart");
            Check(s.Db.Query("SELECT time FROM reminders WHERE id=1")[0]["time"]=="11:11","reminder setting after process restart");
            Check(s.Db.Query("SELECT * FROM favorites").Count==1,"favorite after process restart");
            Check(s.Db.Query("SELECT * FROM weights").Count==1,"weight after process restart");
        }
        File.WriteAllText(path+".reopen-results.txt","PASS: food, reminder settings, favorites, and weight persisted across separate application processes.");
    }
    public static void Run(string path) {
        if(File.Exists(path))throw new InvalidOperationException("Use a fresh test database path.");
        DateTime day=new DateTime(2026,9,3);string backup=path+".backup";
        using(var s=new Store(path)) {
            Check(s.Defaults().SequenceEqual(new double[]{2450,190,240,75}),"default targets");Check(s.Db.Query("SELECT * FROM reminders").Count==5,"default reminders");
            s.SaveFood(null,day,"Chicken & rice",new double[]{650,55,70,15},"Lunch","1 bowl","test","12:30:00");
            s.SaveFood(null,day,"Beer",new double[]{120,0,0,0},"Drink","1 bottle","","18:00:00");
            Check(s.Totals(day).SequenceEqual(new double[]{770,55,70,15}),"macro totals and independent drink calories");
            var food=s.Foods(day).First(x=>x["name"]=="Chicken & rice");s.Favorite(food);s.Favorite(food);Check(s.Db.Query("SELECT * FROM favorites").Count==1,"favorite saved without duplicates");Check(s.Recent().Count==2,"recent foods");
            s.SaveFood(food["id"],day,"Chicken & rice",new double[]{700,60,75,16},"Lunch","large bowl","edited",food["time"]);Check(s.Totals(day)[0]==820,"edit updates total");
            s.Db.Query("DELETE FROM foods WHERE name='Beer'");Check(s.Totals(day)[0]==700,"deletion updates total");
            s.EnsureDay(day.AddDays(1));Check(s.Totals(day.AddDays(1)).All(v=>v==0),"next day empty");Check(s.Foods(day).Count==1,"history preserved");
            s.SaveTargets(new double[]{2200,180,200,70},false);s.EnsureDay(day.AddDays(2));Check(s.Targets(day)[0]==2450,"historical targets retained");Check(s.Targets(day.AddDays(2))[0]==2200,"new targets carried forward");
            bool invalid=false;try{s.SaveFood(null,day,"Bad",new double[]{-1,0,0,0},"Lunch","","","00:00:00");}catch(ArgumentException){invalid=true;}Check(invalid,"negative input rejected");
            invalid=false;try{s.SaveFood(null,day," ",new double[]{1,0,0,0},"Lunch","","","00:00:00");}catch(ArgumentException){invalid=true;}Check(invalid,"blank name rejected");
            s.Db.Query("UPDATE reminders SET time='11:11',message='Test persistence' WHERE id=1");var now=day.AddHours(11).AddMinutes(11);Check(s.Due(now).Any(r=>r["id"]=="1"),"reminder due");s.Fired("1",now);Check(!s.Due(now).Any(r=>r["id"]=="1"),"reminder fires once per day");s.Snooze("1",now);Check(!s.Due(now.AddMinutes(14)).Any(r=>r["id"]=="1"),"snooze waits");Check(s.Due(now.AddMinutes(15)).Any(r=>r["id"]=="1"),"snooze fires");
            Check(s.Context("Dinner.",day.AddHours(19)).Contains("protein remaining"),"contextual protein reminder");
            s.Db.Query("INSERT INTO weights VALUES(?,?)",Store.Day(day),80);s.Set("setup","1");s.Backup(backup);s.Export(path+".csv",false,"kg");Check(File.ReadAllText(path+".csv").Contains("Chicken & rice"),"CSV export");
        }
        using(var s=new Store(path)) {
            Check(s.Foods(day).Count==1&&s.Totals(day)[0]==700,"database persists across reopen");Check(s.Db.Query("SELECT * FROM reminders WHERE id=1")[0]["time"]=="11:11","reminder settings persist");Check(s.Db.Query("SELECT * FROM favorites").Count==1&&s.Recent().Count==1,"favorites and recent persist");Check(s.Db.Query("SELECT * FROM weights").Count==1,"weight persists");
            s.Db.Query("DELETE FROM foods");s.Restore(backup);Check(s.Foods(day).Count==1,"restore recovers food");Check(File.Exists(path+".before-restore.bak"),"restore safety backup");
            var f=s.Db.Query("SELECT * FROM favorites")[0];s.SaveFood(null,day.AddDays(1),f["name"],Store.Values(f),f["category"],f["serving"],f["notes"],"08:00:00");Check(s.Totals(day.AddDays(1))[0]==650,"reuse favorite macros");
            using(var bad=new Database(path+".invalid")){bad.Query("CREATE TABLE unrelated(id INTEGER)");}bool rejected=false;try{s.Restore(path+".invalid");}catch{rejected=true;}Check(rejected&&s.Foods(day).Count==1,"invalid restore rejected without data loss");
        }
        File.WriteAllText(path+".results.txt","PASS: "+checks+" integration checks.\r\nIncludes database reopen, totals, edit/delete, new day, history, target snapshots, favorites/recent, reminders/snooze, weight, CSV, backup/restore, and invalid input.\r\n");
    }
    public static void Smoke(MainWindow window,Store s) {
        string folder=System.IO.Path.GetDirectoryName(s.Path);var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(650)};int stage=0;string[] pages={"Welcome","Dashboard","Food Log","Library","Day Targets","Check-In","History","Weight","Settings"};
        timer.Tick+=delegate {try {
            if(stage==0){s.Set("setup","1");s.SaveFood(null,DateTime.Today,"Chicken & rice",new double[]{650,55,70,15},"Lunch","1 bowl","","12:30:00");s.SaveFood(null,DateTime.Today,"Greek yogurt & berries",new double[]{260,25,30,5},"Breakfast","1 bowl","","08:00:00");s.SaveFood(null,DateTime.Today,"Protein shake",new double[]{160,30,5,2},"Snack","1 scoop","","15:00:00");s.Favorite(s.Foods(DateTime.Today)[0]);for(int i=0;i<9;i++)s.Db.Query("INSERT OR REPLACE INTO weights VALUES(?,?)",Store.Day(DateTime.Today.AddDays(-i)),82+i*0.12);}
            if(stage<pages.Length){window.Navigate(pages[stage]);window.UpdateLayout();Capture(window,System.IO.Path.Combine(folder,pages[stage].Replace(" ","")+".png"));stage++;return;}
            timer.Stop();
            // Drive the real food dialog: fill its TextBoxes and invoke the default save button.
            var uiTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(400)};uiTimer.Tick+=delegate {uiTimer.Stop();try{Window dialog=Application.Current.Windows.Cast<Window>().First(w=>w!=window);var boxes=Descendants(dialog).OfType<TextBox>().ToList();boxes.First(b=>b.Name=="FoodName").Text="UI smoke food";boxes.First(b=>b.Name=="FoodCalories").Text="321";boxes.First(b=>b.Name=="FoodProtein").Text="25";boxes.First(b=>b.Name=="FoodCarbs").Text="30";boxes.First(b=>b.Name=="FoodFat").Text="11";Capture(dialog,System.IO.Path.Combine(folder,"QuickAdd.png"));Descendants(dialog).OfType<Button>().First(b=>b.IsDefault).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}catch(Exception ex){File.WriteAllText(s.Path+".smoke-error.txt",ex.ToString());window.Exit();}};uiTimer.Start();window.AddFood(null,false);
            Check(s.Foods(DateTime.Today).Any(r=>r["name"]=="UI smoke food"),"real UI save");window.Navigate("Dashboard");window.UpdateLayout();Capture(window,System.IO.Path.Combine(folder,"Dashboard-final.png"));File.WriteAllText(s.Path+".smoke-results.txt","PASS: all six screens rendered; actual Quick Add dialog accepted inputs and saved food via its Save button.");window.Exit();
        }catch(Exception e){timer.Stop();File.WriteAllText(s.Path+".smoke-error.txt",e.ToString());window.Exit();}};timer.Start();
    }
    static System.Collections.Generic.IEnumerable<DependencyObject> Descendants(DependencyObject root) {for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);yield return child;foreach(var d in Descendants(child))yield return d;}}
    static void Capture(Window window,string path) {window.UpdateLayout();var visual=window.Content as FrameworkElement;var bmp=new RenderTargetBitmap((int)visual.ActualWidth,(int)visual.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(visual);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bmp));using(var f=File.Create(path))encoder.Save(f);}
}
}
