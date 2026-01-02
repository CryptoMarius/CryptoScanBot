using Avalonia.Controls;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Commands;

public class CommandCalculateDlzForSymbol : CommandBase
{
    public override async void Execute(object? parameter)
    {
        if (GetObjectInformation(parameter, out parameterObjects dto) && dto.symbol != null)
        {
            System.Diagnostics.Debug.WriteLine($"Calculate dlz for {dto.symbol.Name}");
            foreach (string intervalName in GlobalData.Settings.Signal.ZonesDlz.IntervalList)
            {
                if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? intervalX))
                {
                    GlobalData.ThreadZoneCalculate?.AddToQueue(dto.symbol, intervalX);
                }
            }
        }
    }
}
