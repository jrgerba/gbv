namespace GBV.Core.Cartridge;

public interface SRam
{
    public bool SaveToDisk { get; }

    public void WriteByte(ushort address, byte value);
    public byte ReadByte(ushort address);
}