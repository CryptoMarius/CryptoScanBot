using CryptoScanner.Core.Core;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Commands;

public class CommandClearLogAndTicker : CommandBase
{
    public override void Execute(object? parameter)
    {
        //TextBoxLog.Clear(); // todo....
        GlobalData.CreatedSignalCount = 0;

        SignalExecute.ResetAnalyseCount();
        ExchangeBase.KLineTicker!.Reset();
        //ExchangeBase.PriceTicker!.Reset(); // gone..
    }
}
