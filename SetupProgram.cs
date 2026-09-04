using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Collections.Generic;

namespace MacroTracker {
public static class SetupProgram {
    [STAThread] public static int Main(string[] args) {
        if(args.Length==2&&args[0]=="--self-test")return SetupTests.Run(args[1]);
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
            var warnings=new List<string>();
            TryOptional("creating the Desktop shortcut",delegate {
                ShellShortcut.Create(ShortcutPath(Environment.SpecialFolder.DesktopDirectory,"Macro Tracker.lnk"),target);
            },warnings);
            TryOptional("creating the Start-menu shortcut",delegate {
                ShellShortcut.Create(ShortcutPath(Environment.SpecialFolder.StartMenu,Path.Combine("Programs","Macro Tracker.lnk")),target);
            },warnings);
            TryOptional("updating the existing Windows startup entry",delegate {
            using(var startup=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run",true))
                if(startup!=null&&startup.GetValue("MacroTracker")!=null)
                    startup.SetValue("MacroTracker",UpdatePackage.Quote(Path.Combine(target,"MacroTracker.exe"))+" --tray");
            },warnings);
            if(warnings.Count>0) {
                MessageBox.Show("Macro Tracker "+ReleaseInfo.Version+" was installed, but these optional steps could not finish:\n\n"+string.Join("\n\n",warnings)+"\n\nApplication location: "+Path.Combine(target,"MacroTracker.exe")+"\n\nSetup has not started the app. Your diary and USDA key were not changed.","Macro Tracker setup",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                return 0;
            }
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
    static string ShortcutPath(Environment.SpecialFolder folder,string name) {
        string root=Environment.GetFolderPath(folder);
        if(string.IsNullOrWhiteSpace(root))throw new IOException("Windows did not provide the shortcut folder.");
        return Path.Combine(root,name);
    }
    internal static void TryOptional(string step,Action action,List<string> warnings) {
        try {action();}
        catch(Exception error) {
            string log=null;
            try {
                string candidate=Path.Combine(Path.GetTempPath(),"MacroTracker-setup-warning-"+Guid.NewGuid().ToString("N")+".log");
                File.WriteAllText(candidate,"Macro Tracker "+ReleaseInfo.Version+Environment.NewLine+step+Environment.NewLine+error);log=candidate;
            }catch { }
            Exception cause=error.GetBaseException();
            warnings.Add(step+": "+cause.GetType().Name+": "+cause.Message+(log==null?"\nAn error log could not be saved.":"\nDetails: "+log));
        }
    }
}
}
