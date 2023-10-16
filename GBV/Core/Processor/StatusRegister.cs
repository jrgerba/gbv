namespace GBV.Core.Processor;

public struct StatusRegister
{
    public static implicit operator StatusRegister(byte value) => new(value);
    public static explicit operator byte(StatusRegister value) => value.Value;

    public const byte BitZ = 0x80;
    public const byte BitN = 0x40;
    public const byte BitH = 0x20;
    public const byte BitC = 0x10;
    
    public byte Value { get; set; }
    
    public bool Z
    {
        get => (Value & BitZ) == BitZ;
        set => Value = (byte)(value ? Value | BitZ : Value & ~BitZ);
    }
    
    public bool N
    {
        get => (Value & BitN) == BitN;
        set => Value = (byte)(value ? Value | BitN : Value & ~BitN);
    }
    
    public bool H
    {
        get => (Value & BitH) == BitH;
        set => Value = (byte)(value ? Value | BitH : Value & ~BitH);
    }
    
    public bool C
    {
        get => (Value & BitC) == BitC;
        set => Value = (byte)(value ? Value | BitC : Value & ~BitC);
    }
    
    public StatusRegister(byte value)
    {
        Value = value;
    }
}