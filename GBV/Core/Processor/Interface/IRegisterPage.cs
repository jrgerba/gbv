namespace GBV.Core.Processor;

public interface IRegisterPage
{
    public byte A { get; set; }
    public byte B { get; set; }
    public byte C { get; set; }
    public byte D { get; set; }
    public byte E { get; set; }
    public byte F { get; set; }
    public byte H { get; set; }
    public byte L { get; set; }
    public ushort SP { get; set; }
    public ushort PC { get; set; }
    public ushort AF { get; set; }
    public ushort BC { get; set; }
    public ushort DE { get; set; }
    public ushort HL { get; set; }
}