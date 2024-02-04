using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Settings;

namespace CryptoSbmScanner.SettingsDialog;

public partial class UserControlEverything : UserControl
{
    public UserControlEverything()
    {
        InitializeComponent();
    }

    public void InitControls(bool isForTrading, CryptoTradeSide side)
    {
        // Trading long/short (initialize list etc)
        UserControlStrategy.InitControls(isForTrading, side);
        UserControlInterval.InitControls();
        UserControlIntervalTrend.InitControls(side);
        if (isForTrading)
            UserControlBarometer.InitControls("Trade alleen indien de barometer"); 
        else
            UserControlBarometer.InitControls("Genereer signalen indien de barometer");
    }

    public void LoadConfig(SettingsTextual settings)
    {
        UserControlStrategy.LoadConfig(settings.Strategy);
        UserControlIntervalTrend.LoadConfig(settings.IntervalTrend);
        UserControlInterval.LoadConfig(settings.Interval);
        UserControlBarometer.LoadConfig(settings.Barometer);
        UserControlMarketTrend.LoadConfig(settings.MarketTrend);
    }

    public void SaveConfig(SettingsTextual settings)
    {
        UserControlStrategy.SaveConfig(settings.Strategy);
        UserControlIntervalTrend.SaveConfig(settings.IntervalTrend);
        UserControlInterval.SaveConfig(settings.Interval);
        UserControlBarometer.SaveConfig(settings.Barometer);
        UserControlMarketTrend.SaveConfig(settings.MarketTrend);
    }

}
