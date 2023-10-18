namespace GBV.Core.Bus;

public class UnhandledAddressException : Exception
{
    private const string DefaultMessage = "The specified address is not handled in this context";
    
    public UnhandledAddressException(string message = DefaultMessage) :
        base(message) {}
}