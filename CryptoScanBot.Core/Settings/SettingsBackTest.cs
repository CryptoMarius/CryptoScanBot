using CryptoScanBot.Core.Enums;

namespace CryptoScanBot.Core.Settings;

[Serializable]
public class SettingsBackTest
{
    public string BackTestSymbol { get; set; } = "BTCUSDT";
    public string BackTestInterval { get; set; } = "1M";
    public DateTime BackTestTime { get; set; } = DateTime.Now;
    public CryptoOrderSide BackTestMode { get; set; } = CryptoOrderSide.Buy;
    public CryptoSignalStrategy BackTestAlgoritm { get; set; } = CryptoSignalStrategy.Sbm1;
}

