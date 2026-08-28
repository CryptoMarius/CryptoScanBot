using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Trader;

public static class TradingRules
{
    private static void CalculateTradingRules(PauseTradingRule pause, CandleTime candleUnixDate, uint candleDuration)
    {
        // Als een munt (met name BTC) snel gedaald is dan stoppen
        var exchange = GlobalData.ActiveExchange;
        if (exchange != null)
        {
            int index = 0;
            foreach (Settings.PauseTradingRule rule in GlobalData.Settings.Trading.PauseTradingRules)
            {
                index++;
                // A rule without a symbol of its own watches the bitcoin pair of the exchange we are on.
                // Every exchange spells that differently (BTCUSDT, BTCUSD, XBTUSDC, UBTCUSDC), so this
                // keeps the rule working after switching exchange instead of pointing at a symbol that
                // is not listed there.
                string ruleSymbol = rule.Symbol;
                if (string.IsNullOrEmpty(ruleSymbol))
                    ruleSymbol = Exchange.ExchangeBase.ExchangeOptions.PauseSymbol;

                // The rule holds a bare pair (BTCUSDT) while the symbol list is keyed on the
                // product-suffixed name (BTCUSDT.PERP), so resolve via the pair lookup.
                if (exchange.TryGetSymbolByPair(ruleSymbol, out CryptoSymbol? symbol))
                {
                    CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(rule.Interval);
                    if (symbolInterval.CandleList.Count != 0)
                    {
                        bool missingCandles = false;
                        decimal low = decimal.MaxValue;
                        decimal high = decimal.MinValue;
                        CandleTime loop = IntervalTools.StartOfIntervalCandle2(candleUnixDate, candleDuration, symbolInterval.Interval.Duration);
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
                            if (symbolInterval.CandleList.TryGetValue(loop, out CryptoCandle candle))
                            {
                                low = Math.Min(low, candle.Low);
                                high = Math.Max(high, candle.High);
                                //GlobalData.AddTextToLogTab(candle.OhlcText(symbol, symbolInterval.Interval, symbol.PriceDisplayFormat));
                            }
                            else
                            {
                                missingCandles = true;
                                //GlobalData.AddTextToLogTab($"Missing candles for tradingrules? {symbol.Name} {candleUnixDate.ToDateTime()} {symbolInterval.Interval.Name} {loop.ToDateTime()}  (debug2)");
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
                                CandleTime pauseUntil = candleUnixDate + candleDuration * rule.CoolDown; // * 60;
                                DateTime pauseUntilDate = pauseUntil.ToDateTime();

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
                // An empty symbol list is not a missing coin: it is an exchange that has not been read
                // yet, or one that was just cleared because the user switched to another exchange while
                // this loader was still finishing. Reporting "does not exist" there is misleading -
                // BTCUSDT is listed on every exchange this ever ran on.
                else if (exchange.SymbolListName.Count == 0)
                    GlobalData.AddTextToLogTab($"Pause rule: the symbol list of {exchange.Name} is not available (yet), rule #{index} skipped");
                else
                    GlobalData.AddErrorToLogTab($"Pause rule: symbol {ruleSymbol} does not exist on {exchange.Name}");
            }
        }
    }


    public static bool CheckTradingRules(PauseTradingRule pause, CandleTime candleDate, uint candleDuration)
    {
        // Controleer de trading pauseer regels


        // Ongeveer iedere minuut c.q. candle berekenen
        DateTime lastCandle1mCloseTime = (candleDate + candleDuration).ToDateTime();
        if (!pause.Calculated.HasValue || pause.Calculated < lastCandle1mCloseTime)
        {
            pause.Text = "";
            pause.Calculated = lastCandle1mCloseTime;

            //GlobalData.AddTextToLogTab("CheckTradingRules()");
            CalculateTradingRules(pause, candleDate, candleDuration);

            GlobalData.SendMvvmMessage(new StatusesHaveChangedMessage());
            if (pause.Text != "")
                return false;
        }

        if (pause.Until.HasValue && pause.Until >= lastCandle1mCloseTime)
            return false;
        else
            return true;
    }


    /// Check barometer(s) and cache that value
    public static bool CheckBarometerConditions(Model.CryptoExchange activeExchange,
        string quoteName, CryptoTradeSide side, CandleTime candleUnixDate, uint candleDuration, out string reaction)
    {
        reaction = "";
        CryptoPauseBarometer? pause = activeExchange.Data.GetPauseRule(quoteName, side);

        // Ongeveer iedere minuut c.q. candle berekenen
        DateTime lastCandle1mCloseTime = (candleUnixDate + candleDuration).ToDateTime();
        if (!pause.Calculated.HasValue || pause.Calculated < lastCandle1mCloseTime)
        {
            //GlobalData.AddTextToLogTab($"{symbol.QuoteData.Name} CheckBarometerValues()");

            // Als de barometer gedaald is onder de limieten dan "stoppen"
            pause.Text = "";
            pause.Calculated = lastCandle1mCloseTime;

            if (!BarometerHelper.ValidBarometerConditions(activeExchange, quoteName, TradingConfig.Trading[side].Barometer, out reaction))
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
            if (pause.Text != null)
                reaction = pause.Text;
            if (reaction == "")
                reaction = "Barometer low?";
            return false;
        }

        reaction = "";
        return true;
    }

}
