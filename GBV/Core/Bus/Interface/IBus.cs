using GBV.Core.Processor;
using Timer = GBV.Core.Processor.Timer;

namespace GBV.Core.Bus;

public interface IBus
{
    public ICPU Processor { get; }
    public Timer Timer { get; }
    public InterruptHandler InterruptHandler { get; }
    public int WorkTime { get; set; }
    
    byte ReadByte(ushort address);
    
    ushort ReadShort(ushort address);
    
    void Write(ushort address, byte value);
    
    void Write(ushort address, ushort value);

    public void Clock();

    public void AttachCpu(ICPU processor);
    public void AttachTimer(Timer timer);
    public void AttachInterruptHandler(InterruptHandler handler);
    
}