using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

using Dapper.Contrib.Extensions;

using System.Text.Json.Serialization;

namespace CryptoScanner.Core.Model;

public class CryptoQuoteData
{
    public required string Name { get; set; }

    public string DisplayFormat { get; set; } = "N8";

    // Basecoin data
    public bool FetchCandles { get; set; }
    public double MinimalVolume { get; set; }
    public decimal MinimalPrice { get; set; }
    // Trading: The initial entry amount
    public decimal EntryAmount { get; set; }
    // Trading: The initial entry percentage of PF
    public float EntryPercentage { get; set; }

    /// <summary>
    /// Trading: The start capital handed out to this quote coin in paper trading and the emulator,
    /// expressed in the quote coin itself. Zero means "not filled in", and then the general
    /// <see cref="Settings.SettingsTrading.PaperAssetStartCapital"/> is used - or the amount typed in
    /// the paper-assets screen or in an emulator run.
    /// <para>
    /// One amount for every quote coin cannot work: 10.000 is a sensible amount of USDT and an absurd
    /// amount of BTC, while the capital line adds every coin up in USDT (see
    /// <see cref="Trader.AssetSnapshotTools.ReferenceCoin"/>).
    /// </para>
    /// </summary>
    public decimal StartCapital { get; set; }

    // Color of the base coin in signal grid
    public CoreColor DisplayColor { get; set; } = CoreColor.FromArgb(0x00, 0xFF, 0x95, 0xA5);

    // List of symbols (for this quote)
    [Computed]
    [JsonIgnore]
    public List<CryptoSymbol> SymbolList { get; } = [];

    // The pausing values for each side
    [Computed]
    [JsonIgnore]
    public Dictionary<CryptoTradeSide, CryptoPauseBarometer> PauseBarometerList { get; set; } = [];

    // The barometer values for each interval
    [Computed]
    [JsonIgnore]
    public Dictionary<CryptoIntervalPeriod, CryptoBarometerData> BarometerDataList { get; set; } = [];



    public CryptoQuoteData()
    {
        // Initialize sides
        PauseBarometerList = new()
        {
            { CryptoTradeSide.Long, new CryptoPauseBarometer() },
            { CryptoTradeSide.Short, new CryptoPauseBarometer() }
        };

        // Initialize intervals
        for (CryptoIntervalPeriod interval = CryptoIntervalPeriod.interval1m; interval <= CryptoIntervalPeriod.interval1w; interval++)
            BarometerDataList[interval] = new CryptoBarometerData();
    }
}
