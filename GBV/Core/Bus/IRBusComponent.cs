namespace GBV.Core.Bus;

public interface IRBusComponent
{
    public byte Read(ushort address);
}