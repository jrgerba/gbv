using GBV.Core.Bus;

namespace GBV.Core.Cartridge;

public class Mbc1 : ICartridge
{
    public const ushort RamEnableStart = 0x0000;
    public const ushort RamEnableEnd = 0x1FFF;
    
    public readonly bool HasRam;
    public readonly bool HasBattery;
    public readonly int RomBankCount;
    public readonly int RamBankCount;
    public int CurrentRomBankA { get; private set; }
    public int CurrentRomBankB { get; private set; }
    public int CurrentRamBank { get; private set; }
    public string MBC { get; }
    public Span<byte> RawHeader => _rawData.AsSpan(0, 0x150);
    private byte[] _rawData;
    private byte _ramBankMask;
    private byte _romBankMask;
    private byte[,] _rom;

    private SRam _sram;
    
    public bool RamEnable { get; set; }
    
    public void Write(ushort address, byte value)
    {
        switch (address)
        {
            case > RamEnableStart and <= RamEnableEnd:
                RamEnable = (value & 0x0F) == 0x0A;
                break;
        }
    }

    public byte Read(ushort address)
    {
        throw new NotImplementedException();
    }

    private byte ReadRam(ushort address)
    {
        if (!RamEnable)
            return 0xFF;

        return _sram.ReadByte((ushort)((CurrentRamBank * MemoryMap.ERamLength) + address));
    }

    private byte ReadRom(ushort address)
    {
        if (address <= MemoryMap.Bank0End)
            return _rom[CurrentRomBankA, address - MemoryMap.Bank0Start];

        return _rom[CurrentRomBankB, MemoryMap.BankNStart - address];
    }

    private void WriteRam(ushort address, byte value)
    {
        if (!RamEnable)
            return;
        address -= MemoryMap.ERamStart;
        _sram.WriteByte((ushort)((CurrentRamBank * MemoryMap.ERamLength) + address), value);
    }

    private void WriteRom(ushort address, byte value)
    {
        
    }
    
    public void Clock()
    {
        throw new NotImplementedException();
    }
}