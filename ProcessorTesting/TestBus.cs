using System;
using GBV;
using GBV.Core.Bus;

namespace ProcessorTesting;

public class TestBus : IBus
{
    private byte[] _data = new byte[0x10000];

    public byte ReadByte(ushort address) => _data[address];

    public ushort ReadShort(ushort address)
    {
        byte low = ReadByte(address++);
        byte high = ReadByte(address);

        return IntegerHelper.JoinBytes(high, low);
    }

    public void Write(ushort address, byte value)
    {
        if (address == 0xFF01)
            Console.Write((char)value);
        
        _data[address] = value;
    }

    public void Write(ushort address, ushort value)
    {
        (byte high, byte low) = IntegerHelper.SplitShort(value);
        
        Write(address++, low);
        Write(address, high);
    }
}