using System.ComponentModel;
using GBV.Core.Bus;

namespace GBV.Core.Processor;

[Flags]
public enum CpuFlags : byte
{
    None = 0,
    Z = 0b1000_0000,
    N = 0b0100_0000,
    H = 0b0010_0000,
    C = 0b0001_0000,
    
    ZN = Z | N,
    ZH = Z | H,
    ZC = Z | C,
    
    ZNH = ZN | H,
    ZNC = ZN | C,
    
    ZHC = ZH | C,
    
    ZNHC = ZNH | C,
    
    NH = N | H,
    NC = N | C,
    
    NHC = NH | C,
    
    HC = H | C
}

public enum Register
{
    A, B, C, D, E, F, H, L, AF, BC, DE, HL, HLI, HLD, SP, HLPtr
}

public struct RegisterPage
{
    private byte _f;
    public byte A, B, C, D, E, H, L;

    public byte F
    {
        get => (byte)(_f & 0xF0);
        set => _f = value;
    }

    public CpuFlags Flags => (CpuFlags)F;

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

    public void ApplyFlags(CpuFlags set, CpuFlags mask = CpuFlags.ZNHC)
    {
        F &= (byte)~mask;
        F |= (byte)(set & mask);
    }

    public int GetRegister(Register reg, IBus? bus = null) => reg switch
    {
        Register.A => A,
        Register.B => B,
        Register.C => C,
        Register.D => D,
        Register.E => E,
        Register.F => F,
        Register.H => H,
        Register.L => L,
        Register.AF => AF,
        Register.BC => BC,
        Register.DE => DE,
        Register.HL => HL,
        Register.HLI => HL++,
        Register.HLD => HL--,
        Register.SP => SP,
        Register.HLPtr => bus?.ReadByte(HL) ?? throw new NullReferenceException("No bus passed for HL pointer"),
        _ => throw new InvalidEnumArgumentException()
    };

    public void SetRegister(Register reg, int value, IBus bus = null)
    {
        switch (reg)
        {
            case Register.A:
                A = (byte)value;
                break;
            case Register.B:
                B = (byte)value;
                break;
            case Register.C:
                C = (byte)value;
                break;
            case Register.D:
                D = (byte)value;
                break;
            case Register.E:
                E = (byte)value;
                break;
            case Register.F:
                F = (byte)value;
                break;
            case Register.H:
                H = (byte)value;
                break;
            case Register.L:
                L = (byte)value;
                break;
            case Register.AF:
                AF = (ushort)value;
                break;
            case Register.BC:
                BC = (ushort)value;
                break;
            case Register.DE:
                DE = (ushort)value;
                break;
            case Register.HL:
                HL = (ushort)value;
                break;
            case Register.SP:
                SP = (ushort)value;
                break;
            case Register.HLPtr:
                bus.Write(HL, (byte)value);
                break;
            case Register.HLI:
            case Register.HLD:
            default:
                throw new InvalidEnumArgumentException();
        }
    }
}