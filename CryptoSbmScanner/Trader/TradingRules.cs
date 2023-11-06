using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;

namespace CryptoSbmScanner.Trader;

public static class TradingRules
{
    private static void CalculateTradingRules(CryptoCandle lastCandle1m)
    {
        // alias
        var pause = GlobalData.PauseTrading;

        // Als een munt (met name BTC) snel gedaald is dan stoppen
        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
        {
            foreach (PauseTradingRule rule in GlobalData.Settings.Trading.PauseTradingRules)
            {
                if (exchange.SymbolListName.TryGetValue(rule.Symbol, out CryptoSymbol symbol))
                {
                    CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(rule.Interval);
                    if (symbolInterval.CandleList.Any())
                    {
                        // Niet geschikt voor de emulator vanwege de Last()
                        // De LastCandle1m even terug rekenen naar het interval
                        bool missingCandles = false;
                        CryptoCandle candleLast = symbolInterval.CandleList.Values.Last();
                        decimal low = candleLast.Low;
                        decimal high = candleLast.High;

                        long time = candleLast.OpenTime;
                        int candleCount = rule.Candles - 1;
                        while (candleCount-- > 0)
                        {
                            time -= symbolInterval.Interval.Duration;
                            if (symbolInterval.CandleList.TryGetValue(time, out CryptoCandle candle))
                            {
                                low = Math.Min(low, candle.Low);
                                high = Math.Max(high, candle.High);
                                //GlobalData.AddTextToLogTab(candle.OhlcText(symbol, symbolInterval.Interval, symbol.PriceDisplayFormat));
                            }
                            else missingCandles = true;
                        }

                        // todo: het percentage wordt echt niet negatief als je met de high en low werkt, duh
                        if (!missingCandles)
                        {
                            double percentage = (double)(100m * ((high / low) - 1m));
                            if (percentage >= rule.Percentage || percentage <= -rule.Percentage)
                            {
                                long pauseUntil = candleLast.OpenTime + rule.CoolDown * symbolInterval.Interval.Duration;
                                DateTime pauseUntilDate = CandleTools.GetUnixDate(pauseUntil);

                                if (!pause.Until.HasValue || pauseUntilDate > pause.Until)
                                {
                                    pause.Until = pauseUntilDate;
                                    pause.Text = string.Format("{0} heeft {1:N2}% bewogen (gepauseerd tot {2}) - 2", rule.Symbol, percentage, pauseUntilDate.ToLocalTime());
                                    GlobalData.AddTextToLogTab(GlobalData.PauseTrading.Text);
                                    GlobalData.AddTextToTelegram(GlobalData.PauseTrading.Text);
                                }
                            }
                            else
                            {
                                //if (percentage > 0.5 || percentage < -0.5)
                                //{
                                //    GlobalData.AddTextToLogTab(string.Format("{0} heeft {1:N2}% bewogen", rule.Symbol, percentage));
                                //    GlobalData.AddTextToTelegram(string.Format("{0} heeft {1:N2}% bewogen", rule.Symbol, percentage));
                                //}
                            }
                        }
                    }
                }
                else GlobalData.AddTextToLogTab("Pauze regel: symbol " + rule.Symbol + " bestaat niet");
            }
        }
    }


    public static bool CheckTradingRules(CryptoCandle lastCandle1m)
    {
        // Controleer de trading pauseer regels

        // alias
        var pause = GlobalData.PauseTrading;


        // Ongeveer iedere minuut c.q. candle berekenen
        DateTime lastCandle1mCloseTime = CandleTools.GetUnixDate(lastCandle1m.OpenTime + 60);
        if (!pause.Calculated.HasValue || pause.Calculated < lastCandle1mCloseTime)
        {
            pause.Text = "";
            pause.Calculated = lastCandle1mCloseTime;

            //GlobalData.AddTextToLogTab("CheckTradingRules()");
            CalculateTradingRules(lastCandle1m);

            if (pause.Text != "")
                return false;
        }

        // Is gepauseerd
        if (pause.Until >= lastCandle1mCloseTime)
            return false;

        return true;
    }


    /// Controleer de barometer(s)
    public static bool CheckBarometerValues(CryptoSymbol symbol, CryptoCandle lastCandle1m, out string reaction)
    {
        // alias
        var pause = symbol.QuoteData.PauseTrading;

        // Ongeveer iedere minuut c.q. candle berekenen
        DateTime lastCandle1mCloseTime = CandleTools.GetUnixDate(lastCandle1m.OpenTime + 60);
        if (!pause.Calculated.HasValue || pause.Calculated < lastCandle1mCloseTime)
        {
            // Als de barometer gedaald is onder de limieten dan "stoppen"
            pause.Text = "";
            pause.Calculated = lastCandle1mCloseTime;

            //GlobalData.AddTextToLogTab($"{symbol.QuoteData.Name} CheckBarometerValues()");
            if (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval15m, GlobalData.Settings.Trading.Barometer15mBotMinimal, out reaction) ||
            (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval30m, GlobalData.Settings.Trading.Barometer30mBotMinimal, out reaction)) ||
            (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval1h, GlobalData.Settings.Trading.Barometer01hBotMinimal, out reaction)) ||
            (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval4h, GlobalData.Settings.Trading.Barometer04hBotMinimal, out reaction)) ||
            (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval1d, GlobalData.Settings.Trading.Barometer24hBotMinimal, out reaction)))
                pause.Text = reaction;

            if (pause.Text != "")
            {
                pause.Until = lastCandle1mCloseTime.AddMinutes(5);
                reaction = symbol.QuoteData.PauseTrading.Text;
                return false;
            }
        }

        // Is al gepauseerd
        if (pause.Until >= lastCandle1mCloseTime)
        {
            reaction = symbol.QuoteData.PauseTrading.Text;
            if (reaction == "")
                reaction = "Barometer low?";
            return false;
        }

        reaction = "";
        return true;
    }

}
