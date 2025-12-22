using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Json;

using System.Text.Json;

namespace CryptoScanner.Core.Zones;

public enum TrendLines
{
    PrimaryTrend,
    SecondaryTrend,
    DlzZoneTrend,
    None,
}


[Serializable]
public class ZoneSession
{
    // symbol
    public string SymbolBase { get; set; } = "BTC";
    public string SymbolQuote { get; set; } = "USDT";
    public string IntervalName { get; set; } = "1h";

    // Period = UtcNow - X candles
    public long MinDate { get; set; }
    public long MaxDate { get; set; }
    public CryptoIntervalPeriod ActiveInterval { get; set; } = CryptoIntervalPeriod.interval1h;

    // hidden
    public bool UseOptimizing { get; set; } = false;
    public decimal Deviation { get; set; } = 1m;

    // trend
    public TrendType TrendType { get; set; } = TrendType.Primary;
    public bool TrendShowZigZag { get; set; } = false;

    // dlz
    public bool DlzShowBoxes { get; set; } = true;

    // fib
    public TrendType FibTrend { get; set; } = TrendType.Primary;
    public bool FibShowRetracement { get; set; } = false;
    public bool FibShowZigZag { get; set; } = false;

    // misc
    public bool ShowPoints { get; set; } = false;
    public bool ForceCalculation { get; set; } = false;
    public bool ShowSignals { get; set; } = false;
    public bool ShowFvgZones { get; set; } = false;
    public bool ShowDtb { get; set; } = false;
    public bool ShowNadarayaWatsonEnvelope { get; set; } = true;
    public bool ShowNadarayaWatsonEnvelopeRepainting { get; set; } = true;
    public bool ShowBollingerBand { get; set; } = false;
    public bool ShowSmaLinesSbm { get; set; } = true;
    public bool ShowTrendLines { get; set; } = false;


    public static ZoneSession LoadSessionSettings()
    {
        // load previous Session settings
        string folderName = Path.Combine(GlobalData.GetBaseDir(), "Pivots");
        string fileName = Path.Combine(folderName, $"session.json");
        if (File.Exists(fileName))
        {
            string text = File.ReadAllText(fileName);
            var session = JsonSerializer.Deserialize<ZoneSession>(text, JsonTools.DeSerializerOptions);
            if (session != null)
                return session;
        }

        return new();
    }

    public void SaveSessionSettings()
    {
        // save current Session settings
        string folderName = Path.Combine(GlobalData.GetBaseDir(), "Pivots");
        string fileName = Path.Combine(folderName, $"session.json");
        Directory.CreateDirectory(folderName);
        string text = JsonSerializer.Serialize(this, JsonTools.JsonSerializerIndented);
        File.WriteAllText(fileName, text);
    }
}