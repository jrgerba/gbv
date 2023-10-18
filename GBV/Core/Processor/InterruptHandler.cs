using System.ComponentModel;
using GBV.Core.Bus;

namespace GBV.Core.Processor;

[Flags]
public enum Interrupt : byte
{
    None = 0,
    VBlank = 1,
    LCD = 1 << 1,
    Timer = 1 << 2,
    Serial = 1 << 3,
    Joypad = 1 << 4
}

public ref struct InterruptHandler
{
    public const int ISRTime = 5;
    
    private ref bool _ime;
    private IBus _bus;
    
    public ushort GetInterruptVector(Interrupt interrupt) => interrupt switch
    {
        Interrupt.VBlank => MemoryMap.IntVBlank,
        Interrupt.LCD => MemoryMap.IntStat,
        Interrupt.Timer => MemoryMap.IntTimer,
        Interrupt.Serial => MemoryMap.IntSerial,
        Interrupt.Joypad => MemoryMap.IntJoypad,
        _ => throw new InvalidEnumArgumentException()
    };

    public void HandleInterrupt(Interrupt interrupt, IRegisterPage page, ref int workTime)
    {
        // Push address to stack
        _bus.Write(page.SP -= 2, page.PC);
        FlaggedInterrupts &= ~interrupt;
        _ime = false;
        workTime += ISRTime;
        page.PC = GetInterruptVector(interrupt);
    }
    
    public bool IsInterruptWaiting => WaitingInterrupts != Interrupt.None;

    public Interrupt NextInterrupt
    {
        get
        {
            Interrupt interrupt = WaitingInterrupts;

            return interrupt switch
            {
                _ when ((interrupt & Interrupt.VBlank) != Interrupt.None) => Interrupt.VBlank,
                _ when ((interrupt & Interrupt.LCD) != Interrupt.None) => Interrupt.LCD,
                _ when ((interrupt & Interrupt.Timer) != Interrupt.None) => Interrupt.Timer,
                _ when ((interrupt & Interrupt.Serial) != Interrupt.None) => Interrupt.Serial,
                _ when ((interrupt & Interrupt.Joypad) != Interrupt.None) => Interrupt.Joypad,
                _ => Interrupt.None
            };
        }
    }

    public Interrupt WaitingInterrupts => _ime ? FlaggedInterrupts & EnabledInterrupts : Interrupt.None;

    public Interrupt FlaggedInterrupts
    {
        get => (Interrupt)_bus.ReadByte(MemoryMap.IF);
        set => _bus.Write(MemoryMap.IF, (byte)value);
    }

    public Interrupt EnabledInterrupts
    {
        get => (Interrupt)_bus.ReadByte(MemoryMap.IE);
        set => _bus.Write(MemoryMap.IE, (byte)value);
    }

    public InterruptHandler(ref bool ime, IBus bus)
    {
        _ime = ref ime;
        _bus = bus;
    }
}