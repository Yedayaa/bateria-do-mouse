using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

class Installer {
    const string AppName="Bateria do mouse";
    const string UninstallKey="Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\BateriaDoMouse";
    static string TargetDirectory { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","Bateria do Mouse"); } }
    static void Extract(string name,string target) {
        using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream(name)) {
            if(input==null) throw new InvalidOperationException("Arquivo do instalador ausente.");
            using(var output=File.Create(target)) input.CopyTo(output);
        }
    }
    static void Install(bool startup) {
        string dir=TargetDirectory, exe=Path.Combine(dir,"BateriaMouse.exe");
        if(Directory.Exists(dir) && (File.GetAttributes(dir)&FileAttributes.ReparsePoint)!=0) throw new IOException("A pasta de instalação não pode ser um atalho de diretório.");
        Directory.CreateDirectory(dir);
        foreach(var process in Process.GetProcessesByName("BateriaMouse")) using(process) {
            try {
                if(String.Equals(process.MainModule.FileName,exe,StringComparison.OrdinalIgnoreCase)) { process.Kill(); process.WaitForExit(5000); }
            } catch(System.ComponentModel.Win32Exception) { throw new IOException("Feche o indicador instalado e tente novamente."); }
              catch(InvalidOperationException) { }
        }
        if(File.Exists(exe)) File.Copy(exe,exe+".previous",true);
        Extract("App.exe",exe);
        Extract("App.config",exe+".config");
        Extract("Uninstall.ps1",Path.Combine(dir,"Desinstalar.ps1"));
        Extract("README.md",Path.Combine(dir,"LEIA-ME.md"));
        string command="\""+exe+"\" --background";
        using(var key=Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run")) {
            if(startup) key.SetValue("K1NGProBattery",command,RegistryValueKind.String);
            else if(String.Equals(key.GetValue("K1NGProBattery") as string,command,StringComparison.OrdinalIgnoreCase)) key.DeleteValue("K1NGProBattery",false);
        }
        object shell=null, shortcut=null;
        try {
            Type type=Type.GetTypeFromProgID("WScript.Shell"); shell=Activator.CreateInstance(type);
            string shortcutPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),AppName+".lnk");
            shortcut=type.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{shortcutPath});
            Type st=shortcut.GetType();
            st.InvokeMember("TargetPath",BindingFlags.SetProperty,null,shortcut,new object[]{exe});
            st.InvokeMember("WorkingDirectory",BindingFlags.SetProperty,null,shortcut,new object[]{dir});
            st.InvokeMember("IconLocation",BindingFlags.SetProperty,null,shortcut,new object[]{exe+",0"});
            st.InvokeMember("Description",BindingFlags.SetProperty,null,shortcut,new object[]{"Bateria do mouse na área de notificações"});
            st.InvokeMember("Save",BindingFlags.InvokeMethod,null,shortcut,null);
        } finally {
            if(shortcut!=null) Marshal.FinalReleaseComObject(shortcut); if(shell!=null) Marshal.FinalReleaseComObject(shell);
        }
        string powershell=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell","v1.0","powershell.exe");
        using(var key=Registry.CurrentUser.CreateSubKey(UninstallKey)) {
            key.SetValue("DisplayName",AppName);
            key.SetValue("DisplayVersion","1.3.1");
            key.SetValue("Publisher","Projeto Bateria do Mouse");
            key.SetValue("DisplayIcon",exe+",0");
            key.SetValue("InstallLocation",dir);
            key.SetValue("UninstallString","\""+powershell+"\" -NoProfile -WindowStyle Hidden -File \""+Path.Combine(dir,"Desinstalar.ps1")+"\"");
            key.SetValue("NoModify",1,RegistryValueKind.DWord); key.SetValue("NoRepair",1,RegistryValueKind.DWord);
            key.SetValue("EstimatedSize",128,RegistryValueKind.DWord);
        }
    }
    [STAThread] static int Main(string[] args) {
        bool silent=Array.IndexOf(args,"--silent")>=0;
        if(silent) {
            try { Install(true); return 0; } catch { return 1; }
        }
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        using(var form=new Form()) {
            form.Text="Instalar Bateria do mouse"; form.ClientSize=new Size(454,286);
            form.Font=new Font("Segoe UI",10); form.FormBorderStyle=FormBorderStyle.FixedDialog;
            form.MaximizeBox=false; form.MinimizeBox=false; form.StartPosition=FormStartPosition.CenterScreen;
            form.Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            var heading=new Label { Text=AppName,Font=new Font("Segoe UI",20),AutoSize=true,Location=new Point(22,20) };
            var text=new Label { Text="Veja a bateria do seu K1NG Pro 1K perto do relógio.\nInstala somente para seu usuário, sem acesso de administrador.",Location=new Point(24,75),Size=new Size(410,47) };
            var location=new Label { Text="Pasta: "+TargetDirectory,AutoEllipsis=true,Location=new Point(24,132),Size=new Size(410,39),ForeColor=SystemColors.GrayText };
            var startup=new CheckBox { Text="Iniciar com o Windows",Checked=true,Location=new Point(24,183),Size=new Size(390,25) };
            var install=new Button { Text="Instalar",Location=new Point(324,233),Size=new Size(106,32) };
            var cancel=new Button { Text="Cancelar",Location=new Point(206,233),Size=new Size(106,32),DialogResult=DialogResult.Cancel };
            form.Controls.AddRange(new Control[]{heading,text,location,startup,install,cancel});
            form.AcceptButton=install; form.CancelButton=cancel;
            install.Click+=delegate {
                install.Enabled=false;
                try {
                    Install(startup.Checked);
                    Process.Start(new ProcessStartInfo(Path.Combine(TargetDirectory,"BateriaMouse.exe")) { UseShellExecute=true });
                    form.Close();
                } catch { install.Enabled=true; MessageBox.Show(form,"Não foi possível concluir a instalação. Feche uma versão anterior do indicador e tente novamente.",AppName,MessageBoxButtons.OK,MessageBoxIcon.Information); }
            };
            Application.Run(form);
        }
        return 0;
    }
}
