using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using GBV.Core.Bus;
using GBV.Core.Processor;

namespace GBV.Core;

public class DMGEngine
{
    public int WorkTime { get; private set; }
    public StatusRegister _internalF;

    private InstructionInfo[] _baseInfo;
    private InstructionInfo[] _extendedInfo;
    
    public DMGEngine()
    {
        _baseInfo = GenerateInstructionInfoTable();
        _extendedInfo = GenerateExtendedInfoTable();
    }
    
    public void Execute(byte operation, IRegisterPage page, IBus bus)
    {
        InstructionInfo info = GetInstructionInfo(operation, false);

        WorkTime = info.BaseTime;
        _internalF = 0;
        
        switch (operation)
        {
            // 00 - NOP
            case 0:
                break;
            // 01 - LD BC,u16
            case 0x01:
                page.BC = Immediate16(page, bus);
                break;
            // 02 - LD (BC),A
            case 0x02:
                bus.Write(page.BC, page.A);
                break;
            // 03 - INC BC
            case 0x03:
                page.BC++;
                break;
            // 04 - INC B
            case 0x04:
                page.B = Inc(page.B);
                break;
            // 05 - DEC B
            case 0x05:
                page.B = Dec(page.B);
                break;
            // 06 - LD B,u8
            case 0x06:
                page.B = Immediate(page, bus);
                break;
            // 07 - RLCA
            case 0x07:
                page.A = Rlc(page.A, false);
                break;
            // 08 - LD (u16),SP
            case 0x08:
                bus.Write(Immediate16(page, bus), page.SP);
                break;
            // 09 - ADD HL,BC
            case 0x09:
                page.HL = Add(page.HL, page.BC);
                break;
            // 0A - LD A,(BC)
            case 0x0A:
                page.A = bus.ReadByte(page.BC);
                break;
            // 0B - DEC BC
            case 0x0B:
                page.BC--;
                break;
            // 0C - INC C
            case 0x0C:
                page.C = Inc(page.C);
                break;
            // 0D - DEC C
            case 0x0D:
                page.C = Dec(page.C);
                break;
            // 0E - LD C,u8
            case 0x0E:
                page.C = Immediate(page, bus);
                break;
            // 0F - RRCA
            case 0x0F:
                page.A = Rrc(page.A, false);
                break;
            // 10 - STOP
            case 0x10:
                Stop();
                break;
            // 11 - LD DE,u16
            case 0x11:
                page.DE = Immediate16(page, bus);
                break;
            // 12 - LD (DE),A
            case 0x12:
                bus.Write(page.DE, page.A);
                break;
            // 13 - INC DE
            case 0x13:
                page.DE++;
                break;
            // 14 - INC D
            case 0x14:
                page.D = Inc(page.D);
                break;
            // 15 - DEC D
            case 0x15:
                page.D = Dec(page.D);
                break;
            // 16 - LD D,u8
            case 0x16:
                page.D = Immediate(page, bus);
                break;
            // 17 - RLA
            case 0x17:
                page.A = Rl(page.A, ((StatusRegister)page.F).C, false);
                break;
            // 18 - JR i8
            case 0x18:
                Jr((sbyte)Immediate(page, bus), BranchCondition.None, page);
                break;
            // 19 - ADD HL,DE
            case 0x19:
                page.HL = Add(page.HL, page.DE);
                break;
            // 1A - LD A,(DE)
            case 0x1A:
                page.A = bus.ReadByte(page.DE);
                break;
            // 1B - DEC DE
            case 0x1B:
                page.DE--;
                break;
            // 1C - INC E
            case 0x1C:
                page.E = Inc(page.E);
                break;
            // 1D - DEC E
            case 0x1D:
                page.E = Dec(page.E);
                break;
            // 1E - LD E,u8
            case 0x1E:
                page.E = Immediate(page, bus);
                break;
            // 1F - RRA
            case 0x1F:
                page.A = Rr(page.A, ((StatusRegister)page.F).C, false);
                break;
            // 20 - JR NZ,i8
            case 0x20:
                Jr((sbyte)Immediate(page, bus), BranchCondition.NZ, page);
                break;
            // 21 - LD HL,u16
            case 0x21:
                page.HL = Immediate16(page, bus);
                break;
            // 22 - LD (HL+),A
            case 0x22:
                bus.Write(page.HL++, page.A);
                break;
            // 23 - INC HL
            case 0x23:
                page.HL++;
                break;
            // 24 - INC H
            case 0x24:
                page.H = Inc(page.H);
                break;
            // 25 - DEC H
            case 0x25:
                page.H = Dec(page.H);
                break;
            // 26 - LD H,u8
            case 0x26:
                page.H = Immediate(page, bus);
                break;
            // 27 - DAA
            case 0x27:
                page.A = Daa(page.A, page.F);
                break;
            // 28 - JR Z,i8
            case 0x28:
                Jr((sbyte)Immediate(page, bus), BranchCondition.Z, page);
                break;
            // 29 - ADD HL,HL
            case 0x29:
                page.HL = Add(page.HL, page.HL);
                break;
            // 2A - LD A,(HL+)
            case 0x2A:
                page.A = bus.ReadByte(page.HL++);
                break;
            // 2B - DEC HL
            case 0x2B:
                page.HL--;
                break;
            // 2C - INC L
            case 0x2C:
                page.L = Inc(page.L);
                break;
            // 2D - DEC L
            case 0x2D:
                page.L = Dec(page.L);
                break;
            // 2E - LD L,u8
            case 0x2E:
                page.L = Immediate(page, bus);
                break;
            // 2F - CPL
            case 0x2F:
                page.A = Cpl(page.A);
                break;
            // 30 - JR NC,i8
            case 0x30:
                Jr((sbyte)Immediate(page, bus), BranchCondition.NC, page);
                break;
            // 31 - LD SP,u16
            case 0x31:
                page.SP = Immediate16(page, bus);
                break;
            // 32 - LD (HL-),A
            case 0x32:
                bus.Write(page.HL--, page.A);
                break;
            // 33 - INC SP
            case 0x33:
                page.SP++;
                break;
            // 34 - INC (HL)
            case 0x34:
                bus.Write(page.HL, Inc(bus.ReadByte(page.HL)));
                break;
            // 35 - DEC (HL)
            case 0x35:
                bus.Write(page.HL, Dec(bus.ReadByte(page.HL)));
                break;
            // 36 - LD (HL),u8
            case 0x36:
                bus.Write(page.HL, Immediate(page, bus));
                break;
            // 37 - SCF
            case 0x37:
                Scf();
                break;
            // 38 - JR C,i8
            case 0x38:
                Jr((sbyte)Immediate(page, bus), BranchCondition.C, page);
                break;
            // 39 - ADD HL,SP
            case 0x39:
                page.HL = Add(page.HL, page.SP);
                break;
            // 3A - LD A,(HL-)
            case 0x3A:
                page.A = bus.ReadByte(page.HL--);
                break;
            // 3B - DEC SP
            case 0x3B:
                page.SP--;
                break;
            // 3C - INC A
            case 0x3C:
                page.A = Inc(page.A);
                break;
            // 3D - DEC A
            case 0x3D:
                page.A = Dec(page.A);
                break;
            // 3E - LD A,u8
            case 0x3E:
                page.A = Immediate(page, bus);
                break;
            // 3F - CCF
            case 0x3F:
                Ccf(((StatusRegister)page.F).C);
                break;
            case 0x76:
                Halt();
                break;
            // LD Dest Src
            case < 0x80:
                int dest = (operation >> 3) & 0b111;
                int src = operation & 0b111;
                WriteRegister(page, bus, dest, ReadRegister(page, bus, src));
                break;
            // ALU r8
            case < 0xB8:
                page.A = AluOperation(operation);
                break;
            case < 0xC0:
                int reg = operation & 0b111;
                _ = Sub(page.A, ReadRegister(page, bus, reg), false);
                break;
            // C0 - RET NZ
            case 0xC0:
                Ret(BranchCondition.NZ, page, bus);
                break;
            // C1 - POP BC
            case 0xC1:
                page.BC = Pop(page, bus);
                break;
            // C2 - JP NZ,u16
            case 0xC2:
                Jp(Immediate16(page, bus), BranchCondition.NZ, page);
                break;
            // C3 - JP u16
            case 0xC3:
                Jp(Immediate16(page, bus), BranchCondition.None, page);
                break;
            // C4 - CALL NZ,u16
            case 0xC4:
                Call(Immediate16(page, bus), BranchCondition.NZ, page, bus);
                break;
            // C5 - PUSH BC
            case 0xC5:
                Push(page.BC, page, bus);
                break;
            // C6 - ADD A,u8
            case 0xC6:
                page.A = Add(page.A, Immediate(page, bus), false);
                break;
            // C7 - RST 00h
            case 0xC7:
                Call(0x0000, BranchCondition.None, page, bus);
                break;
            // C8 - RET Z
            case 0xC8:
                Ret(BranchCondition.Z, page, bus);
                break;
            // C9 - RET
            case 0xC9:
                Ret(BranchCondition.None, page, bus);
                break;
            // CA - JP Z,u16
            case 0xCA:
                Jp(Immediate16(page, bus), BranchCondition.Z, page);
                break;
            // CB - PREFIX CB
            case 0xCB:
                byte extendedInst = Immediate(page, bus);
                info = GetInstructionInfo(extendedInst, true);
                ExecuteCB(extendedInst, page, bus);
                break;
            // CC - CALL Z,u16
            case 0xCC:
                Call(Immediate16(page, bus), BranchCondition.Z, page, bus);
                break;
            // CD - CALL u16
            case 0xCD:
                Call(Immediate16(page, bus), BranchCondition.None, page, bus);
                break;
            // CE - ADC A,u8
            case 0xCE:
                page.A = Add(page.A, Immediate(page, bus), ((StatusRegister)page.F).C);
                break;
            // CF - RST 08h
            case 0xCF:
                Call(0x0008, BranchCondition.None, page, bus);
                break;
            // D0 - RET NC
            case 0xD0:
                Ret(BranchCondition.NC, page, bus);
                break;
            // D1 - POP DE
            case 0xD1:
                page.DE = Pop(page, bus);
                break;
            // D2 - JP NC,u16
            case 0xD2:
                Jp(Immediate16(page, bus), BranchCondition.NC, page);
                break;
            // D3 - Illegal Operation
            case 0xD3:
                Illegal();
                break;
            // D4 - CALL NC,u16
            case 0xD4:
                Call(Immediate16(page, bus), BranchCondition.NC, page, bus);
                break;
            // D5 - PUSH DE
            case 0xD5:
                Push(page.DE, page, bus);
                break;
            // D6 - SUB A,u8
            case 0xD6:
                page.A = Sub(page.A, Immediate(page, bus), false);
                break;
            // D7 - RST 10h
            case 0xD7:
                Call(0x0010, BranchCondition.None, page, bus);
                break;
            // D8 - RET C
            case 0xD8:
                Ret(BranchCondition.C, page, bus);
                break;
            // D9 - RETI
            case 0xD9:
                Reti();
                break;
            // DA - JP C,u16
            case 0xDA:
                Jp(Immediate16(page, bus), BranchCondition.C, page);
                break;
            // DB - Illegal Operation
            case 0xDB:
                Illegal();
                break;
            // DC - CALL C,u16
            case 0xDC:
                Call(Immediate16(page, bus), BranchCondition.C, page, bus);
                break;
            // DD - Illegal Operation
            case 0xDD:
                Illegal();
                break;
            // DE - SBC A,u8
            case 0xDE:
                page.A = Sub(page.A, Immediate(page, bus), ((StatusRegister)page.F).C);
                break;
            // DF - RST 18h
            case 0xDF:
                Call(0x0018, BranchCondition.None, page, bus);
                break;
            // E0 - LD (FF00+u8),A
            case 0xE0:
                bus.Write((ushort)(0xFF00 + Immediate(page, bus)), page.A);
                break;
            // E1 - POP HL
            case 0xE1:
                page.HL = Pop(page, bus);
                break;
            // E2 - LD (FF00+C),A
            case 0xE2:
                bus.Write((ushort)(0xFF00 + page.C), page.A);
                break;
            // E3 - Illegal Operation
            case 0xE3:
            // E4 - Illegal Operation
            case 0xE4:
                Illegal();
                break;
            // E5 - PUSH HL
            case 0xE5:
                Push(page.HL, page, bus);
                break;
            // E6 - AND A,u8
            case 0xE6:
                page.A = And(page.A, Immediate(page, bus));
                break;
            // E7 - RST 20h
            case 0xE7:
                Call(0x0020, BranchCondition.None, page, bus);
                break;
            // E8 - ADD SP,i8
            case 0xE8:
                page.SP = Add(page.SP, (sbyte)Immediate(page, bus));
                break;
            // E9 - JP HL
            case 0xE9:
                page.PC = page.HL;
                break;
            // EA - LD (u16),A
            case 0xEA:
                bus.Write(Immediate16(page, bus), page.A);
                break;
            // EB - Illegal Operation
            case 0xEB:
            // EC - Illegal Operation
            case 0xEC:
            // ED - Illegal Operation
            case 0xED:
                Illegal();
                break;
            // EE - XOR A,u8
            case 0xEE:
                page.A = Xor(page.A, Immediate(page, bus));
                break;
            // EF - RST 28h
            case 0xEF:
                Call(0x0028, BranchCondition.None, page, bus);
                break;
            // F0 - LD A,(FF00+u8)
            case 0xF0:
                page.A = bus.ReadByte((ushort)(0xFF00 + Immediate(page, bus)));
                break;
            // F1 - POP AF
            case 0xF1:
                page.AF = Pop(page, bus);
                break;
            // F2 - LD A,(FF00+C)
            case 0xF2:
                page.A = bus.ReadByte((ushort)(0xFF00 + page.C));
                break;
            // F3 - DI
            case 0xF3:
                Di();
                break;
            // F4 - Illegal Operation
            case 0xF4:
                Illegal();
                break;
            // F5 - PUSH AF
            case 0xF5:
                Push(page.AF, page, bus);
                break;
            // F6 - OR A,u8
            case 0xF6:
                page.A = Or(page.A, Immediate(page, bus));
                break;
            // F7 - RST 30h
            case 0xF7:
                Call(0x0030, BranchCondition.None, page, bus);
                break;
            // F8 - LD HL,SP+i8
            case 0xF8:
                page.HL = Add(page.SP, (sbyte)Immediate(page, bus));
                break;
            // F9 - LD SP,HL
            case 0xF9:
                page.SP = page.HL;
                break;
            // FA - LD A,(u16)
            case 0xFA:
                page.A = bus.ReadByte(Immediate16(page, bus));
                break;
            // FB - EI
            case 0xFB:
                Ei();
                break;
            // FC - Illegal Operation
            case 0xFC:
            // FD - Illegal Operation
            case 0xFD:
                Illegal();
                break;
            // FE - CP A,u8
            case 0xFE:
                _ = Sub(page.A, Immediate(page, bus), false);
                break;
            // FF - RST 38h
            default:
                Call(0x0038, BranchCondition.None, page, bus);
                break;
        }

        page.F &= (byte)~info.FlagMask;
        _internalF = (byte)(_internalF.Value & info.FlagMask);
        page.F |= (byte)_internalF;
        
        return;

        byte AluOperation(byte instruction)
        {
            int op = (instruction >> 3) & 0b111;
            int reg = instruction & 0b111;
            switch (op)
            {
                // ADD r8
                case 0:
                    return Add(page.A, ReadRegister(page, bus, reg), false);
                
                // ADC r8
                case 1:
                    return Add(page.A, ReadRegister(page, bus, reg), ((StatusRegister)page.F).C);
                
                // SUB r8
                case 2:
                    return Sub(page.A, ReadRegister(page, bus, reg), false);
                
                // SBC r8
                case 3:
                    return Sub(page.A, ReadRegister(page, bus, reg), ((StatusRegister)page.F).C);
                
                // AND r8
                case 4:
                    return And(page.A, ReadRegister(page, bus, reg));
                
                // XOR r8
                case 5:
                    return Xor(page.A, ReadRegister(page, bus, reg));
                
                // OR r8
                case 6:
                    return Or(page.A, ReadRegister(page, bus, reg));
                
                default:
                    throw new UnreachableException();
            }
        }
    }

    private void ExecuteCB(byte operation, IRegisterPage page, IBus bus)
    {
        int reg = operation & 0b111;
        
        switch (operation)
        {
            case < 0x08:
                WriteRegister(page, bus, reg, Rlc(ReadRegister(page, bus, reg), true));
                break;
            
            case < 0x10:
                WriteRegister(page, bus, reg, Rrc(ReadRegister(page, bus, reg), true));
                break;
            
            case < 0x18:
                WriteRegister(page, bus, reg, Rl(ReadRegister(page, bus, reg), ((StatusRegister)page.F).C, true));
                break;
            
            case < 0x20:
                WriteRegister(page, bus, reg, Rr(ReadRegister(page, bus, reg), ((StatusRegister)page.F).C, true));
                break;
                
            case < 0x28:
                WriteRegister(page, bus, reg, Sla(ReadRegister(page, bus, reg)));
                break;
            
            case < 0x30:
                WriteRegister(page, bus, reg, Sra(ReadRegister(page, bus, reg)));
                break;
            
            case < 0x38:
                WriteRegister(page, bus, reg, Swap(ReadRegister(page, bus, reg)));
                break;
            
            case < 0x40:
                WriteRegister(page, bus, reg, Srl(ReadRegister(page, bus, reg)));
                break;

            case < 0x80:
                Bit(ReadRegister(page, bus, reg), (operation >> 3) & 0b111);
                break;
            
            case < 0xC0:
                WriteRegister(page, bus, reg, SetBit(ReadRegister(page, bus, reg), (operation >> 3) & 0b111, false));
                break;

            case <= 0xFF:
                WriteRegister(page, bus, reg, SetBit(ReadRegister(page, bus, reg), (operation >> 3) & 0b111, true));
                break;
        }
    }
        
    byte ReadRegister(IRegisterPage page, IBus bus, int reg) => reg switch
    {
        0 => page.B,
        1 => page.C,
        2 => page.D,
        3 => page.E,
        4 => page.H,
        5 => page.L,
        6 => bus.ReadByte(page.HL),
        7 => page.A,
        _ => throw new UnreachableException()
    };
    
    void WriteRegister(IRegisterPage page, IBus bus, int reg, byte value)
    {
        switch (reg)
        {
            case 0:
                page.B = value;
                break;
            case 1:
                page.C = value;
                break;
            case 2:
                page.D = value;
                break;
            case 3:
                page.E = value;
                break;
            case 4:
                page.H = value;
                break;
            case 5:
                page.L = value;
                break;
            case 6:
                bus.Write(page.HL, value);
                break;
            case 7:
                page.A = value;
                break;
            default:
                throw new UnreachableException();
        }
    }
    
    private InstructionInfo GetInstructionInfo(byte operation, bool extended) =>
        extended ? _extendedInfo[operation] : _baseInfo[operation];
    
    private byte Immediate(IRegisterPage page, IBus bus) => bus.ReadByte(page.PC++);

    private ushort Immediate16(IRegisterPage page, IBus bus)
    {
        byte low = Immediate(page, bus);
        return IntegerHelper.JoinBytes(Immediate(page, bus), low);
    }

    private static InstructionInfo[] GenerateExtendedInfoTable() => new InstructionInfo[]
    {
        new(0x00, "RLC B", 0b1111_0000, 8),
        new(0x01, "RLC C", 0b1111_0000, 8),
        new(0x02, "RLC D", 0b1111_0000, 8),
        new(0x03, "RLC E", 0b1111_0000, 8),
        new(0x04, "RLC H", 0b1111_0000, 8),
        new(0x05, "RLC L", 0b1111_0000, 8),
        new(0x06, "RLC (HL)", 0b1111_0000, 16),
        new(0x07, "RLC A", 0b1111_0000, 8),
        new(0x08, "RRC B", 0b1111_0000, 8),
        new(0x09, "RRC C", 0b1111_0000, 8),
        new(0x0A, "RRC D", 0b1111_0000, 8),
        new(0x0B, "RRC E", 0b1111_0000, 8),
        new(0x0C, "RRC H", 0b1111_0000, 8),
        new(0x0D, "RRC L", 0b1111_0000, 8),
        new(0x0E, "RRC (HL)", 0b1111_0000, 16),
        new(0x0F, "RRC A", 0b1111_0000, 8),
        new(0x10, "RL B", 0b1111_0000, 8),
        new(0x11, "RL C", 0b1111_0000, 8),
        new(0x12, "RL D", 0b1111_0000, 8),
        new(0x13, "RL E", 0b1111_0000, 8),
        new(0x14, "RL H", 0b1111_0000, 8),
        new(0x15, "RL L", 0b1111_0000, 8),
        new(0x16, "RL (HL)", 0b1111_0000, 16),
        new(0x17, "RL A", 0b1111_0000, 8),
        new(0x18, "RR B", 0b1111_0000, 8),
        new(0x19, "RR C", 0b1111_0000, 8),
        new(0x1A, "RR D", 0b1111_0000, 8),
        new(0x1B, "RR E", 0b1111_0000, 8),
        new(0x1C, "RR H", 0b1111_0000, 8),
        new(0x1D, "RR L", 0b1111_0000, 8),
        new(0x1E, "RR (HL)", 0b1111_0000, 16),
        new(0x1F, "RR A", 0b1111_0000, 8),
        new(0x20, "SLA B", 0b1111_0000, 8),
        new(0x21, "SLA C", 0b1111_0000, 8),
        new(0x22, "SLA D", 0b1111_0000, 8),
        new(0x23, "SLA E", 0b1111_0000, 8),
        new(0x24, "SLA H", 0b1111_0000, 8),
        new(0x25, "SLA L", 0b1111_0000, 8),
        new(0x26, "SLA (HL)", 0b1111_0000, 16),
        new(0x27, "SLA A", 0b1111_0000, 8),
        new(0x28, "SRA B", 0b1111_0000, 8),
        new(0x29, "SRA C", 0b1111_0000, 8),
        new(0x2A, "SRA D", 0b1111_0000, 8),
        new(0x2B, "SRA E", 0b1111_0000, 8),
        new(0x2C, "SRA H", 0b1111_0000, 8),
        new(0x2D, "SRA L", 0b1111_0000, 8),
        new(0x2E, "SRA (HL)", 0b1111_0000, 16),
        new(0x2F, "SRA A", 0b1111_0000, 8),
        new(0x30, "SWAP B", 0b1111_0000, 8),
        new(0x31, "SWAP C", 0b1111_0000, 8),
        new(0x32, "SWAP D", 0b1111_0000, 8),
        new(0x33, "SWAP E", 0b1111_0000, 8),
        new(0x34, "SWAP H", 0b1111_0000, 8),
        new(0x35, "SWAP L", 0b1111_0000, 8),
        new(0x36, "SWAP (HL)", 0b1111_0000, 16),
        new(0x37, "SWAP A", 0b1111_0000, 8),
        new(0x38, "SRL B", 0b1111_0000, 8),
        new(0x39, "SRL C", 0b1111_0000, 8),
        new(0x3A, "SRL D", 0b1111_0000, 8),
        new(0x3B, "SRL E", 0b1111_0000, 8),
        new(0x3C, "SRL H", 0b1111_0000, 8),
        new(0x3D, "SRL L", 0b1111_0000, 8),
        new(0x3E, "SRL (HL)", 0b1111_0000, 16),
        new(0x3F, "SRL A", 0b1111_0000, 8),
        new(0x40, "BIT 0,B", 0b1110_0000, 8),
        new(0x41, "BIT 0,C", 0b1110_0000, 8),
        new(0x42, "BIT 0,D", 0b1110_0000, 8),
        new(0x43, "BIT 0,E", 0b1110_0000, 8),
        new(0x44, "BIT 0,H", 0b1110_0000, 8),
        new(0x45, "BIT 0,L", 0b1110_0000, 8),
        new(0x46, "BIT 0,(HL)", 0b1110_0000, 12),
        new(0x47, "BIT 0,A", 0b1110_0000, 8),
        new(0x48, "BIT 1,B", 0b1110_0000, 8),
        new(0x49, "BIT 1,C", 0b1110_0000, 8),
        new(0x4A, "BIT 1,D", 0b1110_0000, 8),
        new(0x4B, "BIT 1,E", 0b1110_0000, 8),
        new(0x4C, "BIT 1,H", 0b1110_0000, 8),
        new(0x4D, "BIT 1,L", 0b1110_0000, 8),
        new(0x4E, "BIT 1,(HL)", 0b1110_0000, 12),
        new(0x4F, "BIT 1,A", 0b1110_0000, 8),
        new(0x50, "BIT 2,B", 0b1110_0000, 8),
        new(0x51, "BIT 2,C", 0b1110_0000, 8),
        new(0x52, "BIT 2,D", 0b1110_0000, 8),
        new(0x53, "BIT 2,E", 0b1110_0000, 8),
        new(0x54, "BIT 2,H", 0b1110_0000, 8),
        new(0x55, "BIT 2,L", 0b1110_0000, 8),
        new(0x56, "BIT 2,(HL)", 0b1110_0000, 12),
        new(0x57, "BIT 2,A", 0b1110_0000, 8),
        new(0x58, "BIT 3,B", 0b1110_0000, 8),
        new(0x59, "BIT 3,C", 0b1110_0000, 8),
        new(0x5A, "BIT 3,D", 0b1110_0000, 8),
        new(0x5B, "BIT 3,E", 0b1110_0000, 8),
        new(0x5C, "BIT 3,H", 0b1110_0000, 8),
        new(0x5D, "BIT 3,L", 0b1110_0000, 8),
        new(0x5E, "BIT 3,(HL)", 0b1110_0000, 12),
        new(0x5F, "BIT 3,A", 0b1110_0000, 8),
        new(0x60, "BIT 4,B", 0b1110_0000, 8),
        new(0x61, "BIT 4,C", 0b1110_0000, 8),
        new(0x62, "BIT 4,D", 0b1110_0000, 8),
        new(0x63, "BIT 4,E", 0b1110_0000, 8),
        new(0x64, "BIT 4,H", 0b1110_0000, 8),
        new(0x65, "BIT 4,L", 0b1110_0000, 8),
        new(0x66, "BIT 4,(HL)", 0b1110_0000, 12),
        new(0x67, "BIT 4,A", 0b1110_0000, 8),
        new(0x68, "BIT 5,B", 0b1110_0000, 8),
        new(0x69, "BIT 5,C", 0b1110_0000, 8),
        new(0x6A, "BIT 5,D", 0b1110_0000, 8),
        new(0x6B, "BIT 5,E", 0b1110_0000, 8),
        new(0x6C, "BIT 5,H", 0b1110_0000, 8),
        new(0x6D, "BIT 5,L", 0b1110_0000, 8),
        new(0x6E, "BIT 5,(HL)", 0b1110_0000, 12),
        new(0x6F, "BIT 5,A", 0b1110_0000, 8),
        new(0x70, "BIT 6,B", 0b1110_0000, 8),
        new(0x71, "BIT 6,C", 0b1110_0000, 8),
        new(0x72, "BIT 6,D", 0b1110_0000, 8),
        new(0x73, "BIT 6,E", 0b1110_0000, 8),
        new(0x74, "BIT 6,H", 0b1110_0000, 8),
        new(0x75, "BIT 6,L", 0b1110_0000, 8),
        new(0x76, "BIT 6,(HL)", 0b1110_0000, 12),
        new(0x77, "BIT 6,A", 0b1110_0000, 8),
        new(0x78, "BIT 7,B", 0b1110_0000, 8),
        new(0x79, "BIT 7,C", 0b1110_0000, 8),
        new(0x7A, "BIT 7,D", 0b1110_0000, 8),
        new(0x7B, "BIT 7,E", 0b1110_0000, 8),
        new(0x7C, "BIT 7,H", 0b1110_0000, 8),
        new(0x7D, "BIT 7,L", 0b1110_0000, 8),
        new(0x7E, "BIT 7,(HL)", 0b1110_0000, 12),
        new(0x7F, "BIT 7,A", 0b1110_0000, 8),
        new(0x80, "RES 0,B", 0b0000_0000, 8),
        new(0x81, "RES 0,C", 0b0000_0000, 8),
        new(0x82, "RES 0,D", 0b0000_0000, 8),
        new(0x83, "RES 0,E", 0b0000_0000, 8),
        new(0x84, "RES 0,H", 0b0000_0000, 8),
        new(0x85, "RES 0,L", 0b0000_0000, 8),
        new(0x86, "RES 0,(HL)", 0b0000_0000, 16),
        new(0x87, "RES 0,A", 0b0000_0000, 8),
        new(0x88, "RES 1,B", 0b0000_0000, 8),
        new(0x89, "RES 1,C", 0b0000_0000, 8),
        new(0x8A, "RES 1,D", 0b0000_0000, 8),
        new(0x8B, "RES 1,E", 0b0000_0000, 8),
        new(0x8C, "RES 1,H", 0b0000_0000, 8),
        new(0x8D, "RES 1,L", 0b0000_0000, 8),
        new(0x8E, "RES 1,(HL)", 0b0000_0000, 16),
        new(0x8F, "RES 1,A", 0b0000_0000, 8),
        new(0x90, "RES 2,B", 0b0000_0000, 8),
        new(0x91, "RES 2,C", 0b0000_0000, 8),
        new(0x92, "RES 2,D", 0b0000_0000, 8),
        new(0x93, "RES 2,E", 0b0000_0000, 8),
        new(0x94, "RES 2,H", 0b0000_0000, 8),
        new(0x95, "RES 2,L", 0b0000_0000, 8),
        new(0x96, "RES 2,(HL)", 0b0000_0000, 16),
        new(0x97, "RES 2,A", 0b0000_0000, 8),
        new(0x98, "RES 3,B", 0b0000_0000, 8),
        new(0x99, "RES 3,C", 0b0000_0000, 8),
        new(0x9A, "RES 3,D", 0b0000_0000, 8),
        new(0x9B, "RES 3,E", 0b0000_0000, 8),
        new(0x9C, "RES 3,H", 0b0000_0000, 8),
        new(0x9D, "RES 3,L", 0b0000_0000, 8),
        new(0x9E, "RES 3,(HL)", 0b0000_0000, 16),
        new(0x9F, "RES 3,A", 0b0000_0000, 8),
        new(0xA0, "RES 4,B", 0b0000_0000, 8),
        new(0xA1, "RES 4,C", 0b0000_0000, 8),
        new(0xA2, "RES 4,D", 0b0000_0000, 8),
        new(0xA3, "RES 4,E", 0b0000_0000, 8),
        new(0xA4, "RES 4,H", 0b0000_0000, 8),
        new(0xA5, "RES 4,L", 0b0000_0000, 8),
        new(0xA6, "RES 4,(HL)", 0b0000_0000, 16),
        new(0xA7, "RES 4,A", 0b0000_0000, 8),
        new(0xA8, "RES 5,B", 0b0000_0000, 8),
        new(0xA9, "RES 5,C", 0b0000_0000, 8),
        new(0xAA, "RES 5,D", 0b0000_0000, 8),
        new(0xAB, "RES 5,E", 0b0000_0000, 8),
        new(0xAC, "RES 5,H", 0b0000_0000, 8),
        new(0xAD, "RES 5,L", 0b0000_0000, 8),
        new(0xAE, "RES 5,(HL)", 0b0000_0000, 16),
        new(0xAF, "RES 5,A", 0b0000_0000, 8),
        new(0xB0, "RES 6,B", 0b0000_0000, 8),
        new(0xB1, "RES 6,C", 0b0000_0000, 8),
        new(0xB2, "RES 6,D", 0b0000_0000, 8),
        new(0xB3, "RES 6,E", 0b0000_0000, 8),
        new(0xB4, "RES 6,H", 0b0000_0000, 8),
        new(0xB5, "RES 6,L", 0b0000_0000, 8),
        new(0xB6, "RES 6,(HL)", 0b0000_0000, 16),
        new(0xB7, "RES 6,A", 0b0000_0000, 8),
        new(0xB8, "RES 7,B", 0b0000_0000, 8),
        new(0xB9, "RES 7,C", 0b0000_0000, 8),
        new(0xBA, "RES 7,D", 0b0000_0000, 8),
        new(0xBB, "RES 7,E", 0b0000_0000, 8),
        new(0xBC, "RES 7,H", 0b0000_0000, 8),
        new(0xBD, "RES 7,L", 0b0000_0000, 8),
        new(0xBE, "RES 7,(HL)", 0b0000_0000, 16),
        new(0xBF, "RES 7,A", 0b0000_0000, 8),
        new(0xC0, "SET 0,B", 0b0000_0000, 8),
        new(0xC1, "SET 0,C", 0b0000_0000, 8),
        new(0xC2, "SET 0,D", 0b0000_0000, 8),
        new(0xC3, "SET 0,E", 0b0000_0000, 8),
        new(0xC4, "SET 0,H", 0b0000_0000, 8),
        new(0xC5, "SET 0,L", 0b0000_0000, 8),
        new(0xC6, "SET 0,(HL)", 0b0000_0000, 16),
        new(0xC7, "SET 0,A", 0b0000_0000, 8),
        new(0xC8, "SET 1,B", 0b0000_0000, 8),
        new(0xC9, "SET 1,C", 0b0000_0000, 8),
        new(0xCA, "SET 1,D", 0b0000_0000, 8),
        new(0xCB, "SET 1,E", 0b0000_0000, 8),
        new(0xCC, "SET 1,H", 0b0000_0000, 8),
        new(0xCD, "SET 1,L", 0b0000_0000, 8),
        new(0xCE, "SET 1,(HL)", 0b0000_0000, 16),
        new(0xCF, "SET 1,A", 0b0000_0000, 8),
        new(0xD0, "SET 2,B", 0b0000_0000, 8),
        new(0xD1, "SET 2,C", 0b0000_0000, 8),
        new(0xD2, "SET 2,D", 0b0000_0000, 8),
        new(0xD3, "SET 2,E", 0b0000_0000, 8),
        new(0xD4, "SET 2,H", 0b0000_0000, 8),
        new(0xD5, "SET 2,L", 0b0000_0000, 8),
        new(0xD6, "SET 2,(HL)", 0b0000_0000, 16),
        new(0xD7, "SET 2,A", 0b0000_0000, 8),
        new(0xD8, "SET 3,B", 0b0000_0000, 8),
        new(0xD9, "SET 3,C", 0b0000_0000, 8),
        new(0xDA, "SET 3,D", 0b0000_0000, 8),
        new(0xDB, "SET 3,E", 0b0000_0000, 8),
        new(0xDC, "SET 3,H", 0b0000_0000, 8),
        new(0xDD, "SET 3,L", 0b0000_0000, 8),
        new(0xDE, "SET 3,(HL)", 0b0000_0000, 16),
        new(0xDF, "SET 3,A", 0b0000_0000, 8),
        new(0xE0, "SET 4,B", 0b0000_0000, 8),
        new(0xE1, "SET 4,C", 0b0000_0000, 8),
        new(0xE2, "SET 4,D", 0b0000_0000, 8),
        new(0xE3, "SET 4,E", 0b0000_0000, 8),
        new(0xE4, "SET 4,H", 0b0000_0000, 8),
        new(0xE5, "SET 4,L", 0b0000_0000, 8),
        new(0xE6, "SET 4,(HL)", 0b0000_0000, 16),
        new(0xE7, "SET 4,A", 0b0000_0000, 8),
        new(0xE8, "SET 5,B", 0b0000_0000, 8),
        new(0xE9, "SET 5,C", 0b0000_0000, 8),
        new(0xEA, "SET 5,D", 0b0000_0000, 8),
        new(0xEB, "SET 5,E", 0b0000_0000, 8),
        new(0xEC, "SET 5,H", 0b0000_0000, 8),
        new(0xED, "SET 5,L", 0b0000_0000, 8),
        new(0xEE, "SET 5,(HL)", 0b0000_0000, 16),
        new(0xEF, "SET 5,A", 0b0000_0000, 8),
        new(0xF0, "SET 6,B", 0b0000_0000, 8),
        new(0xF1, "SET 6,C", 0b0000_0000, 8),
        new(0xF2, "SET 6,D", 0b0000_0000, 8),
        new(0xF3, "SET 6,E", 0b0000_0000, 8),
        new(0xF4, "SET 6,H", 0b0000_0000, 8),
        new(0xF5, "SET 6,L", 0b0000_0000, 8),
        new(0xF6, "SET 6,(HL)", 0b0000_0000, 16),
        new(0xF7, "SET 6,A", 0b0000_0000, 8),
        new(0xF8, "SET 7,B", 0b0000_0000, 8),
        new(0xF9, "SET 7,C", 0b0000_0000, 8),
        new(0xFA, "SET 7,D", 0b0000_0000, 8),
        new(0xFB, "SET 7,E", 0b0000_0000, 8),
        new(0xFC, "SET 7,H", 0b0000_0000, 8),
        new(0xFD, "SET 7,L", 0b0000_0000, 8),
        new(0xFE, "SET 7,(HL)", 0b0000_0000, 16),
        new(0xFF, "SET 7,A", 0b0000_0000, 8),
    };
    
    private static InstructionInfo[] GenerateInstructionInfoTable() => new InstructionInfo[]
    {
        new(0x00, "NOP", 0b0000_0000, 4),
        new(0x01, "LD BC,u16", 0b0000_0000, 12),
        new(0x02, "LD (BC),A", 0b0000_0000, 8),
        new(0x03, "INC BC", 0b0000_0000, 8),
        new(0x04, "INC B", 0b1110_0000, 4),
        new(0x05, "DEC B", 0b1110_0000, 4),
        new(0x06, "LD B,u8", 0b0000_0000, 8),
        new(0x07, "RLCA", 0b1111_0000, 4),
        new(0x08, "LD (u16),SP", 0b0000_0000, 20),
        new(0x09, "ADD HL,BC", 0b0111_0000, 8),
        new(0x0A, "LD A,(BC)", 0b0000_0000, 8),
        new(0x0B, "DEC BC", 0b0000_0000, 8),
        new(0x0C, "INC C", 0b1110_0000, 4),
        new(0x0D, "DEC C", 0b1110_0000, 4),
        new(0x0E, "LD C,u8", 0b0000_0000, 8),
        new(0x0F, "RRCA", 0b1111_0000, 4),
        new(0x10, "STOP", 0b0000_0000, 4),
        new(0x11, "LD DE,u16", 0b0000_0000, 12),
        new(0x12, "LD (DE),A", 0b0000_0000, 8),
        new(0x13, "INC DE", 0b0000_0000, 8),
        new(0x14, "INC D", 0b1110_0000, 4),
        new(0x15, "DEC D", 0b1110_0000, 4),
        new(0x16, "LD D,u8", 0b0000_0000, 8),
        new(0x17, "RLA", 0b1111_0000, 4),
        new(0x18, "JR i8", 0b0000_0000, 12),
        new(0x19, "ADD HL,DE", 0b0111_0000, 8),
        new(0x1A, "LD A,(DE)", 0b0000_0000, 8),
        new(0x1B, "DEC DE", 0b0000_0000, 8),
        new(0x1C, "INC E", 0b1110_0000, 4),
        new(0x1D, "DEC E", 0b1110_0000, 4),
        new(0x1E, "LD E,u8", 0b0000_0000, 8),
        new(0x1F, "RRA", 0b1111_0000, 4),
        new(0x20, "JR NZ,i8", 0b0000_0000, 8),
        new(0x21, "LD HL,u16", 0b0000_0000, 12),
        new(0x22, "LD (HL+),A", 0b0000_0000, 8),
        new(0x23, "INC HL", 0b0000_0000, 8),
        new(0x24, "INC H", 0b1110_0000, 4),
        new(0x25, "DEC H", 0b1110_0000, 4),
        new(0x26, "LD H,u8", 0b0000_0000, 8),
        new(0x27, "DAA", 0b1011_0000, 4),
        new(0x28, "JR Z,i8", 0b0000_0000, 8),
        new(0x29, "ADD HL,HL", 0b0111_0000, 8),
        new(0x2A, "LD A,(HL+)", 0b0000_0000, 8),
        new(0x2B, "DEC HL", 0b0000_0000, 8),
        new(0x2C, "INC L", 0b1110_0000, 4),
        new(0x2D, "DEC L", 0b1110_0000, 4),
        new(0x2E, "LD L,u8", 0b0000_0000, 8),
        new(0x2F, "CPL", 0b0110_0000, 4),
        new(0x30, "JR NC,i8", 0b0000_0000, 8),
        new(0x31, "LD SP,u16", 0b0000_0000, 12),
        new(0x32, "LD (HL-),A", 0b0000_0000, 8),
        new(0x33, "INC SP", 0b0000_0000, 8),
        new(0x34, "INC (HL)", 0b1110_0000, 12),
        new(0x35, "DEC (HL)", 0b1110_0000, 12),
        new(0x36, "LD (HL),u8", 0b0000_0000, 12),
        new(0x37, "SCF", 0b0111_0000, 4),
        new(0x38, "JR C,i8", 0b0000_0000, 8),
        new(0x39, "ADD HL,SP", 0b0111_0000, 8),
        new(0x3A, "LD A,(HL-)", 0b0000_0000, 8),
        new(0x3B, "DEC SP", 0b0000_0000, 8),
        new(0x3C, "INC A", 0b1110_0000, 4),
        new(0x3D, "DEC A", 0b1110_0000, 4),
        new(0x3E, "LD A,u8", 0b0000_0000, 8),
        new(0x3F, "CCF", 0b0111_0000, 4),
        new(0x40, "LD B,B", 0b0000_0000, 4),
        new(0x41, "LD B,C", 0b0000_0000, 4),
        new(0x42, "LD B,D", 0b0000_0000, 4),
        new(0x43, "LD B,E", 0b0000_0000, 4),
        new(0x44, "LD B,H", 0b0000_0000, 4),
        new(0x45, "LD B,L", 0b0000_0000, 4),
        new(0x46, "LD B,(HL)", 0b0000_0000, 8),
        new(0x47, "LD B,A", 0b0000_0000, 4),
        new(0x48, "LD C,B", 0b0000_0000, 4),
        new(0x49, "LD C,C", 0b0000_0000, 4),
        new(0x4A, "LD C,D", 0b0000_0000, 4),
        new(0x4B, "LD C,E", 0b0000_0000, 4),
        new(0x4C, "LD C,H", 0b0000_0000, 4),
        new(0x4D, "LD C,L", 0b0000_0000, 4),
        new(0x4E, "LD C,(HL)", 0b0000_0000, 8),
        new(0x4F, "LD C,A", 0b0000_0000, 4),
        new(0x50, "LD D,B", 0b0000_0000, 4),
        new(0x51, "LD D,C", 0b0000_0000, 4),
        new(0x52, "LD D,D", 0b0000_0000, 4),
        new(0x53, "LD D,E", 0b0000_0000, 4),
        new(0x54, "LD D,H", 0b0000_0000, 4),
        new(0x55, "LD D,L", 0b0000_0000, 4),
        new(0x56, "LD D,(HL)", 0b0000_0000, 8),
        new(0x57, "LD D,A", 0b0000_0000, 4),
        new(0x58, "LD E,B", 0b0000_0000, 4),
        new(0x59, "LD E,C", 0b0000_0000, 4),
        new(0x5A, "LD E,D", 0b0000_0000, 4),
        new(0x5B, "LD E,E", 0b0000_0000, 4),
        new(0x5C, "LD E,H", 0b0000_0000, 4),
        new(0x5D, "LD E,L", 0b0000_0000, 4),
        new(0x5E, "LD E,(HL)", 0b0000_0000, 8),
        new(0x5F, "LD E,A", 0b0000_0000, 4),
        new(0x60, "LD H,B", 0b0000_0000, 4),
        new(0x61, "LD H,C", 0b0000_0000, 4),
        new(0x62, "LD H,D", 0b0000_0000, 4),
        new(0x63, "LD H,E", 0b0000_0000, 4),
        new(0x64, "LD H,H", 0b0000_0000, 4),
        new(0x65, "LD H,L", 0b0000_0000, 4),
        new(0x66, "LD H,(HL)", 0b0000_0000, 8),
        new(0x67, "LD H,A", 0b0000_0000, 4),
        new(0x68, "LD L,B", 0b0000_0000, 4),
        new(0x69, "LD L,C", 0b0000_0000, 4),
        new(0x6A, "LD L,D", 0b0000_0000, 4),
        new(0x6B, "LD L,E", 0b0000_0000, 4),
        new(0x6C, "LD L,H", 0b0000_0000, 4),
        new(0x6D, "LD L,L", 0b0000_0000, 4),
        new(0x6E, "LD L,(HL)", 0b0000_0000, 8),
        new(0x6F, "LD L,A", 0b0000_0000, 4),
        new(0x70, "LD (HL),B", 0b0000_0000, 8),
        new(0x71, "LD (HL),C", 0b0000_0000, 8),
        new(0x72, "LD (HL),D", 0b0000_0000, 8),
        new(0x73, "LD (HL),E", 0b0000_0000, 8),
        new(0x74, "LD (HL),H", 0b0000_0000, 8),
        new(0x75, "LD (HL),L", 0b0000_0000, 8),
        new(0x76, "HALT", 0b0000_0000, 4),
        new(0x77, "LD (HL),A", 0b0000_0000, 8),
        new(0x78, "LD A,B", 0b0000_0000, 4),
        new(0x79, "LD A,C", 0b0000_0000, 4),
        new(0x7A, "LD A,D", 0b0000_0000, 4),
        new(0x7B, "LD A,E", 0b0000_0000, 4),
        new(0x7C, "LD A,H", 0b0000_0000, 4),
        new(0x7D, "LD A,L", 0b0000_0000, 4),
        new(0x7E, "LD A,(HL)", 0b0000_0000, 8),
        new(0x7F, "LD A,A", 0b0000_0000, 4),
        new(0x80, "ADD A,B", 0b1111_0000, 4),
        new(0x81, "ADD A,C", 0b1111_0000, 4),
        new(0x82, "ADD A,D", 0b1111_0000, 4),
        new(0x83, "ADD A,E", 0b1111_0000, 4),
        new(0x84, "ADD A,H", 0b1111_0000, 4),
        new(0x85, "ADD A,L", 0b1111_0000, 4),
        new(0x86, "ADD A,(HL)", 0b1111_0000, 8),
        new(0x87, "ADD A,A", 0b1111_0000, 4),
        new(0x88, "ADC A,B", 0b1111_0000, 4),
        new(0x89, "ADC A,C", 0b1111_0000, 4),
        new(0x8A, "ADC A,D", 0b1111_0000, 4),
        new(0x8B, "ADC A,E", 0b1111_0000, 4),
        new(0x8C, "ADC A,H", 0b1111_0000, 4),
        new(0x8D, "ADC A,L", 0b1111_0000, 4),
        new(0x8E, "ADC A,(HL)", 0b1111_0000, 8),
        new(0x8F, "ADC A,A", 0b1111_0000, 4),
        new(0x90, "SUB A,B", 0b1111_0000, 4),
        new(0x91, "SUB A,C", 0b1111_0000, 4),
        new(0x92, "SUB A,D", 0b1111_0000, 4),
        new(0x93, "SUB A,E", 0b1111_0000, 4),
        new(0x94, "SUB A,H", 0b1111_0000, 4),
        new(0x95, "SUB A,L", 0b1111_0000, 4),
        new(0x96, "SUB A,(HL)", 0b1111_0000, 8),
        new(0x97, "SUB A,A", 0b1111_0000, 4),
        new(0x98, "SBC A,B", 0b1111_0000, 4),
        new(0x99, "SBC A,C", 0b1111_0000, 4),
        new(0x9A, "SBC A,D", 0b1111_0000, 4),
        new(0x9B, "SBC A,E", 0b1111_0000, 4),
        new(0x9C, "SBC A,H", 0b1111_0000, 4),
        new(0x9D, "SBC A,L", 0b1111_0000, 4),
        new(0x9E, "SBC A,(HL)", 0b1111_0000, 8),
        new(0x9F, "SBC A,A", 0b1111_0000, 4),
        new(0xA0, "AND A,B", 0b1111_0000, 4),
        new(0xA1, "AND A,C", 0b1111_0000, 4),
        new(0xA2, "AND A,D", 0b1111_0000, 4),
        new(0xA3, "AND A,E", 0b1111_0000, 4),
        new(0xA4, "AND A,H", 0b1111_0000, 4),
        new(0xA5, "AND A,L", 0b1111_0000, 4),
        new(0xA6, "AND A,(HL)", 0b1111_0000, 8),
        new(0xA7, "AND A,A", 0b1111_0000, 4),
        new(0xA8, "XOR A,B", 0b1111_0000, 4),
        new(0xA9, "XOR A,C", 0b1111_0000, 4),
        new(0xAA, "XOR A,D", 0b1111_0000, 4),
        new(0xAB, "XOR A,E", 0b1111_0000, 4),
        new(0xAC, "XOR A,H", 0b1111_0000, 4),
        new(0xAD, "XOR A,L", 0b1111_0000, 4),
        new(0xAE, "XOR A,(HL)", 0b1111_0000, 8),
        new(0xAF, "XOR A,A", 0b1111_0000, 4),
        new(0xB0, "OR A,B", 0b1111_0000, 4),
        new(0xB1, "OR A,C", 0b1111_0000, 4),
        new(0xB2, "OR A,D", 0b1111_0000, 4),
        new(0xB3, "OR A,E", 0b1111_0000, 4),
        new(0xB4, "OR A,H", 0b1111_0000, 4),
        new(0xB5, "OR A,L", 0b1111_0000, 4),
        new(0xB6, "OR A,(HL)", 0b1111_0000, 8),
        new(0xB7, "OR A,A", 0b1111_0000, 4),
        new(0xB8, "CP A,B", 0b1111_0000, 4),
        new(0xB9, "CP A,C", 0b1111_0000, 4),
        new(0xBA, "CP A,D", 0b1111_0000, 4),
        new(0xBB, "CP A,E", 0b1111_0000, 4),
        new(0xBC, "CP A,H", 0b1111_0000, 4),
        new(0xBD, "CP A,L", 0b1111_0000, 4),
        new(0xBE, "CP A,(HL)", 0b1111_0000, 8),
        new(0xBF, "CP A,A", 0b1111_0000, 4),
        new(0xC0, "RET NZ", 0b0000_0000, 8),
        new(0xC1, "POP BC", 0b0000_0000, 12),
        new(0xC2, "JP NZ,u16", 0b0000_0000, 12),
        new(0xC3, "JP u16", 0b0000_0000, 16),
        new(0xC4, "CALL NZ,u16", 0b0000_0000, 12),
        new(0xC5, "PUSH BC", 0b0000_0000, 16),
        new(0xC6, "ADD A,u8", 0b1111_0000, 8),
        new(0xC7, "RST 00h", 0b0000_0000, 16),
        new(0xC8, "RET Z", 0b0000_0000, 8),
        new(0xC9, "RET", 0b0000_0000, 16),
        new(0xCA, "JP Z,u16", 0b0000_0000, 12),
        new(0xCB, "PREFIX CB", 0b0000_0000, 4),
        new(0xCC, "CALL Z,u16", 0b0000_0000, 12),
        new(0xCD, "CALL u16", 0b0000_0000, 24),
        new(0xCE, "ADC A,u8", 0b1111_0000, 8),
        new(0xCF, "RST 08h", 0b0000_0000, 16),
        new(0xD0, "RET NC", 0b0000_0000, 8),
        new(0xD1, "POP DE", 0b0000_0000, 12),
        new(0xD2, "JP NC,u16", 0b0000_0000, 12),
        new(0xD3, "Illegal Operation", 0000, 0),
        new(0xD4, "CALL NC,u16", 0b0000_0000, 12),
        new(0xD5, "PUSH DE", 0b0000_0000, 16),
        new(0xD6, "SUB A,u8", 0b1111_0000, 8),
        new(0xD7, "RST 10h", 0b0000_0000, 16),
        new(0xD8, "RET C", 0b0000_0000, 8),
        new(0xD9, "RETI", 0b0000_0000, 16),
        new(0xDA, "JP C,u16", 0b0000_0000, 12),
        new(0xDB, "Illegal Operation", 0000, 0),
        new(0xDC, "CALL C,u16", 0b0000_0000, 12),
        new(0xDD, "Illegal Operation", 0000, 0),
        new(0xDE, "SBC A,u8", 0b1111_0000, 8),
        new(0xDF, "RST 18h", 0b0000_0000, 16),
        new(0xE0, "LD (FF00+u8),A", 0b0000_0000, 12),
        new(0xE1, "POP HL", 0b0000_0000, 12),
        new(0xE2, "LD (FF00+C),A", 0b0000_0000, 8),
        new(0xE3, "Illegal Operation", 0000, 0),
        new(0xE4, "Illegal Operation", 0000, 0),
        new(0xE5, "PUSH HL", 0b0000_0000, 16),
        new(0xE6, "AND A,u8", 0b1111_0000, 8),
        new(0xE7, "RST 20h", 0b0000_0000, 16),
        new(0xE8, "ADD SP,i8", 0b1111_0000, 16),
        new(0xE9, "JP HL", 0b0000_0000, 4),
        new(0xEA, "LD (u16),A", 0b0000_0000, 16),
        new(0xEB, "Illegal Operation", 0000, 0),
        new(0xEC, "Illegal Operation", 0000, 0),
        new(0xED, "Illegal Operation", 0000, 0),
        new(0xEE, "XOR A,u8", 0b1111_0000, 8),
        new(0xEF, "RST 28h", 0b0000_0000, 16),
        new(0xF0, "LD A,(FF00+u8)", 0b0000_0000, 12),
        new(0xF1, "POP AF", 0b0000_0000, 12),
        new(0xF2, "LD A,(FF00+C)", 0b0000_0000, 8),
        new(0xF3, "DI", 0b0000_0000, 4),
        new(0xF4, "Illegal Operation", 0000, 0),
        new(0xF5, "PUSH AF", 0b0000_0000, 16),
        new(0xF6, "OR A,u8", 0b1111_0000, 8),
        new(0xF7, "RST 30h", 0b0000_0000, 16),
        new(0xF8, "LD HL,SP+i8", 0b1111_0000, 12),
        new(0xF9, "LD SP,HL", 0b0000_0000, 8),
        new(0xFA, "LD A,(u16)", 0b0000_0000, 16),
        new(0xFB, "EI", 0b0000_0000, 4),
        new(0xFC, "Illegal Operation", 0000, 0),
        new(0xFD, "Illegal Operation", 0000, 0),
        new(0xFE, "CP A,u8", 0b1111_0000, 8),
        new(0xFF, "RST 38h", 0b0000_0000, 16),
    };
    
    /*
     * Instruction implementation
     * It is expected behavior that any callers of these functions mask, clear, and apply _internalF properly
     * per instruction. Problems will arise otherwise.
     * Example: the Dec/Inc instructions, which do not set the C flag, use the Sub/Add functions which do
     */
    
    // 8-bit alu
    private byte Add(byte a, byte b, bool c)
    {
        int carry = c ? 1 : 0;
        int temp = a + b + carry;

        _internalF.Z = (byte)temp == 0;
        _internalF.H = (((a & 0x0F) + (b & 0x0F) + carry) & 0x10) == 0x10;
        _internalF.C = (temp & 0x100) == 0x100;

        return (byte)temp;
    }

    private byte And(byte a, byte b)
    {
        a &= b;

        _internalF.Z = a == 0;
        _internalF.H = true;

        return a;
    }

    private byte Dec(byte v) => Sub(v, 1, false);

    private byte Inc(byte v) => Add(v, 1, false);

    private byte Or(byte a, byte b)
    {
        a |= b;

        _internalF.Z = a == 0;

        return a;
    }
    
    private byte Sub(byte a, byte b, bool c)
    {
        int carry = c ? 1 : 0;
        int temp = a - b - carry;

        _internalF.Z = (byte)temp == 0;
        _internalF.N = true;
        _internalF.H = (((a & 0x0F) - (b & 0x0F) - carry) & 0x10) == 0x10;
        _internalF.C = (temp & 0x100) == 0x100;

        return (byte)temp;
    }
    
    private byte Xor(byte a, byte b)
    {
        a ^= b;

        _internalF.Z = a == 0;

        return a;
    }
    
    // 16-bit alu
    private ushort Add(ushort a, ushort b)
    {
        int temp = a + b;

        _internalF.H = (((a & 0x0FFF) + (b & 0x0FFF)) & 0x1000) == 0x1000;
        _internalF.C = (temp & 0x10000) == 0x10000;

        return (ushort)temp;
    }

    private ushort Add(ushort a, sbyte b)
    {
        _internalF.H = (((a & 0x0F) + (b & 0x0F)) & 0x10) == 0x10;
        _internalF.C = (((a & 0xFF) + (b & 0xFF)) & 0x100) == 0x100;

        return (ushort)(a + b);
    }
    
    // 8-bit bit ops
    private void Bit(byte v, int bit)
    {
        _internalF.Z = (v & (1 << bit)) == 0;
        _internalF.H = true;
    }

    private byte SetBit(byte v, int bit, bool set)
    {
        byte mask = (byte)(1 << bit);

        return (byte)(set ? v | mask : v & ~mask);
    }

    private byte Swap(byte v)
    {
        v = (byte)((v << 4) | (v >> 4));

        _internalF.Z = v == 0;

        return v;
    }

    private byte Rl(byte v, bool c, bool setZ)
    {
        int carry = c ? 1 : 0;
        int temp = (v << 1) | carry;

        _internalF.Z = (byte)temp == 0 && setZ;
        _internalF.C = (temp & 0x100) != 0;

        return (byte)temp;
    }

    private byte Rlc(byte v, bool setZ)
    {
        int temp = (v << 1) | (v >> 7);

        _internalF.Z = (byte)temp == 0 && setZ;
        _internalF.C = (temp & 0x100) == 0x100;

        return (byte)temp;
    }

    private byte Rr(byte v, bool c, bool setZ)
    {
        int carry = c ? 0x80 : 0;
        int temp = (v >> 1) | carry;

        _internalF.Z = (byte)temp == 0 && setZ;
        _internalF.C = (v & 1) == 1;

        return (byte)temp;
    }

    private byte Rrc(byte v, bool setZ)
    {
        int temp = (v >> 1) | (v << 7);

        _internalF.Z = (byte)temp == 0 && setZ;
        _internalF.C = (v & 1) == 1;

        return (byte)temp;
    }

    private byte Sla(byte v)
    {
        _internalF.C = (v & 0x80) == 0x80;

        v <<= 1;

        _internalF.Z = v == 0;

        return v;
    }

    private byte Sra(byte v)
    {
        _internalF.C = (v & 1) == 1;

        v = (byte)((v >> 1) | (v & 0x80));

        _internalF.Z = v == 0;

        return v;
    }

    private byte Srl(byte v)
    {
        _internalF.C = (v & 1) == 1;

        v >>= 1;

        _internalF.Z = v == 0;

        return v;
    }
    
    // Branch

    private bool EvaluateBranch(BranchCondition cc, IRegisterPage page, int branchTime)
    {
        bool branch = cc switch
        {
            BranchCondition.C => ((StatusRegister)page.F).C,
            BranchCondition.NC => !((StatusRegister)page.F).C,
            BranchCondition.Z => ((StatusRegister)page.F).Z,
            BranchCondition.NZ => !((StatusRegister)page.F).Z,
            BranchCondition.None => true,
            _ => throw new InvalidEnumArgumentException()
        };
        
        if (!branch)
            return false;

        if (cc != BranchCondition.None)
            WorkTime += branchTime;
        
        return true;
    }
    
    private void Call(ushort addr, BranchCondition cc, IRegisterPage page, IBus bus)
    {
        if (!EvaluateBranch(cc, page, 3)) 
            return;

        Push(page.PC, page, bus);
        page.PC = addr;
    }
    
    private void Jp(ushort addr,  BranchCondition cc, IRegisterPage page)
    {
        if (!EvaluateBranch(cc, page, 1))
            return;

        page.PC = addr;
    }

    private void Jr(sbyte offset, BranchCondition cc, IRegisterPage page)
    {
        if (!EvaluateBranch(cc, page, 1))
            return;

        page.PC = (ushort)(page.PC + offset);
    }

    private void Ret(BranchCondition cc, IRegisterPage page, IBus bus)
    {
        if (!EvaluateBranch(cc, page, 3))
            return;

        page.PC = Pop(page, bus);
    }

    private void Reti()
    {
        throw new NotImplementedException();
    }

    private void Ccf(bool c)
    {
        _internalF.C = !c;
    }

    private byte Cpl(byte v)
    {
        v = (byte)~v;

        _internalF.N = true;
        _internalF.H = true;

        return v;
    }

    // This function is from https://ehaskins.com/2018-01-30%20Z80%20DAA/
    private byte Daa(byte a, StatusRegister r)
    {
        int correction = 0;

        bool setFlagC = false;
        if (r.H || (!r.N && (a & 0xf) > 9))
            correction |= 0x6;

        if (r.C || (!r.N && a > 0x99))
        {
            correction |= 0x60;
            setFlagC = true;
        }

        a += (byte)(r.N ? -correction : correction);

        _internalF.Z = a == 0;
        _internalF.C = setFlagC;

        return a;
    }

    private void Di()
    {
        
    }

    private void Ei() 
    {
        
    }

    private void Halt() => throw new NotImplementedException();
    
    private void Nop() {}

    private void Scf()
    {
        _internalF.C = true;
    }

    private void Stop() => throw new NotImplementedException();

    // Stack
    private ushort Pop(IRegisterPage page, IBus bus)
    {
        byte low = bus.ReadByte(page.SP++);
        return IntegerHelper.JoinBytes(bus.ReadByte(page.SP++), low);
    }
    
    private void Push(ushort value, IRegisterPage page, IBus bus)
    {
        (byte high, byte low) = IntegerHelper.SplitShort(value);
        
        bus.Write(--page.SP, high);
        bus.Write(--page.SP, low);
    }
    
    // Misc
    private void Illegal() => throw new NotImplementedException();

    private enum BranchCondition
    {
        C,
        NC,
        Z,
        NZ,
        None
    }
}