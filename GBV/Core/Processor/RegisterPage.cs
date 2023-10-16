namespace GBV.Core.Processor;

public enum Register8 { A, B, C, D, E, F, H, L }

public enum Register16 { AF, BC, DE, HL, SP, PC }

public class RegisterPage : IRegisterPage
{
    private byte _f;
    public byte A { get; set; }
    public byte B { get; set; }
    public byte C { get; set; }
    public byte D { get; set; }
    public byte E { get; set; }

    public byte F
    {
        get => _f;
        set => _f = (byte)(value & 0xF0);
    }
    public byte H { get; set; }
    public byte L { get; set; }

    public ushort SP { get; set; }
    
    public ushort PC { get; set; }
    
    public ushort AF
    {
        get => IntegerHelper.JoinBytes(A, F);
        set => (A, F) = IntegerHelper.SplitShort(value);
    }

    public ushort BC
    {
        get => IntegerHelper.JoinBytes(B, C);
        set => (B, C) = IntegerHelper.SplitShort(value);
    }

    public ushort DE
    {
        get => IntegerHelper.JoinBytes(D, E);
        set => (D, E) = IntegerHelper.SplitShort(value);
    }

    public ushort HL
    {
        get => IntegerHelper.JoinBytes(H, L);
        set => (H, L) = IntegerHelper.SplitShort(value);
    }

}