using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MacroTracker {
public static class PortionTests {
    static int count;
    static void Check(bool value,string name){if(!value)throw new Exception("FAIL: "+name);count++;}
    public static void Run(string path) {
        if(File.Exists(path))throw new Exception("Use a fresh isolated test path.");
        var date=new DateTime(2026,9,4);string id;
        var one=new double[]{72,6.3,0.4,4.8};
        using(var store=new Store(path)) {
            store.SaveFood(null,date,"Eggs",FoodPortion.Scale(one,2),"Breakfast","1 whole egg","","8:15 AM",2);
            var row=store.Foods(date)[0];id=row["id"];
            Check(Store.Values(row).SequenceEqual(new double[]{144,12.6,0.8,9.6}),"two eggs have doubled macros in one entry");
            Check(FoodPortion.Quantity(row)==2&&row["time"]=="08:15:00","quantity and time saved");
            store.SaveFood(id,date,"Eggs",FoodPortion.Scale(one,3),"Breakfast","1 whole egg","","18:45",3);
            row=store.Foods(date)[0];Check(row["time"]=="18:45:00"&&FoodPortion.Quantity(row)==3,"edit updates time and quantity");
            Check(store.Foods(date).Count==1&&store.Totals(date)[0]==216,"edit changes totals without duplicate rows");
            store.Favorite(row);var favorite=store.Db.Query("SELECT * FROM favorites")[0];
            Check(FoodPortion.Quantity(favorite)==3,"favorite retains quantity");
            var unit=Store.Values(favorite).Select(v=>v/FoodPortion.Quantity(favorite)).ToArray();
            store.SaveFood(null,date,"Eggs",FoodPortion.Scale(unit,0.5),"Breakfast",favorite["serving"],"","12:00 PM",0.5);
            Check(store.Foods(date).First(r=>r["id"]!=id)["calories"]=="36.0"||store.Foods(date).First(r=>r["id"]!=id)["calories"]=="36","reuse half a serving from favorite");
            Check(store.Foods(date)[0]["id"]==id,"entries sorted by edited meal time");
            Check(FoodPortion.Quantity(store.Recent()[0])==0.5,"recent quantity retained");
            bool bad=false;try{store.SaveFood(id,date,"Eggs",one,"Breakfast","1 egg","","25:10",1);}catch(ArgumentException){bad=true;}Check(bad&&store.Foods(date)[0]["time"]=="18:45:00","invalid time rejected without changing entry");
            bad=false;try{FoodPortion.Scale(one,0);}catch(ArgumentException){bad=true;}Check(bad,"zero quantity rejected");
            Check(FoodPortion.NormalizeTime("12:00 AM")=="00:00:00"&&FoodPortion.NormalizeTime("12:00 PM")=="12:00:00","AM PM noon and midnight handled");
            Check(FoodPortion.NormalizeTime("08:12:34")=="08:12:34","existing seconds preserved");
            store.Export(path+".csv",false,"kg");Check(File.ReadAllText(path+".csv").Contains("quantity"),"CSV includes quantity");
            store.Backup(path+".backup");store.Db.Query("DELETE FROM foods");store.Restore(path+".backup");Check(FoodPortion.Quantity(store.Foods(date)[0])==3,"backup restore retains quantity");
        }
        using(var store=new Store(path))Check(FoodPortion.Quantity(store.Foods(date)[0])==3&&store.Foods(date)[0]["time"]=="18:45:00","quantity and edited time persist on reopen");
        var portions=UsdaClient.ParsePortions("{\"foodPortions\":[{\"gramWeight\":50,\"amount\":1,\"modifier\":\"large\"},{\"gramWeight\":0,\"amount\":1,\"modifier\":\"invalid\"}]}");
        var egg=new UsdaFood{BasisUnit="g",Portions=portions};Check(portions.Count==1&&egg.BaseAmount(2,portions[0].Display)==100,"two USDA large eggs use sourced gram weight");
        CreateLegacy(path+".old");
        using(var upgraded=new Store(path+".old")) {
            Check(File.Exists(path+".old.before-quantity-upgrade.bak"),"pre-upgrade safety backup");
            Check(upgraded.Get("schema","")=="2"&&upgraded.Totals(date)[0]==144&&FoodPortion.Quantity(upgraded.Foods(date)[0])==1,"legacy totals preserved with quantity one");
            Check(FoodPortion.Quantity(upgraded.Db.Query("SELECT * FROM favorites")[0])==1,"legacy favorite migrated");
            upgraded.Restore(path+".old.before-quantity-upgrade.bak");Check(upgraded.Get("schema","")=="2"&&upgraded.Totals(date)[0]==144,"legacy backup restored and upgraded");
        }
        File.WriteAllText(path+".portion-results.txt","PASS: "+count+" quantity, meal-time, portion, favorite, CSV, persistence, migration, and restore checks.");
    }
    static void CreateLegacy(string path) {
        using(var db=new Database(path)) {
            db.Query("CREATE TABLE settings(key TEXT PRIMARY KEY,value TEXT NOT NULL)");db.Query("INSERT INTO settings VALUES('schema','1')");db.Query("INSERT INTO settings VALUES('targets','2450|190|240|75')");
            db.Query("CREATE TABLE days(day TEXT PRIMARY KEY,calories REAL,protein REAL,carbs REAL,fat REAL)");db.Query("INSERT INTO days VALUES('2026-09-04',2450,190,240,75)");
            db.Query("CREATE TABLE foods(id INTEGER PRIMARY KEY,day TEXT,time TEXT,name TEXT,calories REAL,protein REAL,carbs REAL,fat REAL,category TEXT,serving TEXT,notes TEXT)");
            db.Query("INSERT INTO foods VALUES(1,'2026-09-04','09:00:00','Old eggs',144,12.6,0.8,9.6,'Breakfast','2 eggs','')");
            db.Query("CREATE TABLE favorites(id INTEGER PRIMARY KEY,name TEXT,calories REAL,protein REAL,carbs REAL,fat REAL,category TEXT,serving TEXT,notes TEXT)");
            db.Query("INSERT INTO favorites VALUES(1,'Old eggs',144,12.6,0.8,9.6,'Breakfast','2 eggs','')");
            db.Query("CREATE TABLE weights(day TEXT PRIMARY KEY,kg REAL)");db.Query("CREATE TABLE reminders(id INTEGER PRIMARY KEY,time TEXT,message TEXT,enabled INTEGER,last_day TEXT,snooze TEXT)");
        }
    }
    static IEnumerable<DependencyObject> All(DependencyObject root) {for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);yield return child;foreach(var d in All(child))yield return d;}}
    static TextBox Field(Window w,string name){return All(w).OfType<TextBox>().First(x=>x.Name==name);}
    public static void Smoke(MainWindow window,Store store) {
        var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(500)};int step=0;
        timer.Tick+=delegate {
            try {
                if(step==0){step++;window.Dispatcher.BeginInvoke(new Action(delegate{window.AddFood(null,false);}));return;}
                if(step==2){Check(store.Foods(DateTime.Today).Count==1&&store.Totals(DateTime.Today)[0]==144,"UI adds two eggs in one entry");step++;window.Dispatcher.BeginInvoke(new Action(delegate{window.AddFood(store.Foods(DateTime.Today)[0],true);}));return;}
                if(step==4){timer.Stop();var row=store.Foods(DateTime.Today)[0];Check(FoodPortion.Quantity(row)==3&&row["time"]=="19:10:00"&&store.Totals(DateTime.Today)[0]==216,"UI edits quantity and meal time");File.WriteAllText(store.Path+".portion-ui-results.txt","PASS: UI saved 2 whole eggs at 8:15 AM, reopened Edit, changed quantity to 3 and time to 7:10 PM; verified totals, one entry, and saved time.");window.Exit();return;}
                var dialog=Application.Current.Windows.Cast<Window>().Last(w=>w!=window);
                if(step==1) {
                    Field(dialog,"FoodName").Text="Whole eggs";Field(dialog,"FoodQuantity").Text="2";Field(dialog,"FoodServing").Text="1 whole egg";
                    Field(dialog,"FoodCalories").Text="72";Field(dialog,"FoodProtein").Text="6.3";Field(dialog,"FoodCarbs").Text="0.4";Field(dialog,"FoodFat").Text="4.8";Field(dialog,"FoodTime").Text="8:15 AM";
                    dialog.UpdateLayout();var visual=(FrameworkElement)dialog.Content;var bmp=new RenderTargetBitmap((int)visual.ActualWidth,(int)visual.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(visual);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bmp));using(var f=File.Create(store.Path+".quantity.png"))png.Save(f);
                } else if(step==3){Field(dialog,"FoodQuantity").Text="3";Field(dialog,"FoodTime").Text="7:10 PM";}
                step++;All(dialog).OfType<Button>().First(b=>b.Name=="FoodSave").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }catch(Exception e){timer.Stop();File.WriteAllText(store.Path+".portion-ui-error.txt",e.ToString());window.Exit();}
        };timer.Start();
    }
}
}
