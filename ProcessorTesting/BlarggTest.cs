using System;
using System.IO;
using GBV.Core.Bus;
using GBV.Core.Processor;
using NUnit.Framework;

namespace ProcessorTesting;

// Test logs received from https://github.com/wheremyfoodat/Gameboy-logs
// Test roms received from https://github.com/retrio/gb-test-roms

public class Tests
{
    private MainBus Bus;
    private byte[][] TestRoms = new byte[11][];
    private StreamReader[] TestLogs = new StreamReader[11];

    [SetUp]
    public void Setup()
    {
        Bus = new MainBus();

        Bus.AttachCpu(new GBProcessor(Bus));
        Bus.AttachTimer(new Timer());
        Bus.AttachInterruptHandler(new InterruptHandler(Bus));
        
        Bus.Reset();

        for (int i = 1; i < 12; i++)
        {
            TestRoms[i - 1] = File.ReadAllBytes($"TestRoms/blargg{i}.gb");
            TestLogs[i - 1] = new StreamReader(File.OpenRead($"TestLogs/blargg{i}.txt"));
        }
    }

    private (bool, int) CompareTest(StreamReader reader, bool ignoreBoot = true)
    {
        int count = 0;
        string oldTest = "", oldGood = "";
        
        while (!reader.EndOfStream)
        {
            if (Bus.Processor.RegisterPage.PC < 0x100 && ignoreBoot)
            {
                Bus.Clock();
                continue;
            }
            
            string test = Bus.Processor.ToString(), 
                good = reader.ReadLine();


            if (test != good)
            {
                Console.WriteLine($"Good case: {good}\nTest case: {test}");
                Console.WriteLine($"\tPrevious State:\n\tGood case: {oldGood}\n\tTest case: {oldTest}");
                return (false, count);
            }

            Bus.Clock();
            count++;

            oldTest = test;
            oldGood = good;
        }

        return (true, count);
    }
    
    private void TestGeneral(int tCase, bool ignoreBoot = true)
    {
        int i = 0;
        foreach (byte b in TestRoms[tCase])
            Bus.Write((ushort)i++, b);

        (bool passed, int count) = CompareTest(TestLogs[tCase], ignoreBoot);
        
        Console.WriteLine($"Executed {count} instructions");
        
        Assert.That(passed, Is.True);
    }

    [Test]
    public void Test1() => TestGeneral(0);
    [Test]
    public void Test2()
    {
        Bus.Processor.RegisterPage.PC++;
        TestGeneral(1, false);
    }

    [Test]
    public void Test3() => TestGeneral(2);
    [Test]
    public void Test4() => TestGeneral(3);
    [Test]
    public void Test5() => TestGeneral(4);

    [Test]
    public void Test6()
    {
        Bus.Processor.RegisterPage.PC++;
        TestGeneral(5);
    }

    [Test]
    public void Test7() => TestGeneral(6);
    [Test]
    public void Test8() => TestGeneral(7);
    [Test]
    public void Test9() => TestGeneral(8);
    [Test]
    public void Test10() => TestGeneral(9);
    [Test]
    public void Test11() => TestGeneral(10);
}