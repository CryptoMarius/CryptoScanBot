using CryptoScanner.Core.Enums;

using Dapper.Contrib.Extensions;

using System.Text.Json.Serialization;

namespace CryptoScanner.Core.Model;

[Table("Symbol")]
public partial class CryptoSymbol
{
    [Key]
    public int Id { get; set; }
    public int ExchangeId { get; set; }
    [Computed]
    public virtual required CryptoExchange Exchange { get; set; }

    public required string Name { get; set; }
    public required string Base { get; set; } // (ADA, NKN, LTC, etc)
    public required string Quote { get; set; } // (BTC, ETH, EUR, USDT etc)
    public required string ExchangeName { get; set; }
    /// <summary>
    /// Which market inside the exchange this symbol lives on, empty for the exchange's own market.
    /// <para>
    /// One exchange can run several order books side by side. HyperLiquid lets outside parties deploy
    /// their own perpetual market on its infrastructure - same address, same account, same USDC as
    /// margin - and names those markets after their deployer: the gold market of the party calling
    /// itself XYZ is "xyz:GOLD", next to the plain "BTC" of HyperLiquid's own market. This field holds
    /// that short name ("xyz", "hyna"), so the symbol list can tell the two apart and the user
    /// interface can mark them.
    /// </para>
    /// <para>
    /// It is not a contract type: everything the scanner accepts is a linear perpetual, whichever
    /// market it comes from. What differs is the order book, its depth, and who runs it.
    /// </para>
    /// </summary>
    public string SubMarket { get; set; } = "";

    /// <summary>
    /// How the symbol is written wherever a user reads it: the name, followed by the market it lives
    /// on when that is not the exchange's own one. "BTCUSDC" stays "BTCUSDC", "xyz:GOLD" reads as
    /// "XYZGOLDUSDC (xyz)".
    /// <para>
    /// Text rather than a picture on purpose: every grid in both user interfaces shows the symbol in
    /// a plain text column, so this reaches all of them at once - the symbol list, the signals, the
    /// positions, the live data, the dashboard and the chart selector. A graphic would need a
    /// template column in each of those grids separately.
    /// </para>
    /// <para>
    /// Precomputed on first use and cached, the same reason the view models cache their own texts:
    /// a grid asks for this on every scroll, and building the string each time is what made other
    /// columns slow.
    /// </para>
    /// <para>
    /// Never use this to look a symbol up or to hand a name to an exchange - that is what
    /// <see cref="Name"/> and <see cref="ExchangeName"/> are for.
    /// </para>
    /// </summary>
    [Computed]
    [JsonIgnore]
    public string DisplayName
    {
        get
        {
            _displayName ??= SubMarket.Length > 0 ? $"{Name} ({SubMarket})" : Name;
            return _displayName;
        }
    }
    private string? _displayName;

    public int Status { get; set; } // 0 for inactive, 1 voor active

    // The minimal quantity of an order
    public decimal QuantityMinimum { get; set; }
    //The maximum quantity of an order
    public decimal QuantityMaximum { get; set; }
    // The tick size of the quantity. The quantity can not have more precision as this and can only be incremented in steps of this.
    public decimal QuantityTickSize { get; set; }
    [Computed]
    public string QuantityDisplayFormat { get; set; } = "N8";


    // The minimal value of an order
    public decimal QuoteValueMinimum { get; set; }
    // The maximum value of an order
    public decimal QuoteValueMaximum { get; set; }


    //The minimal price the order can be for
    public decimal PriceMinimum { get; set; }
    //The max price the order can be for
    public decimal PriceMaximum { get; set; }
    //The tick size of the price. The price can not have more precision as this and
    //can only be incremented in steps of this.
    public decimal PriceTickSize { get; set; }
    [Computed]
    public byte PriceDecimals { get; set; } = 2;
    [Computed]
    public string PriceDisplayFormat { get; set; } = "N8";

    // TODO: never used in this scanner sofar
    //public bool IsSpotTradingAllowed { get; set; }
    //public bool IsMarginTradingAllowed { get; set; }


    // Bybit Perpetual, ondersteunen van de FundingRate en FundingInterval
    // Wat het inhoud weet ik niet maar toegevoegde waarde is er voor het traden wel.
    // https://bybit-exchange.github.io/docs/v5/market/History-fund-rate
    public decimal FundingRate { get; set; }
    // (minute)
    public decimal FundingInterval { get; set; }


    // Last value from the symbol ticker or candle.Close
    [Computed]
    public decimal? LastPrice { get; set; } = null;

    // Volume in the last 24 hour
    public double Volume { get; set; }

    /// <summary>
    /// For fetching the trades
    /// </summary>
    public long? LastTradeIdFetched { get; set; }
    public DateTime? LastTradeFetched { get; set; }


    /// <summary>
    /// Last time we traded on this symbol (cooldown)
    /// </summary>
    public DateTime? LastTradeDate { get; set; }

    /// <summary>
    /// Close time of the last position on this symbol that ended at a loss, for the loss cooldown
    /// (SettingsTrading.LossCooldownTime). Never cleared by a later winning trade: the cooldown
    /// expires on its own, and by then this value no longer influences anything.
    /// </summary>
    public DateTime? LastLossDate { get; set; }

    [Computed]
    // Quote: display format
    public required virtual CryptoQuoteData QuoteData { get; set; }

    [Computed]
    // Interval related data like candles and last candle fetched
    public CryptoSymbolData Data { get; set; } = new();

    public CryptoSymbolInterval GetSymbolInterval(CryptoIntervalPeriod intervalPeriod)
    {
        return Data.SymbolIntervalList[(int)intervalPeriod];
    }

    public CryptoSymbolInterval GetSymbolInterval(CryptoInterval interval)
    {
        return Data.SymbolIntervalList[(int)interval.IntervalPeriod];
    }

    // Hysteresis around QuoteData.MinimalVolume. A symbol whose 24 hour volume hovers around the limit
    // would otherwise flip on every refresh, and the two states are treated very differently: candles
    // fetched or not, kept or released, ticker subscribed or not. Coming in still costs the same
    // 0.9 x MinimalVolume as before, but dropping out now needs a clearly lower volume, so a symbol in
    // the band between the two keeps whatever answer it had.
    public const double VolumeThresholdEnterFactor = 0.9;
    public const double VolumeThresholdLeaveFactor = 0.75;

    /// <summary>
    /// Outcome of the last <see cref="UpdateEnoughVolume"/>; null means no decision was taken yet
    /// (a symbol straight from the database). Not persisted — the first refresh decides again.
    /// </summary>
    [Computed]
    public bool? VolumeAboveThreshold { get; set; }

    /// <summary>
    /// The single place where the "enough volume" decision changes. Call it once per refresh cycle,
    /// after the 24 hour volume has been updated; everything else reads the outcome through
    /// <see cref="EnoughVolume"/> so the whole cycle sees one consistent answer. Deliberately not
    /// done from the Volume setter: the price ticker updates that value continuously, and the
    /// decision must not move underneath a cycle that is already running.
    /// </summary>
    public void UpdateEnoughVolume()
    {
        if (QuoteData == null || QuoteData.MinimalVolume == 0)
        {
            VolumeAboveThreshold = true;
            return;
        }

        double factor = VolumeAboveThreshold == true ? VolumeThresholdLeaveFactor : VolumeThresholdEnterFactor;
        VolumeAboveThreshold = Volume > factor * QuoteData.MinimalVolume;
    }

    public bool EnoughVolume()
    {
        if (QuoteData == null || QuoteData.MinimalVolume == 0)
            return true;
        // No decision taken yet: answer exactly as this method did before the hysteresis was added,
        // without writing state from what callers expect to be a plain read.
        if (VolumeAboveThreshold == null)
            return Volume > VolumeThresholdEnterFactor * QuoteData.MinimalVolume;
        return VolumeAboveThreshold.Value;
    }

    public bool ClearCandles()
    {
        int count = 0;
        foreach (var symbolInterval in Data.SymbolIntervalList)
        {
            count += symbolInterval.CandleList.Count;
            symbolInterval.CandleList.Clear();
        }
        return count > 0;
    }

    public bool IsTrading()
    {
        return Exchange!.Data.PositionList.ContainsKey(Name);
    }
}
