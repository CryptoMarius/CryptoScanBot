using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Json;
using CryptoScanBot.Core.Model;

using System.Text.Json;

namespace CryptoScanBot.ZoneVisualisation.Zones;

[Serializable]
public class CryptoZoneSession
{
    public string SymbolBase { get; set; } = "BTC";
    public string SymbolQuote { get; set; } = "USDT";
    public string IntervalName { get; set; } = "1h";

    //public bool UseHighLow { get; set; } = false;
    public decimal Deviation { get; set; } = 1m;
    // Period = UtcNow - X candles
    public long MinDate { get; set; }
    public long MaxDate { get; set; }
    public CryptoIntervalPeriod ActiveInterval { get; set; } = CryptoIntervalPeriod.interval1h;

    public bool UseOptimizing { get; set; } = true;

    public bool ZoomLiqBoxes { get; set; } = true;
    public bool ShowLiqBoxes { get; set; } = true;
    public bool ShowLiqZigZag { get; set; } = true;

    public bool ShowFib { get; set; } = true;
    public bool ShowFibZigZag { get; set; } = false;
    public bool ShowSecondary { get; set; } = false;
    public bool ShowPoints { get; set; } = false;
    public bool UseBatchProcess { get; set; } = false;
    public bool ForceCalculation { get; set; } = false;
    public bool ShowSignals { get; set; } = false;
    public bool ShowPositions { get; set; } = false;

    public static CryptoZoneSession LoadSessionSettings()
    {
        // load previous Session settings
        string baseFolder = GlobalData.GetBaseDir() + @"Pivots\";
        string filename = baseFolder + $"session.json";
        if (File.Exists(filename))
        {
            string text = File.ReadAllText(filename);
            var session = JsonSerializer.Deserialize<CryptoZoneSession>(text, JsonTools.JsonSerializerIndented);
            if (session != null)
                return session;
        }

        return new();
    }

    public void SaveSessionSettings()
    {
        // save current Session settings
        string baseFolder = GlobalData.GetBaseDir() + @"\Pivots\";
        string filename = baseFolder + $"session.json";
        Directory.CreateDirectory(baseFolder);
        string text = JsonSerializer.Serialize(this, JsonTools.JsonSerializerIndented);
        File.WriteAllText(filename, text);
    }
}