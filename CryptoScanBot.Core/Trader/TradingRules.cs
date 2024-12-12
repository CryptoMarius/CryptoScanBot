using CryptoScanBot.Core.Barometer;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Trader;

public static class TradingRules
{
    private static void CalculateTradingRules(PauseTradingRule pause, long candleUnixDate, int candleDuration)
    {
        // Als een munt (met name BTC) snel gedaald is dan stoppen
        var exchange = GlobalData.Settings.General.Exchange;
        if (exchange != null)
        {
            int index = 0;
            foreach (Settings.PauseTradingRule rule in GlobalData.Settings.Trading.PauseTradingRules)
            {
                index++;
                if (exchange.SymbolListName.TryGetValue(rule.Symbol, out CryptoSymbol? symbol))
                {
                    CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(rule.Interval);
                    if (symbolInterval.CandleList.Count != 0)
                    {
                        bool missingCandles = false;
                        decimal low = decimal.MaxValue;
                        decimal high = decimal.MinValue;
                        long loop = IntervalTools.StartOfIntervalCandle2(candleUnixDate, candleDuration, symbolInterval.Interval.Duration);
                        //DateTime loopDate = CandleTools.GetUnixDate(loop);
                        if (!symbolInterval.CandleList.ContainsKey(loop))
                        {
                            loop -= symbolInterval.Interval.Duration;
                            //loopDate = CandleTools.GetUnixDate(loop);
                            //GlobalData.AddTextToLogTab($"Missing candles for tradingrules? {symbol.Name} {CandleTools.GetUnixDate(candleUnixDate)} {symbolInterval.Interval.Name} {CandleTools.GetUnixDate(loop)}  (debug1)");
                        }



                        int candleCount = rule.Candles;
                        while (candleCount-- > 0)
                        {
                            if (symbolInterval.CandleList.TryGetValue(loop, out CryptoCandle? candle))
                            {
                                low = Math.Min(low, candle.Low);
                                high = Math.Max(high, candle.High);
                                //GlobalData.AddTextToLogTab(candle.OhlcText(symbol, symbolInterval.Interval, symbol.PriceDisplayFormat));
                            }
                            else
                            {
                                missingCandles = true;
                                GlobalData.AddTextToLogTab($"Missing candles for tradingrules? {symbol.Name} {CandleTools.GetUnixDate(candleUnixDate)} {symbolInterval.Interval.Name} {CandleTools.GetUnixDate(loop)}  (debug2)");
                            }
                            loop -= symbolInterval.Interval.Duration;
                            //loopDate = CandleTools.GetUnixDate(loop);
                        }

                        if (!missingCandles)
                        {
                            // TODO: het percentage wordt echt niet negatief als je met de high en low werkt..
                            double percentage = (double)(100m * (high / low - 1m));
                            if (percentage >= rule.Percentage || percentage <= -rule.Percentage)
                            {
                                long pauseUntil = candleUnixDate + candleDuration * rule.CoolDown; // * 60;
                                DateTime pauseUntilDate = CandleTools.GetUnixDate(pauseUntil);

                                if (!pause.Until.HasValue || pauseUntilDate > pause.Until)
                                {
                                    pause.Until = pauseUntilDate;
                                    pause.Text = $"{symbol.Name} #{index} price={symbol.LastPrice.ToString0()} heeft {percentage:N2}% bewogen (gepauseerd tot {pauseUntilDate.ToLocalTime()})";
                                    GlobalData.AddTextToLogTab(pause.Text);
                                    GlobalData.AddTextToTelegram(pause.Text);
                                }
                            }
                        }
                    }
                }
                else GlobalData.AddTextToLogTab($"Pauze regel: symbol {rule.Symbol} bestaat niet");
            }
        }
    }


    public static bool CheckTradingRules(PauseTradingRule pause, long candleDate, int candleDuration)
    {
        // Controleer de trading pauseer regels


        // Ongeveer iedere minuut c.q. candle berekenen
        DateTime lastCandle1mCloseTime = CandleTools.GetUnixDate(candleDate + candleDuration);
        if (!pause.Calculated.HasValue || pause.Calculated < lastCandle1mCloseTime)
        {
            pause.Text = "";
            pause.Calculated = lastCandle1mCloseTime;

            //GlobalData.AddTextToLogTab("CheckTradingRules()");
            CalculateTradingRules(pause, candleDate, candleDuration);

            GlobalData.StatusesHaveChangedEvent?.Invoke("");
            if (pause.Text != "")
                return false;
        }

        if (pause.Until.HasValue && pause.Until >= lastCandle1mCloseTime)
            return false;
        else
            return true;
    }


    /// Check barometer(s) and cache that value
    public static bool CheckBarometerConditions(CryptoAccount tradeAccount, string quoteName, CryptoTradeSide side, long candleUnixDate, uint candleDuration, out string reaction)
    {
        PauseBarometer? pause = tradeAccount.Data.GetPauseRule(quoteName, side);

        // Ongeveer iedere minuut c.q. candle berekenen
        DateTime lastCandle1mCloseTime = CandleTools.GetUnixDate(candleUnixDate + candleDuration);
        if (!pause.Calculated.HasValue || pause.Calculated < lastCandle1mCloseTime)
        {
            //GlobalData.AddTextToLogTab($"{symbol.QuoteData.Name} CheckBarometerValues()");

            // Als de barometer gedaald is onder de limieten dan "stoppen"
            pause.Text = "";
            pause.Calculated = lastCandle1mCloseTime;

            if (!BarometerHelper.ValidBarometerConditions(tradeAccount, quoteName, TradingConfig.Trading[side].Barometer, out reaction))
                pause.Text = reaction;

            if (pause.Text != "")
            {
                pause.Until = lastCandle1mCloseTime.AddMinutes(5);
                reaction = pause.Text;
                return false;
            }
        }

        // Is al gepauseerd
        if (pause.Until.HasValue && pause.Until >= lastCandle1mCloseTime)
        {
            reaction = pause.Text;
            if (reaction == "")
                reaction = "Barometer low?";
            return false;
        }

        reaction = "";
        return true;
    }

}
