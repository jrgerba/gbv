using System.IO;
using System.Net;
using GBV.Core;
using GBV.Core.Processor;
using NUnit.Framework;

namespace ProcessorTesting;

public class DMGEngineTestCPU : ICPU
{
    private DMGEngine _engine = new();
    private TestBus _bus = new();
    private RegisterPage _page = new();

    public RegisterPage Page => _page;
    
    public void Clock()
    {
        byte inst = _bus.ReadByte(_page.PC++);

        _engine.Execute(inst, _page, _bus);
    }

    public void LoadTestRom(string rom)
    {
        byte[] file = File.ReadAllBytes(@$"TestRoms\{rom}.gb");

        for (int i = 0; i < 0x8000; i++)
        {
            _bus.Write((ushort)i, file[i]);
        }
    }

    public DMGEngineTestCPU()
    {
        _page.A = 0x01;
        _page.F = 0XB0;
        _page.B = 0x00;
        _page.C = 0x13;
        _page.D = 0x00;
        _page.E = 0xD8;
        _page.H = 0x01;
        _page.L = 0x4D;
        _page.SP = 0xFFFE;
        _page.PC = 0x0100;
    }

    public override string ToString()
    {
        return $"A: {_page.A:X2} F: {_page.F:X2} B: {_page.B:X2} C: {_page.C:X2} D: {_page.D:X2} E: {_page.E:X2} H: {_page.H:X2} L: {_page.L:X2} SP: {_page.SP:X4} PC: 00:{_page.PC:X4} ({_bus.ReadByte(_page.PC):X2} {_bus.ReadByte((ushort)(_page.PC+1)):X2} {_bus.ReadByte((ushort)(_page.PC+2)):X2} {_bus.ReadByte((ushort)(_page.PC+3)):X2})";
    }
}