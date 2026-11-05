using System;
using MouseBattery;
class Probe {
    static int Main() {
        foreach(var device in HidBattery.Enumerate(true)) Console.WriteLine("INTERFACE "+device);
        var reading=HidBattery.Query(Console.WriteLine);
        if(reading==null) { Console.WriteLine("SEM_LEITURA"); return 1; }
        Console.WriteLine("BATERIA="+reading.Percent+"%; CARREGANDO="+reading.Charging+"; TENSAO="+reading.Millivolts+"mV");
        return 0;
    }
}
