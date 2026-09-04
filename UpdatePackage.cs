using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace MacroTracker {
public static class UpdatePackage {
    public static readonly string[] Files={"MacroTracker.exe","MacroTracker.exe.config","MacroTracker.Updater.exe"};
    public static string Hash(string path) {using(var sha=SHA256.Create())using(var f=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
    public static Version VersionOf(string value) {
        Version parsed;
        if(!Version.TryParse((value??"").TrimStart('v'),out parsed)||parsed.Build<0)
            throw new InvalidDataException("The release version must be a stable major.minor.patch version.");
        return new Version(parsed.Major,parsed.Minor,parsed.Build,Math.Max(0,parsed.Revision));
    }
    public static void Extract(string zip,string expectedHash,string version,string stage) {
        if(expectedHash.Length!=64||expectedHash.Any(c=>!Uri.IsHexDigit(c))||!string.Equals(Hash(zip),expectedHash,StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The update download failed its SHA-256 integrity check. Nothing was installed.");
        var expected=VersionOf(version);Directory.CreateDirectory(stage);
        using(var archive=ZipFile.OpenRead(zip)) {
            if(archive.Entries.Count!=Files.Length)throw new InvalidDataException("Unexpected files in the update package.");
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var entry in archive.Entries) {
                if(!Files.Contains(entry.FullName)||!seen.Add(entry.FullName)||entry.Length<=0||entry.Length>30000000)
                    throw new InvalidDataException("The update package contains an invalid file.");
                entry.ExtractToFile(Path.Combine(stage,entry.FullName),false);
            }
        }
        foreach(string binary in new[]{"MacroTracker.exe","MacroTracker.Updater.exe"})
            if(AssemblyName.GetAssemblyName(Path.Combine(stage,binary)).Version!=expected)
                throw new InvalidDataException("The downloaded program version doesn't match the release.");
    }
    public static void Apply(string stage,string target,Action<int> testHook=null) {
        target=Path.GetFullPath(target);stage=Path.GetFullPath(stage);
        if(string.Equals(target,stage,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Staging and install folders must differ.");
        Directory.CreateDirectory(target);
        // Probe write access before touching any installed program file.
        string probe=Path.Combine(target,".macro-write-"+Guid.NewGuid().ToString("N"));File.WriteAllText(probe,"");File.Delete(probe);
        string backup=Path.Combine(stage,"previous");Directory.CreateDirectory(backup);
        var existing=new HashSet<string>();
        foreach(string name in Files) {
            if(!File.Exists(Path.Combine(stage,name)))throw new InvalidDataException("The staged update is incomplete.");
            if(File.Exists(Path.Combine(target,name))){File.Copy(Path.Combine(target,name),Path.Combine(backup,name),true);existing.Add(name);}
        }
        var replaced=new List<string>();
        try {
            for(int i=0;i<Files.Length;i++) {
                string name=Files[i];replaced.Add(name);
                File.Copy(Path.Combine(stage,name),Path.Combine(target,name),true);
                if(testHook!=null)testHook(i);
            }
        } catch(Exception failure) {
            try {
                foreach(string name in replaced) {
                    if(existing.Contains(name))File.Copy(Path.Combine(backup,name),Path.Combine(target,name),true);
                    else if(File.Exists(Path.Combine(target,name)))File.Delete(Path.Combine(target,name));
                }
            } catch(Exception rollback) {throw new IOException("Update failed and program-file rollback needs attention. Your diary was not touched. Previous program files: "+backup,new AggregateException(failure,rollback));}
            throw new IOException("Update failed. The previous program files were restored. Your diary was not touched.",failure);
        }
    }
    public static string Quote(string text) {
        if(text.IndexOf('"')>=0)throw new ArgumentException("Unexpected quote in a path.");
        // Windows command-line quoting: double trailing slashes before the closing quote.
        int trailing=text.Length-text.TrimEnd('\\').Length;
        return "\""+text+new string('\\',trailing)+"\"";
    }
}
}
