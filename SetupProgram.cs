using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MacroTracker {
public static class SetupProgram {
    [STAThread] public static int Main(string[] args) {
        string step="starting setup";
        bool filesInstalled=false;
        try {
            string target=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","MacroTracker");
            if(MessageBox.Show("Install Macro Tracker "+ReleaseInfo.Version+" for your Windows account?\n\nIt will be available from your Desktop and Start menu. Future updates install from inside the app. Your diary and USDA key are kept.\n\nClose any running Macro Tracker window using its tray menu → Exit first.","Macro Tracker setup",MessageBoxButtons.OKCancel,MessageBoxIcon.Information)!=DialogResult.OK)return 0;
            step="unpacking the application";
            string stage=Path.Combine(Path.GetTempPath(),"MacroTracker-setup-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);
            string zip=Path.Combine(stage,"app.zip");using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream("AppPackage"))using(var output=File.Create(zip))input.CopyTo(output);
            step="verifying the application package";
            string files=Path.Combine(stage,"verified");UpdatePackage.Extract(zip,UpdatePackage.Hash(zip),ReleaseInfo.Version,files);
            step="installing application files";UpdatePackage.Apply(files,target);filesInstalled=true;
            step="creating the Desktop shortcut";
            Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"Macro Tracker.lnk"),target);
            step="creating the Start-menu shortcut";
            Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),"Programs","Macro Tracker.lnk"),target);
            step="updating the existing Windows startup entry";
            using(var startup=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run",true))
                if(startup!=null&&startup.GetValue("MacroTracker")!=null)
                    startup.SetValue("MacroTracker",UpdatePackage.Quote(Path.Combine(target,"MacroTracker.exe"))+" --tray");
            step="starting Macro Tracker";
            Process.Start(new ProcessStartInfo(Path.Combine(target,"MacroTracker.exe")){UseShellExecute=true,WorkingDirectory=target});return 0;
        }catch(Exception e){
            string logPath=null;
            try {
                string candidate=Path.Combine(Path.GetTempPath(),"MacroTracker-setup-error-"+Guid.NewGuid().ToString("N")+".log");
                File.WriteAllText(candidate,"Macro Tracker setup "+ReleaseInfo.Version+Environment.NewLine+DateTimeOffset.Now.ToString("o")+Environment.NewLine+"Step: "+step+Environment.NewLine+"Application files installed: "+filesInstalled+Environment.NewLine+e.ToString());
                logPath=candidate;
            }catch { /* Report the original failure even if logging is blocked. */ }
            MessageBox.Show(FailureMessage(e,step,filesInstalled,logPath),"Macro Tracker setup",MessageBoxButtons.OK,MessageBoxIcon.Warning);return 1;
        }
    }
    internal static string FailureMessage(Exception error,string step,bool filesInstalled,string logPath) {
        Exception cause=error.GetBaseException();
        return "Setup stopped while "+step+".\n\n"+cause.GetType().Name+": "+cause.Message+
            (filesInstalled?"\n\nApplication files were copied, but setup did not finish.":"")+
            (logPath==null?"\n\nAn error log could not be saved.":"\n\nError details: "+logPath)+
            "\n\nIf security software reported an alert, stop here and share the alert with your IT/security team. Do not disable protection or add an exclusion.";
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
