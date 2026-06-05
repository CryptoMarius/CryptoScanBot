using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace ExchangeTest;

internal class EmulatorTest
{
    private static void AnalyzeSignalCreated(CryptoSignal signal)
    {
        GlobalData.CreatedSignalCount++;
        string text = signal.CloseDate.ToLocalTime() + " Analyze signal " + signal.Symbol.Name + " " + signal.Interval.Name + " " + signal.SideText + " " + signal.StrategyText + " " + signal.EventText;
        GlobalData.AddTextToLogTab(text);

        // signal.BackTest removed; legacy emulator path obsolete.

        //if (!signal.IsInvalid)
        //{
        //    GlobalData.SignalList.Add(signal);
        //    GlobalData.SignalQueue.Enqueue(signal);
        //}
    }

    public static void Test()
    {
        //GlobalData.IsEmulatorMode = true;
        //GlobalData.SetTradingAccounts();


        //// Tickers can be active, but they need to be non ative when GlobalData.IsEmulatorMode is true?
        //// Fixed: Stop Price tickers (yes) 
        //// Fixed: Stop KLine tickers (yes) 
        //// not needed: Stop User Ticker -> not the right tradingAccount (add to database as foreign key / lazy object?)
        //// MAAAAAR hoe zit het met orders en trades (id's in beide accounts)

        //// Fixed: Clear signals in main screen
        //// Fixed: Clear positions in main screen
        //// TODO: Clear statistics?


        //GlobalData.AnalyzeSignalCreated = AnalyzeSignalCreated;
        //if (GlobalData.ThreadCheckPosition == null)
        //{
        //    GlobalData.ThreadCheckPosition = new ThreadCheckFinishedPosition();
        //    _ = Task.Run(async () => { await GlobalData.ThreadCheckPosition!.ExecuteAsync(); });
        //}

        //GlobalData.Settings.BackTest.BackTestSymbol = "ONDOUSDT";
        //if (GlobalData.ExchangeListName.TryGetValue("Bybit Spot", out CryptoScanner.Core.Model.CryptoExchange exchange))
        //{
        //    if (exchange.SymbolListName.TryGetValue(GlobalData.Settings.BackTest.BackTestSymbol, out CryptoSymbol? symbol) &&
        //          exchange.SymbolListName.TryGetValue("BTCUSDT", out CryptoSymbol? btcSymbol))
        //    {
        //        var whateverx = Task.Run(async () => { await Emulator.Execute(btcSymbol, symbol); });
        //    }
        //}

        //// Fixed: Load signals in main screen
        //// Fixed: Load positions in main screen
        //// TODO: Refresh statistics
        //// TODO: Herstel de scanner status?
    }

}
