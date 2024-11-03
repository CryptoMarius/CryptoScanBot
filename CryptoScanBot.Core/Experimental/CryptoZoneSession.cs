using CryptoScanBot.Core.Intern;

using System.Text.Json;

namespace CryptoScanBot.Core.Experimental;

[Serializable]
public class CryptoZoneSession
{
    public string SymbolBase { get; set; } = "BTC";
    public string SymbolQuote { get; set; } = "USDT";
    public string IntervalName { get; set; } = "1h";

    public bool UseHighLow { get; set; } = false;

    public bool ZoomLiqBoxes { get; set; } = true;
    public bool ShowLiqBoxes { get; set; } = true;

    public bool ShowZigZag { get; set; } = true;
    public decimal OptimizeZigZag { get; set; } = 1m;


    public static CryptoZoneSession LoadSessionSettings()
    {
        // load previous Session settings
        string baseFolder = GlobalData.GetBaseDir() + @"Pivots\";
        string filename = baseFolder + $"session.json";
        if (File.Exists(filename))
        {
            string text = File.ReadAllText(filename);
            return JsonSerializer.Deserialize<CryptoZoneSession>(text, CryptoCandles.JsonSerializerIndented);
        }

        return new();
    }

    public void SaveSessionSettings()
    {
        // save current Session settings
        string baseFolder = GlobalData.GetBaseDir() + @"\Pivots\";
        string filename = baseFolder + $"session.json";
        Directory.CreateDirectory(baseFolder);
        string text = JsonSerializer.Serialize(this, CryptoCandles.JsonSerializerIndented);
        File.WriteAllText(filename, text);
    }
}