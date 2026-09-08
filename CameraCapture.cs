using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Media;

namespace MacroTracker {
public static class CameraCapture {
    const int WS_CHILD=0x40000000,WS_VISIBLE=0x10000000;
    const uint WM_CAP_DRIVER_CONNECT=0x40A,WM_CAP_DRIVER_DISCONNECT=0x40B,WM_CAP_EDIT_COPY=0x41E,WM_CAP_SET_PREVIEW=0x432,WM_CAP_SET_PREVIEWRATE=0x434,WM_CAP_SET_SCALE=0x435,WM_CAP_GRAB_FRAME_NOSTOP=0x43D;
    [DllImport("avicap32.dll",CharSet=CharSet.Ansi)] static extern IntPtr capCreateCaptureWindow(string title,int style,int x,int y,int width,int height,IntPtr parent,int id);
    [DllImport("avicap32.dll",CharSet=CharSet.Ansi)] static extern bool capGetDriverDescription(int index,StringBuilder name,int nameSize,StringBuilder version,int versionSize);
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr window,uint message,IntPtr wParam,IntPtr lParam);
    [DllImport("user32.dll")] static extern bool MoveWindow(IntPtr window,int x,int y,int width,int height,bool repaint);
    [DllImport("user32.dll")] static extern bool DestroyWindow(IntPtr window);

    sealed class Device {public int Index;public string Name;public override string ToString(){return Name;}}
    static List<Device> Devices(){var devices=new List<Device>();for(int i=0;i<10;i++){var name=new StringBuilder(256);var version=new StringBuilder(256);if(capGetDriverDescription(i,name,name.Capacity,version,version.Capacity))devices.Add(new Device{Index=i,Name=name.Length==0?"Camera "+(i+1):name.ToString()});}return devices;}

    public static string Take(Window owner) {
        string result=null;var devices=Devices();
        var window=new Window{Title="Take a nutrition label photo",Owner=owner,Width=860,Height=680,MinWidth=640,MinHeight=500,Background=owner.Background,Foreground=owner.Foreground,Resources=owner.Resources,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var root=new Grid{Margin=new Thickness(22)};root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});root.RowDefinitions.Add(new RowDefinition());root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});window.Content=root;
        var top=new StackPanel();top.Children.Add(new TextBlock{Text="Position the full nutrition label inside the frame",FontSize=22,Foreground=owner.Foreground,Margin=new Thickness(0,0,0,8)});
        var chooser=new ComboBox{ItemsSource=devices,SelectedIndex=devices.Count==0?-1:0,MinWidth=260,HorizontalAlignment=HorizontalAlignment.Left,Padding=new Thickness(8),Margin=new Thickness(0,0,0,12)};top.Children.Add(chooser);root.Children.Add(top);
        var panel=new System.Windows.Forms.Panel{BackColor=System.Drawing.Color.Black};var host=new WindowsFormsHost{Child=panel,Margin=new Thickness(0,4,0,12)};Grid.SetRow(host,1);root.Children.Add(host);
        var bottom=new StackPanel();Grid.SetRow(bottom,2);root.Children.Add(bottom);var status=new TextBlock{Text=devices.Count==0?"No Windows camera was found. Check camera privacy settings, then try again.":"Starting camera…",Foreground=owner.Foreground,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,10)};bottom.Children.Add(status);
        var actions=new StackPanel{Orientation=Orientation.Horizontal};bottom.Children.Add(actions);var captureButton=new Button{Content="Take photo",IsDefault=true,IsEnabled=false};var settingsButton=new Button{Content="Camera privacy settings"};var cancelButton=new Button{Content="Cancel",IsCancel=true};actions.Children.Add(captureButton);actions.Children.Add(settingsButton);actions.Children.Add(cancelButton);
        IntPtr capture=IntPtr.Zero;Action disconnect=delegate{if(capture==IntPtr.Zero)return;SendMessage(capture,WM_CAP_DRIVER_DISCONNECT,IntPtr.Zero,IntPtr.Zero);DestroyWindow(capture);capture=IntPtr.Zero;captureButton.IsEnabled=false;};
        Action connect=delegate{disconnect();var device=chooser.SelectedItem as Device;if(device==null)return;capture=capCreateCaptureWindow("Macro Tracker camera",WS_CHILD|WS_VISIBLE,0,0,Math.Max(1,panel.ClientSize.Width),Math.Max(1,panel.ClientSize.Height),panel.Handle,0);if(capture==IntPtr.Zero||SendMessage(capture,WM_CAP_DRIVER_CONNECT,new IntPtr(device.Index),IntPtr.Zero)==IntPtr.Zero){disconnect();status.Text="Windows could not open this camera. Close other camera apps or check camera privacy settings.";return;}SendMessage(capture,WM_CAP_SET_SCALE,new IntPtr(1),IntPtr.Zero);SendMessage(capture,WM_CAP_SET_PREVIEWRATE,new IntPtr(33),IntPtr.Zero);SendMessage(capture,WM_CAP_SET_PREVIEW,new IntPtr(1),IntPtr.Zero);captureButton.IsEnabled=true;status.Text="Hold the label steady, make the text fill the frame, then choose Take photo.";};
        panel.Resize+=delegate{if(capture!=IntPtr.Zero)MoveWindow(capture,0,0,Math.Max(1,panel.ClientSize.Width),Math.Max(1,panel.ClientSize.Height),true);};chooser.SelectionChanged+=delegate{if(window.IsLoaded)connect();};
        captureButton.Click+=delegate{try{if(capture==IntPtr.Zero)throw new InvalidOperationException("The camera is not ready.");SendMessage(capture,WM_CAP_GRAB_FRAME_NOSTOP,IntPtr.Zero,IntPtr.Zero);SendMessage(capture,WM_CAP_EDIT_COPY,IntPtr.Zero,IntPtr.Zero);using(var image=System.Windows.Forms.Clipboard.GetImage()){if(image==null)throw new IOException("Windows did not return a camera image. Try again or choose an existing image.");result=Path.Combine(Path.GetTempPath(),"MacroTracker-camera-"+Guid.NewGuid().ToString("N")+".png");image.Save(result,System.Drawing.Imaging.ImageFormat.Png);}window.DialogResult=true;}catch(Exception e){status.Text=e.Message;}};
        settingsButton.Click+=delegate{try{Process.Start(new ProcessStartInfo("ms-settings:privacy-webcam"){UseShellExecute=true});}catch(Exception e){status.Text="Could not open camera privacy settings. "+e.Message;}};
        window.ContentRendered+=delegate{if(devices.Count>0)connect();};window.Closed+=delegate{disconnect();};window.ShowDialog();return result;
    }
}
}
