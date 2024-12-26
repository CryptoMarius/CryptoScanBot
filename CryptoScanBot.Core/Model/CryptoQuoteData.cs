using CryptoScanBot.Core.Json;

using Dapper.Contrib.Extensions;

using System.Text.Json.Serialization;

namespace CryptoScanBot.Core.Model;

public class CryptoQuoteData
{
    public required string Name { get; set; }

    public string DisplayFormat { get; set; } = "N8";

    // Basecoin data
    public bool FetchCandles { get; set; }
    public decimal MinimalVolume { get; set; }
    public decimal MinimalPrice { get; set; }
    // Trading: The initial entry amount
    public decimal EntryAmount { get; set; }
    // Trading: The initial entry percentage of PF
    public decimal EntryPercentage { get; set; }
    // Color of the base coin in signal grid
    [JsonConverter(typeof(ColorConverter))]
    public System.Drawing.Color DisplayColor { get; set; } = System.Drawing.Color.White;

    // List of symbols (for this quote)
    [Computed]
    [JsonIgnore]
    public List<CryptoSymbol> SymbolList { get; } = [];
}


