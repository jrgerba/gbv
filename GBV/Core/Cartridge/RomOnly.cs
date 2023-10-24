namespace GBV.Core.Cartridge;

public class RomOnly : ICartridge
{
    public int CurrentRomBankA => 0;
    public int CurrentRomBankB => 1;
    public string MBC => "N/A";
    public Span<byte> RawHeader => new Span<byte>(_rawData, 0, 0x150);

    private byte[] _rawData;
    
    public void Write(ushort address, byte value) { }

    public byte Read(ushort address)
    {
        return _rawData[address];
    }

    public void Clock() { }

    public RomOnly(byte[] data)
    {
        _rawData = data;
    }
}