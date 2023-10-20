using System.Text;

namespace GBV.Core.Processor;

public interface ICPU
{
    public IRegisterPage RegisterPage { get; }
    public ExecutionState ExecutionState { get; set; }
    
    public void Clock();
}