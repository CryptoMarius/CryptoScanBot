using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Excel;
using CryptoScanner.Core.Helpers;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;
using CryptoScanner.Core.Trend;

using Dapper;
using Dapper.Contrib.Extensions;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CryptoScanner.UI.Services;

public class GridCommandService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly SymbolService _symbolService;
    private readonly NavigationManager _navigationManager;

    public GridCommandService(IJSRuntime jsRuntime, SymbolService symbolService, NavigationManager navigationManager)
    {
        _jsRuntime = jsRuntime;
        _symbolService = symbolService;
        _navigationManager = navigationManager;
    }

    public void OpenChart(CryptoSymbol symbol)
    {
        _symbolService.SetSelectedSymbol(symbol);
        _navigationManager.NavigateTo("/chart");
    }

    public void OpenTradingViewInternal(CryptoSymbol symbol, CryptoInterval? interval = null)
    {
        // No NavigateTo("/tradingview") any more: that tab is gone. It was an iframe, and
        // www.tradingview.com refuses to be framed, so it could only ever show the anonymous embed
        // widget. ActivateTradingApp below reaches the host, which opens a real second window.
        _symbolService.SetSelectedSymbol(symbol);

        interval ??= GlobalData.IntervalListPeriod[GlobalData.Settings.General.DefaultInterval];
        ActivateTradingApp(CryptoTradingApp.TradingView, symbol, interval, CryptoExternalUrlType.Internal);
    }

    public void OpenTradingApp(CryptoSymbol symbol, CryptoInterval? interval)
    {
        interval ??= GlobalData.IntervalListPeriod[GlobalData.Settings.General.DefaultInterval];

        CryptoExternalUrlType tradingAppInternExtern = CryptoExternalUrlType.External;
        if (GlobalData.Settings.General.TradingApp == CryptoTradingApp.TradingView ||
            GlobalData.Settings.General.TradingApp == CryptoTradingApp.ExchangeUrl)
            tradingAppInternExtern = GlobalData.Settings.General.TradingAppInternExtern;

        ActivateTradingApp(GlobalData.Settings.General.TradingApp, symbol, interval, tradingAppInternExtern);
    }

    public void OpenTradingViewExternal(CryptoSymbol symbol, CryptoInterval? interval)
    {
        interval ??= GlobalData.IntervalListPeriod[GlobalData.Settings.General.DefaultInterval];
        ActivateTradingApp(CryptoTradingApp.TradingView, symbol, interval, CryptoExternalUrlType.External);
    }

    public void OpenExchange(CryptoSymbol symbol, CryptoInterval? interval)
    {
        interval ??= GlobalData.IntervalListPeriod[GlobalData.Settings.General.DefaultInterval];
        ActivateTradingApp(CryptoTradingApp.ExchangeUrl, symbol, interval, CryptoExternalUrlType.External);
    }

    public async Task CopySymbolNameAsync(CryptoSymbol symbol)
    {
        await _jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", symbol.Name);
    }

    public void CalculateLiquidityZones(CryptoSymbol symbol)
    {
        foreach (string intervalName in GlobalData.Settings.Signal.ZonesDlz.IntervalList)
        {
            if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? intervalX))
            {
                GlobalData.ThreadZoneCalculate?.AddToQueue(symbol, intervalX);
            }
        }
    }

    public async Task CopyDataCellsAsync(string text)
    {
        await _jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
    }

    /// <summary>
    /// Log the market trend for a symbol and write "Trend information.txt", identical to the
    /// Avalonia CommandShowTrendInformation (this host had two different half implementations:
    /// one that logged but wrote no file, one that wrote the file but skipped the market trend).
    /// </summary>
    public void ExportTrendToLog(CryptoSymbol symbol)
    {
        Task.Run(async () =>
        {
            try
            {
                var trend = GlobalData.Settings.Trend.Primary;
                var log = new System.Text.StringBuilder();
                log.AppendLine($"Markettrend {symbol.Name}");
                GlobalData.AddTextToLogTab("");
                GlobalData.AddTextToLogTab($"Markettrend {symbol.Name}");

                var symbolTrend = await MarketTrend.CalculateMarketTrendAsync(symbol, trend, log);

                log.AppendLine("");
                log.AppendLine("");

                foreach (var interval in GlobalData.IntervalList)
                {
                    var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                    var intervalTrend = trend.TrendType == TrendType.Primary ? symbolInterval.TrendPrimary : symbolInterval.TrendSecondary;

                    string s = intervalTrend.Trend switch
                    {
                        CryptoTrendIndicator.Bullish => $"{symbol.Name} {interval.Name} trend=bullish",
                        CryptoTrendIndicator.Bearish => $"{symbol.Name} {interval.Name} trend=bearish",
                        _ => $"{symbol.Name} {interval.Name} trend=sideway's",
                    };
                    GlobalData.AddTextToLogTab(s);
                    log.AppendLine(s);
                }

                string t;
                if (symbolTrend.Percentage == null)
                    t = $"{symbol.Name} Markettrend unknown";
                else
                {
                    var marketTrend = symbolTrend.Percentage;
                    if (marketTrend < 0)
                        t = $"{symbol.Name} Markettrend={marketTrend:N2}% bearish";
                    else if (marketTrend > 0)
                        t = $"{symbol.Name} Markettrend={marketTrend:N2}% bullish";
                    else
                        t = $"{symbol.Name} Markettrend={marketTrend:N2}% unknown";
                }
                GlobalData.AddTextToLogTab(t);
                log.AppendLine(t);

                string filename = Path.Combine(GlobalData.AppDataFolder, "Trend information.txt");
                File.WriteAllText(filename, log.ToString());
            }
            catch (Exception ex)
            {
                GlobalData.AddTextToLogTab($"Error exporting trend info for {symbol.Name}: {ex.Message}");
            }
        });
    }

    public void ExportSymbolToExcel(CryptoSymbol symbol)
    {
        Task.Run(() => new ExcelSymbolDump(symbol).ExportToExcel());
    }

    public void ExportBarometerToExcel(CryptoSymbol symbol)
    {
        Task.Run(() => new ExcelBarometerDump(symbol).ExportToExcel());
    }

    public void ExportSignalToExcel(CryptoSignal signal)
    {
        Task.Run(() => new ExcelSignalDump(signal).ExportToExcel());
    }

    public void PositionRecalculate(CryptoPosition position)
    {
        Task.Run(async () =>
        {
            try
            {
                using CryptoDatabase db = new();
                db.Connection.Open();
                PositionTools.LoadPosition(db, position);
                await TradeTools.CalculatePositionResultsViaOrders(db, position, forceCalculation: true);
                GlobalData.AddTextToLogTab($"{position.Symbol.Name} manually recalculated position {position.Id}");
            }
            catch (Exception ex)
            {
                ScannerLog.Logger.Error(ex, "");
                GlobalData.AddTextToLogTab($"Error calculating position {position.Symbol.Name}: {ex.Message}");
            }
        });
    }

    public void PositionDelete(CryptoPosition position)
    {
        Task.Run(async () =>
        {
            try
            {
                using CryptoDatabase db = new();
                db.Connection.Open();
                PositionTools.LoadPosition(db, position);

                using var transaction = db.BeginTransaction();
                db.Connection.Execute($"delete from positionstep where positionid={position.Id}", transaction);
                db.Connection.Execute($"delete from positionpart where positionid={position.Id}", transaction);
                db.Connection.Execute($"delete from position where id={position.Id}", transaction);
                transaction.Commit();

                position.Symbol.LastTradeDate = null;
                position.Symbol.LastLossDate = null;
                GlobalData.ThreadSaveObjects!.AddToQueue(position.Symbol);

                // Tell the grids the position is gone (the Avalonia CommandPositionDelete does the
                // same); without this the row stayed visible until a manual reload.
                GlobalData.SendMvvmMessage(new PositionIsDeletedMessage(position));
                GlobalData.PositionDeleted?.Invoke(position);

                PositionTools.RemovePosition(GlobalData.ActiveExchange!, position, false);
                GlobalData.AddTextToLogTab($"{position.Symbol.Name} manually deleted position {position.Id} from the database");
            }
            catch (Exception ex)
            {
                ScannerLog.Logger.Error(ex, "");
                GlobalData.AddTextToLogTab($"Error deleting position {position.Id}: {ex.Message}");
            }
        });
    }

    public void PositionAddDca(CryptoPosition position)
    {
        Task.Run(async () =>
        {
            try
            {
                using CryptoDatabase db = new();
                db.Connection.Open();
                PositionTools.LoadPosition(db, position);
                await TradeTools.CalculatePositionResultsViaOrders(db, position, forceCalculation: true);

                if (position.Symbol.LastPrice.HasValue)
                {
                    decimal price = (decimal)position.Symbol.LastPrice;
                    if (position.Side == CryptoTradeSide.Long)
                    {
                        if (position.Symbol.LastPrice < price)
                            price = (decimal)position.Symbol.LastPrice - position.Symbol.PriceTickSize;
                    }
                    else
                    {
                        if (position.Symbol.LastPrice > price)
                            price = (decimal)position.Symbol.LastPrice + position.Symbol.PriceTickSize;
                    }

                    PositionTools.ExtendPosition(db, position, CryptoPartPurpose.Dca, position.Interval!, position.Strategy,
                        price, GlobalData.Clock.UtcNow, true);
                    GlobalData.AddTextToLogTab($"{position.Symbol.Name} manually added DCA to position {position.Id}");

                    var symbolPeriod = position.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
                    if (symbolPeriod.CandleList.Count > 0)
                    {
                        var lastCandle1m = symbolPeriod.CandleList.Values.Last();
                        using PositionMonitor positionMonitor = new(position.Symbol, lastCandle1m);
                        await positionMonitor.HandlePosition(position);
                    }
                }
            }
            catch (Exception ex)
            {
                ScannerLog.Logger.Error(ex, "");
                GlobalData.AddTextToLogTab($"Error adding DCA for {position.Symbol.Name}: {ex.Message}");
            }
        });
    }

    public void PositionCancelDca(CryptoPosition position)
    {
        Task.Run(async () =>
        {
            try
            {
                using CryptoDatabase db = new();
                db.Connection.Open();
                PositionTools.LoadPosition(db, position);
                await TradeTools.CalculatePositionResultsViaOrders(db, position, forceCalculation: true);

                var symbolPeriod = position.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
                if (symbolPeriod.CandleList.Count > 0)
                {
                    var lastCandle1m = symbolPeriod.CandleList.Values.Last();
                    CandleTime lastCandle1mCloseTime = lastCandle1m.OpenTime + 1;
                    DateTime lastCandle1mCloseTimeDate = lastCandle1mCloseTime.ToDateTime();

                    using PositionMonitor positionMonitor = new(position.Symbol, lastCandle1m);
                    await positionMonitor.HandlePosition(position);

                    var entryOrderSide = position.GetEntryOrderSide();
                    foreach (CryptoPositionPart part in position.PartList.Values.ToList())
                    {
                        if (!part.CloseTime.HasValue && part.Purpose == CryptoPartPurpose.Dca)
                        {
                            foreach (CryptoPositionStep step in part.StepList.Values.ToList())
                            {
                                if (!step.CloseTime.HasValue && step.Side == entryOrderSide)
                                {
                                    string cancelReason = $"cancel due to manual DCA cancellation of position {position.Id}";
                                    var (success, _) = await TradeTools.CancelOrder(db, position, part, step,
                                        lastCandle1mCloseTimeDate, CryptoOrderStatus.ManuallyByUser, cancelReason);
                                    if (success)
                                    {
                                        part.CloseTime = DateTime.UtcNow;
                                        db.Connection.Update<CryptoPositionPart>(part);
                                        position.ActiveDca = false;
                                        db.Connection.Update<CryptoPosition>(position);
                                        GlobalData.AddTextToLogTab($"{position.Symbol.Name} position {position.Id} manually cancelled open DCA {part.PartNumber}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ScannerLog.Logger.Error(ex, "");
                GlobalData.AddTextToLogTab($"Error cancelling DCA for {position.Symbol.Name}: {ex.Message}");
            }
        });
    }

    public static void ExportPositionToExcel(CryptoPosition position)
    {
        Task.Run(() =>
        {
            try
            {
                new ExcelPositionDump(position).ExportToExcel();
                GlobalData.AddTextToLogTab($"Exported position {position.Symbol.Name} to Excel");
            }
            catch (Exception ex)
            {
                GlobalData.AddTextToLogTab($"Error exporting position to Excel: {ex.Message}");
            }
        });
    }

    public void PositionDeleteAll()
    {
        Task.Run(() =>
        {
            try
            {
                var exchange = GlobalData.ActiveExchange;
                if (exchange == null)
                    return;

                using CryptoDatabase db = new();
                db.Connection.Open();
                using var transaction = db.BeginTransaction();
                db.Connection.Execute("delete from positionstep", transaction);
                db.Connection.Execute("delete from positionpart", transaction);
                db.Connection.Execute("delete from position", transaction);
                transaction.Commit();

                exchange.Data.PositionList.Clear();
                foreach (var symbol in exchange.SymbolListId.Values)
                {
                    symbol.LastTradeDate = null;
                    symbol.LastLossDate = null;
                    GlobalData.ThreadSaveObjects!.AddToQueue(symbol);
                }

                // Remove the positions from open or closed positions
                GlobalData.SendMvvmMessage(new PositionDeleteAllMessage());
                GlobalData.PositionDeletedAll?.Invoke();

                GlobalData.AddTextToLogTab("Manually deleted all positions from the database");
            }
            catch (Exception ex)
            {
                ScannerLog.Logger.Error(ex, "");
                GlobalData.AddTextToLogTab($"Error deleting all positions: {ex.Message}");
            }
        });
    }

    public void HandleDoubleClick(CryptoSymbol symbol, CryptoInterval? interval)
    {
        if (GlobalData.Settings.General.DoubleClickAction == CryptoDoubleClickAction.ActivateChartForm)
        {
            // Chart window is Avalonia-only; in Blazor we open TradingView externally as fallback
            OpenTradingViewExternal(symbol, interval);
        }
        else
        {
            OpenTradingApp(symbol, interval);
        }
    }

    /// <summary>
    /// Open a plain URL, honouring the internal browser tab when the host provides one.
    /// </summary>
    public void OpenUrl(string url, bool switchTab = true)
    {
        if (string.IsNullOrEmpty(url))
            return;

        if (ExternalLinkHelper.OpenInternalBrowser != null)
        {
            ExternalLinkHelper.OpenInternalBrowser.Invoke(url, switchTab);
            return;
        }

        ExternalLinkHelper.OpenSystemBrowser(url);
    }

    private static void ActivateTradingApp(CryptoTradingApp tradingApp,
        CryptoSymbol symbol, CryptoInterval interval, CryptoExternalUrlType viaTradingBrowser)
    {
        // Route through the shared helper so the Blazor hosts get exactly the same behaviour as
        // Avalonia: "internal" really means the embedded browser tab, and the hidden-browser
        // path (Altrady deep links) uses a real browser instead of a bare HTTP GET.
        ExternalLinkHelper.ActivateTradingApp(tradingApp, symbol, interval, viaTradingBrowser);
    }
}
