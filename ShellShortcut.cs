using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace MacroTracker {
// Windows Shell's documented IShellLinkW vtable; HRESULT failures become COMException.
[ComImport, Guid("00021401-0000-0000-C000-000000000046")]
internal class ShellLinkObject { }
[ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellLinkW {
    void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path,int size,IntPtr findData,uint flags);
    void GetIDList(out IntPtr list);
    void SetIDList(IntPtr list);
    void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder text,int size);
    void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string text);
    void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path,int size);
    void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string path);
    void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder args,int size);
    void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string args);
    void GetHotkey(out short hotkey);
    void SetHotkey(short hotkey);
    void GetShowCmd(out int command);
    void SetShowCmd(int command);
    void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path,int size,out int index);
    void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string path,int index);
    void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string path,uint reserved);
    void Resolve(IntPtr window,uint flags);
    void SetPath([MarshalAs(UnmanagedType.LPWStr)] string path);
}
internal static class ShellShortcut {
    internal static void Create(string path,string target) {
        if(string.IsNullOrWhiteSpace(path)||!Path.IsPathRooted(path))throw new IOException("Windows did not provide an absolute shortcut location.");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        object instance=new ShellLinkObject();
        try {
            var link=(IShellLinkW)instance;
            string executable=Path.Combine(target,"MacroTracker.exe");
            link.SetPath(executable);link.SetWorkingDirectory(target);
            link.SetDescription("Macro Tracker");link.SetIconLocation(executable,0);link.SetShowCmd(1);
            ((IPersistFile)instance).Save(path,true);
        } finally {Marshal.FinalReleaseComObject(instance);}
    }
}
}
