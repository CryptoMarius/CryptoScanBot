namespace CryptoScanBot.Core.Core;

/// Data of pause trading rulez or barometer if the price drops
public class PauseTradingRule
{
    public DateTime? Calculated { get; set; }
    public DateTime? Until { get; set; }
    public string? Text { get; set; }


    public void Clear()
    {
        Calculated = null;
        Until = null;
        Text = "";
    }
}
