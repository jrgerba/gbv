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

public class InterruptHandler : IWBusComponent, IRBusComponent, IEBusComponent
{
    public const int ISRTime = 5;
    public bool IME { get; set; }
    private int _imeDelay;
    
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
        IF &= ~interrupt;
        IME = false;
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
                _ when (interrupt & Interrupt.VBlank) != Interrupt.None => Interrupt.VBlank,
                _ when (interrupt & Interrupt.LCD) != Interrupt.None => Interrupt.LCD,
                _ when (interrupt & Interrupt.Timer) != Interrupt.None => Interrupt.Timer,
                _ when (interrupt & Interrupt.Serial) != Interrupt.None => Interrupt.Serial,
                _ when (interrupt & Interrupt.Joypad) != Interrupt.None => Interrupt.Joypad,
                _ => Interrupt.None
            };
        }
    }

    public Interrupt WaitingInterrupts => IME ? IF & IE : Interrupt.None;

    public Interrupt IF { get; set; }

    public Interrupt IE { get; set; }

    public InterruptHandler(IBus bus)
    {
        _bus = bus;
    }

    public void Write(ushort address, byte value)
    {
        switch (address)
        {
            case MemoryMap.IF:
                IF = (Interrupt)value;
                break;
            
            case MemoryMap.IE:
                IE = (Interrupt)value;
                break;
            
            default:
                throw new UnhandledAddressException();
        }
    }

    public byte Read(ushort address) => address switch
    {
        MemoryMap.IF => (byte)IF,
        MemoryMap.IE => (byte)IE,
        _ => throw new UnhandledAddressException()
    };

    public void DelaySetIME()
    {
        _imeDelay = 2;
    }

    public void Clock()
    {
        if (IsInterruptWaiting)
        {
            Interrupt i = NextInterrupt;
            
            _bus.Write(_bus.Processor.RegisterPage.SP -= 2, _bus.Processor.RegisterPage.PC);
            _bus.WorkTime += 5;
            _bus.Processor.RegisterPage.PC = GetInterruptVector(i);
            IF &= ~i;
            IME = false;
        }

        if (_imeDelay == 2)
            _imeDelay--;
        else if (_imeDelay != 0)
        {
            _imeDelay = 0;
            IME = true;
        }
    }
}