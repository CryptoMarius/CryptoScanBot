using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Settings;

[Serializable]
public class SettingsBackTest
{
    public string BackTestSymbol { get; set; } = "BTCUSDT";
    public string BackTestInterval { get; set; } = "1M";
    public DateTime BackTestTime { get; set; } = DateTime.Now;
    public CryptoTradeDirection BackTestMode{ get; set; } = CryptoTradeDirection.Long;
    public SignalStrategy BackTestAlgoritm { get; set; } = SignalStrategy.Sbm1;
}

