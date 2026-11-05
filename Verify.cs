using System;
using MouseBattery;
class Verify {
    static void Check(bool condition,string message) { if(!condition) throw new Exception(message); }
    static void Checksum(byte[] p) { int sum=0; for(int i=0;i<16;i++) sum+=p[i]; p[16]=(byte)((0x55-sum)&255); }
    static int Main() {
        byte[] captured={0x08,0x04,0,0,0,0x02,0x50,0,0x0f,0xae,0,0,0,0,0,0,0x3a};
        var reading=HidBattery.Parse(captured);
        Check(reading!=null && reading.Percent==80 && !reading.Charging && reading.Millivolts==4014,"Captured 1K dongle response");
        byte[] p=(byte[])captured.Clone(); p[5]=4; p[7]=1; Checksum(p);
        Check(HidBattery.Parse(p)!=null && HidBattery.Parse(p).Charging,"Four-byte payload and charging");
        p=(byte[])captured.Clone(); p[6]^=1; Check(HidBattery.Parse(p)==null,"Reject corrupt checksum");
        p=(byte[])captured.Clone(); p[1]=0x0a; Checksum(p); Check(HidBattery.Parse(p)==null,"Reject unsolicited status");
        p=(byte[])captured.Clone(); p[6]=101; Checksum(p); Check(HidBattery.Parse(p)==null,"Reject impossible percent");
        p=(byte[])captured.Clone(); p[8]=0; p[9]=0; Checksum(p); Check(HidBattery.Parse(p)==null,"Reject placeholder voltage");
        p=(byte[])captured.Clone(); p[5]=1; Checksum(p); Check(HidBattery.Parse(p)==null,"Reject echo instead of battery");
        Check(HidBattery.Parse(new byte[16])==null && HidBattery.Parse(null)==null,"Reject truncated response");
        p=HidBattery.BuildPacket(); int packetSum=0; foreach(byte b in p) packetSum+=b;
        Check(p.Length==17 && p[0]==8 && p[1]==4 && (packetSum&255)==0x55,"Only battery-query command is constructed");
        Check(p[5]==0,"Empty battery-query payload");
        for(int i=2;i<16;i++) Check(p[i]==0,"No wake, configuration or other payload");
        var alerts=new BatteryAlerts();
        Check(alerts.Update(16,false)==BatteryAlert.None,"No warning above 15 percent");
        Check(alerts.Update(15,false)==BatteryAlert.Low,"Warn at 15 percent");
        Check(alerts.Update(15,false)==BatteryAlert.None && alerts.Update(14,false)==BatteryAlert.None,"Do not repeat low alert");
        Check(alerts.Update(5,false)==BatteryAlert.Critical,"Critical alert at 5 percent after low alert");
        Check(alerts.Update(5,false)==BatteryAlert.None && alerts.Update(4,false)==BatteryAlert.None,"Do not repeat critical alert");
        Check(alerts.Update(6,false)==BatteryAlert.None && alerts.Update(5,false)==BatteryAlert.None,"Do not repeat on small fluctuations");
        Check(alerts.Update(4,true)==BatteryAlert.None,"Do not warn while charging");
        Check(alerts.Update(5,false)==BatteryAlert.Critical,"Rearm after charging");
        alerts=new BatteryAlerts();
        Check(alerts.Update(3,false)==BatteryAlert.Critical && alerts.Update(3,false)==BatteryAlert.None,"Only one critical alert if launched below 5 percent");
        alerts=new BatteryAlerts();
        Check(alerts.Update(14,false)==BatteryAlert.Low && alerts.Update(4,false)==BatteryAlert.Critical,"Warn when firmware skips exact thresholds");
        Check(alerts.Update(30,false)==BatteryAlert.None && alerts.Update(15,false)==BatteryAlert.Low,"Rearm on battery recovery");
        Check(alerts.Update(-1,false)==BatteryAlert.None && alerts.Update(101,false)==BatteryAlert.None,"Ignore invalid alert inputs");
        alerts=new BatteryAlerts();
        Check(alerts.Update(99,true)==BatteryAlert.None,"No full alert before 100 percent");
        Check(alerts.Update(100,true)==BatteryAlert.Full,"Full alert at 100 percent while charging");
        Check(alerts.Update(100,true)==BatteryAlert.None,"Do not repeat full alert");
        Check(alerts.Update(99,true)==BatteryAlert.None && alerts.Update(100,true)==BatteryAlert.None,"No repeated full alert on charge fluctuation");
        Check(alerts.Update(100,false)==BatteryAlert.None && alerts.Update(100,true)==BatteryAlert.Full,"Rearm full alert on next cable connection");
        Check(alerts.Update(94,true)==BatteryAlert.None && alerts.Update(100,true)==BatteryAlert.Full,"Rearm full alert after real battery recovery");
        Check(new BatteryAlerts().Update(100,false)==BatteryAlert.None,"No full alert when not charging");
        Console.WriteLine("OK: hardware response, protocol validation, safe commands, 15/5/full-charge alerts, no repeats, charging and recovery.");
        return 0;
    }
}
