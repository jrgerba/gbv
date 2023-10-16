using System;
using System.IO;
using GBV.Core.Processor;
using NUnit.Framework;

namespace ProcessorTesting;

// Test logs received from https://github.com/wheremyfoodat/Gameboy-logs
// Test roms received from https://github.com/retrio/gb-test-roms

public class Tests
{
    private DMGEngineTestCPU _cpu;
    
    [SetUp]
    public void Setup()
    {
        _cpu = new DMGEngineTestCPU();
    }

    public void TestGeneral(string testStr)
    {
        StreamReader f = new StreamReader(File.OpenRead($"TestLogs/{testStr}.txt"));
        _cpu.LoadTestRom(testStr);

        int line = 0;
        
        string goodLogOld = "";
        string myLogOld = "";
        
        while (!f.EndOfStream)
        {
            string myLog = _cpu.ToString();
            string goodLog = f.ReadLine();
            line++;

            try
            {
                if (_cpu.Page.PC >= 0x100)
                    Assert.That(goodLog, Is.EqualTo(myLog));
            }
            catch (AssertionException e)
            {
                Console.WriteLine($"Executed {line} instructions");
                Console.WriteLine($"$Logs did not match:\nGood: {goodLog}\nTest: {myLog}");
                Console.WriteLine($"Previous state:\n\tGood: {goodLogOld}\n\tTest: {myLogOld}");
                throw;
            }

            if (_cpu.Page.PC >= 0x100)
            {
                goodLogOld = goodLog;
                myLogOld = myLog;

            }            _cpu.Clock();
        }
        Console.WriteLine($"Executed {line} instructions");
    }

    [Test]
    public void Test1() => TestGeneral("blargg1");
    [Test]
    public void Test2()
    {
        _cpu.Page.PC++;
        TestGeneral("blargg2");
    }

    [Test]
    public void Test3() => TestGeneral("blargg3");
    [Test]
    public void Test4() => TestGeneral("blargg4");
    [Test]
    public void Test5() => TestGeneral("blargg5");

    [Test]
    public void Test6()
    {
        _cpu.Page.PC++;
        TestGeneral("blargg6");
    }

    [Test]
    public void Test7() => TestGeneral("blargg7");
    [Test]
    public void Test8() => TestGeneral("blargg8");
    [Test]
    public void Test9() => TestGeneral("blargg9");
    [Test]
    public void Test10() => TestGeneral("blargg10");
    [Test]
    public void Test11() => TestGeneral("blargg11");
}