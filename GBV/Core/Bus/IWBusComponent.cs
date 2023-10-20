namespace GBV.Core.Bus;

public interface IWBusComponent
{
    public void Write(ushort address, byte value);
}