using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace MacroTracker {
public static class UpdaterProgram {
    [STAThread] public static int Main(string[] args) {
        if(args.Length!=8||args[0]!="--apply") {MessageBox.Show("Open Macro Tracker to check for updates.","Macro Tracker updater");return 2;}
        try {
            int pid=int.Parse(args[1]);long started=long.Parse(args[2]);
            string zip=Path.GetFullPath(args[3]),target=Path.GetFullPath(args[4]);
            string hash=args[5],version=args[6],data=args[7];
            string stage=Path.Combine(Path.GetDirectoryName(zip),"verified-"+Guid.NewGuid().ToString("N"));
            UpdatePackage.Extract(zip,hash,version,stage);
            try {using(var parent=Process.GetProcessById(pid)) {
                if(parent.StartTime.ToUniversalTime().Ticks==started && !parent.WaitForExit(60000))
                    throw new IOException("Macro Tracker is still open. Close it and try the update again.");
            }}catch(ArgumentException){} // The original process already exited.
            UpdatePackage.Apply(stage,target);
            var start=new ProcessStartInfo(Path.Combine(target,"MacroTracker.exe"),"--data "+UpdatePackage.Quote(data)){UseShellExecute=true,WorkingDirectory=target};
            Process.Start(start);return 0;
        } catch(Exception e) {
            MessageBox.Show(e.Message+"\n\nYou can reopen Macro Tracker and retry. Your diary and settings remain in their separate data folder.","Macro Tracker update",MessageBoxButtons.OK,MessageBoxIcon.Warning);return 1;
        }
    }
}
}
