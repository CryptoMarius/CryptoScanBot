using CryptoScanner.Core.Const;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Core.SignalR;

/// <summary>
/// Collects dashboard data from Core-level sources and from externally registered market indicators.
/// The UI project registers TradingView/F&amp;G values via <see cref="SetMarketIndicator"/>.
/// </summary>
public static class DashboardDataCollector
{
    private static readonly object _lock = new();
    private static readonly List<MarketIndicatorDto> _marketIndicators = [];

    /// <summary>
    /// Called from the UI project to register/update a market indicator value.
    /// </summary>
    public static void SetMarketIndicator(string type, string symbol, string name, decimal? price, double? volume)
    {
        lock (_lock)
        {
            var existing = _marketIndicators.Find(m => m.Symbol == symbol);
            if (existing != null)
            {
                existing.Price = price;
                existing.Volume = volume;
            }
            else
            {
                _marketIndicators.Add(new MarketIndicatorDto
                {
                    Type = type,
                    Symbol = symbol,
                    Name = name,
                    Price = price,
                    Volume = volume,
                });
            }
        }
    }

    public static DashboardUpdateDto CollectUpdate(string selectedQuote, string selectedInterval)
    {
        var dto = new DashboardUpdateDto();

        // Barometer summary values (1h, 4h, 1d) + Ready/Progress. Filled unconditionally, before the
        // exchange check below, so the UI's candle-load progress keeps flowing even during startup when
        // ActiveExchange is still null.
        dto.BarometerValues = GetBarometerValues(selectedQuote);

        var exchange = GlobalData.ActiveExchange;
        if (exchange == null)
            return dto;

        // Latest barometer point
        dto.LatestBarometerPoint = GetLatestBarometerPoint(selectedQuote, selectedInterval);

        // Market indicators (TradingView, F&G)
        lock (_lock)
        {
            dto.MarketIndicators = _marketIndicators.Select(m => new MarketIndicatorDto
            {
                Type = m.Type,
                Symbol = m.Symbol,
                Name = m.Name,
                Price = m.Price,
                Volume = m.Volume,
            }).ToList();
        }

        // Exchange symbol prices
        dto.SymbolPrices = GetSymbolPrices(exchange);

        // Ticker stats
        dto.TickerStats = GetTickerStats(exchange);

        return dto;
    }

    private static BarometerPointDto? GetLatestBarometerPoint(string quote, string interval)
    {
        var exchange = GlobalData.ActiveExchange;
        if (exchange == null || string.IsNullOrEmpty(quote) || string.IsNullOrEmpty(interval))
            return null;

        if (!GlobalData.IntervalListPeriodName.TryGetValue(interval, out CryptoInterval? cryptoInterval))
            return null;

        string symbolName = Constants.SymbolNameBarometerPrice + quote;
        if (!exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
            return null;

        var symbolInterval = symbol.GetSymbolInterval(cryptoInterval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return null;

        if (!symbolInterval.CandleList.TryGetLastCandle(out CryptoCandle lastCandle))
            return null;

        return new BarometerPointDto
        {
            Time = lastCandle.OpenTime.ToDateTime(),
            Value = lastCandle.Close,
        };
    }

    /// <summary>
    /// Returns the barometer summary values (1h/4h/1d) for a single quote. Public so the hub can expose
    /// it as an RPC: the dashboard push always uses the quote selected in the desktop app, while a remote
    /// client may be showing a different quote.
    /// </summary>
    public static BarometerValuesDto GetBarometerValues(string quote)
    {
        var dto = new BarometerValuesDto
        {
            Quote = quote,
            // Ready/Progress mirror the scanner's own candle-load state so the UI can show a live
            // "Loading candles N/M" line and flip the graph the instant loading finishes. Set first so
            // they are always populated, even on the early returns below.
            Ready = GlobalData.ApplicationStatus == CryptoApplicationStatus.Running,
            Progress = GlobalData.CandleProgressText,
        };
        var exchange = GlobalData.ActiveExchange;
        if (exchange == null || string.IsNullOrEmpty(quote))
            return dto;

        if (!GlobalData.Settings.QuoteCoins.TryGetValue(quote, out var quoteData))
            return dto;

        CryptoIntervalPeriod[] periods = [CryptoIntervalPeriod.interval1h, CryptoIntervalPeriod.interval4h, CryptoIntervalPeriod.interval1d];
        CandleTime? barometerTime = null;
        foreach (var period in periods)
        {
            var barometerData = exchange.Data.GetBarometer(quoteData.Name, period);
            if (barometerData?.PriceBarometer != null)
            {
                if (period == CryptoIntervalPeriod.interval1h)
                {
                    dto.Barometer1h = barometerData.PriceBarometer.Value;
                    dto.Rising1h = barometerData.PricePercentageRising ?? 0;
                    dto.Movement1h = barometerData.PriceMovement ?? 0;
                    dto.BitcoinVersusMarket1h = barometerData.PriceBitcoinVersusMarket ?? 0;
                }
                else if (period == CryptoIntervalPeriod.interval4h)
                {
                    dto.Barometer4h = barometerData.PriceBarometer.Value;
                    dto.Rising4h = barometerData.PricePercentageRising ?? 0;
                }
                else if (period == CryptoIntervalPeriod.interval1d)
                {
                    dto.Barometer1d = barometerData.PriceBarometer.Value;
                    dto.Rising1d = barometerData.PricePercentageRising ?? 0;
                }

                // The symbol pool is the same for every interval, so the last one wins - they only
                // differ when candles are missing at one of the two ends of a longer interval.
                dto.SymbolCount = barometerData.PriceSymbolCount ?? 0;

                // Track the most recent computed minute for the barometer timestamp (see below).
                if (barometerData.PriceDateTime.HasValue &&
                    (barometerTime == null || barometerData.PriceDateTime.Value > barometerTime.Value))
                    barometerTime = barometerData.PriceDateTime.Value;
            }
        }

        // Barometer time. The barometer is computed per minute but stored under the aggregate interval
        // periods (15m/1h/4h/1d, never 1m), so the "$BMP" symbol has no 1m candle list to read a time
        // from - that lookup always came back empty. Use PriceDateTime from the barometer data we just
        // fetched instead; it holds the OpenTime of the last computed minute. (+1 = the closing minute,
        // matching the previous formatting.)
        if (barometerTime.HasValue)
            dto.BarometerTime = (barometerTime.Value + 1).ToDateTime().ToLocalTime().ToString("HH:mm");

        return dto;
    }

    private static List<SymbolPriceDto> GetSymbolPrices(Model.CryptoExchange exchange)
    {
        var result = new List<SymbolPriceDto>();
        foreach (string baseName in GlobalData.Settings.ShowSymbolInformation)
        {
            foreach (var quoteCoin in GlobalData.Settings.QuoteCoins)
            {
                string symbolName = baseName + quoteCoin.Key;
                if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
                {
                    decimal price = 0;
                    if (symbol.LastPrice.HasValue)
                    {
                        price = symbol.LastPrice.Value;
                    }
                    else
                    {
                        var si = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
                        try
                        {
                            if (si.CandleList.Count > 0)
                            {
                                var c = si.CandleList.Values.Last();
                                price = c.Close;
                            }
                        }
                        catch
                        {
                            // concurrent modification
                        }
                    }
                    result.Add(new SymbolPriceDto
                    {
                        Symbol = symbolName,
                        Price = price,
                        Volume = symbol.Volume,
                    });
                    break;
                }
            }
        }
        return result;
    }

    private static TickerStatsDto GetTickerStats(Model.CryptoExchange exchange)
    {
        int positionCount = 0;
        string positionText = "";
        if (GlobalData.Settings.Trading.Active)
        {
            if (exchange.Data.PositionList.Count != 0)
            {
                foreach (var _ in exchange.Data.PositionList.Values)
                    positionCount++;
            }
            positionText = $"({GlobalData.Settings.Trading.SlotsMaximalLong}/{GlobalData.Settings.Trading.SlotsMaximalShort}) {positionCount}";
        }

        return new TickerStatsDto
        {
            KlineTickerCount = ExchangeBase.KLineTicker?.Count() ?? 0,
            ScannerExecuteCount = SignalExecute.AnalyseCount,
            ScannerSignalCount = GlobalData.CreatedSignalCount,
            ScannerPositionCount = positionText,
        };
    }
}
