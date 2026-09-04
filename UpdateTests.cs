using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;

namespace MacroTracker {
public static class UpdateTests {
    static int count;
    static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);count++;}
    public static void Run(string path) {
        string root=path+".update-tests";Directory.CreateDirectory(root);
        string source=Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string payload=Path.Combine(root,"payload");Directory.CreateDirectory(payload);
        foreach(string name in UpdatePackage.Files)File.Copy(Path.Combine(source,name),Path.Combine(payload,name));
        string zip=Path.Combine(root,"app.zip");ZipFile.CreateFromDirectory(payload,zip);string hash=UpdatePackage.Hash(zip);
        string stage=Path.Combine(root,"stage");UpdatePackage.Extract(zip,hash,ReleaseInfo.Version,stage);
        Check(UpdatePackage.Files.All(name=>File.Exists(Path.Combine(stage,name))),"exact update payload verified and extracted");
        bool fail=false;try{UpdatePackage.Extract(zip,new string('0',64),ReleaseInfo.Version,Path.Combine(root,"bad-hash"));}catch(InvalidDataException){fail=true;}Check(fail,"tampered checksum rejected");
        fail=false;try{UpdatePackage.Extract(zip,hash,"99.0.0",Path.Combine(root,"bad-version"));}catch(InvalidDataException){fail=true;}Check(fail,"binary version mismatch rejected");
        string badZip=Path.Combine(root,"traversal.zip");using(var archive=ZipFile.Open(badZip,ZipArchiveMode.Create)){archive.CreateEntry("../outside.exe");archive.CreateEntry("MacroTracker.exe");archive.CreateEntry("MacroTracker.Updater.exe");}
        fail=false;try{UpdatePackage.Extract(badZip,UpdatePackage.Hash(badZip),ReleaseInfo.Version,Path.Combine(root,"bad-path"));}catch(InvalidDataException){fail=true;}Check(fail&&!File.Exists(Path.Combine(root,"outside.exe")),"ZIP path traversal rejected");
        var metadata=new {draft=false,prerelease=false,tag_name="v1.3.0",assets=new[]{new{name=ReleaseInfo.Asset,browser_download_url=ReleaseInfo.Home+"/releases/download/v1.3.0/"+ReleaseInfo.Asset,digest="sha256:"+hash,size=new FileInfo(zip).Length}}};
        var json=new JavaScriptSerializer().Serialize(metadata);Check(ReleaseClient.Parse(json).Hash==hash,"GitHub release metadata parsed");
        fail=false;try{ReleaseClient.Parse(json.Replace("https://github.com/","https://evil.example/"));}catch(InvalidDataException){fail=true;}Check(fail,"foreign update host rejected");
        fail=false;try{ReleaseClient.Parse(json.Replace("sha256:","missing:"));}catch(InvalidDataException){fail=true;}Check(fail,"missing trusted digest rejected");
        Check(UpdatePackage.VersionOf("v1.10.0")>UpdatePackage.VersionOf("1.9.0"),"versions compared numerically");
        string target=Path.Combine(root,"installed");Directory.CreateDirectory(target);
        foreach(string name in UpdatePackage.Files)File.WriteAllText(Path.Combine(target,name),"previous "+name);
        File.WriteAllText(Path.Combine(target,"diary.db"),"keep diary");File.WriteAllText(Path.Combine(target,"personal.usda-key"),"keep key");
        fail=false;try{UpdatePackage.Apply(stage,target,delegate(int i){if(i==1)throw new IOException("Simulated failed update");});}catch(IOException){fail=true;}
        Check(fail&&UpdatePackage.Files.All(name=>File.ReadAllText(Path.Combine(target,name))=="previous "+name),"partial update rolls back all changed program files");
        UpdatePackage.Apply(stage,target);
        Check(UpdatePackage.Files.All(name=>UpdatePackage.Hash(Path.Combine(target,name))==UpdatePackage.Hash(Path.Combine(payload,name))),"in-place update replaces program files");
        Check(File.ReadAllText(Path.Combine(target,"diary.db"))=="keep diary"&&File.ReadAllText(Path.Combine(target,"personal.usda-key"))=="keep key","update preserves unrelated data and credentials");
        Check(UpdatePackage.Quote("C:\\Folder With Spaces\\")=="\"C:\\Folder With Spaces\\\\\"","Windows path quoting");
        File.WriteAllText(path+".update-results.txt","PASS: "+count+" updater checks: payload/hash/version verification, unsafe packages, release metadata, version ordering, rollback, replacement, data preservation, path quoting.");
    }
}
}
