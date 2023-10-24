using System.ComponentModel;

namespace GBV.Core.Cartridge;

public enum CartridgeType
{
    RomOnly = 0,
    Mbc1 = 0b0001,
    Mbc2 = 0b0010,
    Mbc3 = 0b0011,
    Mbc5 = 0b0100,
    Mbc6 = 0b0101,
    Mbc7 = 0b0110,
    Mmm01 = 0b0111,
    PocketCamera = 0b1000,
    BandaiTama5 = 0b1001,
    HuC3 = 0b1010,
    HuC1 = 0b1011,
    
    // Components
    Ram = 1 << 4,
    Battery = 1 << 5,
    Timer = 1 << 6,
    Rumble = 1 << 7,
    Sensor = 1 << 8
}

public static class CartridgeTypeExtension
{
    public static CartridgeType EnumerateType(byte type) => type switch
    {
        0 => CartridgeType.RomOnly,
        1 => CartridgeType.Mbc1,
        2 => CartridgeType.Mbc1 | CartridgeType.Ram,
        3 => CartridgeType.Mbc1 | CartridgeType.Ram | CartridgeType.Battery,
        5 => CartridgeType.Mbc2,
        6 => CartridgeType.Mbc2 | CartridgeType.Battery,
        8 => CartridgeType.RomOnly | CartridgeType.Ram,
        9 => CartridgeType.RomOnly | CartridgeType.Ram | CartridgeType.Battery,
        0xB => CartridgeType.Mmm01,
        0xC => CartridgeType.Mmm01 | CartridgeType.Ram,
        0xD => CartridgeType.Mmm01 | CartridgeType.Ram | CartridgeType.Battery,
        0xF => CartridgeType.Mbc3 | CartridgeType.Timer | CartridgeType.Battery,
        0x10 => CartridgeType.Mbc3 | CartridgeType.Timer | CartridgeType.Ram | CartridgeType.Battery,
        0x11 => CartridgeType.Mbc3,
        0x12 => CartridgeType.Mbc3 | CartridgeType.Ram,
        0x13 => CartridgeType.Mbc3 | CartridgeType.Ram | CartridgeType.Battery,
        0x19 => CartridgeType.Mbc5,
        0x1A => CartridgeType.Mbc5 | CartridgeType.Ram,
        0x1B => CartridgeType.Mbc5 | CartridgeType.Ram | CartridgeType.Battery,
        0x1C => CartridgeType.Mbc5 | CartridgeType.Rumble,
        0x1D => CartridgeType.Mbc5 | CartridgeType.Rumble | CartridgeType.Ram,
        0x1E => CartridgeType.Mbc5 | CartridgeType.Rumble | CartridgeType.Ram | CartridgeType.Battery,
        0x20 => CartridgeType.Mbc6,
        0x22 => CartridgeType.Mbc7 | CartridgeType.Sensor | CartridgeType.Rumble | CartridgeType.Ram |
                CartridgeType.Battery,
        0xFC => CartridgeType.PocketCamera,
        0xFD => CartridgeType.BandaiTama5,
        0xFE => CartridgeType.HuC3,
        0xFF => CartridgeType.HuC1 | CartridgeType.Ram | CartridgeType.Battery,
        _ => throw new InvalidEnumArgumentException()
    };
}