using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
namespace VinsPDF {
internal static class Associations {
    const string ProgId="VinsPDF.Document";
    [DllImport("shell32.dll")] static extern void SHChangeNotify(uint eventId,uint flags,IntPtr item1,IntPtr item2);
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)] internal static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
    public static void Register() {
        string exe=Application.ExecutablePath,icon=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"VinsPDF-document.ico"),command="\""+exe+"\" \"%1\"";
        using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Classes\"+ProgId)){key.SetValue("","VinsPDF PDF document");using(var i=key.CreateSubKey("DefaultIcon"))i.SetValue("","\""+icon+"\",0");using(var c=key.CreateSubKey(@"shell\open\command"))c.SetValue("",command);}
        using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Classes\.pdf\OpenWithProgids"))key.SetValue(ProgId,"",RegistryValueKind.String);
        using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Classes\Applications\VinsPDF.exe")){key.SetValue("FriendlyAppName","VinsPDF");using(var s=key.CreateSubKey("SupportedTypes"))s.SetValue(".pdf","");using(var c=key.CreateSubKey(@"shell\open\command"))c.SetValue("",command);using(var i=key.CreateSubKey("DefaultIcon"))i.SetValue("","\""+exe+"\",0");}
        using(var key=Registry.CurrentUser.CreateSubKey(@"Software\VinsPDF\Capabilities")){key.SetValue("ApplicationName","VinsPDF");key.SetValue("ApplicationDescription","Lightweight PDF viewer and editor");key.SetValue("ApplicationIcon","\""+exe+"\",0");using(var f=key.CreateSubKey("FileAssociations"))f.SetValue(".pdf",ProgId);}
        using(var key=Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications"))key.SetValue("VinsPDF",@"Software\VinsPDF\Capabilities");
        // Windows owns UserChoice. Registration offers the app; it does not hijack defaults.
        SHChangeNotify(0x08000000,0,IntPtr.Zero,IntPtr.Zero);
        foreach(string path in new[]{exe,icon}){IntPtr item=Marshal.StringToHGlobalUni(path);try{SHChangeNotify(0x00002000,0x0005,item,IntPtr.Zero);}finally{Marshal.FreeHGlobal(item);}}
    }
    public static void Setup(Form owner){try{Register();Process.Start(new ProcessStartInfo("ms-settings:defaultapps?registeredAppUser=VinsPDF"){UseShellExecute=true});}catch(Exception ex){DarkMessage.Show(owner,ex.Message,"PDF association");}}
}
}
