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

    // Color of the base coin in signal grid
    public CoreColor DisplayColor { get; set; } = CoreColor.FromArgb(0x00, 0xFF, 0x95, 0xA5);

    // List of symbols (for this quote)
    // Rebuilt by ThreadLoadData.IndexQuoteDataSymbols, at startup and on every hourly symbol refresh.
    // It is replaced as a whole instead of cleared and refilled: the barometer walks it by index
    // without a lock, and a reader that started on the old list simply finishes on the old list.
    [Computed]
    [JsonIgnore]
    public List<CryptoSymbol> SymbolList { get; set; } = [];

    // The pausing values for each side
    [Computed]
    [JsonIgnore]
    public Dictionary<CryptoTradeSide, CryptoPauseBarometer> PauseBarometerList { get; set; } = [];

    // The barometer values for each interval
    [Computed]
    [JsonIgnore]
    public Dictionary<CryptoIntervalPeriod, CryptoBarometerData> BarometerDataList { get; set; } = [];

    /// <summary>
    /// The instruments the exchange offers for this quote at its symbol refresh (by their name at the
    /// exchange), counted before any filter: also those of a switched off product. So the settings
    /// screen can show what there is to trade, not only what is fetched now (31-10-2026). Not saved,
    /// every refresh fills it again; an instrument that left the exchange is counted until a restart.
    /// </summary>
    private readonly HashSet<string> _offeredInstruments = [];

    public void NoteOffered(string instrument)
    {
        lock (_offeredInstruments)
            _offeredInstruments.Add(instrument);
    }

    [Computed]
    [JsonIgnore]
    public int OfferedCount
    {
        get
        {
            lock (_offeredInstruments)
                return _offeredInstruments.Count;
        }
    }



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
