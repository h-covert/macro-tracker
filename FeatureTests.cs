using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace MacroTracker {
public static class FeatureTests {
 static int count;static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;}
 public static void Run(string path){using(var s=new Store(path)){
 var day=new DateTime(2026,8,30);Check(Store.Sunday(day.AddDays(6))==day,"Sunday boundaries");s.Db.Query("INSERT INTO presets VALUES('Rest',2000,160,180,70)");s.Db.Query("INSERT INTO schedule VALUES(0,'Rest')");s.EnsureDay(day);Check(s.Targets(day)[0]==2000,"Sunday preset");s.Db.Query("UPDATE presets SET calories=2100 WHERE name='Rest'");Check(s.Targets(day)[0]==2000,"saved targets preserved");Check(s.Targets(day.AddDays(7))[0]==2100,"future schedule");
 s.SaveFood(null,day,"Egg",new double[]{140,12,1,10},"Breakfast","egg","","08:15",2);s.MarkComplete(day,true);Check(s.Complete(day),"mark complete");s.SaveLibrary(null,"Breakfast","Meal",s.Foods(day).Select(LibraryItem.From).ToList(),1,100);var record=s.Db.Query("SELECT * FROM library")[0];s.LogLibrary(record,day,50,true,"09:00","Breakfast");Check(s.Totals(day)[0]==210,"recipe grams scale");Check(!s.Complete(day),"editing reopens day");Check(s.Foods(day).Count==2,"copy independent");s.SetDayTargets(day,new double[]{2300,180,220,80});Check(s.Targets(day)[0]==2300,"override");
 bool fail=false;try{s.LogLibrary(record,day,-1,false,"09:00","Lunch");}catch(ArgumentException){fail=true;}Check(fail&&s.Foods(day).Count==2,"invalid portion no writes");
 var values=LabelReader.Parse("Calories 140\nTotal Fat 10g 12%\nTotal Carbohydrate 1g\nProtein 12g");Check(values.SequenceEqual(new[]{"140","12","1","10"}),"label nutrients");Check(LabelReader.Parse("Calories 100\nCalories 200")[0]=="","ambiguous calorie columns blank");Check(LabelReader.Parse("Protein 0g")[1]=="0","zero protein preserved");Check(LabelReader.Parse("Total Fat 1g")[2]=="","missing carb blank");
 string photographed="Nutrition Facts\nAbout 16 servings per container\nServing size 1 oz (28g/about 39 pieces)\nAmount per serving\nCalories\nTotal Fat 14g\nSaturated Fat 2g\nTrans Fat Og\n170\n% Daily vaue•\nPolyunsaturated Fat 4.5g\nMonounsaturated Fat 7g\nCholesterol Omg\n75mg\nTotal Carbohydrate 5g\nDietary Fiber 2g\nTotal Sugars lg\nIncludes Og Added Sugars\nProtein 70\n0%\n3%\n2%\n8%";Check(LabelReader.Parse(photographed).SequenceEqual(new[]{"170","7","5","14"}),"photographed label line order and misread protein unit corrected");
 string cameraFolder=path+".camera-test";Directory.CreateDirectory(cameraFolder);string oldPhoto=Path.Combine(cameraFolder,"old.jpg");File.WriteAllText(oldPhoto,"old");var cameraBefore=CameraCapture.Snapshot(new[]{cameraFolder});string newPhoto=Path.Combine(cameraFolder,"new.jpg");File.WriteAllText(newPhoto,"new");Check(CameraCapture.FindNew(new[]{cameraFolder},cameraBefore)==newPhoto,"new Camera Roll photo detected");
 s.Backup(path+".backup");s.Db.Query("DELETE FROM library");s.Restore(path+".backup");Check(s.Db.Query("SELECT * FROM library").Count==1,"library backup restore");
 }File.WriteAllText(path+".feature-results.txt","PASS: "+count+" feature checks");}
}
}
