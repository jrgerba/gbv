namespace GBV.Core.Bus;

public interface IBus
{
    byte ReadByte(ushort address);
    ushort ReadShort(ushort address);

    void Write(ushort address, byte value);
    void Write(ushort address, ushort value);
}