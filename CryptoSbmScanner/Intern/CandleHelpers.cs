using System.Globalization;
using System.Text;

using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Intern;

public static class Helper
{
    public static decimal ConvertRadiansToDegrees(this decimal radians)
    {
        double degrees = (double)radians * (180 / Math.PI);
        return (decimal)degrees;
    }


    public static decimal ConvertDegreesToRadians(this decimal degrees)
    {
        double radians = (double)degrees * (Math.PI / 180);
        return (decimal)radians;
    }


    /// <summary>Remove trailing zeroes on the decimal.</summary>
    /// <param name="value">The value to normalize.</param>
    /// From the CryptoAdvisor sources, thanks!
    /// <returns>1.230000 becomes 1.23</returns>
    public static decimal Normalize(this decimal value)
    {
        return value / 1.000000000000000000000000000000000m;
    }

    public static decimal GetLowest(this CryptoCandle candle, bool useWicks)
    {
        if (useWicks)
            return (candle.Low);
        else
            return Math.Min(candle.Open, candle.Close);
    }

    public static decimal GetHighest(this CryptoCandle candle, bool useWicks)
    {
        if (useWicks)
            return (candle.High);
        else
            return Math.Max(candle.Open, candle.Close);
    }
    
    
    public static string OhlcText(this CryptoCandle candle, CryptoSymbol symbol, CryptoInterval interval, 
        string fmt, bool includeSymbol = false, bool includeInterval = false, bool includeVolume = false)
    {
        // Include the next time so it is clear what candle has focus (it saves a lot of questions)
        DateTime date = CandleTools.GetUnixDate(candle.OpenTime);
        string s = date.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + "-" + date.AddSeconds(interval.Duration).ToLocalTime().ToString("HH:mm");

        if (includeSymbol)
            s += " " + symbol.Name;

        if (includeInterval)
            s = s + " interval=" + interval.Name;

        s = s + " open=" + candle.Open.ToString(fmt);
        s = s + " high=" + candle.High.ToString(fmt);
        s = s + " low=" + candle.Low.ToString(fmt);
        s = s + " close=" + candle.Close.ToString(fmt);
        if (includeVolume)
            s = s + " volume=" + candle.Volume.ToString();
        return s;
    }

    /// <summary>
    /// Remove any trailing 0's 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="fmt"></param>
    /// <returns></returns>
    public static string ToString0(this decimal? value, string fmt = "N8")
    {
        // Een alternatief hievoor is de Normalize() functie herboven
        // (maar dat zal qua performance niet veel uitmaken denk ik)
        string text = value.HasValue ? ((decimal)value).ToString(fmt) : "0"; //Get the stock string

        //If there is a decimal point present
        string seperator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        if (text.Contains(seperator))
        {
            //Remove all trailing zeros
            text = text.TrimEnd('0');

            //If all we are left with is a decimal point
            if (text.EndsWith(seperator)) //then remove it
                text = text.TrimEnd(seperator[0]);
        }

        return text;
    }

    /// <summary>
    /// Remove any trailing 0's 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="fmt"></param>
    /// <returns></returns>
    public static string ToString0(this decimal value, string fmt = "N15")
    {
        string text = value.ToString(fmt); //Get the stock string

        //If there is a decimal point present
        string seperator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        if (text.Contains(seperator))
        {
            //Remove all trailing zeros
            text = text.TrimEnd('0');

            //If all we are left with is a decimal point
            if (text.EndsWith(seperator)) //then remove it
                text = text.TrimEnd(seperator[0]);
        }

        return text;
    }


    /// <summary>
    /// Clamp a decimal to a min and max value
    /// </summary>
    /// <param name="minValue">Min value</param>
    /// <param name="maxValue">Max value</param>
    /// <param name="stepSize">Smallest unit value should be evenly divisible by</param>
    /// <param name="value">Value to clamp</param>
    /// Uit de CryptoAdvisor sources, thanks!
    /// <returns>Clamped value</returns>
    public static decimal Clamp(this decimal value, decimal minValue, decimal maxValue, decimal? stepSize)
    {
        // TODO: Bybit heeft geen min- of maxPrice!?
        // Deze moeten dus nullable worden en moeten hieronder gecontroleerd worden

        if (minValue < 0)
            throw new ArgumentOutOfRangeException(nameof(minValue));
        else if (maxValue < 0)
            throw new ArgumentOutOfRangeException(nameof(maxValue));
        else if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        else if (minValue > maxValue)
            throw new ArgumentOutOfRangeException(nameof(minValue));

        if (stepSize.HasValue)
        {
            if (stepSize < 0)
                throw new ArgumentOutOfRangeException(nameof(stepSize));
            decimal mod = value % stepSize.Value;
            value -= mod;
        }

        if (maxValue > 0)
            value = Math.Min(maxValue, value);

        value = Math.Max(minValue, value);

        return value.Normalize();
    }



    public static bool IsBarometerSymbol(this CryptoSymbol symbol)
    {
        return symbol.Base.StartsWith("$"); // de $BMV (Volume) of $BMP (Price)
        //return ((symbol.Base.Equals(Constants.SymbolNameBarometerPrice)) || (symbol.Base.Equals(Constants.SymbolNameBarometerVolume));
    }


    public static bool CheckValidMinimalVolume(this CryptoSymbol symbol, out string text)
    {
        if (symbol.QuoteData.MinimalVolume > 0)
        {
            // Controleer of de munt actief is (beetje raar)
            if (!symbol.QuoteData.FetchCandles)
            {
                text = string.Format("{0} Er worden geen candles opgehaald", symbol.Name);
                return false;
            }

            // Controleer of er genoeg volume is (van de afgelopen 24 uur)
            if (symbol.Volume < symbol.QuoteData.MinimalVolume)
            {
                text = string.Format("{0} 24 uur volume {1} onder het minimum {2}", symbol.Name, symbol.Volume.ToString0(), symbol.QuoteData.MinimalVolume.ToString0());
                return false;
            }
        }

        text = "";
        return true;
    }

    public static bool CheckValidMinimalPrice(this CryptoSymbol symbol, out string text)
    {
        // Controleer of de munt actief is (beetje raar)
        if (!symbol.QuoteData.FetchCandles)
        {
            text = string.Format("{0} Er worden geen candles opgehaald", symbol.Name);
            return false;
        }

        // Controleer de prijs van de munt
        if (symbol.QuoteData.MinimalPrice > 0 && symbol.LastPrice < symbol.QuoteData.MinimalPrice)
        {
            text = string.Format("{0} Prijs {1} onder het minimum {2}", symbol.Name, symbol.LastPrice.ToString0(), symbol.QuoteData.MinimalPrice.ToString0());
            return false;
        }

        text = "";
        return true;
    }


    public static bool CheckBollingerBandsWidth(this CryptoCandle candle, double minValue, double maxValue)
    {
        double boundary = minValue;
        if (boundary > 0 && candle.CandleData.BollingerBandsPercentage <= boundary)
            return false;

        boundary = maxValue;
        if (boundary > 0 && candle.CandleData.BollingerBandsPercentage >= boundary)
            return false;

        return true;
    }


    public static bool IsBelowBollingerBands(this CryptoCandle candle, bool useLowHigh)
    {
        // Geopend of gesloten onder de bollinger band
        decimal value;
        if (useLowHigh)
            value = candle.Low;
        else
            value = Math.Min(candle.Open, candle.Close);
        double? band = candle.CandleData.Sma20 - candle.CandleData.BollingerBandsDeviation;
        //band = band.Clamp(candle.Symbol.PriceMinimum, candle.Symbol.PriceMaximum, candle.Symbol.PriceTickSize);
        if (value <= (decimal)band)
            return true;
        return false;
    }


    public static bool IsAboveBollingerBands(this CryptoCandle candle, bool useLowHigh)
    {
        // Geopend of gesloten boven de bollinger band
        decimal value;
        if (useLowHigh)
            value = candle.High;
        else
            value = Math.Max(candle.Open, candle.Close);
        double? band = candle.CandleData.Sma20 + candle.CandleData.BollingerBandsDeviation;
        //band = band.Clamp(candle.Symbol.PriceMinimum, candle.Symbol.PriceMaximum, candle.Symbol.PriceTickSize);
        if (value >= (decimal)band)
            return true;
        return false;
    }


    public static bool IsBetween<T>(this T item, T start, T end)
    {
        return Comparer<T>.Default.Compare(item, start) >= 0
            && Comparer<T>.Default.Compare(item, end) <= 0;
    }



    public static bool InsideBoundaries(this CryptoSymbol symbol, decimal? quantity, decimal? price, out string text)
    {
        if (quantity.HasValue)
        {
            if (quantity < symbol.QuantityMinimum)
            {
                text = string.Format("ERROR minimum quantity {0} < {1}", quantity.ToString0("N6"), symbol.QuantityMinimum.ToString0());
                return false;
            }
            if (symbol.QuantityMaximum > 0 && quantity > symbol.QuantityMaximum)
            {
                text = string.Format("ERROR maximum quantity {0} > {1}", quantity.ToString0("N6"), symbol.QuantityMaximum.ToString0());
                return false;
            }
        }


        if (price.HasValue)
        {
            if (price < symbol.PriceMinimum)
            {
                text = string.Format("ERROR minimum price {0} < {1}", price.ToString0("N6"), symbol.PriceMinimum.ToString0());
                return false;
            }
            if (symbol.PriceMaximum > 0 && price > symbol.PriceMaximum)
            {
                text = string.Format("ERROR maximum price {0} > {1}", price.ToString0("N6"), symbol.PriceMaximum.ToString0());
                return false;
            }
        }


        //if (quantity.HasValue && price.HasValue)
        //{
        //    // En product van de twee
        //    if (price * quantity <= symbol.MinNotional)
        //    {
        //        //(buyPrice * buyQuantity).ToString0()
        //        text = string.Format("ERROR minimal notation {0} * {1} <= {2}", quantity.ToString0("N6"), price.ToString0("N6"), symbol.MinNotional.ToString0());
        //        return false;
        //    }
        //}

        text = "";
        return true;
    }


    static public void ShowAssets(CryptoTradeAccount tradeAccount, StringBuilder stringBuilder, out decimal valueUsdt, out decimal valueBtc)
    {
        valueBtc = 0;
        valueUsdt = 0;

        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
        {
            tradeAccount.AssetListSemaphore.Wait();
            {
                try
                {
                    try
                    {
                        stringBuilder.AppendLine("Assets:");

                        //AddTextToLogTab("Assets changed");
                        foreach (CryptoAsset asset in tradeAccount.AssetList.Values)
                        {
                            if (asset.Total.ToString0() == asset.Free.ToString0())
                                stringBuilder.AppendLine(string.Format("{0} {1}", asset.Name, asset.Total.ToString0()));
                            else
                                stringBuilder.AppendLine(string.Format("{0} {1} Free={2}", asset.Name, asset.Total.ToString0(), asset.Free.ToString0()));


                            CryptoSymbol symbol;
                            if (asset.Name == "USDT")
                                valueUsdt += asset.Total;
                            else if (exchange.SymbolListName.TryGetValue(asset.Name + "USDT", out symbol))
                            {
                                if (symbol.LastPrice.HasValue)
                                    valueUsdt += (decimal)symbol.LastPrice * asset.Total;
                            }
                            else if (exchange.SymbolListName.TryGetValue("USDT" + asset.Name, out symbol))
                            {
                                if (symbol.LastPrice.HasValue)
                                    valueUsdt += asset.Total / (decimal)symbol.LastPrice;
                            }


                            if (asset.Name == "BTC")
                                valueBtc += asset.Total;
                            else if (exchange.SymbolListName.TryGetValue(asset.Name + "BTC", out symbol))
                            {
                                if (symbol.LastPrice.HasValue)
                                    valueBtc += (decimal)symbol.LastPrice * asset.Total;
                            }
                            else if (exchange.SymbolListName.TryGetValue("BTC" + asset.Name, out symbol))
                            {
                                if (symbol.LastPrice.HasValue)
                                    valueBtc += asset.Total / (decimal)symbol.LastPrice;
                            }
                        }
                        stringBuilder.AppendLine(string.Format("Totaal USDT=${0} BTC=₿{1}", valueUsdt.ToString0("N2"), valueBtc.ToString0("N8")));
                    }
                    catch (Exception error)
                    {
                        stringBuilder.AppendLine(string.Format("ERROR assets " + error.ToString()));
                        GlobalData.Logger.Error(error, "ERROR assets");
                    }
                    // Dat doet de aanroepende partij (telegram of knop Show wallets)
                    //GlobalData.AddTextToLogTab(stringBuilder.ToString());
                }
                finally
                {
                    tradeAccount.AssetListSemaphore.Release();
                }
            }
        }
    }


    static public void ShowPosition(StringBuilder stringBuilder, CryptoPosition position)
    {
        decimal diffPercentage = 0;
        decimal investedInTrades = position.Invested - position.Returned; // - position.Commission;
        if (position.Symbol.LastPrice.HasValue && position.SellPrice.HasValue)
        {
            decimal currentValue = (position.Quantity * (decimal)position.Symbol.LastPrice) - position.Commission;
            diffPercentage = (100 * (currentValue / investedInTrades)) - 100;
        }


        string s = string.Format("{0} {1}  {2}%", position.Symbol.Name,
            investedInTrades.ToString("N2"), diffPercentage.ToString0("N2"));
        //if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.BackTest)
        //    s += string.Format(" ({0})", position.TradeAccount.Name);
        //else if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.PaperTrade)
        //    s += string.Format(" ({0})", position.TradeAccount.Name);
        if (position.PartCount > 1)
            s += string.Format(" ({0})", position.PartCount);
        stringBuilder.AppendLine(s);
        //positionCount++;
    }


    static public void ShowPositions(StringBuilder stringBuilder)
    {
        int positionTotal = 0;
        foreach (var tradeAccount in GlobalData.TradeAccountList.Values)
        {
            if (tradeAccount.PositionList.Any())
            {
                int positionCount = 0;
                stringBuilder.AppendLine(tradeAccount.Name);
                foreach (var positionList in tradeAccount.PositionList.Values)
                {
                    //De muntparen toevoegen aan de userinterface
                    foreach (CryptoPosition position in positionList.Values)
                    {
                        ShowPosition(stringBuilder, position);
                        positionCount++;
                        positionTotal++;
                    }
                }
                stringBuilder.AppendLine(string.Format("{0} posities", positionCount));
            }
        }
        if (positionTotal == 0)
            stringBuilder.AppendLine("no posities");
    }
}
