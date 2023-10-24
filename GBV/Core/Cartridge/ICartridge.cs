using GBV.Core.Bus;

namespace GBV.Core.Cartridge;

public interface ICartridge : IWBusComponent, IRBusComponent, IEBusComponent
{  
    public int CurrentRomBankA { get; }
    public int CurrentRomBankB { get; }
    public string MBC { get; }
    public Span<byte> RawHeader { get; }
    
}