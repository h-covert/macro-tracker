using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace MacroTracker {
public static class FeatureUiTests {
 static IEnumerable<DependencyObject> Walk(DependencyObject root){for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var c=VisualTreeHelper.GetChild(root,i);yield return c;foreach(var d in Walk(c))yield return d;}}
 static void Click(Window w,string text){Walk(w).OfType<Button>().First(b=>(string)b.Content==text).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}
 static void Capture(Window w,string file){w.UpdateLayout();var v=(FrameworkElement)w.Content;var b=new RenderTargetBitmap((int)v.ActualWidth,(int)v.ActualHeight,96,96,PixelFormats.Pbgra32);b.Render(v);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(b));using(var f=File.Create(file))encoder.Save(f);}
 static void Modal(MainWindow main,string method,object[] args,Action<Window> test){Exception failure=null;var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(250)};timer.Tick+=delegate{timer.Stop();var w=Application.Current.Windows.Cast<Window>().First(x=>x!=main);try{test(w);}catch(Exception e){failure=e;w.Close();}};timer.Start();typeof(MainWindow).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(main,args);if(failure!=null)throw failure;}
 public static void Run(MainWindow main,Store s){var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(350)};timer.Tick+=delegate{timer.Stop();try{
 s.Set("setup","1");s.SaveFood(null,DateTime.Today,"Egg",new double[]{70,6,1,5},"Breakfast","1 egg","","08:00");
 var items=s.Foods(DateTime.Today).Select(LibraryItem.From).ToList();
 Modal(main,"LibraryEditor",new object[]{items,null},delegate(Window w){var fields=Walk(w).OfType<TextBox>().ToList();fields[0].Text="UI breakfast";fields[1].Text="2";fields[2].Text="100";Capture(w,s.Path+".recipe.png");Click(w,"Save to library");});
 var record=s.Db.Query("SELECT * FROM library WHERE name='UI breakfast'").Single();
 Modal(main,"LogSaved",new object[]{record},delegate(Window w){Click(w,"Add all ingredients");});
 if(s.Totals(DateTime.Today)[0]!=105)throw new Exception("UI recipe scaling failed");
 Modal(main,"ScanLabel",new object[0],delegate(Window w){if(!Walk(w).OfType<Button>().Any(b=>(string)b.Content=="Access camera"))throw new Exception("Nutrition label camera button missing");var fields=Walk(w).OfType<TextBox>().ToList();fields[0].Text="Serving size 1 cup\nCalories 200\nTotal Fat 5g\nTotal Carbohydrate 25g\nProtein 15g\nSodium 120mg";Click(w,"Extract from text");fields[1].Text="UI reviewed label";Walk(w).OfType<CheckBox>().Single().IsChecked=true;Capture(w,s.Path+".label.png");Click(w,"Save reviewed food to favorites");});
 if(s.Db.Query("SELECT * FROM favorites WHERE name='UI reviewed label'").Count!=1)throw new Exception("Label review save failed");
 main.Navigate("Day Targets");var targets=Walk(main).OfType<TextBox>().Where(x=>x.Name!="PART_TextBox").ToList();targets[0].Text="UI Rest";Click(main,"Save preset");if(s.Db.Query("SELECT * FROM presets WHERE name='UI Rest'").Count!=1)throw new Exception("Preset UI save failed");
 main.Navigate("Check-In");Click(main,"Save check-in · keep targets");if(s.Db.Query("SELECT * FROM checkins").Count!=1)throw new Exception("Check-in UI save failed");
 foreach(string theme in new[]{"Graphite","Purple","White / Red / Blue"}){main.Navigate("Settings");Walk(main).OfType<ComboBox>().First(c=>c.Items.Cast<object>().Contains("Graphite")).SelectedItem=theme;Click(main,"Apply theme");main.UpdateLayout();main.Dispatcher.Invoke(new Action(delegate{}),DispatcherPriority.ContextIdle);var settingsContent=(StackPanel)typeof(MainWindow).GetField("content",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(main);var plain=Walk(settingsContent).OfType<Button>().Where(b=>b.Background==null||b.Background.ToString()!=MainWindow.Green.ToString()).Select(b=>Convert.ToString(b.Content)).ToList();if(plain.Count>0)throw new Exception("Settings buttons without theme accent: "+string.Join(", ",plain));var picker=Walk(main).OfType<ComboBox>().First(c=>c.Items.Cast<object>().Contains("Graphite"));picker.ApplyTemplate();var toggle=Walk(picker).OfType<System.Windows.Controls.Primitives.ToggleButton>().First();toggle.SetCurrentValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty,true);main.Dispatcher.Invoke(new Action(delegate{}),DispatcherPriority.DataBind);if(!picker.IsDropDownOpen)throw new Exception("Rounded dropdown toggle did not open");main.Dispatcher.Invoke(new Action(delegate{}),DispatcherPriority.Render);Capture(main,s.Path+"."+theme.Split(' ')[0]+"-settings.png");var option=(ComboBoxItem)picker.ItemContainerGenerator.ContainerFromIndex(1);if(option==null)throw new Exception("Dropdown items not generated");option.IsSelected=true;if((string)picker.SelectedItem!="Purple")throw new Exception("Dropdown selection failed");picker.IsDropDownOpen=false;main.Navigate("Dashboard");Capture(main,s.Path+"."+theme.Split(' ')[0]+".png");}
 File.WriteAllText(s.Path+".feature-ui-results.txt","PASS: real recipe creation/logging, label camera control, label extraction/review/favorite save, preset save, check-in save, three themes, and accented Settings actions.");
 }catch(Exception e){File.WriteAllText(s.Path+".feature-ui-error.txt",e.ToString());}finally{main.Exit();}};timer.Start();}
}
}
