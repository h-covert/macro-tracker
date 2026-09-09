using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MacroTracker {
public sealed partial class MainWindow {
 void ReminderSettings(){
  var reminders=store.Db.Query("SELECT * FROM reminders ORDER BY time");
  var panel=new StackPanel();panel.Children.Add(Label("Reminders",21,Ink));panel.Children.Add(Label("Choose one reminder to view or edit. Keep Macro Tracker running in the tray to receive it.",13,Muted));
  if(reminders.Count>0){
   var labels=reminders.Select(r=>ReminderLabel(r)).ToArray();string saved=store.Get("settings-reminder",reminders[0]["id"]);int index=Math.Max(0,reminders.FindIndex(r=>r["id"]==saved));
   var picker=Choose(panel,"Choose a reminder",labels,labels[index]);picker.Name="ReminderPicker";picker.Width=650;picker.HorizontalAlignment=HorizontalAlignment.Left;
   var editor=new StackPanel();panel.Children.Add(editor);
   Action<int> show=delegate(int at){
    editor.Children.Clear();var item=reminders[at];store.Set("settings-reminder",item["id"]);
    var row=Row();var enabled=new CheckBox{Content="Enabled",IsChecked=item["enabled"]=="1",Width=100};row.Children.Add(enabled);var time=new TextBox{Text=item["time"],Width=100,Margin=new Thickness(0,0,12,0),Name="ReminderTime"};row.Children.Add(time);row.Children.Add(Label("24-hour time (HH:mm)",12,Muted));editor.Children.Add(row);
    var message=Field(editor,"Message",item["message"]);message.Name="ReminderMessage";message.Width=760;message.HorizontalAlignment=HorizontalAlignment.Left;
    var buttons=Row();buttons.Children.Add(Button("Save reminder",delegate{DateTime parsed;if(!DateTime.TryParseExact(time.Text,"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out parsed))throw new ArgumentException("Enter a time such as 08:00 or 19:00.");if(string.IsNullOrWhiteSpace(message.Text)||message.Text.Length>180)throw new ArgumentException("Reminder message must be 1–180 characters.");store.Db.Query("UPDATE reminders SET time=?,message=?,enabled=?,snooze='' WHERE id=?",time.Text,message.Text,enabled.IsChecked==true?1:0,item["id"]);Navigate(page);Notice("Reminder saved");},true));buttons.Children.Add(Button("Snooze 15 min",delegate{store.Snooze(item["id"],DateTime.Now);Notice("Reminder snoozed for 15 minutes");},true));buttons.Children.Add(Button("Delete reminder",delegate{store.Db.Query("DELETE FROM reminders WHERE id=?",item["id"]);store.Set("settings-reminder","");Navigate(page);},true));editor.Children.Add(buttons);
   };
   picker.SelectionChanged+=delegate{if(picker.SelectedIndex>=0)show(picker.SelectedIndex);};show(index);
  }else panel.Children.Add(Label("No reminders yet. Add one when you want a gentle prompt to log food.",14,Muted));
  var extra=Row();extra.Children.Add(Button("+ Add reminder",delegate{store.Db.Query("INSERT INTO reminders(time,message,enabled) VALUES('10:00','Time to log your food.',0)");var added=store.Db.Query("SELECT id FROM reminders ORDER BY id DESC LIMIT 1")[0]["id"];store.Set("settings-reminder",added);Navigate(page);},true));extra.Children.Add(Button("Test notification",delegate{Notify("Test reminder",store.Context("Anything to log?",DateTime.Now));Notice("Test notification sent to Windows");},true));panel.Children.Add(extra);AddSettingsBox(panel);
 }
 static string ReminderLabel(Dictionary<string,string> r){DateTime time;string shown=DateTime.TryParseExact(r["time"],"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out time)?time.ToString("h:mm tt",CultureInfo.InvariantCulture):r["time"];string message=r["message"].Length>52?r["message"].Substring(0,49)+"…":r["message"];return shown+"  ·  "+message+(r["enabled"]=="1"?"":"  ·  Off");}
}
}
