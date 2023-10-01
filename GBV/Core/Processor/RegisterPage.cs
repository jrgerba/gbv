using gbv;

namespace GBV.Core.Processor;

[Flags]
public enum CpuFlags : byte
{
    Z = 0b1000_0000,
    N = 0b0100_0000,
    H = 0b0010_0000,
    C = 0b0001_0000
}

public struct RegisterPage
{
    public byte A, B, C, D, E, F, H, L;

    public byte MaskedF => (byte)(F & 0xF0);

    public ushort SP, PC;
    
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