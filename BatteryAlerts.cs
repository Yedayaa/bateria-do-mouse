namespace MouseBattery {
public enum BatteryAlert { None, Low, Critical, Full }
public sealed class BatteryAlerts {
    bool lowShown, criticalShown, fullShown;
    public BatteryAlert Update(int percent, bool charging) {
        if(percent<0 || percent>100) return BatteryAlert.None;
        if(charging) {
            lowShown=false; criticalShown=false;
            if(percent<=95) fullShown=false;
            if(percent==100 && !fullShown) { fullShown=true; return BatteryAlert.Full; }
            return BatteryAlert.None;
        }
        fullShown=false;
        // A small recovery margin prevents repeated alerts from fluctuating readings.
        if(percent>20) lowShown=false;
        if(percent>10) criticalShown=false;
        if(percent<=5 && !criticalShown) {
            criticalShown=true; lowShown=true;
            return BatteryAlert.Critical;
        }
        if(percent<=15 && !lowShown) { lowShown=true; return BatteryAlert.Low; }
        return BatteryAlert.None;
    }
}
}
