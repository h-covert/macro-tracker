using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;

namespace MacroTracker {
public static class CameraCapture {
    static readonly string[] extensions={".jpg",".jpeg",".png",".bmp"};
    static readonly Guid cameraRollId=new Guid("AB5FB87B-7CE2-4F83-915D-550846C9537B");
    [DllImport("shell32.dll")] static extern int SHGetKnownFolderPath(ref Guid folder,int flags,IntPtr token,out IntPtr path);

    static IEnumerable<string> CameraFolders() {
        var folders=new List<string>();IntPtr native=IntPtr.Zero;Guid id=cameraRollId;
        try{if(SHGetKnownFolderPath(ref id,0,IntPtr.Zero,out native)==0&&native!=IntPtr.Zero)folders.Add(Marshal.PtrToStringUni(native));}catch{}finally{if(native!=IntPtr.Zero)Marshal.FreeCoTaskMem(native);}
        string pictures=Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),profile=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if(!string.IsNullOrWhiteSpace(pictures)){folders.Add(Path.Combine(pictures,"Camera Roll"));folders.Add(Path.Combine(pictures,"Pictures","Camera Roll"));}
        if(!string.IsNullOrWhiteSpace(profile))folders.Add(Path.Combine(profile,"Pictures","Camera Roll"));
        foreach(string variable in new[]{"OneDrive","OneDriveConsumer","OneDriveCommercial"}){string root=Environment.GetEnvironmentVariable(variable);if(!string.IsNullOrWhiteSpace(root))folders.Add(Path.Combine(root,"Pictures","Camera Roll"));}
        return folders.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase);
    }
    static IEnumerable<string> Photos(IEnumerable<string> folders){foreach(string folder in folders){IEnumerable<string> files;try{files=Directory.Exists(folder)?Directory.EnumerateFiles(folder):new string[0];}catch{continue;}foreach(string file in files)if(extensions.Contains(Path.GetExtension(file),StringComparer.OrdinalIgnoreCase))yield return file;}}
    static string Signature(string file){try{var info=new FileInfo(file);return info.Length+"|"+info.LastWriteTimeUtc.Ticks;}catch{return "";}}
    public static Dictionary<string,string> Snapshot(IEnumerable<string> folders){var result=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);foreach(string file in Photos(folders))result[file]=Signature(file);return result;}
    public static string FindNew(IEnumerable<string> folders,Dictionary<string,string> before){return Photos(folders).Select(file=>new{File=file,Signature=Signature(file),Time=File.GetLastWriteTimeUtc(file)}).Where(x=>x.Signature!=""&&(!before.ContainsKey(x.File)||before[x.File]!=x.Signature)).OrderByDescending(x=>x.Time).Select(x=>x.File).FirstOrDefault();}

    public static string Take(Window owner) {
        var folders=CameraFolders().ToArray();var before=Snapshot(folders);string result=null,candidate=null;
        var window=new Window{Title="Take a nutrition label photo",Owner=owner,Width=650,Height=360,MinWidth=560,MinHeight=320,Background=owner.Background,Foreground=owner.Foreground,Resources=owner.Resources,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var root=new StackPanel{Margin=new Thickness(26)};window.Content=root;
        root.Children.Add(new TextBlock{Text="Take a clear photo in Windows Camera",FontSize=24,Foreground=owner.Foreground,Margin=new Thickness(0,0,0,10)});
        root.Children.Add(new TextBlock{Text="Keep the full nutrition label in the frame and press the Camera shutter. Return to Macro Tracker when the photo is saved.",FontSize=14,Foreground=owner.Foreground,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,18)});
        var status=new TextBlock{Text="Opening Windows Camera…",FontSize=14,Foreground=owner.Foreground,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,18)};root.Children.Add(status);
        var actions=new WrapPanel();root.Children.Add(actions);var use=new Button{Content="Use captured photo",IsDefault=true,IsEnabled=false};var reopen=new Button{Content="Open Camera again"};var choose=new Button{Content="Choose Camera Roll photo"};var cancel=new Button{Content="Cancel",IsCancel=true};actions.Children.Add(use);actions.Children.Add(reopen);actions.Children.Add(choose);actions.Children.Add(cancel);
        Action refresh=delegate{candidate=FindNew(folders,before);if(candidate==null)return;use.IsEnabled=true;status.Text="Photo found: "+Path.GetFileName(candidate)+"\nChoose Use captured photo to scan it.";};
        Action launch=delegate{try{Process.Start(new ProcessStartInfo("microsoft.windows.camera:"){UseShellExecute=true});status.Text="Windows Camera is open. Take the photo, then return here.";}catch(Exception e){status.Text="Windows Camera could not open. Use Choose Camera Roll photo or Choose image on the label page. "+e.Message;}};
        use.Click+=delegate{refresh();if(candidate==null||!File.Exists(candidate)){status.Text="No new camera photo was found yet. Take a photo, wait for it to save, and try again.";return;}result=candidate;window.DialogResult=true;};
        reopen.Click+=delegate{launch();};choose.Click+=delegate{var dialog=new OpenFileDialog{Title="Choose a camera photo",Filter="Label image|*.png;*.jpg;*.jpeg;*.bmp",InitialDirectory=folders.FirstOrDefault(Directory.Exists)};if(dialog.ShowDialog(window)==true){result=dialog.FileName;window.DialogResult=true;}};
        var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(750)};timer.Tick+=delegate{refresh();};window.Activated+=delegate{refresh();};window.ContentRendered+=delegate{timer.Start();launch();};window.Closed+=delegate{timer.Stop();};window.ShowDialog();return result;
    }
}
}
