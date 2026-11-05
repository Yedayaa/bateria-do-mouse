using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
class MakeIcon {
    static GraphicsPath Round(float x,float y,float w,float h,float r) {
        var p=new GraphicsPath(); p.AddArc(x,y,r,r,180,90); p.AddArc(x+w-r,y,r,r,270,90);
        p.AddArc(x+w-r,y+h-r,r,r,0,90); p.AddArc(x,y+h-r,r,r,90,90); p.CloseFigure(); return p;
    }
    static void Main(string[] args) {
        int[] sizes={16,32,48,64,256}; var images=new byte[sizes.Length][];
        for(int i=0;i<sizes.Length;i++) using(var b=new Bitmap(sizes[i],sizes[i])) using(var g=Graphics.FromImage(b)) {
            g.Clear(Color.Transparent); g.SmoothingMode=SmoothingMode.AntiAlias; g.ScaleTransform(sizes[i]/32f,sizes[i]/32f);
            using(var bg=new LinearGradientBrush(new Point(0,0),new Point(32,32),Color.FromArgb(0,120,212),Color.FromArgb(0,78,165)))
            using(var shape=Round(1,1,30,30,8)) g.FillPath(bg,shape);
            using(var shape=Round(10,4,12,19,12)) g.FillPath(Brushes.White,shape);
            using(var pen=new Pen(Color.FromArgb(0,100,190),1.2f)) { g.DrawLine(pen,16,4,16,11); g.DrawLine(pen,10,11,22,11); }
            using(var shape=Round(7,22,18,8,3)) g.FillPath(Brushes.White,shape);
            using(var fill=new SolidBrush(Color.FromArgb(16,140,70))) g.FillRectangle(fill,9,24,11,4);
            g.FillRectangle(Brushes.White,25,24,1.5f,4);
            using(var stream=new MemoryStream()) { b.Save(stream,ImageFormat.Png); images[i]=stream.ToArray(); }
        }
        using(var output=new BinaryWriter(File.Create(args[0]))) {
            output.Write((ushort)0); output.Write((ushort)1); output.Write((ushort)sizes.Length);
            int offset=6+16*sizes.Length;
            for(int i=0;i<sizes.Length;i++) {
                output.Write((byte)(sizes[i]==256?0:sizes[i])); output.Write((byte)(sizes[i]==256?0:sizes[i]));
                output.Write((byte)0); output.Write((byte)0); output.Write((ushort)1); output.Write((ushort)32);
                output.Write(images[i].Length); output.Write(offset); offset+=images[i].Length;
            }
            foreach(var data in images) output.Write(data);
        }
    }
}
