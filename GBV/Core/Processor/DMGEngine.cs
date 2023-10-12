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
    
    private int _workTime;

    private InstructionInfo[] _iInfo;
    private Operation[] _operations = new Operation[0x100];

    public int ExecuteInstruction(ref RegisterPage page, IBus bus, byte operation)
    {
        page = _operations[operation](page, bus);
        int time = GetInfo(operation).BaseTime + _workTime;

        return time;
    }
    
    private InstructionInfo GetInfo(byte op) => _iInfo[op];
    private BinaryPatternMatcher<byte, Operation> GenerateBinaryMatcher()
    {
        BinaryPatternMatcher<byte, Operation> match = new(IllegalOpCase);
        
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
                    _workTime += info.BranchTime;
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
        
        // ld r8, r8
        match.AddMatch("01**_****", op =>
        {
            Register dest = R8((op >> 3) & 0b111);
            Register src = R8(op & 0b111);

            return (page, bus) =>
            {
                page.SetRegister(dest, page.GetRegister(src));
                return page;
            };
        });
        
        // ALU_OP r8
        match.AddMatch("10**_****", op =>
        {
            Register r = R8(op & 0b111);
            InstructionInfo info = GetInfo(op);

            return ((op >> 3) & 0b111) switch
            {
                // add r8
                0 => (page, bus) =>
                {
                    (CpuFlags f, page.A) = Add(page.A, (byte)page.GetRegister(r, bus));
                    page.ApplyFlags(f, info.FlagMask);
                    return page;
                },
                // adc r8
                1 => (page, bus) =>
                {
                    (CpuFlags f, page.A) = Add(page.A, (byte)page.GetRegister(r, bus),
                        page.Flags.HasFlag(CpuFlags.C) ? 1 : 0);
                    page.ApplyFlags(f, info.FlagMask);
                    return page;
                },
                // sub r8
                2 => (page, bus) =>
                {
                    (CpuFlags f, page.A) = Sub(page.A, (byte)page.GetRegister(r, bus));
                    page.ApplyFlags(f, info.FlagMask);
                    return page;
                },
                // sbc r8
                3 => (page, bus) =>
                {
                    (CpuFlags f, page.A) = Sub(page.A, (byte)page.GetRegister(r, bus),
                        page.Flags.HasFlag(CpuFlags.C) ? 1 : 0);
                    page.ApplyFlags(f, info.FlagMask);
                    return page;
                },
                // and r8
                4 => (page, bus) =>
                {
                    (CpuFlags f, page.A) = AluBit(AluMisc.And, page.A, (byte)page.GetRegister(r, bus));
                    page.ApplyFlags(f, info.FlagMask);
                    return page;
                },
                // xor r8
                5 => (page, bus) =>
                {
                    (CpuFlags f, page.A) = AluBit(AluMisc.Xor, page.A, (byte)page.GetRegister(r, bus));
                    page.ApplyFlags(f, info.FlagMask);
                    return page;
                },
                // or r8
                6 => (page, bus) =>
                {
                    (CpuFlags f, page.A) = AluBit(AluMisc.Or, page.A, (byte)page.GetRegister(r, bus));
                    page.ApplyFlags(f, info.FlagMask);
                    return page;
                },
                // cp r8
                7 => (page, bus) =>
                {
                    (CpuFlags f, _) = Sub(page.A, (byte)page.GetRegister(r, bus));
                    page.ApplyFlags(f, info.FlagMask);
                    return page;
                },
                
                _ => throw new UnreachableException()
            };
        });

        // ret cc
        match.AddMatch("110*_*000", op =>
        {
            BranchCondition cc = (BranchCondition)((op >> 3) & 0b11);
            BranchInstructionInfo info = (BranchInstructionInfo)GetInfo(op);

            return (page, bus) =>
            {
                if (EvalBranch(page.Flags, cc))
                {
                    _workTime += info.BranchTime;
                    page.PC = Pop(ref page, bus);
                }

                return page;
            };
        });
        
        // ld (FF00+u8), a
        match.AddMatch("1110_0000", op => (page, bus) =>
        {
            bus.Write((ushort)(0xFF00 + ImmediateByte(ref page, bus)), page.A);
            return page;
        });
        
        // add sp, i8
        match.AddMatch("1110_1000", op => (page, bus) =>
        {
            (CpuFlags f, page.SP) = Add(page.SP, (sbyte)ImmediateByte(ref page, bus));
            page.ApplyFlags(f, GetInfo(op).FlagMask);
            return page;
        });
        
        // ld a, (FF00+u8)
        match.AddMatch("1111_0000", op => (page, bus) =>
        {
            page.A = bus.ReadByte((ushort)(0xFF00 + ImmediateByte(ref page, bus)));
            return page;
        });
        
        // ld hl, SP+i8
        match.AddMatch("1111_1000", op => (page, bus) =>
        {
            (CpuFlags f, page.HL) = Add(page.SP, (sbyte)ImmediateByte(ref page, bus));
            page.ApplyFlags(f, GetInfo(op).FlagMask);
            return page;
        });
        
        // pop r16
        match.AddMatch("11**_0001", op =>
        {
            Register param = R16(((op >> 4) & 0b11), 3);

            return (page, bus) =>
            {
                page.SetRegister(param, Pop(ref page, bus));
                return page;
            };
        });
        
        // ret
        match.AddMatch("1100_1001", op => (page, bus) =>
        {
            page.PC = Pop(ref page, bus);
            return page;
        });
        
        // reti
        match.AddMatch("1101_1001", op => (page, bus) => throw new NotImplementedException());
        
        // jp hl
        match.AddMatch("1110_1001", op => (page, bus) =>
        {
            page.PC = page.HL;
            return page;
        });
        
        // ld sp, hl
        match.AddMatch("1111_1001", op => (page, bus) =>
        {
            page.SP = page.HL;
            return page;
        });
        
        // jp cc
        match.AddMatch("110*_*010", op =>
        {
            BranchCondition cc = (BranchCondition)((op >> 3) & 0b11);
            BranchInstructionInfo info = (BranchInstructionInfo)GetInfo(op);

            return (page, bus) =>
            {
                if (EvalBranch(page.Flags, cc))
                {
                    page.PC = ImmediateShort(ref page, bus);
                    _workTime += info.BranchTime;
                }

                return page;
            };
        });
        
        // ld (FF00+C), a
        match.AddMatch("1110_0010", op => (page, bus) =>
        {
            bus.Write((ushort)(0xFF00 + page.C), page.A);
            return page;
        });
        
        // ld (u16), a
        match.AddMatch("1110_1010", op => (page, bus) =>
        {
            bus.Write(ImmediateShort(ref page, bus), page.A);
            return page;
        });
        
        // ld a, (FF00+C)
        match.AddMatch("1111_0010", op => (page, bus) =>
        {
            page.A = bus.ReadByte((ushort)(0xFF00 + page.C));
            return page;
        });
        
        // ld a, (u16)
        match.AddMatch("1111_1010", op => (page, bus) =>
        {
            page.A = bus.ReadByte(ImmediateByte(ref page, bus));
            return page;
        });
        
        // illegals
        match.AddMatch("11**_*011", IllegalOpCase);
        
        // jp u16
        match.AddMatch("1100_0011", op => (page, bus) =>
        {
            page.PC = ImmediateShort(ref page, bus);
            return page;
        });
        
        // (cb prefix)
        match.AddMatch("1100_1011", op => throw new NotImplementedException());
        
        // di
        match.AddMatch("1111_0011", op => throw new NotImplementedException());
        
        // ei
        match.AddMatch("1111_1011", op => throw new NotImplementedException());
        
        // call cc 
        match.AddMatch("110*_*100", op =>
        {
            BranchCondition cc = (BranchCondition)((op >> 3) & 0b11);
            BranchInstructionInfo info = (BranchInstructionInfo)GetInfo(op);

            return (page, bus) =>
            {
                ushort dest = ImmediateShort(ref page, bus);

                if (EvalBranch(page.Flags, cc))
                {
                    _workTime += info.BranchTime;
                    Push(ref page, bus, page.PC);
                    page.PC = dest;
                }

                return page;
            };
        });
        
        // push r16
        match.AddMatch("11**_0101", op =>
        {
            Register param = R16((op >> 4) & 0b11, 3);

            return (page, bus) =>
            {
                Push(ref page, bus, (ushort)page.GetRegister(param));
                return page;
            };
        });
        
        // call
        match.AddMatch("1100_1101", op => (page, bus) =>
        {
            ushort dest = ImmediateShort(ref page, bus);
            Push(ref page, bus, page.PC);
            page.PC = dest;
            return page;
        });
        
        // ALU_OP u8
        match.AddMatch("11**_*110", op =>
        {
            InstructionInfo info = GetInfo(op);

            return ((op >> 3) & 0b111) switch
            {
                // add u8
                0 => (page, bus) =>
                {
                    (CpuFlags f, page.A) = Add(page.A, ImmediateByte(ref page, bus));
                    page.ApplyFlags(f, info.FlagMask);
                    return page;
                },
                // adc u8
                1 => (page, bus) =>
                {
                    (CpuFlags f, page.A) = Add(page.A, ImmediateByte(ref page, bus),
                        page.Flags.HasFlag(CpuFlags.C) ? 1 : 0);
                    page.ApplyFlags(f, info.FlagMask);
                    return page;
                },
                // sub u8
                2 => (page, bus) =>
                {
                    (CpuFlags f, page.A) = Sub(page.A, ImmediateByte(ref page, bus));
                    page.ApplyFlags(f, info.FlagMask);
                    return page;
                },
                // sbc u8
                3 => (page, bus) =>
                {
                    (CpuFlags f, page.A) = Sub(page.A, ImmediateByte(ref page, bus),
                        page.Flags.HasFlag(CpuFlags.C) ? 1 : 0);
                    page.ApplyFlags(f, info.FlagMask);
                    return page;
                },
                // and u8
                4 => (page, bus) =>
                {
                    (CpuFlags f, page.A) = AluBit(AluMisc.And, page.A, ImmediateByte(ref page, bus));
                    page.ApplyFlags(f, info.FlagMask);
                    return page;
                },
                // xor u8
                5 => (page, bus) =>
                {
                    (CpuFlags f, page.A) = AluBit(AluMisc.Xor, page.A, ImmediateByte(ref page, bus));
                    page.ApplyFlags(f, info.FlagMask);
                    return page;
                },
                // or u8
                6 => (page, bus) =>
                {
                    (CpuFlags f, page.A) = AluBit(AluMisc.Or, page.A, ImmediateByte(ref page, bus));
                    page.ApplyFlags(f, info.FlagMask);
                    return page;
                },
                // cp u8
                7 => (page, bus) =>
                {
                    (CpuFlags f, _) = Sub(page.A, ImmediateByte(ref page, bus));
                    page.ApplyFlags(f, info.FlagMask);
                    return page;
                },
                
                _ => throw new UnreachableException()
            };
        });
        
        // rst vec
        match.AddMatch("11**_*111", op =>
        {
            ushort vec = (ushort)(((op >> 3) & 0b111) * 0x8);

            return (page, bus) =>
            {
                Push(ref page, bus, page.PC);
                page.PC = vec;
                return page;
            };
        });
        
        return match;

        Operation IllegalOpCase(byte op) =>
            (page, _) =>
            {
                InvalidOperation();
                return page;
            };

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
    }
    private InstructionInfo[] GenerateInfoTable() => new InstructionInfo[]
    {
        new(0x00, "NOP", CpuFlags.None, 4),
        new(0x01, "LD BC,u16", CpuFlags.None, 12),
        new(0x02, "LD (BC),A", CpuFlags.None, 8),
        new(0x03, "INC BC", CpuFlags.None, 8),
        new(0x04, "INC B", CpuFlags.ZNH, 4),
        new(0x05, "DEC B", CpuFlags.ZNH, 4),
        new(0x06, "LD B,u8", CpuFlags.None, 8),
        new(0x07, "RLCA", CpuFlags.ZNHC, 4),
        new(0x08, "LD (u16),SP", CpuFlags.None, 20),
        new(0x09, "ADD HL,BC", CpuFlags.NHC, 8),
        new(0x0A, "LD A,(BC)", CpuFlags.None, 8),
        new(0x0B, "DEC BC", CpuFlags.None, 8),
        new(0x0C, "INC C", CpuFlags.ZNH, 4),
        new(0x0D, "DEC C", CpuFlags.ZNH, 4),
        new(0x0E, "LD C,u8", CpuFlags.None, 8),
        new(0x0F, "RRCA", CpuFlags.ZNHC, 4),
        new(0x10, "STOP", CpuFlags.None, 4),
        new(0x11, "LD DE,u16", CpuFlags.None, 12),
        new(0x12, "LD (DE),A", CpuFlags.None, 8),
        new(0x13, "INC DE", CpuFlags.None, 8),
        new(0x14, "INC D", CpuFlags.ZNH, 4),
        new(0x15, "DEC D", CpuFlags.ZNH, 4),
        new(0x16, "LD D,u8", CpuFlags.None, 8),
        new(0x17, "RLA", CpuFlags.ZNHC, 4),
        new BranchInstructionInfo(0x18, "JR i8", CpuFlags.None, 12, 0),
        new(0x19, "ADD HL,DE", CpuFlags.NHC, 8),
        new(0x1A, "LD A,(DE)", CpuFlags.None, 8),
        new(0x1B, "DEC DE", CpuFlags.None, 8),
        new(0x1C, "INC E", CpuFlags.ZNH, 4),
        new(0x1D, "DEC E", CpuFlags.ZNH, 4),
        new(0x1E, "LD E,u8", CpuFlags.None, 8),
        new(0x1F, "RRA", CpuFlags.ZNHC, 4),
        new BranchInstructionInfo(0x20, "JR NZ,i8", CpuFlags.None, 8, 4),
        new(0x21, "LD HL,u16", CpuFlags.None, 12),
        new(0x22, "LD (HL+),A", CpuFlags.None, 8),
        new(0x23, "INC HL", CpuFlags.None, 8),
        new(0x24, "INC H", CpuFlags.ZNH, 4),
        new(0x25, "DEC H", CpuFlags.ZNH, 4),
        new(0x26, "LD H,u8", CpuFlags.None, 8),
        new(0x27, "DAA", CpuFlags.ZHC, 4),
        new BranchInstructionInfo(0x28, "JR Z,i8", CpuFlags.None, 8, 4),
        new(0x29, "ADD HL,HL", CpuFlags.NHC, 8),
        new(0x2A, "LD A,(HL+)", CpuFlags.None, 8),
        new(0x2B, "DEC HL", CpuFlags.None, 8),
        new(0x2C, "INC L", CpuFlags.ZNH, 4),
        new(0x2D, "DEC L", CpuFlags.ZNH, 4),
        new(0x2E, "LD L,u8", CpuFlags.None, 8),
        new(0x2F, "CPL", CpuFlags.NH, 4),
        new(0x30, "JR NC,i8", CpuFlags.None, 8),
        new(0x31, "LD SP,u16", CpuFlags.None, 12),
        new(0x32, "LD (HL-),A", CpuFlags.None, 8),
        new(0x33, "INC SP", CpuFlags.None, 8),
        new(0x34, "INC (HL)", CpuFlags.ZNH, 12),
        new(0x35, "DEC (HL)", CpuFlags.ZNH, 12),
        new(0x36, "LD (HL),u8", CpuFlags.None, 12),
        new(0x37, "SCF", CpuFlags.NHC, 4),
        new(0x38, "JR C,i8", CpuFlags.None, 8),
        new(0x39, "ADD HL,SP", CpuFlags.NHC, 8),
        new(0x3A, "LD A,(HL-)", CpuFlags.None, 8),
        new(0x3B, "DEC SP", CpuFlags.None, 8),
        new(0x3C, "INC A", CpuFlags.ZNH, 4),
        new(0x3D, "DEC A", CpuFlags.ZNH, 4),
        new(0x3E, "LD A,u8", CpuFlags.None, 8),
        new(0x3F, "CCF", CpuFlags.NHC, 4),
        new(0x40, "LD B,B", CpuFlags.None, 4),
        new(0x41, "LD B,C", CpuFlags.None, 4),
        new(0x42, "LD B,D", CpuFlags.None, 4),
        new(0x43, "LD B,E", CpuFlags.None, 4),
        new(0x44, "LD B,H", CpuFlags.None, 4),
        new(0x45, "LD B,L", CpuFlags.None, 4),
        new(0x46, "LD B,(HL)", CpuFlags.None, 8),
        new(0x47, "LD B,A", CpuFlags.None, 4),
        new(0x48, "LD C,B", CpuFlags.None, 4),
        new(0x49, "LD C,C", CpuFlags.None, 4),
        new(0x4A, "LD C,D", CpuFlags.None, 4),
        new(0x4B, "LD C,E", CpuFlags.None, 4),
        new(0x4C, "LD C,H", CpuFlags.None, 4),
        new(0x4D, "LD C,L", CpuFlags.None, 4),
        new(0x4E, "LD C,(HL)", CpuFlags.None, 8),
        new(0x4F, "LD C,A", CpuFlags.None, 4),
        new(0x50, "LD D,B", CpuFlags.None, 4),
        new(0x51, "LD D,C", CpuFlags.None, 4),
        new(0x52, "LD D,D", CpuFlags.None, 4),
        new(0x53, "LD D,E", CpuFlags.None, 4),
        new(0x54, "LD D,H", CpuFlags.None, 4),
        new(0x55, "LD D,L", CpuFlags.None, 4),
        new(0x56, "LD D,(HL)", CpuFlags.None, 8),
        new(0x57, "LD D,A", CpuFlags.None, 4),
        new(0x58, "LD E,B", CpuFlags.None, 4),
        new(0x59, "LD E,C", CpuFlags.None, 4),
        new(0x5A, "LD E,D", CpuFlags.None, 4),
        new(0x5B, "LD E,E", CpuFlags.None, 4),
        new(0x5C, "LD E,H", CpuFlags.None, 4),
        new(0x5D, "LD E,L", CpuFlags.None, 4),
        new(0x5E, "LD E,(HL)", CpuFlags.None, 8),
        new(0x5F, "LD E,A", CpuFlags.None, 4),
        new(0x60, "LD H,B", CpuFlags.None, 4),
        new(0x61, "LD H,C", CpuFlags.None, 4),
        new(0x62, "LD H,D", CpuFlags.None, 4),
        new(0x63, "LD H,E", CpuFlags.None, 4),
        new(0x64, "LD H,H", CpuFlags.None, 4),
        new(0x65, "LD H,L", CpuFlags.None, 4),
        new(0x66, "LD H,(HL)", CpuFlags.None, 8),
        new(0x67, "LD H,A", CpuFlags.None, 4),
        new(0x68, "LD L,B", CpuFlags.None, 4),
        new(0x69, "LD L,C", CpuFlags.None, 4),
        new(0x6A, "LD L,D", CpuFlags.None, 4),
        new(0x6B, "LD L,E", CpuFlags.None, 4),
        new(0x6C, "LD L,H", CpuFlags.None, 4),
        new(0x6D, "LD L,L", CpuFlags.None, 4),
        new(0x6E, "LD L,(HL)", CpuFlags.None, 8),
        new(0x6F, "LD L,A", CpuFlags.None, 4),
        new(0x70, "LD (HL),B", CpuFlags.None, 8),
        new(0x71, "LD (HL),C", CpuFlags.None, 8),
        new(0x72, "LD (HL),D", CpuFlags.None, 8),
        new(0x73, "LD (HL),E", CpuFlags.None, 8),
        new(0x74, "LD (HL),H", CpuFlags.None, 8),
        new(0x75, "LD (HL),L", CpuFlags.None, 8),
        new(0x76, "HALT", CpuFlags.None, 4),
        new(0x77, "LD (HL),A", CpuFlags.None, 8),
        new(0x78, "LD A,B", CpuFlags.None, 4),
        new(0x79, "LD A,C", CpuFlags.None, 4),
        new(0x7A, "LD A,D", CpuFlags.None, 4),
        new(0x7B, "LD A,E", CpuFlags.None, 4),
        new(0x7C, "LD A,H", CpuFlags.None, 4),
        new(0x7D, "LD A,L", CpuFlags.None, 4),
        new(0x7E, "LD A,(HL)", CpuFlags.None, 8),
        new(0x7F, "LD A,A", CpuFlags.None, 4),
        new(0x80, "ADD A,B", CpuFlags.ZNHC, 4),
        new(0x81, "ADD A,C", CpuFlags.ZNHC, 4),
        new(0x82, "ADD A,D", CpuFlags.ZNHC, 4),
        new(0x83, "ADD A,E", CpuFlags.ZNHC, 4),
        new(0x84, "ADD A,H", CpuFlags.ZNHC, 4),
        new(0x85, "ADD A,L", CpuFlags.ZNHC, 4),
        new(0x86, "ADD A,(HL)", CpuFlags.ZNHC, 8),
        new(0x87, "ADD A,A", CpuFlags.ZNHC, 4),
        new(0x88, "ADC A,B", CpuFlags.ZNHC, 4),
        new(0x89, "ADC A,C", CpuFlags.ZNHC, 4),
        new(0x8A, "ADC A,D", CpuFlags.ZNHC, 4),
        new(0x8B, "ADC A,E", CpuFlags.ZNHC, 4),
        new(0x8C, "ADC A,H", CpuFlags.ZNHC, 4),
        new(0x8D, "ADC A,L", CpuFlags.ZNHC, 4),
        new(0x8E, "ADC A,(HL)", CpuFlags.ZNHC, 8),
        new(0x8F, "ADC A,A", CpuFlags.ZNHC, 4),
        new(0x90, "SUB A,B", CpuFlags.ZNHC, 4),
        new(0x91, "SUB A,C", CpuFlags.ZNHC, 4),
        new(0x92, "SUB A,D", CpuFlags.ZNHC, 4),
        new(0x93, "SUB A,E", CpuFlags.ZNHC, 4),
        new(0x94, "SUB A,H", CpuFlags.ZNHC, 4),
        new(0x95, "SUB A,L", CpuFlags.ZNHC, 4),
        new(0x96, "SUB A,(HL)", CpuFlags.ZNHC, 8),
        new(0x97, "SUB A,A", CpuFlags.ZNHC, 4),
        new(0x98, "SBC A,B", CpuFlags.ZNHC, 4),
        new(0x99, "SBC A,C", CpuFlags.ZNHC, 4),
        new(0x9A, "SBC A,D", CpuFlags.ZNHC, 4),
        new(0x9B, "SBC A,E", CpuFlags.ZNHC, 4),
        new(0x9C, "SBC A,H", CpuFlags.ZNHC, 4),
        new(0x9D, "SBC A,L", CpuFlags.ZNHC, 4),
        new(0x9E, "SBC A,(HL)", CpuFlags.ZNHC, 8),
        new(0x9F, "SBC A,A", CpuFlags.ZNHC, 4),
        new(0xA0, "AND A,B", CpuFlags.ZNHC, 4),
        new(0xA1, "AND A,C", CpuFlags.ZNHC, 4),
        new(0xA2, "AND A,D", CpuFlags.ZNHC, 4),
        new(0xA3, "AND A,E", CpuFlags.ZNHC, 4),
        new(0xA4, "AND A,H", CpuFlags.ZNHC, 4),
        new(0xA5, "AND A,L", CpuFlags.ZNHC, 4),
        new(0xA6, "AND A,(HL)", CpuFlags.ZNHC, 8),
        new(0xA7, "AND A,A", CpuFlags.ZNHC, 4),
        new(0xA8, "XOR A,B", CpuFlags.ZNHC, 4),
        new(0xA9, "XOR A,C", CpuFlags.ZNHC, 4),
        new(0xAA, "XOR A,D", CpuFlags.ZNHC, 4),
        new(0xAB, "XOR A,E", CpuFlags.ZNHC, 4),
        new(0xAC, "XOR A,H", CpuFlags.ZNHC, 4),
        new(0xAD, "XOR A,L", CpuFlags.ZNHC, 4),
        new(0xAE, "XOR A,(HL)", CpuFlags.ZNHC, 8),
        new(0xAF, "XOR A,A", CpuFlags.ZNHC, 4),
        new(0xB0, "OR A,B", CpuFlags.ZNHC, 4),
        new(0xB1, "OR A,C", CpuFlags.ZNHC, 4),
        new(0xB2, "OR A,D", CpuFlags.ZNHC, 4),
        new(0xB3, "OR A,E", CpuFlags.ZNHC, 4),
        new(0xB4, "OR A,H", CpuFlags.ZNHC, 4),
        new(0xB5, "OR A,L", CpuFlags.ZNHC, 4),
        new(0xB6, "OR A,(HL)", CpuFlags.ZNHC, 8),
        new(0xB7, "OR A,A", CpuFlags.ZNHC, 4),
        new(0xB8, "CP A,B", CpuFlags.ZNHC, 4),
        new(0xB9, "CP A,C", CpuFlags.ZNHC, 4),
        new(0xBA, "CP A,D", CpuFlags.ZNHC, 4),
        new(0xBB, "CP A,E", CpuFlags.ZNHC, 4),
        new(0xBC, "CP A,H", CpuFlags.ZNHC, 4),
        new(0xBD, "CP A,L", CpuFlags.ZNHC, 4),
        new(0xBE, "CP A,(HL)", CpuFlags.ZNHC, 8),
        new(0xBF, "CP A,A", CpuFlags.ZNHC, 4),
        new BranchInstructionInfo(0xC0, "RET NZ", CpuFlags.None, 8, 12),
        new(0xC1, "POP BC", CpuFlags.None, 12),
        new BranchInstructionInfo(0xC2, "JP NZ,u16", CpuFlags.None, 12, 4),
        new BranchInstructionInfo(0xC3, "JP u16", CpuFlags.None, 16, 0),
        new BranchInstructionInfo(0xC4, "CALL NZ,u16", CpuFlags.None, 12, 12),
        new(0xC5, "PUSH BC", CpuFlags.None, 16),
        new(0xC6, "ADD A,u8", CpuFlags.ZNHC, 8),
        new(0xC7, "RST 00h", CpuFlags.None, 16),
        new BranchInstructionInfo(0xC8, "RET Z", CpuFlags.None, 8, 12),
        new BranchInstructionInfo(0xC9, "RET", CpuFlags.None, 16, 0),
        new BranchInstructionInfo(0xCA, "JP Z,u16", CpuFlags.None, 12, 4),
        new(0xCB, "PREFIX CB", CpuFlags.None, 4),
        new BranchInstructionInfo(0xCC, "CALL Z,u16", CpuFlags.None, 12, 12),
        new BranchInstructionInfo(0xCD, "CALL u16", CpuFlags.None, 24, 0),
        new(0xCE, "ADC A,u8", CpuFlags.ZNHC, 8),
        new(0xCF, "RST 08h", CpuFlags.None, 16),
        new BranchInstructionInfo(0xD0, "RET NC", CpuFlags.None, 8, 12),
        new(0xD1, "POP DE", CpuFlags.None, 12),
        new BranchInstructionInfo(0xD2, "JP NC,u16", CpuFlags.None, 12, 4),
        new(0xD3, "Illegal Operation", CpuFlags.None, 0),
        new BranchInstructionInfo(0xD4, "CALL NC,u16", CpuFlags.None, 12, 12),
        new(0xD5, "PUSH DE", CpuFlags.None, 16),
        new(0xD6, "SUB A,u8", CpuFlags.ZNHC, 8),
        new(0xD7, "RST 10h", CpuFlags.None, 16),
        new BranchInstructionInfo(0xD8, "RET C", CpuFlags.None, 8, 12),
        new BranchInstructionInfo(0xD9, "RETI", CpuFlags.None, 16, 0),
        new BranchInstructionInfo(0xDA, "JP C,u16", CpuFlags.None, 12, 4),
        new(0xDB, "Illegal Operation", CpuFlags.None, 0),
        new BranchInstructionInfo(0xDC, "CALL C,u16", CpuFlags.None, 12, 12),
        new(0xDD, "Illegal Operation", CpuFlags.None, 0),
        new(0xDE, "SBC A,u8", CpuFlags.ZNHC, 8),
        new(0xDF, "RST 18h", CpuFlags.None, 16),
        new(0xE0, "LD (FF00+u8),A", CpuFlags.None, 12),
        new(0xE1, "POP HL", CpuFlags.None, 12),
        new(0xE2, "LD (FF00+C),A", CpuFlags.None, 8),
        new(0xE3, "Illegal Operation", CpuFlags.None, 0),
        new(0xE4, "Illegal Operation", CpuFlags.None, 0),
        new(0xE5, "PUSH HL", CpuFlags.None, 16),
        new(0xE6, "AND A,u8", CpuFlags.ZNHC, 8),
        new(0xE7, "RST 20h", CpuFlags.None, 16),
        new(0xE8, "ADD SP,i8", CpuFlags.ZNHC, 16),
        new BranchInstructionInfo(0xE9, "JP HL", CpuFlags.None, 4, 0),
        new(0xEA, "LD (u16),A", CpuFlags.None, 16),
        new(0xEB, "Illegal Operation", CpuFlags.None, 0),
        new(0xEC, "Illegal Operation", CpuFlags.None, 0),
        new(0xED, "Illegal Operation", CpuFlags.None, 0),
        new(0xEE, "XOR A,u8", CpuFlags.ZNHC, 8),
        new(0xEF, "RST 28h", CpuFlags.None, 16),
        new(0xF0, "LD A,(FF00+u8)", CpuFlags.None, 12),
        new(0xF1, "POP AF", CpuFlags.ZNHC, 12),
        new(0xF2, "LD A,(FF00+C)", CpuFlags.None, 8),
        new(0xF3, "DI", CpuFlags.None, 4),
        new(0xF4, "Illegal Operation", CpuFlags.None, 0),
        new(0xF5, "PUSH AF", CpuFlags.None, 16),
        new(0xF6, "OR A,u8", CpuFlags.ZNHC, 8),
        new(0xF7, "RST 30h", CpuFlags.None, 16),
        new(0xF8, "LD HL,SP+i8", CpuFlags.ZNHC, 12),
        new(0xF9, "LD SP,HL", CpuFlags.None, 8),
        new(0xFA, "LD A,(u16)", CpuFlags.None, 16),
        new(0xFB, "EI", CpuFlags.None, 4),
        new(0xFC, "Illegal Operation", CpuFlags.None, 0),
        new(0xFD, "Illegal Operation", CpuFlags.None, 0),
        new(0xFE, "CP A,u8", CpuFlags.ZNHC, 8),
        new(0xFF, "RST 38h", CpuFlags.None, 16),
    };
    
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

    private ushort Pop(ref RegisterPage page, IBus bus)
    {
        byte low = bus.ReadByte(page.SP++);
        return IntegerHelper.JoinBytes(bus.ReadByte(page.SP++), low);
    }

    private void Push(ref RegisterPage page, IBus bus, ushort value)
    {
        (byte high, byte low) = IntegerHelper.SplitShort(value);
        bus.Write(--page.SP, high);
        bus.Write(--page.SP, low);
    }

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

    public DMGEngine()
    {
        BinaryPatternMatcher<byte, Operation> bpm = GenerateBinaryMatcher();
        _iInfo = GenerateInfoTable();
        
        for (int i = 0; i < 0x100; i++)
            _operations[i] = bpm.Match((byte)i).Execute((byte)i);
    }
}