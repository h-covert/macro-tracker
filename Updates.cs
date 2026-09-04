using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;

namespace MacroTracker {
public sealed class AvailableUpdate {
    public string Version,Url,Hash;
    public long Size;
}
public sealed class ReleaseClient {
    public static AvailableUpdate Parse(string json) {
        var root=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(json);
        if(Convert.ToBoolean(root["draft"])||Convert.ToBoolean(root["prerelease"]))return null;
        string version=Convert.ToString(root["tag_name"]);UpdatePackage.VersionOf(version);
        var assets=(System.Collections.IEnumerable)root["assets"];
        foreach(Dictionary<string,object> asset in assets) {
            if(Convert.ToString(asset["name"])!=ReleaseInfo.Asset)continue;
            string url=Convert.ToString(asset["browser_download_url"]);Uri uri;
            if(!Uri.TryCreate(url,UriKind.Absolute,out uri)||uri.Scheme!="https"||uri.Host!="github.com"||
               !uri.AbsolutePath.StartsWith("/"+ReleaseInfo.Repository+"/releases/download/",StringComparison.Ordinal))
                throw new InvalidDataException("The release download didn't point to the configured GitHub repository.");
            object digest;string hash=asset.TryGetValue("digest",out digest)?Convert.ToString(digest):"";
            if(!hash.StartsWith("sha256:")||hash.Length!=71||hash.Substring(7).Any(c=>!Uri.IsHexDigit(c)))
                throw new InvalidDataException("GitHub hasn't supplied a SHA-256 digest for this release yet. Try again later.");
            long size=Convert.ToInt64(asset["size"]);if(size<=0||size>30000000)throw new InvalidDataException("Unexpected update package size.");
            return new AvailableUpdate{Version=version,Url=url,Hash=hash.Substring(7),Size=size};
        }
        throw new InvalidDataException("The latest release doesn't have its Windows update package yet.");
    }
    static HttpClient Client() {
        ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
        var client=new HttpClient{Timeout=TimeSpan.FromSeconds(45)};
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MacroTracker/"+ReleaseInfo.Version);
        return client;
    }
    public async Task<AvailableUpdate> Check() {
        using(var client=Client())using(var response=await client.GetAsync("https://api.github.com/repos/"+ReleaseInfo.Repository+"/releases/latest")) {
            if(response.StatusCode==HttpStatusCode.NotFound)return null;
            if(!response.IsSuccessStatusCode)throw new IOException("GitHub update check is temporarily unavailable. Try again later.");
            return Parse(await response.Content.ReadAsStringAsync());
        }
    }
    public async Task Download(AvailableUpdate release,string path,IProgress<int> progress) {
        using(var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(90)))
        using(var client=Client())using(var response=await client.GetAsync(release.Url,HttpCompletionOption.ResponseHeadersRead,deadline.Token)) {
            response.EnsureSuccessStatusCode();
            using(var input=await response.Content.ReadAsStreamAsync())using(var output=File.Create(path)) {
                byte[] buffer=new byte[65536];int read;long total=0;
                while((read=await input.ReadAsync(buffer,0,buffer.Length,deadline.Token))>0) {
                    total+=read;if(total>release.Size)throw new InvalidDataException("The update download was larger than expected.");
                    await output.WriteAsync(buffer,0,read);if(progress!=null)progress.Report((int)(total*100/release.Size));
                }
                if(total!=release.Size)throw new InvalidDataException("The download was incomplete. Please try again.");
            }
        }
        if(!string.Equals(UpdatePackage.Hash(path),release.Hash,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Update checksum did not match. Nothing was installed.");
    }
}
public sealed partial class MainWindow {
    StackPanel updateBar;TextBlock updateMessage;Button updateDownload;AvailableUpdate available;
    bool checkingUpdate,downloadingUpdate;
    void InstallUpdateBar(DockPanel area) {
        updateBar=Row();updateBar.Visibility=Visibility.Collapsed;
        updateMessage=Label("",13,Green);updateMessage.MaxWidth=440;updateBar.Children.Add(updateMessage);
        updateDownload=Button("Download update",delegate{} ,true);
        updateDownload.Click+=async delegate{await DownloadUpdate();};updateBar.Children.Add(updateDownload);
        DockPanel.SetDock(updateBar,Dock.Top);area.Children.Add(updateBar);
    }
    public async void CheckUpdatesOnOpen() {if(!test&&store.Get("check_updates","1")=="1")await CheckUpdates(false);}
    async Task CheckUpdates(bool manual) {
        if(checkingUpdate||downloadingUpdate)return;checkingUpdate=true;
        try {
            var release=await new ReleaseClient().Check();
            if(release!=null&&UpdatePackage.VersionOf(release.Version)>UpdatePackage.VersionOf(ReleaseInfo.Version)) {
                available=release;updateMessage.Text="Macro Tracker "+release.Version+" is available.";updateBar.Visibility=Visibility.Visible;
            } else if(manual)MessageBox.Show(this,"You're up to date. Installed version: "+ReleaseInfo.Version,"Macro Tracker updates");
        }catch(Exception) {if(manual)MessageBox.Show(this,"Couldn't check for updates. Check your connection and try again. Your app still works offline.","Macro Tracker updates");}
        finally{checkingUpdate=false;}
    }
    void UpdateSettingsCard() {
        var p=new StackPanel();p.Children.Add(Label("App updates",21,Ink));p.Children.Add(Label("Version "+ReleaseInfo.Version+" · Updates from "+ReleaseInfo.Repository,13,Muted));
        var enabled=new CheckBox{Content="Check for updates when Macro Tracker opens",IsChecked=store.Get("check_updates","1")=="1"};
        enabled.Click+=delegate{Safe(delegate{store.Set("check_updates",enabled.IsChecked==true?"1":"0");});};p.Children.Add(enabled);
        var check=Button("Check for updates",delegate{});check.Click+=async delegate{await CheckUpdates(true);};p.Children.Add(check);
        p.Children.Add(Label("When an update is available, choose Download update. The app checks the download, applies it, and restarts. Your diary and API key stay in their data folder.",13,Muted));
        content.Children.Add(Box(p));
    }
    async Task DownloadUpdate() {
        if(available==null||downloadingUpdate)return;
        if(foodDialog!=null){MessageBox.Show(this,"Finish or cancel your open food entry before updating.","Macro Tracker updates");return;}
        downloadingUpdate=true;updateDownload.IsEnabled=false;
        try {
            string stage=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MacroTracker","updates",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);
            string package=Path.Combine(stage,ReleaseInfo.Asset);var client=new ReleaseClient();
            await client.Download(available,package,new Progress<int>(value=>updateMessage.Text="Downloading update… "+value+"%"));
            updateMessage.Text="Verifying update…";
            string verified=Path.Combine(stage,"preflight");UpdatePackage.Extract(package,available.Hash,available.Version,verified);
            if(foodDialog!=null)throw new IOException("A food entry is open. Finish or cancel it and then retry the update.");
            // Keep the currently trusted updater outside the folder it will replace.
            string target=Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string helper=Path.Combine(stage,"MacroTracker.Updater.exe");File.Copy(Path.Combine(target,"MacroTracker.Updater.exe"),helper);
            store.Backup(store.Path+".before-update.bak");
            var process=Process.GetCurrentProcess();
            string args="--apply "+process.Id+" "+process.StartTime.ToUniversalTime().Ticks+" "+UpdatePackage.Quote(package)+" "+UpdatePackage.Quote(target)+" "+available.Hash+" "+available.Version+" "+UpdatePackage.Quote(store.Path);
            Process.Start(new ProcessStartInfo(helper,args){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=stage});
            Exit();
        }catch(Exception e){updateMessage.Text="Update wasn't installed.";MessageBox.Show(this,e.Message+"\nYour food history and settings are unchanged.","Macro Tracker update");}
        finally{downloadingUpdate=false;updateDownload.IsEnabled=true;}
    }
}
}
