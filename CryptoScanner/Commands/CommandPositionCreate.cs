using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Commands;

public class CommandPositionCreate : CommandBase
{
    public override bool CanExecute(object? parameter)
    {
        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.symbol != null)
        {
            CryptoPosition? position = PositionTools.HasPosition(GlobalData.ActiveExchange!, dto.symbol);
            if (position != null)
                return false;
            return true;
        }
        return false;
    }

    public override void Execute(object? parameter)
    {
        // Fire-and-forget
        _ = ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        System.Diagnostics.Debug.WriteLine($"CommandPositionCreate");
        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.symbol != null)
        {
            var symbol = dto.symbol;
            try
            {
                CryptoTradeSide tradeSide = CryptoTradeSide.Long; // right?
                CryptoSignalStrategy strategy = CryptoSignalStrategy.Stobb; // right?
                CryptoInterval interval = GlobalData.IntervalList[0]; // 1m right..
                CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                CandleTime LastCandle1mCloseTimeDate = CandleTime.FromDateTime(DateTime.Now);

                //var entryQuote = symbol.QuoteData;

                // GetSymbolData available assets from the exchange (as late as possible because of webcall)
                var resultFetchAssets = AssetTools.FetchAssets(GlobalData.ActiveExchange, true);
                if (!resultFetchAssets.success)
                {
                    GlobalData.AddTextToLogTab($"{symbol.Name} {resultFetchAssets.reaction}");
                    //ClearSignals();
                    return;
                }

                // Enough stuff to take position? + entryAmount
                var resultAvailableAssets = AssetTools.CheckAvailableAssets(GlobalData.ActiveExchange!, symbol);
                if (!resultAvailableAssets.success)
                {
                    GlobalData.AddTextToLogTab($"{symbol.Name} {resultAvailableAssets.reaction}");
                    //ClearSignals();
                    return;
                }
                var info = resultAvailableAssets.info; // short alias
                decimal entryQuote = resultAvailableAssets.entryQuoteAsset;

                // Bepaal het entry bedrag
                decimal entryPrice = symbol.LastPrice!.Value.Clamp(symbol.PriceMinimum, symbol.PriceMaximum, symbol.PriceTickSize);
                decimal entryBase = entryQuote / entryPrice;
                entryBase = entryBase.Clamp(symbol.QuantityMinimum, symbol.QuantityMaximum, symbol.QuantityTickSize);
                entryBase = TradeTools.CorrectEntryQuantityIfWayLess(symbol, entryQuote, entryBase, entryPrice);


                using CryptoDatabase databaseThread = new();
                databaseThread.Connection.Open();

                // Create position + entry part
                var position = PositionTools.CreatePosition(symbol, strategy, tradeSide,
                    "Manual", symbolInterval, LastCandle1mCloseTimeDate.ToDateTime());
                //PositionTools.AddSignalProperties(position, null);
                databaseThread.Connection.Insert(position);
                PositionTools.AddPosition(position);
                PositionTools.ExtendPosition(databaseThread, position, CryptoPartPurpose.Entry,
                    interval, strategy, GlobalData.Settings.Trading.EntryStrategy,
                    entryPrice, LastCandle1mCloseTimeDate.ToDateTime());
                GlobalData.AddTextToLogTab($"{symbol.Name} handmatig een positie gemaak {position.Id}");


            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"error removing dca {dto.symbol.Name} {error.Message}");
            }
        }
    }
}
