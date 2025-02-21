using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Commands;

public class CommandCalculateDlzForSymbol : CommandBase
{
    public override void Execute(ToolStripMenuItemCommand item, object sender)
    {
        if (sender != null && sender is CryptoSymbol symbol)
        {
            foreach (string intervalName in GlobalData.Settings.Signal.ZonesDlz.IntervalList)
            {
                if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? intervalX))
                {
                    GlobalData.ThreadZoneCalculate?.AddToQueue(symbol, intervalX);
                }
            }
        }
    }
}
