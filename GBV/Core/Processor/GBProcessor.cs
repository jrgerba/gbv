using GBV.Core.Bus;

namespace GBV.Core.Processor;

public class GBProcessor : ICPU, IRBusComponent, IWBusComponent, IEBusComponent
{
    public readonly IBus Bus;
    public readonly DMGEngine Engine = new();
    
    public IRegisterPage RegisterPage { get; private set; } = new RegisterPage();

    public void Clock()
    {
        byte inst = Bus.ReadByte(RegisterPage.PC++);
        
        Engine.Execute(inst, RegisterPage, Bus);

        Bus.WorkTime += Engine.WorkTime;
    }

    public GBProcessor(IBus bus)
    {
        Bus = bus;
    }

    public byte Read(ushort address)
    {
        throw new NotImplementedException();
    }

    public void Write(ushort address, byte value)
    {
        throw new NotImplementedException();
    }
    
    public override string ToString() =>
        string.Format(
            "A: {0:X2} F: {1:X2} B: {2:X2} C: {3:X2} D: {4:X2} E: {5:X2} H: {6:X2} L: {7:X2} SP: {8:X4} PC: 00:{9:X4} ({10:X2} {11:X2} {12:X2} {13:X2})",
            RegisterPage.A,
            RegisterPage.F,
            RegisterPage.B,
            RegisterPage.C,
            RegisterPage.D,
            RegisterPage.E,
            RegisterPage.H,
            RegisterPage.L,
            RegisterPage.SP,
            RegisterPage.PC,
            Bus.ReadByte(RegisterPage.PC),
            Bus.ReadByte((ushort)(RegisterPage.PC + 1)),
            Bus.ReadByte((ushort)(RegisterPage.PC + 2)),
            Bus.ReadByte((ushort)(RegisterPage.PC + 3))
        );
}