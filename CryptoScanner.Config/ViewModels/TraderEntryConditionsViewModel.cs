using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class TraderEntryConditionsViewModel : ObservableObject
{
    // Entry conditions (all bool - EXACT match)
    [ObservableProperty]
    private bool _checkIncreasingRsi = false;

    [ObservableProperty]
    private bool _checkIncreasingMacd = false;

    [ObservableProperty]
    private bool _checkIncreasingStoch = false;

    [ObservableProperty]
    private bool _checkFurtherPriceMove = false;

    [ObservableProperty]
    private bool _checkSma200Direction = false;
    

    // Slot limits (all int - EXACT match)
    [ObservableProperty]
    private int _slotsMaximalLong = 1;

    [ObservableProperty]
    private int _slotsMaximalShort = 1;

    public void LoadConfig(SettingsTrading settings)
    {
        CheckIncreasingRsi = settings.CheckIncreasingRsi;
        CheckIncreasingMacd = settings.CheckIncreasingMacd;
        CheckIncreasingStoch = settings.CheckIncreasingStoch;
        CheckFurtherPriceMove = settings.CheckFurtherPriceMove;
        //CheckSma200Direction = settings.CheckSma200Direction;

        SlotsMaximalLong = settings.SlotsMaximalLong;
        SlotsMaximalShort = settings.SlotsMaximalShort;
    }

    public void SaveConfig(SettingsTrading settings)
    {
        settings.CheckIncreasingRsi = CheckIncreasingRsi;
        settings.CheckIncreasingMacd = CheckIncreasingMacd;
        settings.CheckIncreasingStoch = CheckIncreasingStoch;
        settings.CheckFurtherPriceMove = CheckFurtherPriceMove;
        //settings.CheckSma200Direction = CheckSma200Direction;

        settings.SlotsMaximalLong = SlotsMaximalLong;
        settings.SlotsMaximalShort = SlotsMaximalShort;
    }
}
