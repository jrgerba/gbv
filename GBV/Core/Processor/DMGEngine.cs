using System.ComponentModel;
using System.Diagnostics;
using GBV.Core.Bus;

namespace GBV.Core.Processor;

public class DMGEngine
{
    private delegate RegisterPage Operation(RegisterPage page, IBus bus);
    
    private enum AluMisc
    {
        And,
        Or, 
        Xor
    }

    private enum RotShiftType
    {
        RotateLeftCarry,
        RotateLeft,
        RotateRightCarry,
        RotateRight,
        ShiftLeftArithmetic,
        ShiftRightArithmetic,
        ShiftRightLogical
    }

    private enum BranchCondition
    {
        NZ = 0,
        Z = 1,
        NC = 2,
        C = 3
    }
    
    public int WorkTime { get; private set; }

    private InstructionInfo GetInfo(byte op) => default;

    private BinaryPatternMatcher<byte, Operation> GenerateBinaryMatcher()
    {
        Register R16(int reg, int group) => group switch
        {
            1 => reg switch
            {
                0 => Register.BC,
                1 => Register.DE,
                2 => Register.HL,
                3 => Register.SP,
                _ => throw new InvalidEnumArgumentException()
            },
            2 => reg switch
            {
                0 => Register.BC,
                1 => Register.DE,
                2 => Register.HLI,
                3 => Register.HLD,
                _ => throw new InvalidEnumArgumentException()
            },
            3 => reg switch
            {
                0 => Register.BC,
                1 => Register.DE,
                2 => Register.HL,
                3 => Register.AF,
                _ => throw new InvalidEnumArgumentException()

            },
            _ => throw new InvalidEnumArgumentException()
        };

        Register R8(int reg) => reg switch
        {
            0 => Register.B,
            1 => Register.C,
            2 => Register.D,
            3 => Register.E,
            4 => Register.H,
            5 => Register.L,
            6 => Register.HLPtr,
            7 => Register.A,
            _ => throw new InvalidEnumArgumentException()
        };

        BinaryPatternMatcher<byte, Operation> match = new(op => (page, _) =>
        {
            InvalidOperation();
            return page;
        });
        
        // nop
        match.AddMatch("0000_0000", op => (page, _) => page);

        // ld (u16), sp
        match.AddMatch("0000_1000", op => (page, bus) =>
        {
            bus.Write(ImmediateShort(ref page, bus), page.SP);
            return page;
        });
        
        // stop
        match.AddMatch("0001_0000", op => (page, bus) => throw new NotImplementedException());
        
        // jr i8
        match.AddMatch("0001_1000", op => (page, bus) =>
        {
            page.PC = (ushort)(page.PC + (sbyte)ImmediateByte(ref page, bus));
            return page;
        });
        
        // jr cc i8
        match.AddMatch("001*_*000", op =>
        {
            BranchCondition cc = (BranchCondition)((op >> 3) & 0b11);
            BranchInstructionInfo info = (BranchInstructionInfo)GetInfo(op);

            return (page, bus) =>
            {
                if (EvalBranch(page.Flags, cc))
                {
                    WorkTime += info.BranchTime;
                    page.PC += (ushort)(page.PC + (sbyte)ImmediateShort(ref page, bus));
                }

                return page;
            };
        });
        
        // ld r16, u16
        match.AddMatch("00**_0001", op =>
        {
            Register r = R16(((op >> 4) & 0b11), 1);

            return (page, bus) =>
            {
                page.SetRegister(r, ImmediateShort(ref page, bus));
                return page;
            };
        });
        
        // add hl, r16
        match.AddMatch("00**_1001", op =>
        {
            Register r = R16(((op >> 4) & 0b11), 1);
            InstructionInfo info = GetInfo(op);

            return (page, bus) =>
            {
                ushort param = (ushort)page.GetRegister(r);
                (CpuFlags f, page.HL) = Add(page.HL, param);

                page.ApplyFlags(f, info.FlagMask);
                return page;
            };
        });
        
        // ld (r16), a
        match.AddMatch("00**_0010", op =>
        {
            Register r = R16(((op >> 4) & 0b11), 2);

            return (page, bus) =>
            {
                ushort dest = (ushort)page.GetRegister(r, bus);
                bus.Write(dest, page.A);

                return page;
            };
        });
        
        // ld a, (r16)
        match.AddMatch("00**_1010", op =>
        {
            Register r = R16((op >> 4) & 0b11, 2);

            return (page, bus) =>
            {
                ushort src = (ushort)page.GetRegister(r, bus);
                bus.Write(page.A, bus.ReadByte(src));

                return page;
            };
        });
        
        // inc/dec r16
        match.AddMatch("00**_*011", op =>
        {
            bool inc = (op & 0b0000_1000) == 0;
            Register r = R16((op >> 4) & 0b11, 1);

            return inc
                ? (page, bus) =>
                {
                    ushort param = (ushort)page.GetRegister(r);
                    page.SetRegister(r, ++param);
                    return page;
                }
                : (page, bus) =>
                {
                    ushort param = (ushort)page.GetRegister(r);
                    page.SetRegister(r, --param);
                    return page;
                };
        });
        
        // inc/dec r8
        match.AddMatch("00**_*10*", op =>
        {
            bool inc = (op & 0b0000_0001) == 0;
            Register r = R8((op >> 3) & 0b111);
            InstructionInfo info = GetInfo(op);

            return inc
                ? (page, bus) =>
                {
                    byte param = (byte)page.GetRegister(r, bus);
                    (CpuFlags f, param) = Inc(param);
                    page.ApplyFlags(f, info.FlagMask);
                    return page;
                }
                : (page, bus) =>
                {
                    byte param = (byte)page.GetRegister(r, bus);
                    (CpuFlags f, param) = Dec(param);
                    page.ApplyFlags(f, info.FlagMask);
                    return page;
                };
        });
        
        // halt
        match.AddMatch("0111_0110", op => (page, bus) => throw new NotImplementedException());

        return match;
    }
    
    private byte ImmediateByte(ref RegisterPage reg, IBus bus) => 0;
    private ushort ImmediateShort(ref RegisterPage reg, IBus bus) => 0;
    
    private (CpuFlags flags, byte value) Add(byte v0, byte v1, int carry = 0)
    {
        int temp = v0 + v1 + carry;

        CpuFlags z = (temp & 0xFF) == 0 ? CpuFlags.Z : CpuFlags.None;
        CpuFlags n = CpuFlags.None;
        CpuFlags h = (((v0 & 0xF) + (v1 & 0xF) + carry) & 0x10) == 0x10 ? CpuFlags.H : CpuFlags.None;
        CpuFlags c = (temp & 0x100) == 0x100 ? CpuFlags.C : CpuFlags.None;
        
        return (z | n | h | c , (byte)temp);
    }

    private (CpuFlags flags, byte value) Sub(byte v0, byte v1, int carry = 0)
    {
        int temp = v0 - v1 - carry;

        CpuFlags z = (temp & 0xFF) == 0 ? CpuFlags.Z : CpuFlags.None;
        CpuFlags h = (((v0 & 0xF) - (v1 & 0xF) - carry) & 0x10) == 0x10 ? CpuFlags.H : CpuFlags.None;
        CpuFlags c = (temp & 0x100) == 0x100 ? CpuFlags.C : CpuFlags.None;

        return (z | h | c, (byte)temp);
    }

    private (CpuFlags, byte) AluBit(AluMisc op, byte v0, byte v1)
    {
        CpuFlags flags = op == AluMisc.And ? CpuFlags.H : CpuFlags.None;

        byte temp = op switch
        {
            AluMisc.And => (byte)(v0 & v1),
            AluMisc.Or => (byte)(v0 | v1),
            AluMisc.Xor => (byte)(v0 ^ v1),
            _ => throw new InvalidEnumArgumentException()
        };
        
        return (flags, temp);
    }

    private (CpuFlags, byte) Inc(byte v)
    {
        CpuFlags z = ++v == 0 ? CpuFlags.Z : CpuFlags.None;
        CpuFlags h = (v & 0xF) == 0 ? CpuFlags.H : CpuFlags.None;

        return (z | h, v);
    }
    
    
    private (CpuFlags, byte) Dec(byte v)
    {
        CpuFlags z = --v == 0 ? CpuFlags.Z : CpuFlags.None;
        CpuFlags h = (v & 0xF) == 0xF ? CpuFlags.H : CpuFlags.None;

        return (z | CpuFlags.N | h, v);
    }

    private (CpuFlags, byte) Daa(byte v, CpuFlags f)
    {
        throw new NotImplementedException();
    }

    private (CpuFlags, byte) Cpl(byte v) => (CpuFlags.N | CpuFlags.H, (byte)~v);

    private (CpuFlags, ushort) Add(ushort v0, ushort v1)
    {
        int temp = v0 + v1;

        CpuFlags h = ((v0 & 0x0FFF) + (v1 & 0x0FFF) & 0x1000) == 0x1000 ? CpuFlags.H : CpuFlags.None;
        CpuFlags c = (temp & 0x10000) == 0x10000 ? CpuFlags.C : CpuFlags.None;

        return (h | c, (byte)temp);
    }

    private (CpuFlags, ushort) Add(ushort v0, sbyte v1)
    {
        CpuFlags h = ((v0 & 0xF) + (v1 & 0xF) & 0x10) == 0x10 ? CpuFlags.H : CpuFlags.None;
        CpuFlags c = ((v0 & 0xFF) + (v1 & 0xFF) & 0x100) == 0x100 ? CpuFlags.C : CpuFlags.None;

        return (h | c, (ushort)(v0 + v1));
    }

    private (CpuFlags, byte) RotShift(byte v, int carryIn, RotShiftType type, bool allowZ = true)
    {
        (byte val, bool carryOut) = type switch
        {
            RotShiftType.RotateLeft => ((byte)((v << 1) | carryIn), (v & 0x80) == 0x80),
            RotShiftType.RotateLeftCarry => ((byte)((v << 1) | (v >> 7)), (v & 0x80) == 0x80),
            RotShiftType.RotateRight => ((byte)((v >> 1) | (carryIn << 7)), (v & 1) == 1),
            RotShiftType.RotateRightCarry => ((byte)((v >> 1) | (v << 7)), (v & 1) == 1),
            RotShiftType.ShiftLeftArithmetic => ((byte)(v << 1), (v & 0x80) == 0x80),
            RotShiftType.ShiftRightArithmetic => ((byte)((v >> 1) | (v & 0x80)), (v & 1) == 1),
            RotShiftType.ShiftRightLogical => ((byte)(v >> 1), (v & 1) == 1),
            _ => throw new InvalidEnumArgumentException()
        };

        return ((allowZ && val == 0 ? CpuFlags.Z : CpuFlags.None) | (carryOut ? CpuFlags.C : CpuFlags.None), val);
    }

    private (CpuFlags, byte) Swap(byte v)
    {
        v = (byte)((v << 4) | (v >> 4));
        return (v == 0 ? CpuFlags.Z : CpuFlags.None, v);
    }

    private bool Bit(byte v, int bit) => (v & (1 << bit)) == 0;

    private byte BitSet(byte v, int bit, bool set) => v = (byte)(set ? (v | (1 << bit)) : (v & ~(1 << bit)));

    private void InvalidOperation()
    {
        throw new NotImplementedException();
    }
    
    private bool EvalBranch(CpuFlags flag, BranchCondition cond) => cond switch
    {
        BranchCondition.Z => flag.HasFlag(CpuFlags.Z),
        BranchCondition.NZ => !flag.HasFlag(CpuFlags.Z),
        BranchCondition.C => flag.HasFlag(CpuFlags.C),
        BranchCondition.NC => !flag.HasFlag(CpuFlags.C),
        _ => throw new InvalidEnumArgumentException()
    };
}