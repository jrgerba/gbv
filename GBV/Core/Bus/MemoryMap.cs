namespace GBV.Core.Bus;

public static class MemoryMap
{
    public const ushort Bank0Start = 0x0000;
    public const ushort Bank0End = 0x3FFF;
    public const ushort Bank0Length = Bank0End - Bank0Start + 1;
    
    // Interrupt sources
    public const ushort IntVBlank = 0x40;
    public const ushort IntStat = 0x48;
    public const ushort IntTimer = 0x50;
    public const ushort IntSerial = 0x58;
    public const ushort IntJoypad = 0x60;

    public const ushort BankNStart = 0x4000;
    public const ushort BankNEnd = 0x7FFF;
    public const ushort BankNLength = BankNEnd - BankNStart + 1;

    public const ushort VRamStart = 0x8000;
    public const ushort VRamEnd = 0x9FFF;
    public const ushort VRamLength = VRamEnd - VRamStart + 1;

    public const ushort ERamStart = 0xA000;
    public const ushort ERamEnd = 0xBFFF;
    public const ushort ERamLength = ERamEnd - ERamStart + 1;

    public const ushort WRam0Start = 0xC000;
    public const ushort WRam0End = 0xCFFF;
    public const ushort WRam0Length = WRam0End - WRam0Start + 1;

    public const ushort WRamNStart = 0xD000;
    public const ushort WRamNEnd = 0xDFFF;
    public const ushort WRamNLenght = WRamNEnd - WRamNStart + 1;

    public const ushort EchoStart = 0xE000;
    public const ushort EchoEnd = 0xFDFF;
    public const ushort EchoLength = EchoEnd - EchoStart + 1;
    public const ushort EchoMirrorAddress = 0xC000;

    public const ushort OAMStart = 0xFE00;
    public const ushort OAMEnd = 0xFE9F;
    public const ushort OAMLength = OAMEnd - OAMStart + 1;

    public const ushort SpecialStart = 0xFEA0;
    public const ushort SpecialEnd = 0xFEFF;
    public const ushort SpecialLength = SpecialEnd - SpecialStart + 1;

    public const ushort IOStart = 0xFF00;
    public const ushort IOEnd = 0xFF7F;
    public const ushort IOLength = IOEnd - IOStart + 1;

    public const ushort HRAMStart = 0xFF80;
    public const ushort HRAMEnd = 0xFFFE;
    public const ushort HRAMLength = HRAMEnd - HRAMStart + 1;

    public const ushort Joypad = 0xFF00;

    /// <summary>
    /// Serial Transfer Data
    /// </summary>
    public const ushort SB = 0xFF01;

    /// <summary>
    /// Serial Transfer Control
    /// </summary>
    public const ushort SC = 0xFF02;

    public const ushort TimerStart = 0xFF04;
    public const ushort TimerEnd = 0xFF07;
    
    /// <summary>
    /// Timer Divide Register
    /// </summary>
    public const ushort Div = 0xFF04;

    /// <summary>
    /// Timer Counter
    /// </summary>
    public const ushort Tima = 0xFF05;
    
    /// <summary>
    /// Timer Modulo
    /// </summary>
    public const ushort Tma = 0xFF06;

    /// <summary>
    /// Timer Control
    /// </summary>
    public const ushort Tac = 0xFF07;

    /// <summary>
    /// Interrupt Flagged Register
    /// </summary>
    public const ushort IF = 0xFF0F;
    
    // Todo: Add constants for audio registers (there's a lot)

    /// <summary>
    /// LCD Control
    /// </summary>
    public const ushort Lcdc = 0xFF40;

    /// <summary>
    /// LCD Status
    /// </summary>
    public const ushort Stat = 0xFF41;

    /// <summary>
    /// Background Viewport Y
    /// </summary>
    public const ushort ScY = 0xFF42;

    /// <summary>
    /// Background Viewport X
    /// </summary>
    public const ushort ScX = 0xFF43;

    /// <summary>
    /// LCD Y Coordinate
    /// </summary>
    public const ushort LY = 0xFF44;

    /// <summary>
    /// LY Compare
    /// </summary>
    public const ushort LYC = 0xFF45;
    
    public const ushort IE = 0xFFFF;
}