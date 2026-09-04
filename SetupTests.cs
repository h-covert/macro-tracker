using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace MacroTracker {
internal static class SetupTests {
    static int count;
    static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);count++;}
    internal static int Run(string root) {
        root=Path.GetFullPath(root);Directory.CreateDirectory(root);
        try {
            string zip=Path.Combine(root,"app.zip");
            using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream("AppPackage"))
            using(var output=File.Create(zip))input.CopyTo(output);
            string stage=Path.Combine(root,"verified"),target=Path.Combine(root,"installed app");
            UpdatePackage.Extract(zip,UpdatePackage.Hash(zip),ReleaseInfo.Version,stage);
            UpdatePackage.Apply(stage,target);
            Check(UpdatePackage.Files.All(name=>UpdatePackage.Hash(Path.Combine(stage,name))==UpdatePackage.Hash(Path.Combine(target,name))),"embedded payload installation");
            File.WriteAllText(Path.Combine(target,"diary.db"),"diary fixture");
            File.WriteAllText(Path.Combine(target,"personal.usda-key"),"key fixture");
            UpdatePackage.Apply(stage,target);
            Check(File.ReadAllText(Path.Combine(target,"diary.db"))=="diary fixture"&&File.ReadAllText(Path.Combine(target,"personal.usda-key"))=="key fixture","repeat installation preserves unrelated files");
            string shortcut=Path.Combine(root,"shortcuts with spaces","Macro Tracker.lnk");
            ShellShortcut.Create(shortcut,target);
            Check(File.Exists(shortcut),"native shortcut saved");
            object instance=new ShellLinkObject();
            try {
                ((IPersistFile)instance).Load(shortcut,0);
                var link=(IShellLinkW)instance;var value=new StringBuilder(32768);
                link.GetPath(value,value.Capacity,IntPtr.Zero,0);
                Check(string.Equals(value.ToString(),Path.Combine(target,"MacroTracker.exe"),StringComparison.OrdinalIgnoreCase),"persisted shortcut target with spaces");
                value.Length=0;link.GetWorkingDirectory(value,value.Capacity);
                Check(string.Equals(value.ToString(),target,StringComparison.OrdinalIgnoreCase),"persisted working directory");
            }finally{Marshal.FinalReleaseComObject(instance);}
            ShellShortcut.Create(shortcut,target);
            Check(File.Exists(shortcut),"existing shortcut can be refreshed");
            var warnings=new List<string>();
            string blocked=Path.Combine(root,"not a directory");File.WriteAllText(blocked,"fixture");
            SetupProgram.TryOptional("creating the Desktop shortcut",delegate {ShellShortcut.Create(Path.Combine(blocked,"test.lnk"),target);},warnings);
            bool nextRan=false;SetupProgram.TryOptional("next optional step",delegate {nextRan=true;},warnings);
            Check(warnings.Count==1&&warnings[0].Contains("creating the Desktop shortcut")&&nextRan,"shortcut failure reported while later optional step continues");
            string message=SetupProgram.FailureMessage(new TargetInvocationException(new UnauthorizedAccessException("fixture denied")),"installing application files",false,null);
            Check(message.Contains("UnauthorizedAccessException: fixture denied")&&message.Contains("installing application files")&&message.Contains("could not be saved"),"underlying failure and unavailable log reported");
            Check(!message.Contains("files were copied"),"failed copy not reported as installed");
            File.WriteAllText(Path.Combine(root,"results.txt"),"PASS: "+count+" setup checks. No user shortcuts, startup entries, diary, or app processes changed.");
            return 0;
        } catch(Exception e){File.WriteAllText(Path.Combine(root,"results.txt"),e.ToString());return 1;}
    }
}
}
