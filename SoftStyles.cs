using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
namespace MacroTracker {
public sealed partial class MainWindow {
 Border sidebarSurface;
 [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
 internal static void RoundWindow(Window window){var handle=new WindowInteropHelper(window).Handle;if(handle==IntPtr.Zero)return;int corner=2;DwmSetWindowAttribute(handle,33,ref corner,4);int dark=Ink.ToString()=="#FF14223B"?0:1;DwmSetWindowAttribute(handle,20,ref dark,4);}
 void InstallSoftStyles(){
 Resources["SoftSurface"]=Bg;Resources["SoftInk"]=Ink;Resources["SoftBorder"]=Brush(Ink.ToString()=="#FF14223B"?"#D8E1EF":"#495063");Resources["SoftAccent"]=Green;
 var text=new Style(typeof(TextBox),(Style)Resources[typeof(TextBox)]);
 text.Setters.Add(new Setter(Control.TemplateProperty,(ControlTemplate)XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='TextBox'><Border x:Name='Frame' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' CornerRadius='12' Background='{TemplateBinding Background}' BorderBrush='{DynamicResource SoftBorder}' BorderThickness='1' Padding='{TemplateBinding Padding}'><ScrollViewer x:Name='PART_ContentHost'/></Border><ControlTemplate.Triggers><Trigger Property='IsKeyboardFocusWithin' Value='True'><Setter TargetName='Frame' Property='BorderBrush' Value='{DynamicResource SoftAccent}'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.5'/></Trigger></ControlTemplate.Triggers></ControlTemplate>")));
 Resources[typeof(TextBox)]=text;
 var combo=(Style)XamlReader.Parse(@"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ComboBox'>
 <Setter Property='Foreground' Value='{DynamicResource SoftInk}'/><Setter Property='Background' Value='{DynamicResource SoftSurface}'/><Setter Property='MinHeight' Value='38'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ComboBox'><Grid>
 <ToggleButton Focusable='False' IsChecked='{Binding IsDropDownOpen, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}'>
 <ToggleButton.Template><ControlTemplate TargetType='ToggleButton'><Border x:Name='Frame' Background='{DynamicResource SoftSurface}' BorderBrush='{DynamicResource SoftBorder}' BorderThickness='1' CornerRadius='12'><Grid><TextBlock Text='⌄' Foreground='{DynamicResource SoftInk}' HorizontalAlignment='Right' VerticalAlignment='Center' Margin='0,0,14,0'/></Grid></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Frame' Property='BorderBrush' Value='{DynamicResource SoftAccent}'/></Trigger></ControlTemplate.Triggers></ControlTemplate></ToggleButton.Template>
 </ToggleButton>
 <ContentPresenter IsHitTestVisible='False' Margin='13,8,36,8' VerticalAlignment='Center' Content='{TemplateBinding SelectionBoxItem}' ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}'/>
 <Popup x:Name='PART_Popup' Placement='Bottom' AllowsTransparency='True' IsOpen='{TemplateBinding IsDropDownOpen}' Focusable='False' PopupAnimation='Fade'><Border MinWidth='{Binding ActualWidth, RelativeSource={RelativeSource TemplatedParent}}' MaxHeight='320' Background='{DynamicResource SoftSurface}' BorderBrush='{DynamicResource SoftBorder}' BorderThickness='1' CornerRadius='12' Padding='5' Margin='0,4,0,0'><ScrollViewer CanContentScroll='True'><StackPanel IsItemsHost='True' KeyboardNavigation.DirectionalNavigation='Contained'/></ScrollViewer></Border></Popup>
 </Grid><ControlTemplate.Triggers><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.5'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>");Resources[typeof(ComboBox)]=combo;
 var item=(Style)XamlReader.Parse(@"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ComboBoxItem'><Setter Property='Foreground' Value='{DynamicResource SoftInk}'/><Setter Property='Padding' Value='12,8'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ComboBoxItem'><Border x:Name='Highlight' CornerRadius='8' Padding='{TemplateBinding Padding}' Background='Transparent'><ContentPresenter/></Border><ControlTemplate.Triggers><Trigger Property='IsHighlighted' Value='True'><Setter TargetName='Highlight' Property='Background' Value='{DynamicResource SoftAccent}'/><Setter Property='Foreground' Value='{DynamicResource SoftSurface}'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>");Resources[typeof(ComboBoxItem)]=item;
 }
}
}
