namespace GBV.Core.Processor;

public record InstructionInfo(byte Opcode, string Name, CpuFlags FlagMask, int BaseTime);

public record BranchInstructionInfo(byte Opcode, string Name, CpuFlags FlagMask, int BaseTime, int BranchTime) : 
    InstructionInfo(Opcode, Name, FlagMask, BaseTime);