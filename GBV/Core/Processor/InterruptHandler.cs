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
    private const ushort InterruptEnableAddress = 0xFFFF;
    private const ushort InterruptPendingAddress = 0xFF0F;

    public ushort GetInterruptVector(Interrupt interrupt) => interrupt switch
    {
        Interrupt.VBlank => 0x40,
        Interrupt.LCD => 0x48,
        Interrupt.Timer => 0x50,
        Interrupt.Serial => 0x58,
        Interrupt.Joypad => 0x60,
        _ => throw new InvalidEnumArgumentException()
    };

    public void HandleInterrupt(Interrupt interrupt, IRegisterPage page, ref int workTime)
    {
        // Push address to stack
        _bus.Write(page.SP -= 2, --page.PC);
        PendingInterrupts &= ~interrupt;
        workTime = ISRTime;
        _ime = false;
        page.PC = GetInterruptVector(interrupt);
    }
    
    public bool IsInterruptWaiting => WaitingInterrupts != Interrupt.None && _ime;

    public Interrupt NextInterrupt
    {
        get
        {
            Interrupt interrupts = WaitingInterrupts;
            
            for (int i = 0; i < 5; i++)
            {
                if ((((int)interrupts >> i) & 1) == 1)
                    return (Interrupt)(0x10 >> (4 - i));
            }

            return Interrupt.None;
        }
    }

    public Interrupt WaitingInterrupts => PendingInterrupts & EnabledInterrupts;

    public Interrupt PendingInterrupts
    {
        get => (Interrupt)_bus.ReadByte(InterruptPendingAddress);
        set => _bus.Write(InterruptPendingAddress, (byte)value);
    }

    public Interrupt EnabledInterrupts
    {
        get => (Interrupt)_bus.ReadByte(InterruptPendingAddress);
        set => _bus.Write(InterruptPendingAddress, (byte)value);
    }

    public InterruptHandler(ref bool ime, IBus bus)
    {
        _ime = ref ime;
        _bus = bus;
    }
}