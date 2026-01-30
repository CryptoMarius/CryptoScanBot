using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;


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
    public IndicatorType Type { get; set; }

    [ObservableProperty]
    private string _symbol;

    [ObservableProperty]
    private string _Name;

    [ObservableProperty]
    private string _priceText = string.Empty;
    private readonly string PriceFormat = "N2";
    private decimal? _previousPrice;
    private decimal? _price;
    public decimal? Price
    {
        get
        {
            return _price;
        }
        set
        {
            if (value >= 0 && (_price == null || _price != value))
            {
                Color = GetColorForChange(_previousPrice, value.Value);
                _previousPrice = _price;
                _price = value;
                PriceText = value.ToString0(PriceFormat);
                OnPropertyChanged(nameof(Price));
            }
        }
    }

    private IBrush _color = App.PriceNeutral;
    public IBrush Color
    {
        get
        {
            return _color;
        }
        set
        {
            if (_color != value)
            {
                _color = value;
                OnPropertyChanged(nameof(Color));
            }
        }
    }

    [ObservableProperty]
    private string? _volumeText;
    private decimal? _volume;
    public decimal? Volume
    {
        get
        {
            return _volume;
        }
        set
        {
            if (value >= 0 && (_volume == null || _volume != value))
            {
                _volume = value;
                OnPropertyChanged(nameof(Volume));
                VolumeText = GetLargeVolumeText(value.Value);
                OnPropertyChanged(nameof(VolumeText));
            }
        }
    }


    public DashboardSymbolViewModel(IndicatorType type, string symbol, string name, string priceFormat = "")
    {
        Type = type;
        Symbol = symbol;
        Name = name;
        PriceFormat = priceFormat;
    }

    // not sure if this needed...
    //public event EventHandler<decimal>? Changed;


    /// <summary>
    /// Updates the symbol with new price and optional volume data
    /// Automatically calculates color based on price change
    /// </summary>
    //public void Update(decimal newPrice, decimal? volume = null)
    //{
    //    if (newPrice >= 0)
    //        Price = newPrice;
    //    if (volume != null)
    //        Volume = volume;
    //}

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


    internal static string GetLargeVolumeText(decimal number)
    {
        if (number >= 1_000_000_000) // Miljard
            return $"{number / 1_000_000_000:N2} B";

        if (number >= 1_000_000) // Miljoen
            return $"{number / 1_000_000:N2} M";

        if (number >= 1_000) // Duizend
            return $"{number / 1_000:N2} K";

        return $"{number:N2}";
    }

}
