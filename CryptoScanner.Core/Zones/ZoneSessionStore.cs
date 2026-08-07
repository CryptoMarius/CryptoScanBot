using CryptoScanner.Core.Core;
using CryptoScanner.Core.Json;

using System.Text.Json;

namespace CryptoScanner.Core.Zones;

/// <summary>
/// Reads and writes the chart session (which overlays/panels are shown, the selected symbol and
/// interval) from "CryptoScanBot-chart.json" in the data folder.
/// <para>
/// The Avalonia chart window had this inline; moving it to Core lets the Blazor chart page
/// remember the same settings — it used plain fields with hardcoded defaults, so every visit
/// reset all toggles.
/// </para>
/// </summary>
public static class ZoneSessionStore
{
    private static string FileName => Path.Combine(GlobalData.AppDataFolder, "CryptoScanBot-chart.json");

    public static ZoneSession Load()
    {
        try
        {
            string fileName = FileName;
            if (File.Exists(fileName))
            {
                string text = File.ReadAllText(fileName);
                var session = JsonSerializer.Deserialize<ZoneSession>(text, JsonTools.DeSerializerOptions);
                if (session != null)
                    return session;
            }
        }
        catch (Exception error)
        {
            // ignore and fallback on new config (not that important)
            ScannerLog.Logger.Error(error);
        }
        return new();
    }

    public static void Save(ZoneSession session)
    {
        try
        {
            Directory.CreateDirectory(GlobalData.AppDataFolder);
            string text = JsonSerializer.Serialize(session, JsonTools.JsonSerializerIndented);
            File.WriteAllText(FileName, text);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error);
        }
    }
}
