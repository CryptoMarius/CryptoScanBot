using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Settings.ViewModels;

public partial class TraderTakeProfitViewModel : ObservableObject
{
    private readonly Dictionary<string, CryptoOrderType> _orderTypeList = new()
    {
        { "Market order", CryptoOrderType.Market },
        { "Limit order", CryptoOrderType.Limit }
    };

    private readonly Dictionary<string, CryptoTakeProfitStrategy> _strategyList = new()
    {
        //{ "Direct na het kopen", CryptoTakeProfitStrategy.Immediately },
        { "Op het opgegeven percentage", CryptoTakeProfitStrategy.FixedPercentage },
        { "Trace via de Keltner Channel en PSAR", CryptoTakeProfitStrategy.TrailViaKcPsar }
    };

    [ObservableProperty]
    private CryptoOrderType _takeProfitOrderType = CryptoOrderType.Limit; // enum (EXACT match)

    [ObservableProperty]
    private CryptoTakeProfitStrategy _takeProfitStrategy = CryptoTakeProfitStrategy.FixedPercentage; // enum (EXACT match)

    [ObservableProperty]
    private decimal _profitPercentage = 1.01m; // decimal (EXACT match)

    [ObservableProperty]
    private bool _addDustToTp = true; // bool (EXACT match)

    public Dictionary<string, CryptoOrderType> OrderTypeList => _orderTypeList;
    public Dictionary<string, CryptoTakeProfitStrategy> StrategyList => _strategyList;

    public void LoadConfig(SettingsTrading settings)
    {
        TakeProfitOrderType = settings.TakeProfitOrderType;
        TakeProfitStrategy = settings.TakeProfitStrategy;
        ProfitPercentage = settings.ProfitPercentage;
        AddDustToTp = settings.AddDustToTp;
    }

    public void SaveConfig(SettingsTrading settings)
    {
        settings.TakeProfitOrderType = TakeProfitOrderType;
        settings.TakeProfitStrategy = TakeProfitStrategy;
        settings.ProfitPercentage = ProfitPercentage;
        settings.AddDustToTp = AddDustToTp;
    }
}
