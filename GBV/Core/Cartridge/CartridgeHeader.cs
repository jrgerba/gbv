namespace GBV.Core.Cartridge;

public ref struct CartridgeHeader
{
    public readonly ReadOnlySpan<byte> HeaderData;

    public int RamSize => HeaderData[CartridgeHeaderMap.RamSize];
    public int RomSize => HeaderData[CartridgeHeaderMap.RomSize];

    public CartridgeType CartridgeType =>
        CartridgeTypeExtension.EnumerateType(HeaderData[CartridgeHeaderMap.CartridgeType]);

    public byte StoredHeaderChecksum => HeaderData[CartridgeHeaderMap.HeaderChecksum];

    public ushort StoredGlobalChecksum => IntegerHelper.JoinBytes(HeaderData[CartridgeHeaderMap.GlobalChecksumEnd],
        HeaderData[CartridgeHeaderMap.GlobalChecksumStart]);

    public byte CalculatedChecksum
    {
        get
        {
            byte checkSum = 0;
            for (int i = 0x0134; i <= 0x014C; i++)
                checkSum -= (byte)(HeaderData[i] + 1);

            return checkSum;
        }
    }
    
    public CartridgeHeader(ReadOnlySpan<byte> headerData)
    {
        HeaderData = headerData;
    }
}