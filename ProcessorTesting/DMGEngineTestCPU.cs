using System.IO;
using System.Net;
using GBV.Core.Processor;

namespace ProcessorTesting;

public class DMGEngineTestCPU : ICPU
{
    private DMGEngine _engine = new();
    private TestBus _bus = new();
    private RegisterPage _page = new();
    
    public void Clock()
    {
        byte inst = _bus.ReadByte(_page.PC++);

        _engine.ExecuteInstruction(ref _page, _bus, inst);
    }

    public void LoadTestRom(string rom)
    {
        byte[] file = File.ReadAllBytes(@$"TestRoms\{rom}.gb");

        for (int i = 0; i < 0x2000; i++)
        {
            _bus.Write((ushort)i, file[i]);
        }
    }

    public override string ToString()
    {
        return $"A: {_page.A:XX} F {_page.F:XX} B: {_page.B:XX} C: {_page.C:XX} D: {_page.D:XX} E: {_page.E:XX} H: {_page.H:XX} L: {_page.L:XX} SP: {_page.SP:XXXX} PC: 00:{_page.PC:XXXX} ({_bus.ReadByte(_page.PC)} {_bus.ReadByte((ushort)(_page.PC+1))} {_bus.ReadByte((ushort)(_page.PC+2))} {_bus.ReadByte((ushort)(_page.PC+3))})";
    }
}