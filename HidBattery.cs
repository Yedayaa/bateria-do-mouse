using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MouseBattery {
public sealed class BatteryReading {
    public int Percent, Millivolts;
    public bool Charging;
    public DateTime Time = DateTime.Now;
}
public sealed class HidDevice {
    internal string Path;
    public ushort Product, UsagePage, Usage, InputLength, OutputLength, FeatureLength;
    public override string ToString() {
        return String.Format("3554:{0:X4} usage={1:X4}:{2:X4} in={3} out={4} feature={5}", Product, UsagePage, Usage, InputLength, OutputLength, FeatureLength);
    }
}
public static class HidBattery {
    [StructLayout(LayoutKind.Sequential)] struct InterfaceData { public int Size; public Guid ClassGuid; public int Flags; public IntPtr Reserved; }
    [StructLayout(LayoutKind.Sequential)] struct Attributes { public int Size; public ushort Vendor, Product, Version; }
    [StructLayout(LayoutKind.Sequential)] struct Caps {
        public ushort Usage, UsagePage, Input, Output, Feature;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=17)] public ushort[] Reserved;
        public ushort LinkNodes, InputButtons, InputValues, InputIndices, OutputButtons, OutputValues, OutputIndices, FeatureButtons, FeatureValues, FeatureIndices;
    }
    [StructLayout(LayoutKind.Sequential)] struct Overlapped { public IntPtr Internal, InternalHigh; public uint Offset, OffsetHigh; public IntPtr Event; }
    [DllImport("hid.dll")] static extern void HidD_GetHidGuid(out Guid guid);
    [DllImport("hid.dll")] static extern bool HidD_GetAttributes(SafeFileHandle handle, ref Attributes attributes);
    [DllImport("hid.dll")] static extern bool HidD_GetPreparsedData(SafeFileHandle handle, out IntPtr data);
    [DllImport("hid.dll")] static extern bool HidD_FreePreparsedData(IntPtr data);
    [DllImport("hid.dll")] static extern int HidP_GetCaps(IntPtr data, out Caps caps);
    [DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern IntPtr SetupDiGetClassDevs(ref Guid guid, string enumerator, IntPtr parent, uint flags);
    [DllImport("setupapi.dll", SetLastError=true)] static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr device, ref Guid guid, uint index, ref InterfaceData data);
    [DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref InterfaceData data, IntPtr detail, uint size, out uint required, IntPtr device);
    [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool ReadFile(SafeFileHandle handle, IntPtr buffer, int count, out uint done, IntPtr overlapped);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool WriteFile(SafeFileHandle handle, IntPtr buffer, int count, out uint done, IntPtr overlapped);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool GetOverlappedResult(SafeFileHandle handle, IntPtr overlapped, out uint done, bool wait);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool CancelIoEx(SafeFileHandle handle, IntPtr overlapped);
    [DllImport("kernel32.dll")] static extern IntPtr CreateEvent(IntPtr security, bool manual, bool initial, string name);
    [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);

    public static List<HidDevice> Enumerate(bool allCollections = false) {
        var devices = new List<HidDevice>(); Guid guid; HidD_GetHidGuid(out guid);
        IntPtr set = SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, 18);
        if (set == new IntPtr(-1)) return devices;
        try {
            for (uint i=0; ; i++) {
                var data = new InterfaceData(); data.Size=Marshal.SizeOf(typeof(InterfaceData));
                if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref guid, i, ref data)) break;
                uint size; SetupDiGetDeviceInterfaceDetail(set, ref data, IntPtr.Zero, 0, out size, IntPtr.Zero);
                if (size < 8 || size > 65536) continue;
                IntPtr detail = Marshal.AllocHGlobal((int)size);
                try {
                    Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                    if (!SetupDiGetDeviceInterfaceDetail(set, ref data, detail, size, out size, IntPtr.Zero)) continue;
                    string path = Marshal.PtrToStringUni(IntPtr.Add(detail,4));
                    string lower = path.ToLowerInvariant();
                    // Only the exact vendor and supported K1NG Pro 1K USB products.
                    if (!lower.Contains("vid_3554&pid_f55d") && !lower.Contains("vid_3554&pid_f5d5") && !lower.Contains("vid_3554&pid_f55e")) continue;
                    using (var handle = CreateFile(path,0,3,IntPtr.Zero,3,0,IntPtr.Zero)) {
                        if (handle.IsInvalid) continue;
                        var a = new Attributes(); a.Size=Marshal.SizeOf(typeof(Attributes));
                        if (!HidD_GetAttributes(handle, ref a) || a.Vendor != 0x3554) continue;
                        IntPtr preparsed;
                        if (!HidD_GetPreparsedData(handle, out preparsed)) continue;
                        try {
                            Caps caps; if (HidP_GetCaps(preparsed,out caps) != 0x110000) continue;
                            // Excludes all keyboard, mouse-motion and consumer input collections.
                            if (!allCollections && (caps.UsagePage != 0xff02 || caps.Usage != 2 || caps.Input != 17 || caps.Output != 17)) continue;
                            devices.Add(new HidDevice { Path=path, Product=a.Product, UsagePage=caps.UsagePage, Usage=caps.Usage, InputLength=caps.Input, OutputLength=caps.Output, FeatureLength=caps.Feature });
                        } finally { HidD_FreePreparsedData(preparsed); }
                    }
                } finally { Marshal.FreeHGlobal(detail); }
            }
        } finally { SetupDiDestroyDeviceInfoList(set); }
        return devices;
    }
    // Only a battery query. No driver heartbeat, wake, pairing or configuration commands.
    public static byte[] BuildPacket() {
        byte[] packet = new byte[17]; packet[0]=8; packet[1]=4;
        int sum=0; for(int i=0;i<16;i++) sum+=packet[i]; packet[16]=(byte)((0x55-sum)&255);
        return packet;
    }
    public static BatteryReading Parse(byte[] packet) {
        if (packet == null || packet.Length != 17 || packet[0]!=8 || packet[1]!=4 || packet[2]!=0 || packet[3]!=0 || packet[4]!=0 || (packet[5]!=2 && packet[5]!=4)) return null;
        int sum=0; foreach (byte value in packet) sum+=value;
        if ((sum&255)!=0x55 || packet[6]>100 || packet[7]>1) return null;
        int mv=(packet[8]<<8)|packet[9];
        if (mv<2500 || mv>4500) return null;
        return new BatteryReading { Percent=packet[6], Charging=packet[7]==1, Millivolts=mv };
    }
    static bool Io(SafeFileHandle handle, byte[] data, bool write, int timeout, out int count) {
        count=0; IntPtr evt=CreateEvent(IntPtr.Zero,true,false,null);
        if(evt==IntPtr.Zero) return false;
        IntPtr ov=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Overlapped)));
        IntPtr buffer=Marshal.AllocHGlobal(data.Length);
        try {
            Marshal.StructureToPtr(new Overlapped { Event=evt },ov,false);
            if(write) Marshal.Copy(data,0,buffer,data.Length);
            uint done;
            bool ok=write ? WriteFile(handle,buffer,data.Length,out done,ov) : ReadFile(handle,buffer,data.Length,out done,ov);
            if(!ok) {
                if(Marshal.GetLastWin32Error()!=997) return false;
                if(WaitForSingleObject(evt,(uint)timeout)!=0) {
                    CancelIoEx(handle,ov); GetOverlappedResult(handle,ov,out done,true); return false;
                }
                if(!GetOverlappedResult(handle,ov,out done,false)) return false;
            }
            count=(int)done;
            if(!write && count<=data.Length) Marshal.Copy(buffer,data,0,count);
            return count==data.Length;
        } finally { Marshal.FreeHGlobal(buffer); Marshal.FreeHGlobal(ov); CloseHandle(evt); }
    }
    static bool Send(SafeFileHandle handle, HidDevice device) {
        byte[] packet=BuildPacket(); int count;
        return device.OutputLength==17 && Io(handle,packet,true,500,out count);
    }
    static BatteryReading Receive(SafeFileHandle handle,HidDevice device,Action<string> diagnostic) {
        if(device.InputLength!=17) return null;
        Stopwatch clock=Stopwatch.StartNew();
        while(clock.ElapsedMilliseconds<1200) {
            byte[] data=new byte[17]; int count;
            if(!Io(handle,data,false,250,out count)) continue;
            if(diagnostic!=null && data[0]==8 && data[1]==4) diagnostic("Resposta de bateria: "+BitConverter.ToString(data));
            BatteryReading reading=Parse(data); if(reading!=null) return reading;
        }
        return null;
    }
    public static BatteryReading Query(Action<string> diagnostic) {
        foreach (HidDevice device in Enumerate()) {
            if(diagnostic!=null) diagnostic(device.ToString());
            using(var handle=CreateFile(device.Path,0xc0000000,3,IntPtr.Zero,3,0x40000000,IntPtr.Zero)) {
                if(handle.IsInvalid) { if(diagnostic!=null) diagnostic("Interface indisponivel, erro "+Marshal.GetLastWin32Error()); continue; }
                if(!Send(handle,device)) { if(diagnostic!=null) diagnostic("Consulta nao enviada"); continue; }
                BatteryReading reading=Receive(handle,device,diagnostic);
                if(reading!=null) return reading;
                if(diagnostic!=null) diagnostic("Sem resposta valida; mova o mouse e tente novamente.");
            }
        }
        return null;
    }
}
}
