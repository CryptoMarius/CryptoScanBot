namespace CryptoScanBot.Core.Settings;

[Serializable]
public class BalanceSymbol
{
    public string Symbol { get; set; }
    public decimal Percentage { get; set; }
}

[Serializable]
public class SettingsBalanceBot
{
    public bool Active { get; set; }
    public bool UseMarketOrder { get; set; }
    public bool ShowAdviceOnly { get; set; }
    public decimal MinimalBuyBarometer { get; set; }
    public decimal MinimalSellBarometer { get; set; }

    public int IntervalPeriod { get; set; }

    // Het DCA percentage bijkopen
    public decimal BuyThresholdPercentage { get; set; }
    // Het DCA percentage voor verkopen
    public decimal SellThresholdPercentage { get; set; }

    // Het start bedrag (winst berekenen), discutabel want ik krijg dat niet voor elkaar..(?)
    public decimal StartAmount { get; set; }


    // Lijst van symbolen met de gewenste verdeling
    public List<BalanceSymbol> CoinList = new List<BalanceSymbol>();

    public SettingsBalanceBot()
    {
        // *****************************************
        // Balance bot settings
        // *****************************************
        Active = false;
        UseMarketOrder = true;
        ShowAdviceOnly = false;

        IntervalPeriod = 1;

        BuyThresholdPercentage = 2.5m;
        SellThresholdPercentage = 2.5m;

        MinimalBuyBarometer = -999;
        MinimalSellBarometer = -999;
    }
}