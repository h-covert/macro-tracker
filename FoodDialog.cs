using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MacroTracker {
public sealed partial class MainWindow {
    sealed class TimeChoice {
        internal ComboBox Clock,Period;
        internal string Value {get {if(Clock.SelectedItem==null||Period.SelectedItem==null)throw new ArgumentException("Choose a time and AM or PM.");return Clock.SelectedItem+" "+Period.SelectedItem;}}
    }
    TimeChoice TimePicker(StackPanel panel,string initial,string namePrefix) {
        DateTime chosen;
        if(string.IsNullOrWhiteSpace(initial)){chosen=DateTime.Now;chosen=new DateTime(chosen.Year,chosen.Month,chosen.Day,chosen.Hour,chosen.Minute/15*15,0);}
        else chosen=DateTime.Today.Add(TimeSpan.ParseExact(FoodPortion.NormalizeTime(initial),"hh\\:mm\\:ss",CultureInfo.InvariantCulture));
        var times=Enumerable.Range(0,48).Select(i=>DateTime.Today.AddMinutes(i*15).ToString("h:mm",CultureInfo.InvariantCulture)).ToList();string exact=chosen.ToString("h:mm",CultureInfo.InvariantCulture);if(!times.Contains(exact))times.Insert(Math.Min(times.Count,(chosen.Hour%12*60+chosen.Minute)/15+1),exact);
        panel.Children.Add(Label("Time eaten · 15-minute intervals",12,Muted));var row=Row();row.Margin=new Thickness(0,0,0,14);var result=new TimeChoice();result.Clock=new ComboBox{ItemsSource=times,SelectedItem=exact,Width=160,Name=namePrefix+"Clock",Padding=new Thickness(10),Margin=new Thickness(0,0,8,0)};result.Period=new ComboBox{ItemsSource=new[]{"AM","PM"},SelectedItem=chosen.ToString("tt",CultureInfo.InvariantCulture),Width=100,Name=namePrefix+"Period",Padding=new Thickness(10)};row.Children.Add(result.Clock);row.Children.Add(result.Period);panel.Children.Add(row);return result;
    }
    static double ReadQuantity(TextBox field) {
        double value;if(!double.TryParse(field.Text,NumberStyles.Float,CultureInfo.CurrentCulture,out value))
            throw new ArgumentException("Enter a numeric quantity such as 2 or 0.5.");
        FoodPortion.ValidateQuantity(value);return value;
    }
    public void AddFood(Dictionary<string,string> entry,bool edit) {
        if(foodDialog!=null){foodDialog.Activate();return;}
        var win=new Window {
            Title=edit?"Edit food":"Quick add food",Width=650,Height=Math.Min(850,SystemParameters.WorkArea.Height-35),
            MinWidth=530,MinHeight=570,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner,
            Background=Bg,Foreground=Ink,FontFamily=FontFamily,Resources=Resources
        };
        win.SourceInitialized+=delegate{RoundWindow(win);};foodDialog=win;
        var root=new DockPanel{Margin=new Thickness(24)};win.Content=new Border{Background=Bg,Child=root};
        var footer=new StackPanel{Margin=new Thickness(0,12,0,0)};DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);
        var preview=Label("",14,Green);footer.Children.Add(preview);
        var error=Label("",12,Brush("#FFB5AA"));error.Name="FoodError";footer.Children.Add(error);
        var p=new StackPanel{Margin=new Thickness(0,0,12,0)};
        root.Children.Add(new ScrollViewer{Content=p,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
        p.Children.Add(Label(edit?"Edit your entry":"What's on the menu?",27,Ink));
        DateTime date=edit?DateTime.Parse(entry["day"],CultureInfo.InvariantCulture):selected;
        p.Children.Add(Label(date.ToString("D"),13,Muted));
        var name=Field(p,"Food or meal name",entry==null?"":entry["name"]);name.Name="FoodName";name.MaxLength=200;
        string initialServing=entry==null||entry["serving"]==""?"1 item / serving":entry["serving"];
        string initialMeasure=FoodPortion.Measure(initialServing),savedItemServing=initialMeasure==FoodPortion.ItemMeasure?initialServing:"1 item / serving",previousMeasure=initialMeasure;
        var quantityRow=new System.Windows.Controls.Primitives.UniformGrid{Columns=2};
        var qPanel=new StackPanel{Margin=new Thickness(0,0,14,0)};
        var quantity=Field(qPanel,"Amount",FoodPortion.Precise(FoodPortion.Quantity(entry)));quantity.Name="FoodQuantity";
        quantityRow.Children.Add(qPanel);
        var measurePanel=new StackPanel();measurePanel.Children.Add(Label("Measure",12,Muted));
        var measure=new ComboBox{ItemsSource=FoodPortion.Measures,SelectedItem=initialMeasure,Padding=new Thickness(10),Margin=new Thickness(0,0,0,14),Name="FoodMeasure"};measurePanel.Children.Add(measure);
        quantityRow.Children.Add(measurePanel);p.Children.Add(quantityRow);
        var servingPanel=new StackPanel();
        var serving=Field(servingPanel,"Item / serving description (e.g. 1 whole egg)",initialServing);serving.Name="FoodServing";
        p.Children.Add(servingPanel);
        var measureHint=Label("",12,Muted);measureHint.Name="FoodMeasureHint";p.Children.Add(measureHint);
        var fields=new TextBox[4];var grid=new System.Windows.Controls.Primitives.UniformGrid{Columns=2};
        var macroLabels=new TextBlock[4];
        for(int i=0;i<4;i++) {
            var cell=new StackPanel{Margin=new Thickness(0,0,12,0)};
            double value=entry==null?0:Store.Values(entry)[i]/FoodPortion.Quantity(entry);
            fields[i]=Field(cell,Store.MacroNames[i]+(i==0?" per measure (kcal)":" per measure (g)"),FoodPortion.Precise(value));
            macroLabels[i]=(TextBlock)cell.Children[0];
            fields[i].Name="Food"+Store.MacroNames[i];grid.Children.Add(cell);
        }
        p.Children.Add(grid);
        var timeCategory=new System.Windows.Controls.Primitives.UniformGrid{Columns=2};
        var timePanel=new StackPanel{Margin=new Thickness(0,0,12,0)};
        var eaten=TimePicker(timePanel,edit?entry["time"]:null,"FoodTime");
        timeCategory.Children.Add(timePanel);
        var categoryPanel=new StackPanel();categoryPanel.Children.Add(Label("Meal category",12,Muted));
        var category=new ComboBox{ItemsSource=Store.Categories,SelectedItem=entry==null?DefaultCategory():entry["category"],Padding=new Thickness(10),Foreground=Ink};
        categoryPanel.Children.Add(category);timeCategory.Children.Add(categoryPanel);p.Children.Add(timeCategory);
        var notes=Field(p,"Notes (optional)",entry==null?"":entry["notes"]);notes.Name="FoodNotes";
        p.Children.Insert(4,Button("Search USDA foods",delegate {
            SearchUsda(win,delegate(UsdaFood food,double basis,string definition,double count) {
                name.Text=food.Name.Length>200?food.Name.Substring(0,200):food.Name;
                for(int i=0;i<4;i++)fields[i].Text=food.Macros[i].HasValue?FoodPortion.Precise(food.Macros[i].Value*basis/100/count):"";
                serving.Text=definition;measure.SelectedItem=FoodPortion.Measure(definition);quantity.Text=FoodPortion.Precise(count);
                notes.Text="Source: USDA FoodData Central · FDC "+food.Id+" · "+food.Type;
            },name.Text);
        },true));
        var favorite=new CheckBox{Content="Save as a favorite",Name="FoodFavorite"};p.Children.Add(favorite);
        Action update=delegate {
            try {
                double amount=ReadQuantity(quantity);string selectedMeasure=(string)measure.SelectedItem;
                var total=FoodPortion.Scale(fields.Select(x=>Number(x,false)).ToArray(),amount);
                preview.Text="TOTAL FOR "+FoodPortion.AmountDescription(amount,selectedMeasure,serving.Text)+"\n"+Store.F(total[0])+" kcal   ·   P "+Store.F(total[1])+"g   ·   C "+Store.F(total[2])+"g   ·   F "+Store.F(total[3])+"g";
                var remaining=store.Targets(date);var logged=store.Totals(date);var previous=edit?Store.Values(entry):new double[4];preview.Text+="\nAfter saving: "+string.Join(" · ",Enumerable.Range(0,4).Select(i=>Store.F(remaining[i]-logged[i]+previous[i]-total[i])+(i==0?" kcal": "g "+Store.MacroNames[i])+" left"));
            } catch(ArgumentException) {preview.Text="Enter a valid amount and all four per-measure macro values to see the total.";}
        };
        Action applyMeasure=delegate {
            string selectedMeasure=(string)measure.SelectedItem;
            if(previousMeasure==FoodPortion.ItemMeasure&&!serving.IsReadOnly&&!string.IsNullOrWhiteSpace(serving.Text))savedItemServing=serving.Text;
            serving.IsReadOnly=selectedMeasure!=FoodPortion.ItemMeasure;
            serving.Text=selectedMeasure==FoodPortion.ItemMeasure?savedItemServing:FoodPortion.Definition(selectedMeasure,"");
            ((TextBlock)servingPanel.Children[0]).Text=selectedMeasure==FoodPortion.ItemMeasure?"Item / serving description (e.g. 1 whole egg)":"Nutrition basis";
            measureHint.Text=selectedMeasure==FoodPortion.ItemMeasure?"Enter macros for one item or serving. Amount multiplies the total.":"Enter macros for "+FoodPortion.BasisLabel(selectedMeasure)+". Amount is the weight you ate.";
            for(int i=0;i<4;i++)macroLabels[i].Text=Store.MacroNames[i]+" per "+FoodPortion.BasisLabel(selectedMeasure)+(i==0?" (kcal)":" (g)");
            previousMeasure=selectedMeasure;update();
        };
        foreach(var field in fields)field.TextChanged+=delegate{update();};quantity.TextChanged+=delegate{update();};serving.TextChanged+=delegate{update();};update();
        measure.SelectionChanged+=delegate{applyMeasure();};applyMeasure();
        var buttons=Row();
        var save=Button(edit?"Save changes":"Add food",delegate {
            try {
                double count=ReadQuantity(quantity);
                var total=FoodPortion.Scale(fields.Select(x=>Number(x,false)).ToArray(),count);
                string definition=FoodPortion.Definition((string)measure.SelectedItem,serving.Text);
                store.SaveFood(edit?entry["id"]:null,date,name.Text,total,(string)category.SelectedItem,definition,notes.Text,eaten.Value,count);
                if(favorite.IsChecked==true) {
                    var saved=edit?store.Foods(date).First(x=>x["id"]==entry["id"]):store.Db.Query("SELECT * FROM foods ORDER BY id DESC LIMIT 1")[0];
                    store.Favorite(saved);
                }
                win.Close();Navigate(page);Notice(edit?"Entry updated":"Food added");
            } catch(Exception ex){error.Text=ex.Message;}
        },true);
        save.IsDefault=true;save.Name="FoodSave";buttons.Children.Add(save);
        var cancel=Button("Cancel",delegate{win.Close();});cancel.IsCancel=true;buttons.Children.Add(cancel);footer.Children.Add(buttons);
        win.Closed+=delegate{foodDialog=null;};win.Loaded+=delegate{name.Focus();};win.ShowDialog();
    }
}
}
