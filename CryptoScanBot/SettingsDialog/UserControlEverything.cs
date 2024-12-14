using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Settings;

namespace CryptoScanBot.SettingsDialog;

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
