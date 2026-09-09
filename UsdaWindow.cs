using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MacroTracker {
public sealed partial class MainWindow {
    readonly UsdaClient usda = new UsdaClient();
    internal Func<string,string,CancellationToken,Task<List<UsdaFood>>> UsdaTestSearch;

    void UsdaSettingsCard() {
        var p=new StackPanel();p.Children.Add(Label("USDA food search",21,Ink));
        p.Children.Add(Label(new UsdaKey(store.Path).Personal ? "Personal API key saved on this Windows account." : "Ready to try with the USDA demo key. Add a free personal key for everyday use.",13,Muted));
        p.Children.Add(SettingsAction("Set up USDA search",delegate{UsdaSetup(this);},220));
        AddSettingsBox(p);
    }

    void UsdaSetup(Window owner) {
        var keys=new UsdaKey(store.Path);
        var win=new Window{Title="USDA search setup",Owner=owner,Width=580,Height=510,Background=Bg,Foreground=Ink,Resources=Resources,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var p=new StackPanel{Margin=new Thickness(26)};win.Content=new ScrollViewer{Content=p,Background=Bg,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
        p.Children.Add(Label("Food search, without the typing",25,Ink));
        p.Children.Add(Label("1. Get your free USDA key using the signup link.\n2. Paste it below and save.\n3. In Add food, choose Search USDA.",15,Muted));
        p.Children.Add(Button("Get a free USDA API key ↗",delegate{Process.Start(new ProcessStartInfo("https://api.data.gov/signup/"){UseShellExecute=true});},true));
        p.Children.Add(Label("Personal API key (kept private on this Windows account)",12,Muted));
        var key=new PasswordBox{Height=42,Padding=new Thickness(10),Margin=new Thickness(0,8,0,12)};p.Children.Add(key);
        var state=Label(keys.Personal?"A key is already saved. Paste a new one only to replace it.":"Demo mode is active: 30 requests/hour, 50/day. No signup needed to try it.",13,Muted);p.Children.Add(state);
        var row=Row();row.Children.Add(Button("Save personal key",delegate{if(string.IsNullOrWhiteSpace(key.Password))throw new ArgumentException("Paste your key first, or choose Use demo key.");keys.Save(key.Password);key.Clear();state.Text="Personal key saved. Close this window and try a food search.";Notice("USDA key saved");},true));
        row.Children.Add(Button("Use demo key",delegate{keys.Save("");key.Clear();state.Text="Demo mode active. You can search now.";},true));p.Children.Add(row);
        p.Children.Add(Label("Search sends your food query to USDA over the internet. Your diary, weight, and targets stay local. Foods you save can be reused offline.",13,Muted));
        p.Children.Add(Button("Done",delegate{win.Close();},true));win.ShowDialog();
    }

    internal void SearchUsda(Window owner,Action<UsdaFood,double,string,double> use,string initial) {
        var win=new Window{Title="Search USDA foods",Owner=owner,Width=820,Height=790,MinWidth=650,MinHeight=600,Background=Bg,Foreground=Ink,Resources=Resources,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var layout=new DockPanel{Margin=new Thickness(24),Background=Bg};win.Content=new Border{Background=Bg,Child=layout};
        var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);layout.Children.Add(top);
        top.Children.Add(Label("Find your food",28,Ink));top.Children.Add(Label("Choose a matching food, then set how much you ate.",13,Muted));
        var query=Field(top,"Food name",initial);query.Name="UsdaQuery";
        var controls=Row();var scope=new ComboBox{ItemsSource=new[]{"Common foods","Branded foods"},SelectedIndex=0,Width=170,Padding=new Thickness(8),Margin=new Thickness(0,0,10,0)};controls.Children.Add(scope);
        var search=Button("Search",delegate{},true);search.Name="UsdaSearch";controls.Children.Add(search);controls.Children.Add(Button("USDA setup",delegate{UsdaSetup(win);}));top.Children.Add(controls);
        var message=Label("Search runs only when you press Search or Enter. Source: USDA FoodData Central.",12,Muted);top.Children.Add(message);

        var bottom=new StackPanel{Margin=new Thickness(0,16,0,0)};DockPanel.SetDock(bottom,Dock.Bottom);layout.Children.Add(bottom);
        var detail=Label("Select a result to preview your serving.",15,Ink);bottom.Children.Add(detail);
        var amountRow=Row();var amount=new TextBox{Text="100",Width=100,Padding=new Thickness(8),Name="UsdaAmount"};amountRow.Children.Add(amount);
        var units=new ComboBox{Width=200,Padding=new Thickness(8),Margin=new Thickness(10,0,0,0),Name="UsdaUnits"};amountRow.Children.Add(units);bottom.Children.Add(amountRow);
        var preview=Label("",13,Green);bottom.Children.Add(preview);
        var apply=Button("Use this food",delegate{},true);apply.Name="UsdaUse";apply.IsEnabled=false;bottom.Children.Add(apply);
        var results=new ListBox{Background=Card,Foreground=Ink,BorderThickness=new Thickness(0),Name="UsdaResults",HorizontalContentAlignment=HorizontalAlignment.Stretch};
        ScrollViewer.SetHorizontalScrollBarVisibility(results,ScrollBarVisibility.Disabled);layout.Children.Add(results);
        UsdaFood picked=null;bool busy=false;bool closed=false;var cancellation=new CancellationTokenSource();
        Action update=delegate {
            apply.IsEnabled=false;if(picked==null)return;
            try {double n;if(!double.TryParse(amount.Text,NumberStyles.Float,CultureInfo.CurrentCulture,out n))throw new ArgumentException("Enter your serving amount.");double basis=picked.BaseAmount(n,(string)units.SelectedItem);
                preview.Text=picked.MacroText(basis);apply.IsEnabled=true;
            } catch(Exception e){preview.Text=e.Message;}
        };
        results.SelectionChanged+=async delegate {
            var item=results.SelectedItem as ListBoxItem;picked=item==null?null:item.Tag as UsdaFood;
            if(picked==null){apply.IsEnabled=false;detail.Text="Select a result to preview your serving.";preview.Text="";return;}
            detail.Text=picked.Name+"\n"+(picked.ServingSize.HasValue?"Label serving: "+Store.F(picked.ServingSize.Value)+" "+picked.BasisUnit+" "+picked.ServingLabel:"Values are per 100 "+picked.BasisUnit)+
                (picked.Macros.Any(v=>!v.HasValue)?"\nSome macros are missing; fill those in before saving.":"");
            var options=new List<string>{picked.BasisUnit,picked.BasisUnit=="g"?"oz (weight)":"US fl oz"};if(picked.ServingSize.HasValue)options.Add("Label servings");
            options.AddRange(picked.Portions.Select(portion=>portion.Display));
            units.ItemsSource=options;units.SelectedIndex=0;amount.Text="100";update();
            var loading=picked;
            if(!test&&loading.Type!="Branded"&&!loading.PortionsLoaded) {
                try {
                    message.Text="Loading portion sizes… You can also use grams or ounces.";
                    await usda.LoadPortions(loading,new UsdaKey(store.Path).Read(),cancellation.Token);
                    if(closed||picked!=loading)return;
                    string previous=(string)units.SelectedItem;
                    options.AddRange(loading.Portions.Select(portion=>portion.Display));
                    units.ItemsSource=options.ToArray();units.SelectedItem=previous;
                    message.Text=loading.Portions.Count==0?"No household portions available for this food. Use grams or ounces.":"Portions are ready in the unit menu (for example, 1 large).";
                }catch(OperationCanceledException){}
                catch(Exception e){if(!closed&&picked==loading)message.Text=e.Message;}
            }
        };
        string previousUnit=null;amount.TextChanged+=delegate{update();};units.SelectionChanged+=delegate{string selectedUnit=units.SelectedItem as string;var portion=picked==null||selectedUnit==null?null:picked.Portions.FirstOrDefault(x=>x.Display==selectedUnit);if(portion!=null&&selectedUnit!=previousUnit)amount.Text=FoodPortion.Precise(portion.DefaultAmount);previousUnit=selectedUnit;update();};
        Func<Task> run=async delegate {
            if(busy)return;busy=true;search.IsEnabled=false;results.Items.Clear();message.Text="Searching USDA…";
            try {
                var data=UsdaTestSearch!=null?await UsdaTestSearch(query.Text,(string)scope.SelectedItem,cancellation.Token):
                    await usda.Search(query.Text,(string)scope.SelectedItem,new UsdaKey(store.Path).Read(),cancellation.Token);
                if(closed)return;
                foreach(var food in data){var p=new StackPanel{Margin=new Thickness(9)};p.Children.Add(Label(food.Name,16,Ink));p.Children.Add(Label((food.Brand==""?"":food.Brand+" · ")+food.Type+" · FDC "+food.Id,12,Muted));p.Children.Add(Label(food.MacroText(100)+" / 100 "+food.BasisUnit,12,Green));results.Items.Add(new ListBoxItem{Content=p,Tag=food,HorizontalContentAlignment=HorizontalAlignment.Stretch});}
                message.Text=data.Count==0?"No usable results. Try another name or switch Common / Branded foods.":data.Count+" results · Check the brand and raw/cooked description before choosing.";
            } catch(OperationCanceledException) {if(!closed)message.Text="Search canceled.";}
            catch(Exception e) {if(!closed)message.Text=e.Message;}
            finally {busy=false;if(!closed)search.IsEnabled=true;}
        };
        search.Click+=async delegate{await run();};query.KeyDown+=async delegate(object sender,KeyEventArgs e){if(e.Key==Key.Enter){e.Handled=true;await run();}};
        apply.Click+=delegate {
            try {if(picked==null)return;double n=double.Parse(amount.Text,CultureInfo.CurrentCulture);double basis=picked.BaseAmount(n,(string)units.SelectedItem);
                string selectedUnit=(string)units.SelectedItem;
                string definition=selectedUnit=="Label servings"?"1 label serving ("+FoodPortion.Precise(picked.ServingSize.Value)+" "+picked.BasisUnit+")":
                    picked.Portions.Any(portion=>portion.Display==selectedUnit)?selectedUnit:"1 "+selectedUnit;
                use(picked,basis,definition,n);win.Close();
            }catch(Exception e){preview.Text=e.Message;}
        };
        win.Closed+=delegate{closed=true;cancellation.Cancel();};win.Loaded+=delegate{query.Focus();};win.ShowDialog();
    }
}
}
