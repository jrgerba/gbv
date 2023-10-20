using GBV.Core.Processor;
using Timer = GBV.Core.Processor.Timer;

namespace GBV.Core.Bus;

public class MainBus : IBus
{
    public ICPU Processor { get; private set; }
    public Timer Timer { get; private set; }
    public InterruptHandler InterruptHandler { get; private set; }
    public int WorkTime { get; set; }

    private byte[] _memory = new byte[0x10000];

    public void Clock()
    {
        if (Processor.ExecutionState == ExecutionState.Running)
            Processor.Clock();
        
        if (Processor.ExecutionState == ExecutionState.Halt)
            WorkTime = 4;

        for (int i = 0; i < WorkTime; i++)
        {
            Timer.Clock();
            if (Timer.PendingInterrupt)
                InterruptHandler.IF |= Interrupt.Timer;
        }
        WorkTime = 0;
        
        if (Processor.ExecutionState == ExecutionState.Halt && InterruptHandler.IFIE != Interrupt.None)
            Processor.ExecutionState = ExecutionState.Running;
        
        InterruptHandler.Clock();
    }

    public void AttachCpu(ICPU processor) => Processor = processor;

    public void AttachTimer(Timer timer) => Timer = timer;

    public void AttachInterruptHandler(InterruptHandler handler) => InterruptHandler = handler;

    public byte ReadByte(ushort address) => address switch
    {
        MemoryMap.LY => 0x90,
        MemoryMap.IE or MemoryMap.IF => InterruptHandler.Read(address),
        > MemoryMap.TimerStart and < MemoryMap.TimerEnd => Timer.Read(address),
        _ => _memory[address]
    };

    public ushort ReadShort(ushort address)
    {
        byte low = ReadByte(address++);
        return IntegerHelper.JoinBytes(ReadByte(address), low);
    }

    public void Write(ushort address, byte value)
    {
        switch (address)
        {
            case MemoryMap.SB:
                Console.Write((char)value);
                goto default;
            case MemoryMap.IF:
            case MemoryMap.IE:
                InterruptHandler.Write(address, value);
                break;
            case >= MemoryMap.TimerStart and <= MemoryMap.TimerEnd:
                Timer.Write(address, value);
                break;
            default:
                _memory[address] = value;
                break;
        }
    }

    public void Write(ushort address, ushort value)
    {
        (byte high, byte low) = IntegerHelper.SplitShort(value);
        Write(address++, low);
        Write(address, high);
    }

    public void Reset()
    {
        Processor.RegisterPage.A = 0x01;
        Processor.RegisterPage.F = 0xB0;
        Processor.RegisterPage.B = 0x00;
        Processor.RegisterPage.C = 0x13;
        Processor.RegisterPage.D = 0x00;
        Processor.RegisterPage.E = 0xD8;
        Processor.RegisterPage.H = 0x01;
        Processor.RegisterPage.L = 0x4D;
        Processor.RegisterPage.SP = 0xFFFE;
        Processor.RegisterPage.PC = 0x0100;
    }
}