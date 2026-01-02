using CryptoScanner.Core.Zones;

namespace CryptoScanner.Commands;

public class CommandCalculateDlzForAll : CommandBase
{
    public override async void Execute(object? parameter)
    {
        System.Diagnostics.Debug.WriteLine($"Calculate dlz for all symbols");
        ZoneThreadCalculate.CalculateZonesForAllSymbolsAsync();
    }
}
