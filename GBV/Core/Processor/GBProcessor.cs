using GBV.Core.Bus;

namespace GBV.Core.Processor;

public class GBProcessor : ICPU
{
    public RegisterPage RegisterPage;
    private IExecutionEngine eEngine;
    private IBus bus;
    
    public void Clock()
    {
        throw new NotImplementedException();
    }
}