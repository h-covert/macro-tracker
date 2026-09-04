using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MacroTracker {
public static class SetupProgram {
    [STAThread] public static int Main(string[] args) {
        try {
            string target=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","MacroTracker");
            if(MessageBox.Show("Install Macro Tracker "+ReleaseInfo.Version+" for your Windows account?\n\nIt will be available from your Desktop and Start menu. Future updates install from inside the app. Your diary and USDA key are kept.\n\nClose any running Macro Tracker window using its tray menu → Exit first.","Macro Tracker setup",MessageBoxButtons.OKCancel,MessageBoxIcon.Information)!=DialogResult.OK)return 0;
            string stage=Path.Combine(Path.GetTempPath(),"MacroTracker-setup-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);
            string zip=Path.Combine(stage,"app.zip");using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream("AppPackage"))using(var output=File.Create(zip))input.CopyTo(output);
            string files=Path.Combine(stage,"verified");UpdatePackage.Extract(zip,UpdatePackage.Hash(zip),ReleaseInfo.Version,files);UpdatePackage.Apply(files,target);
            Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"Macro Tracker.lnk"),target);
            Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),"Programs","Macro Tracker.lnk"),target);
            using(var startup=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run",true))
                if(startup!=null&&startup.GetValue("MacroTracker")!=null)
                    startup.SetValue("MacroTracker",UpdatePackage.Quote(Path.Combine(target,"MacroTracker.exe"))+" --tray");
            Process.Start(new ProcessStartInfo(Path.Combine(target,"MacroTracker.exe")){UseShellExecute=true,WorkingDirectory=target});return 0;
        }catch(Exception e){MessageBox.Show(e.Message,"Macro Tracker setup",MessageBoxButtons.OK,MessageBoxIcon.Warning);return 1;}
    }
    static void Shortcut(string path,string target) {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        Type shellType=Type.GetTypeFromProgID("WScript.Shell");object shell=Activator.CreateInstance(shellType);
        object link=shellType.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});
        Type type=link.GetType();type.InvokeMember("TargetPath",BindingFlags.SetProperty,null,link,new object[]{Path.Combine(target,"MacroTracker.exe")});
        type.InvokeMember("WorkingDirectory",BindingFlags.SetProperty,null,link,new object[]{target});type.InvokeMember("Save",BindingFlags.InvokeMethod,null,link,null);
    }
}
}
