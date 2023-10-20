using GBV.Core.Bus;

namespace GBV.Core.Processor;

public class Timer : IWBusComponent, IRBusComponent, IEBusComponent
{
    private static readonly ushort[] TacMask =
    {
        0b10_0000_0000,
        0b1000,
        0b10_0000,
        0b1000_0000
    };

    private ushort _prevDiv;

    public bool PendingInterrupt { get; private set; } = false;
    public ushort Tima { get; set; }
    public byte Tma { get; set; }
    public byte Tac { get; set; }
    public ushort Div { get; set; }
    public byte TrimmedDiv => (byte)(Div >> 8);

    public void Write(ushort address, byte value)
    {
        switch (address)
        {
            case MemoryMap.Div:
                _prevDiv = Div;
                Div = 0;
                break;
            
            case MemoryMap.Tma:
                Tma = value;
                break;
            
            case MemoryMap.Tac:
                Tac = (byte)(value & 0x7);
                if (ShouldIncrementTima())
                    Tima++;
                break;
            
            case MemoryMap.Tima:
                Tima = value;
                break;
        }
    }

    public byte Read(ushort address) => address switch
    {
        MemoryMap.Div => TrimmedDiv,
        MemoryMap.Tma => Tma,
        MemoryMap.Tac => (byte)(Tac & 0x7),
        MemoryMap.Tima => (byte)Tima,
        _ => throw new UnhandledAddressException()
    };
    
    public void Clock()
    {
        PendingInterrupt = false;
        _prevDiv = Div;
        Div++;

        if (Tima > 0xFF)
        {
            Tima = Tma;
            PendingInterrupt = true;
        }
        
        if (ShouldIncrementTima())
            Tima++;
    }

    private bool ShouldIncrementTima() => 
        DetectFallingEdge(Div, _prevDiv, TacMask[Tac & 0b11]) && (Tac & 0b100) != 0;
    
    private bool DetectFallingEdge(ushort current, ushort previous, ushort mask) =>
        (previous & mask) != 0 && (current & mask) == 0;
}