using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;

using Dapper.Contrib.Extensions;

using System.Drawing;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace CryptoSbmScanner.Settings;

[Serializable]
public class SettingsGeneral
{
    public string ExtraCaption { get; set; } = "";

    // Welke exchange
    public string ExchangeName { get; set; } = "Binance";
    [Computed]
    public int ExchangeId { get; set; } = 1;
    [Computed]
    [JsonIgnore]
    public virtual Model.CryptoExchange Exchange { get; set; }


    public bool BlackTheming { get; set; } = false;
    public CryptoTradingApp TradingApp { get; set; } = CryptoTradingApp.Altrady;
    public int ActivateExchange { get; set; } = 0;
    public bool SoundTradeNotification { get; set; }
    public string SelectedBarometerQuote { get; set; } = "USDT";
    public string SelectedBarometerInterval { get; set; } = "1H";

    public string FontNameNew { get; set; } = "Segoe UI";
    public float FontSizeNew { get; set; } = 9f;

    public int GetCandleInterval { get; set; } = 60;

    public bool ShowInvalidSignals { get; set; } = false;
    public bool HideSymbolsOnTheLeft { get; set; } = false;
    public int RemoveSignalAfterxCandles { get; set; } = 15;

    public int SoundHeartBeatMinutes { get; set; } = 0;
    public string SoundHeartBeat { get; set; } = "sound-heart-beat.wav";

    [JsonConverter(typeof(Intern.RectangleConverter))]
    public Rectangle WindowPosition { get; set; } = new Rectangle();
    public FormWindowState WindowState { get; set; } = FormWindowState.Normal;

    // RSI instelbare oversold /overbought (op verzoek)
    public double RsiValueOversold { get; set; } = 30;
    public double RsiValueOverbought { get; set; } = 70;

    // STOCH instelbare oversold /overbought (op verzoek)
    public double StochValueOversold { get; set; } = 20;
    public double StochValueOverbought { get; set; } = 80;

    // Op welk interval moet de totale markttrend berekend worden (standaard op alle intervallen)
    public List<string> IntervalForMarketTrend = [];

    public SettingsGeneral()
    {
        IntervalForMarketTrend.Add("1m");
        IntervalForMarketTrend.Add("2m");
        IntervalForMarketTrend.Add("3m");
        IntervalForMarketTrend.Add("5m");
        IntervalForMarketTrend.Add("10m");
        IntervalForMarketTrend.Add("15m");
        IntervalForMarketTrend.Add("30m");
        IntervalForMarketTrend.Add("1h");
        IntervalForMarketTrend.Add("2h");
        IntervalForMarketTrend.Add("3h");
        IntervalForMarketTrend.Add("4h");
        IntervalForMarketTrend.Add("6h");
        IntervalForMarketTrend.Add("8h");
        IntervalForMarketTrend.Add("12h");
        IntervalForMarketTrend.Add("1d");
    }
}

