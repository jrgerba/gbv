namespace GBV.Core.Cartridge;

public class CartridgeHeaderMap
{
    public const ushort EntryPoint = 0x0100;
    public const ushort LogoStart = 0x0104;
    public const ushort LogoEnd = 0x0133;

    public const ushort TitleStart = 0x0134;
    public const ushort TitleEnd = 0x0143;

    public const ushort CGBFlag = 0x0143;
    public const ushort SGBFlag = 0x0146;
    
    public const ushort CartridgeType = 0x0147;
    public const ushort RomSize = 0x0148;
    public const ushort RamSize = 0x0149;

    public const ushort HeaderChecksum = 0x014D;
    public const ushort GlobalChecksumStart = 0x014E;
    public const ushort GlobalChecksumEnd = 0x014F;
}