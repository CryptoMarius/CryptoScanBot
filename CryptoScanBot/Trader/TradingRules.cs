using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;
using CryptoScanBot.Settings;

namespace CryptoScanBot.Trader;

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
                                long pauseUntil = candleLast.OpenTime + rule.CoolDown * 60;
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


    //TODO: Side!
    /// Controleer de barometer(s)
    public static bool CheckBarometerValues(PauseRule pause, CryptoQuoteData quoteData, CryptoTradeSide side, CryptoCandle lastCandle1m, out string reaction)
    {
        // Ongeveer iedere minuut c.q. candle berekenen
        DateTime lastCandle1mCloseTime = CandleTools.GetUnixDate(lastCandle1m.OpenTime + 60);
        if (!pause.Calculated.HasValue || pause.Calculated < lastCandle1mCloseTime)
        {
            //GlobalData.AddTextToLogTab($"{symbol.QuoteData.Name} CheckBarometerValues()");

            // Als de barometer gedaald is onder de limieten dan "stoppen"
            pause.Text = "";
            pause.Calculated = lastCandle1mCloseTime;

            if (!BarometerHelper.ValidBarometerConditions(quoteData, TradingConfig.Trading[side].Barometer, out reaction))
                pause.Text = reaction;


            //foreach (KeyValuePair<CryptoIntervalPeriod, (decimal, decimal )> item in TradingConfig.Signals[side].Barometer)
            //{
            //    if (!SymbolTools.CheckValidBarometer(quoteData, item.Key, item.Value, out reaction))
            //    {
            //        pause.Text = reaction;
            //        break;
            //    }
            //}

            if (pause.Text != "")
            {
                pause.Until = lastCandle1mCloseTime.AddMinutes(5);
                reaction = pause.Text;
                return false;
            }
        }

        // Is al gepauseerd
        if (pause.Until >= lastCandle1mCloseTime)
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
