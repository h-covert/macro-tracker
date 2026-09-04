using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MacroTracker {
public sealed partial class MainWindow {
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
        foodDialog=win;
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
        var quantityRow=new System.Windows.Controls.Primitives.UniformGrid{Columns=2};
        var qPanel=new StackPanel{Margin=new Thickness(0,0,14,0)};
        var quantity=Field(qPanel,"Quantity",FoodPortion.Precise(FoodPortion.Quantity(entry)));quantity.Name="FoodQuantity";
        quantityRow.Children.Add(qPanel);
        var servingPanel=new StackPanel();
        var serving=Field(servingPanel,"One item / serving (e.g. 1 whole egg)",entry==null||entry["serving"]==""?"1 serving":entry["serving"]);serving.Name="FoodServing";
        quantityRow.Children.Add(servingPanel);p.Children.Add(quantityRow);
        p.Children.Add(Label("Macros below are for ONE item / serving. Quantity scales the total automatically.",12,Muted));
        var fields=new TextBox[4];var grid=new System.Windows.Controls.Primitives.UniformGrid{Columns=2};
        for(int i=0;i<4;i++) {
            var cell=new StackPanel{Margin=new Thickness(0,0,12,0)};
            double value=entry==null?0:Store.Values(entry)[i]/FoodPortion.Quantity(entry);
            fields[i]=Field(cell,Store.MacroNames[i]+(i==0?" per item (kcal)":" per item (g)"),FoodPortion.Precise(value));
            fields[i].Name="Food"+Store.MacroNames[i];grid.Children.Add(cell);
        }
        p.Children.Add(grid);
        var timeCategory=new System.Windows.Controls.Primitives.UniformGrid{Columns=2};
        var timePanel=new StackPanel{Margin=new Thickness(0,0,12,0)};
        var eaten=Field(timePanel,"Time eaten (AM/PM or 24-hour)",edit?entry["time"]:DateTime.Now.ToString("h:mm tt",CultureInfo.InvariantCulture));eaten.Name="FoodTime";
        timeCategory.Children.Add(timePanel);
        var categoryPanel=new StackPanel();categoryPanel.Children.Add(Label("Meal category",12,Muted));
        var category=new ComboBox{ItemsSource=Store.Categories,SelectedItem=entry==null?DefaultCategory():entry["category"],Padding=new Thickness(10),Foreground=Bg};
        categoryPanel.Children.Add(category);timeCategory.Children.Add(categoryPanel);p.Children.Add(timeCategory);
        var notes=Field(p,"Notes (optional)",entry==null?"":entry["notes"]);notes.Name="FoodNotes";
        p.Children.Insert(4,Button("Search USDA foods",delegate {
            SearchUsda(win,delegate(UsdaFood food,double basis,string definition,double count) {
                name.Text=food.Name.Length>200?food.Name.Substring(0,200):food.Name;
                for(int i=0;i<4;i++)fields[i].Text=food.Macros[i].HasValue?FoodPortion.Precise(food.Macros[i].Value*basis/100/count):"";
                serving.Text=definition;quantity.Text=FoodPortion.Precise(count);
                notes.Text="Source: USDA FoodData Central · FDC "+food.Id+" · "+food.Type;
            },name.Text);
        },true));
        var favorite=new CheckBox{Content="Save as a favorite",Name="FoodFavorite"};p.Children.Add(favorite);
        Action update=delegate {
            try {
                var total=FoodPortion.Scale(fields.Select(x=>Number(x,false)).ToArray(),ReadQuantity(quantity));
                preview.Text="TOTAL FOR "+quantity.Text+" × "+serving.Text+"\n"+Store.F(total[0])+" kcal   ·   P "+Store.F(total[1])+"g   ·   C "+Store.F(total[2])+"g   ·   F "+Store.F(total[3])+"g";
            } catch(ArgumentException) {preview.Text="Enter a valid quantity and all four per-item macro values to see the total.";}
        };
        foreach(var field in fields)field.TextChanged+=delegate{update();};quantity.TextChanged+=delegate{update();};serving.TextChanged+=delegate{update();};update();
        var buttons=Row();
        var save=Button(edit?"Save changes":"Add food",delegate {
            try {
                double count=ReadQuantity(quantity);
                var total=FoodPortion.Scale(fields.Select(x=>Number(x,false)).ToArray(),count);
                if(string.IsNullOrWhiteSpace(serving.Text))throw new ArgumentException("Describe one item / serving, such as 1 whole egg or 100 g.");
                store.SaveFood(edit?entry["id"]:null,date,name.Text,total,(string)category.SelectedItem,serving.Text.Trim(),notes.Text,eaten.Text,count);
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
