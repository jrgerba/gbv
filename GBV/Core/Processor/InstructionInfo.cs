namespace GBV.Core.Processor;

public record InstructionInfo(byte Opcode, string Name, byte FlagMask, int BaseTime);