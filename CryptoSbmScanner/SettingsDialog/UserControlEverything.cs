using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Settings;

namespace CryptoSbmScanner.SettingsDialog;

public partial class UserControlEverything : UserControl
{
    public UserControlEverything()
    {
        InitializeComponent();
    }

    public void InitControls(bool signal, CryptoTradeSide side)
    {
        // Trading long/short (initialize list etc)
        UserControlStrategy.InitControls(side);
        UserControlInterval.InitControls();
        UserControlIntervalTrend.InitControls(side);
        if (signal)
            UserControlBarometer.InitControls("Genereer signalen indien barometer");
        else
            UserControlBarometer.InitControls("Trade alleen indien barometer");
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
