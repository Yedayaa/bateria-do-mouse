using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MouseBattery {
static class Program {
    [STAThread] static void Main(string[] args) {
        bool created;
        using(var single=new Mutex(true,"Local\\K1NGProBattery_90c6eb8a",out created)) {
            if(!created) {
                if(Array.IndexOf(args,"--background")<0) {
                    try { using(var signal=EventWaitHandle.OpenExisting("Local\\K1NGProBattery_Show_90c6eb8a")) signal.Set(); }
                    catch(WaitHandleCannotBeOpenedException) { }
                    catch(UnauthorizedAccessException) { }
                }
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using(var app=new BatteryTray(Array.IndexOf(args,"--background")<0)) Application.Run(app);
        }
    }
}
sealed class BatteryTray : ApplicationContext {
    const string RunKey="Software\\Microsoft\\Windows\\CurrentVersion\\Run", RunName="K1NGProBattery";
    readonly NotifyIcon tray=new NotifyIcon();
    readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
    readonly Form details=new Form();
    readonly Panel batteryCard=new Panel(), settingsCard=new Panel();
    readonly Label percent=new Label(), status=new Label(), timestamp=new Label();
    readonly Label title=new Label(), model=new Label(), batteryTitle=new Label(), alertsLabel=new Label(), intervalLabel=new Label();
    readonly ProgressBar bar=new ProgressBar();
    readonly Button refresh=new Button();
    readonly CheckBox autoStart=new CheckBox();
    readonly ToolStripMenuItem menuStatus=new ToolStripMenuItem("Consultando bateria…");
    readonly ToolStripMenuItem menuRefresh=new ToolStripMenuItem("Atualizar agora");
    readonly ToolStripMenuItem startup=new ToolStripMenuItem("Iniciar com o Windows");
    readonly ContextMenuStrip menu=new ContextMenuStrip();
    readonly EventWaitHandle showSignal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\K1NGProBattery_Show_90c6eb8a");
    readonly BatteryAlerts alerts=new BatteryAlerts();
    RegisteredWaitHandle signalWait;
    BatteryReading last;
    bool busy, fresh, syncStartup, light=true;
    volatile bool closing;
    Icon currentIcon, appIcon;
    Color foreground, secondary, border;
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);

    public BatteryTray(bool showDetails) {
        MakeDetails();
        IntPtr uiHandle=details.Handle;
        signalWait=ThreadPool.RegisterWaitForSingleObject(showSignal,delegate { Dispatch(ShowDetails); },null,Timeout.Infinite,false);
        menuStatus.Enabled=false;
        menu.Items.Add(menuStatus);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(menuRefresh);
        menuRefresh.Click+=delegate { Poll(); };
        menu.Items.Add("Abrir Bateria do mouse",null,delegate { ShowDetails(); });
        menu.Items.Add(startup);
        startup.Click+=delegate { SetStartup(!StartsWithWindows()); };
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Sair",null,delegate { Finish(); });
        menu.Opening+=delegate { SyncStartup(); };
        tray.ContextMenuStrip=menu;
        tray.MouseClick+=delegate(object sender,MouseEventArgs e) { if(e.Button==MouseButtons.Left) ShowDetails(); };
        SyncStartup();
        ApplyTheme();
        tray.Text="K1NG Pro 1K: consultando bateria";
        tray.Visible=true;
        SystemEvents.UserPreferenceChanged+=OnPreferenceChanged;
        timer.Interval=3600000;
        timer.Tick+=delegate { Poll(); };
        timer.Start();
        Poll();
        if(showDetails) ShowDetails();
    }
    void MakeDetails() {
        details.Text="Bateria do mouse";
        details.ClientSize=new Size(430,438);
        details.FormBorderStyle=FormBorderStyle.FixedSingle;
        details.MaximizeBox=false;
        details.StartPosition=FormStartPosition.CenterScreen;
        details.Font=new Font("Segoe UI",10);
        details.AutoScaleMode=AutoScaleMode.Dpi;
        try { appIcon=Icon.ExtractAssociatedIcon(Application.ExecutablePath); details.Icon=appIcon; } catch { }
        Put(details,title,"Bateria do mouse",24,20,382,34,21,FontStyle.Regular);
        Put(details,model,"Redragon K1NG Pro 1K",26,61,378,23,10,FontStyle.Regular);
        batteryCard.SetBounds(24,104,382,184);
        details.Controls.Add(batteryCard);
        batteryCard.Paint+=PaintCard;
        Put(batteryCard,batteryTitle,"Nível de bateria",18,14,344,23,10,FontStyle.Regular);
        Put(batteryCard,percent,"—",14,38,352,76,43,FontStyle.Regular);
        bar.SetBounds(20,119,342,8); bar.Maximum=100; batteryCard.Controls.Add(bar);
        Put(batteryCard,status,"Consultando pelo receptor USB…",18,141,344,23,10,FontStyle.Regular);
        Put(details,timestamp,"Aguardando a primeira leitura",26,300,378,22,9,FontStyle.Regular);
        settingsCard.SetBounds(24,332,382,47); details.Controls.Add(settingsCard);
        settingsCard.Paint+=PaintCard;
        autoStart.SetBounds(16,12,350,24); autoStart.Text="Iniciar com o Windows";
        autoStart.AccessibleName="Iniciar com o Windows"; autoStart.UseVisualStyleBackColor=false;
        autoStart.CheckedChanged+=delegate { if(!syncStartup) SetStartup(autoStart.Checked); };
        settingsCard.Controls.Add(autoStart);
        Put(details,alertsLabel,"Alertas: 15%, 5% e 100%",26,394,240,19,9,FontStyle.Regular);
        Put(details,intervalLabel,"Atualiza a cada 1 hora",26,414,210,19,8,FontStyle.Regular);
        refresh.SetBounds(278,393,128,32); refresh.Text="Atualizar agora";
        refresh.AccessibleName="Atualizar bateria agora";
        refresh.Click+=delegate { Poll(); }; details.Controls.Add(refresh);
        details.FormClosing+=delegate(object sender,FormClosingEventArgs e) {
            if(!closing && e.CloseReason==CloseReason.UserClosing) { e.Cancel=true; details.Hide(); }
        };
    }
    static void Put(Control parent,Label label,string text,int x,int y,int w,int h,float size,FontStyle style) {
        label.Text=text; label.AutoEllipsis=true; label.Font=new Font("Segoe UI",size,style);
        label.SetBounds(x,y,w,h); parent.Controls.Add(label);
    }
    void PaintCard(object sender,PaintEventArgs e) {
        var panel=(Panel)sender;
        using(var pen=new Pen(border)) e.Graphics.DrawRectangle(pen,0,0,panel.Width-1,panel.Height-1);
    }
    void OnPreferenceChanged(object sender,UserPreferenceChangedEventArgs e) { Dispatch(ApplyTheme); }
    void Dispatch(Action action) {
        if(closing || details.IsDisposed) return;
        try { details.BeginInvoke((Action)delegate { if(!closing) action(); }); }
        catch(InvalidOperationException) { }
    }
    void ApplyTheme() {
        light=NativeStyle.LightTheme(false);
        bool contrast=SystemInformation.HighContrast;
        foreground=contrast ? SystemColors.WindowText : light ? Color.FromArgb(26,26,26) : Color.FromArgb(244,244,244);
        secondary=contrast ? SystemColors.WindowText : light ? Color.FromArgb(93,93,93) : Color.FromArgb(190,190,190);
        border=contrast ? SystemColors.WindowText : light ? Color.FromArgb(225,225,225) : Color.FromArgb(62,62,62);
        details.BackColor=contrast ? SystemColors.Control : light ? Color.FromArgb(243,243,243) : Color.FromArgb(32,32,32);
        details.ForeColor=foreground;
        batteryCard.BackColor=settingsCard.BackColor=contrast ? SystemColors.Window : light ? Color.White : Color.FromArgb(43,43,43);
        title.ForeColor=batteryTitle.ForeColor=status.ForeColor=autoStart.ForeColor=foreground;
        model.ForeColor=timestamp.ForeColor=alertsLabel.ForeColor=intervalLabel.ForeColor=secondary;
        autoStart.BackColor=settingsCard.BackColor;
        refresh.BackColor=light ? SystemColors.Control : Color.FromArgb(53,53,53);
        refresh.ForeColor=foreground;
        refresh.FlatStyle=light || contrast ? FlatStyle.System : FlatStyle.Flat;
        refresh.FlatAppearance.BorderColor=border;
        menu.BackColor=light ? SystemColors.Menu : Color.FromArgb(43,43,43);
        menu.ForeColor=foreground; menu.RenderMode=ToolStripRenderMode.System;
        NativeStyle.StyleCaption(details,light || contrast);
        percent.ForeColor=fresh ? BatteryColor(last.Percent) : secondary;
        if(fresh) NativeStyle.SetBatteryState(bar,last.Percent);
        batteryCard.Invalidate(); settingsCard.Invalidate();
        SetIcon(fresh ? last : null);
    }
    void ShowDetails() { SyncStartup(); details.Show(); details.WindowState=FormWindowState.Normal; details.Activate(); Poll(); }
    void Poll() {
        if(busy || closing) return;
        busy=true; refresh.Enabled=false; menuRefresh.Enabled=false;
        Task.Run(delegate {
            BatteryReading reading=null;
            try { reading=HidBattery.Query(null); } catch { /* No personal/device paths are recorded. */ }
            Dispatch(delegate { Apply(reading); });
        });
    }
    void Apply(BatteryReading reading) {
        busy=false; refresh.Enabled=true; menuRefresh.Enabled=true;
        fresh=reading!=null;
        if(fresh) last=reading;
        SetIcon(reading);
        if(fresh) {
            tray.Text="K1NG Pro 1K: "+reading.Percent+"% — "+(reading.Charging ? (reading.Percent==100 ? "carregado" : "carregando") : "na bateria");
            menuStatus.Text="Bateria: "+reading.Percent+"%";
            percent.Text=reading.Percent+"%"; percent.ForeColor=BatteryColor(reading.Percent);
            bar.Value=reading.Percent;
            NativeStyle.SetBatteryState(bar,reading.Percent);
            status.Text=reading.Charging ? (reading.Percent==100 ? "Carregado · Pode desconectar o cabo" : "Carregando") : "Conectado · Receptor USB";
            timestamp.Text="Última atualização: "+reading.Time.ToString("HH:mm:ss");
            BatteryAlert alert=alerts.Update(reading.Percent,reading.Charging);
            if(alert==BatteryAlert.Full) {
                tray.ShowBalloonTip(10000,"Mouse carregado","Seu K1NG Pro 1K chegou a 100%. Você já pode desconectar o cabo.",ToolTipIcon.Info);
            } else if(alert!=BatteryAlert.None) {
                bool critical=alert==BatteryAlert.Critical;
                tray.ShowBalloonTip(10000,critical ? "Bateria do mouse crítica" : "Bateria do mouse baixa",
                    "Seu K1NG Pro 1K está com "+reading.Percent+"%. "+(critical ? "Conecte o cabo agora para carregar." : "Já pode colocar para carregar."),
                    critical ? ToolTipIcon.Error : ToolTipIcon.Warning);
            }
        } else {
            tray.Text="K1NG Pro 1K: sem leitura — mova o mouse";
            menuStatus.Text="Sem leitura · Mova o mouse e atualize";
            percent.Text="—"; percent.ForeColor=secondary; bar.Value=0;
            status.Text="Mova o mouse e atualize para consultar.";
            timestamp.Text=last==null ? "Confira a conexão do receptor USB." : "Última leitura: "+last.Percent+"% às "+last.Time.ToString("HH:mm:ss");
        }
    }
    Color BatteryColor(int value) {
        if(SystemInformation.HighContrast) return SystemColors.WindowText;
        return value<=5 ? (light ? Color.FromArgb(196,43,28) : Color.FromArgb(255,153,164)) :
            value<=15 ? (light ? Color.FromArgb(140,86,0) : Color.FromArgb(252,225,0)) : (light ? Color.FromArgb(15,123,15) : Color.FromArgb(108,203,95));
    }
    void SetIcon(BatteryReading reading) {
        using(var bitmap=new Bitmap(32,32))
        using(var g=Graphics.FromImage(bitmap)) {
            g.Clear(Color.Transparent); g.SmoothingMode=SmoothingMode.AntiAlias;
            Color color=NativeStyle.LightTheme(true) ? Color.FromArgb(30,30,30) : Color.White;
            if(SystemInformation.HighContrast) color=SystemColors.WindowText;
            else if(reading!=null) color=reading.Percent<=5 ? Color.FromArgb(245,92,92) : reading.Percent<=15 ? Color.FromArgb(235,170,35) : Color.FromArgb(56,180,83);
            string text=reading==null ? "--" : reading.Percent.ToString();
            using(var font=new Font("Segoe UI",text.Length==3 ? 16 : 22,FontStyle.Bold,GraphicsUnit.Pixel))
            using(var brush=new SolidBrush(color))
            using(var format=new StringFormat { Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center }) {
                g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
                g.DrawString(text,font,brush,new RectangleF(-1,-2,34,32),format);
            }
            if(reading!=null) using(var pen=new Pen(reading.Charging ? Color.FromArgb(32,180,90) : color,2))
                g.DrawLine(pen,5,28,5+22*reading.Percent/100f,28);
            IntPtr native=bitmap.GetHicon(); Icon next;
            try { using(var borrowed=Icon.FromHandle(native)) next=(Icon)borrowed.Clone(); }
            finally { DestroyIcon(native); }
            Icon old=currentIcon; currentIcon=next; tray.Icon=next;
            if(old!=null) old.Dispose();
        }
    }
    string StartupCommand() { return "\""+Application.ExecutablePath+"\" --background"; }
    bool StartsWithWindows() {
        try { using(var key=Registry.CurrentUser.OpenSubKey(RunKey)) return key!=null && String.Equals(key.GetValue(RunName) as string,StartupCommand(),StringComparison.OrdinalIgnoreCase); }
        catch { return false; }
    }
    void SyncStartup() {
        syncStartup=true;
        try { startup.Checked=autoStart.Checked=StartsWithWindows(); }
        finally { syncStartup=false; }
    }
    void SetStartup(bool enabled) {
        try {
            using(var key=Registry.CurrentUser.CreateSubKey(RunKey)) {
                if(enabled) key.SetValue(RunName,StartupCommand(),RegistryValueKind.String);
                else if(String.Equals(key.GetValue(RunName) as string,StartupCommand(),StringComparison.OrdinalIgnoreCase)) key.DeleteValue(RunName,false);
            }
        } catch { MessageBox.Show(details,"Não foi possível alterar a inicialização automática.","Bateria do mouse",MessageBoxButtons.OK,MessageBoxIcon.Information); }
        SyncStartup();
    }
    void Finish() { closing=true; timer.Stop(); tray.Visible=false; ExitThread(); }
    protected override void Dispose(bool disposing) {
        if(disposing) {
            closing=true; SystemEvents.UserPreferenceChanged-=OnPreferenceChanged;
            timer.Dispose(); tray.Visible=false;
            if(signalWait!=null) signalWait.Unregister(null); showSignal.Dispose();
            tray.Dispose(); menu.Dispose(); details.Dispose();
            if(currentIcon!=null) currentIcon.Dispose(); if(appIcon!=null) appIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}
}
