namespace GBV.Core.Processor;

public record InstructionInfo(byte Opcode, CpuFlags FlagMask);

public record BranchInstructionInfo(byte Opcode, CpuFlags FlagMask, int BranchTime) : 
    InstructionInfo(Opcode, FlagMask);