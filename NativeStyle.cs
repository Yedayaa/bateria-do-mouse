using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MouseBattery {
public static class NativeStyle {
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr window,int message,IntPtr value,IntPtr data);
    public static void SetBatteryState(ProgressBar bar,int percent) {
        // Native Windows progress states: green, red and yellow.
        SendMessage(bar.Handle,0x410,new IntPtr(percent<=5 ? 2 : percent<=15 ? 3 : 1),IntPtr.Zero);
    }
    public static bool LightTheme(bool taskbar) {
        if(SystemInformation.HighContrast) return SystemColors.Window.GetBrightness()>0.5;
        try {
            using(var key=Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize"))
                return key==null || Convert.ToInt32(key.GetValue(taskbar ? "SystemUsesLightTheme" : "AppsUseLightTheme",1))!=0;
        } catch { return true; }
    }
    public static void StyleCaption(Form window,bool light) {
        try {
            int dark=light ? 0 : 1, corner=2;
            DwmSetWindowAttribute(window.Handle,20,ref dark,4);
            DwmSetWindowAttribute(window.Handle,33,ref corner,4);
        } catch(DllNotFoundException) { } catch(EntryPointNotFoundException) { }
    }
}
}
