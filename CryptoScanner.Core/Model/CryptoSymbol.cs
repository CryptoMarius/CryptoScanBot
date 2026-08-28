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
    /// Which instrument this is, in one word. It is the part of <see cref="Name"/> behind the dot,
    /// and the badge shown beside the symbol in every grid - both come from here, so they can never
    /// disagree.
    /// <para>
    /// For a market the exchange runs itself this is one of the codes in <see cref="CryptoProduct"/>:
    /// SPOT, PERP, INVERSE, XPERP, FUTURE. For a market an outside party deployed it is the name of
    /// that party instead - HYNA, XYZ - because on a perpetual exchange that is exactly what sets
    /// the line apart: the contract behaves the same, the order book behind it does not.
    /// </para>
    /// <para>
    /// Both live in one field on purpose. They answer the same question - which of the instruments
    /// on this pair is this one - and a second field for the second half would be two places holding
    /// one fact.
    /// </para>
    /// <para>
    /// This is what makes the name unique. BTC-USDT and BTC-USDT-SWAP are two instruments that both
    /// parse to the pair BTCUSDT; BTCUSDT.SPOT and BTCUSDT.PERP are two names. Without it the second
    /// one silently disappears in <see cref="Core.GlobalData.AddSymbol"/>, which is what used to
    /// make a market carry candles that belonged to another instrument.
    /// </para>
    /// </summary>
    public string Product { get; set; } = "";

    /// <summary>
    /// The pair as a reader wants to see it: base and quote, written the way they always were. This
    /// is what the grids show in their symbol column, with the product beside it as a badge - putting
    /// the product in the text as well said the same thing twice ("SUSDT.PERP" next to a PERP badge).
    /// <para>
    /// It carried a hyphen for a day (S-USDT), which reads better for a base of one or two letters
    /// but made people doubt how a symbol is spelled: the black and white list wants BTCUSDT and
    /// splits a rule on a hyphen, so what the grid showed could not be typed straight into a rule.
    /// One spelling everywhere beats a readable one in one place.
    /// </para>
    /// <para>
    /// For reading only. <see cref="Name"/> is the key and carries the product, because a name has
    /// to be unique; this one does not and may repeat - two instruments on the same pair show the
    /// same text and are told apart by their badge.
    /// </para>
    /// </summary>
    [Computed]
    [JsonIgnore]
    public string PairName
    {
        get
        {
            _pairName ??= Base + Quote;
            return _pairName;
        }
    }
    private string? _pairName;

    /// <summary>
    /// The product badge shown beside the symbol in the grids: the product, but only on a market
    /// that holds more than one product - on a single-product market a badge that is always the
    /// same marks nothing. One property so both UIs and every grid agree on when the badge shows.
    /// </summary>
    [Computed]
    [JsonIgnore]
    public string MarketLabel => Exchange != null && Exchange.HasSeveralProducts ? Product : "";

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
