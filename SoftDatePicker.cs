using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MacroTracker {
public sealed class SoftDatePicker : Button {
 readonly Window owner; DateTime? value;
 public event EventHandler SelectedDateChanged;
 public DateTime? SelectedDate {get{return value;}set{this.value=value;Refresh();}}
 public SoftDatePicker(Window owner,DateTime initial){this.owner=owner;SelectedDate=initial;HorizontalContentAlignment=HorizontalAlignment.Left;Padding=new Thickness(14,10,14,10);Click+=delegate{OpenCalendar();};}
 void Refresh(){Content=value.HasValue?"📅  "+value.Value.ToString("MMM d, yyyy",CultureInfo.CurrentCulture)+"    ⌄":"📅  Choose date    ⌄";}
 Button ActionButton(string text,Action action,bool primary=false){var button=new Button{Content=text,HorizontalContentAlignment=HorizontalAlignment.Center,Margin=new Thickness(2),Padding=new Thickness(10,8,10,8)};if(primary){button.Background=MainWindow.Green;button.Foreground=MainWindow.Bg;}button.Click+=delegate{action();};return button;}
 void OpenCalendar(){
  var window=new Window{Title="Choose a date",Name="MacroCalendar",Width=430,Height=540,MinWidth=390,MinHeight=500,Owner=owner,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=MainWindow.Bg,Foreground=MainWindow.Ink,FontFamily=owner.FontFamily,FontSize=14,Resources=owner.Resources};
  window.SourceInitialized+=delegate{MainWindow.RoundWindow(window);};var outer=new Border{Background=MainWindow.Bg,Padding=new Thickness(22),Child=new StackPanel()};window.Content=outer;var root=(StackPanel)outer.Child;var month=value??DateTime.Today;
  var heading=new TextBlock{FontSize=24,Foreground=MainWindow.Ink,Margin=new Thickness(4,0,4,14)};root.Children.Add(heading);var calendar=new UniformGrid{Columns=7,Margin=new Thickness(0,0,0,12)};root.Children.Add(calendar);
  Action render=null;render=delegate{
   heading.Text=month.ToString("MMMM yyyy",CultureInfo.CurrentCulture);calendar.Children.Clear();
   foreach(var day in new[]{"Sun","Mon","Tue","Wed","Thu","Fri","Sat"})calendar.Children.Add(new TextBlock{Text=day,Foreground=MainWindow.Muted,TextAlignment=TextAlignment.Center,Margin=new Thickness(2,5,2,10)});
   var first=new DateTime(month.Year,month.Month,1);for(int i=0;i<(int)first.DayOfWeek;i++)calendar.Children.Add(new Border());
   for(int d=1;d<=DateTime.DaysInMonth(month.Year,month.Month);d++){var chosen=new DateTime(month.Year,month.Month,d);bool selected=value.HasValue&&chosen.Date==value.Value.Date;bool today=chosen.Date==DateTime.Today;var button=ActionButton(d+(today?" •":""),delegate{value=chosen;Refresh();if(SelectedDateChanged!=null)SelectedDateChanged(this,EventArgs.Empty);window.Close();},selected);button.MinHeight=43;if(today&&!selected)button.Foreground=MainWindow.Green;calendar.Children.Add(button);}
  };
  var nav=new Grid{Margin=new Thickness(0,0,0,10)};nav.ColumnDefinitions.Add(new ColumnDefinition());nav.ColumnDefinitions.Add(new ColumnDefinition());var previous=ActionButton("‹  Previous month",delegate{month=month.AddMonths(-1);render();});var next=ActionButton("Next month  ›",delegate{month=month.AddMonths(1);render();});Grid.SetColumn(next,1);nav.Children.Add(previous);nav.Children.Add(next);root.Children.Insert(1,nav);
  var footer=new Grid();footer.ColumnDefinitions.Add(new ColumnDefinition());footer.ColumnDefinitions.Add(new ColumnDefinition());var todayButton=ActionButton("Today",delegate{value=DateTime.Today;Refresh();if(SelectedDateChanged!=null)SelectedDateChanged(this,EventArgs.Empty);window.Close();},true);var cancel=ActionButton("Cancel",delegate{window.Close();});Grid.SetColumn(cancel,1);footer.Children.Add(todayButton);footer.Children.Add(cancel);root.Children.Add(footer);render();window.ShowDialog();
 }
}
}
