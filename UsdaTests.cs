using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MacroTracker {
public static class UsdaTests {
    static int count;
    static void Check(bool good,string name) {if(!good)throw new Exception("FAIL: "+name);count++;}
    static string Fixture(bool liquid,bool missing) {
        var nutrients=new List<object>();
        int[] ids={1008,1003,1005,1004,2048};double[] values={200,20,15,6,199};
        for(int i=0;i<ids.Length;i++)if(!missing||i!=1)nutrients.Add(new{nutrientId=ids[i],value=values[i],unitName=(i==0||i==4)?"KCAL":"G"});
        return new JavaScriptSerializer().Serialize(new{foods=new[]{new{fdcId=123,description="Test yogurt",brandOwner="Fixture brand",dataType="Branded",servingSize=50,servingSizeUnit=liquid?"ml":"g",foodNutrients=nutrients}}});
    }
    public static List<UsdaFood> Foods() {return UsdaClient.Parse(Fixture(false,false));}
    public static void Run(string path,bool live) {
        Directory.CreateDirectory(Path.GetDirectoryName(path));var food=Foods()[0];
        Check(food.Macros.SequenceEqual(new double?[]{200,20,15,6}),"nutrient ID mapping, energy not double counted");
        Check(food.BaseAmount(2,"Label servings")==100,"branded serving conversion");
        Check(Math.Abs(food.BaseAmount(2,"oz (weight)")-56.69904625)<0.000001,"ounce conversion");
        Check(food.Macros[0]*food.BaseAmount(150,"g")/100==300,"150g energy scaling");
        var liquid=UsdaClient.Parse(Fixture(true,false))[0];Check(liquid.BasisUnit=="ml","liquid basis");
        Check(Math.Abs(liquid.BaseAmount(1,"US fl oz")-29.5735295625)<0.000001,"fluid ounce conversion");
        Check(!UsdaClient.Parse(Fixture(false,true))[0].Macros[1].HasValue,"missing protein remains missing");
        Check(UsdaClient.Parse("{\"foods\":[]}").Count==0,"empty results");
        bool invalid=false;try{food.BaseAmount(double.NaN,"g");}catch(ArgumentException){invalid=true;}Check(invalid,"invalid serving rejected");
        invalid=false;try{liquid.BaseAmount(10,"g");}catch(ArgumentException){invalid=true;}Check(invalid,"mass-volume mixing rejected");
        invalid=false;try{UsdaClient.Parse("not JSON");}catch(InvalidOperationException){invalid=true;}Check(invalid,"malformed API response handled");
        Check(UsdaClient.Error(429).Contains("limit"),"rate limit message");Check(UsdaClient.Error(403).Contains("API key"),"invalid key message");
        var key=new UsdaKey(path);string testKey="SyntheticTestKeyForEncryption12345";key.Save(testKey);
        Check(new UsdaKey(path).Read()==testKey,"encrypted key persists");
        Check(!File.ReadAllText(path+".usda-key").Contains(testKey),"key not stored in plaintext");key.Save("");Check(key.Read()=="DEMO_KEY","demo fallback");
        if(live){var results=new UsdaClient().Search("banana","Common foods","DEMO_KEY",CancellationToken.None).GetAwaiter().GetResult();Check(results.Count>0,"live USDA search returns foods");Check(results.Any(f=>f.Macros.All(v=>v.HasValue)),"live response includes all four macros");}
        if(live) {
            var egg=new UsdaFood{Id="173424",Type="SR Legacy",BasisUnit="g"};
            new UsdaClient().LoadPortions(egg,"DEMO_KEY",CancellationToken.None).GetAwaiter().GetResult();
            var large=egg.Portions.FirstOrDefault(p=>p.Label=="Large");
            Check(large!=null&&egg.BaseAmount(2,large.Display)==100,"live USDA whole-egg portion lookup");
        }
        File.WriteAllText(path+".usda-results.txt","PASS: "+count+" USDA checks"+(live?", including live demo-key search and egg portion lookup.":" (offline fixtures)."));
    }
    static IEnumerable<DependencyObject> All(DependencyObject root) {for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);yield return child;foreach(var d in All(child))yield return d;}}
    static T Named<T>(Window w,string name) where T:FrameworkElement {return All(w).OfType<T>().First(x=>x.Name==name);}
    public static void Smoke(MainWindow window,Store store) {
        window.UsdaTestSearch=delegate(string q,string scope,CancellationToken token){var foods=Foods();foods[0].Portions.Add(new UsdaPortion{Label="Cup",Grams=80,DefaultAmount=0.5});return Task.FromResult(foods);};
        var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(450)};int step=0;
        timer.Tick+=delegate {
            try {
                if(step>=5)return; if(step==0){step++;window.Dispatcher.BeginInvoke(new Action(delegate{window.AddFood(null,false);}));return;}
                var dialog=Application.Current.Windows.Cast<Window>().Last(w=>w!=window);
                if(step==1){step++;window.Dispatcher.BeginInvoke(new Action(delegate{All(dialog).OfType<Button>().First(b=>Convert.ToString(b.Content)=="Search USDA foods").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}));return;}
                if(step==2){Named<TextBox>(dialog,"UsdaQuery").Text="yogurt";step++;Named<Button>(dialog,"UsdaSearch").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));return;}
                if(step==3){Named<ListBox>(dialog,"UsdaResults").SelectedIndex=0;var units=Named<ComboBox>(dialog,"UsdaUnits");units.SelectedItem="Cup";double selectedAmount;Check(double.TryParse(Named<TextBox>(dialog,"UsdaAmount").Text,NumberStyles.Float,CultureInfo.CurrentCulture,out selectedAmount)&&selectedAmount==0.5,"UI separates USDA portion amount from Cup unit");dialog.UpdateLayout();var visual=(FrameworkElement)dialog.Content;var bmp=new RenderTargetBitmap((int)visual.ActualWidth,(int)visual.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(visual);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bmp));using(var f=File.Create(store.Path+".serving.png"))png.Save(f);units.SelectedItem="g";Named<TextBox>(dialog,"UsdaAmount").Text="150";step++;Named<Button>(dialog,"UsdaUse").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));return;}
                if(step==4){step++;All(dialog).OfType<Button>().First(b=>b.IsDefault).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));return;}
            }catch(Exception e){timer.Stop();File.WriteAllText(store.Path+".usda-ui-error.txt",e.ToString());window.Exit();}
        };
        // The final assertion runs after the modal Add Food stack has unwound.
        var finish=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(500)};
        finish.Tick+=delegate{if(step<5)return;timer.Stop();finish.Stop();try{var r=store.Foods(DateTime.Today).First(x=>x["name"]=="Test yogurt");Check(Store.Values(r).SequenceEqual(new double[]{300,30,22.5,9}),"UI search scales and saves exact macros");File.WriteAllText(store.Path+".usda-ui-results.txt","PASS: actual USDA search dialog → select food → 150g preview → autofill → save → verified database totals.");}catch(Exception e){File.WriteAllText(store.Path+".usda-ui-error.txt",e.ToString());}window.Exit();};
        timer.Start();finish.Start();
    }
}
}
