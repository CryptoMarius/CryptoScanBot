using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media;


namespace CryptoScanner.ViewModels;

public enum IndicatorType
{
    Exchange,
    TradingView,
    FearAndGreed
}
/// <summary>
/// Represents a single symbol (market indicator or crypto) with price, color, and optional volume tracking
/// </summary>
public partial class DashboardSymbolViewModel : ObservableObject
{
    //public string Key { get; set; } = ""; //?
    public IndicatorType Type { get; set; }

    [ObservableProperty]
    private string _symbol;

    [ObservableProperty]
    private string _Name;

    [ObservableProperty]
    private decimal? _price;
    private decimal? _previousPrice;

    [ObservableProperty]
    private IBrush _color = App.PriceNeutral;

    [ObservableProperty]
    private decimal? _volume;

    public DashboardSymbolViewModel(IndicatorType type, string symbol, string name)
    {
        Type = type;
        Symbol = symbol;
        Name = name;
    }

    // not sure if this needed...
    //public event EventHandler<decimal>? Changed;


    /// <summary>
    /// Updates the symbol with new price and optional volume data
    /// Automatically calculates color based on price change
    /// </summary>
    public void Update(decimal newPrice, decimal? volume = null)
    {
        if (newPrice >= 0)
        {
            Color = GetColorForChange(_previousPrice, newPrice);
            _previousPrice = Price;
            Price = newPrice;
        }
        if (volume != null) 
            Volume = volume;
    }

    private static IBrush GetColorForChange(decimal? previousValue, decimal newValue)
    {
        if (!previousValue.HasValue)
            return App.PriceNeutral;

        if (newValue > previousValue.Value)
            return App.PriceUp;

        if (newValue < previousValue.Value)
            return App.PriceDown;

        return App.PriceNeutral;
    }

    public string GetUrl()
    {
        return Type switch
        {
            IndicatorType.FearAndGreed => Symbol,
            IndicatorType.TradingView => $"https://www.tradingview.com/chart/?symbol={Symbol}&interval=60",
            _ => "",
        };
    }

}
