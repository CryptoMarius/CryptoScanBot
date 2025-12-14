using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media;
using CryptoScanner.Helpers;

namespace CryptoScanner.DashBoard.Model;


/// <summary>
/// Represents a single symbol (market indicator or crypto) with price, color, and optional volume tracking
/// </summary>
public partial class SymbolData : ObservableObject
{
    [ObservableProperty]
    private decimal? _price;

    [ObservableProperty]
    private IBrush _color = BrushHelper.PriceNeutral;

    [ObservableProperty]
    private decimal? _volume;

    private decimal? _previousPrice;

    /// <summary>
    /// Updates the symbol with new price and optional volume data
    /// Automatically calculates color based on price change
    /// </summary>
    public void Update(decimal newPrice, decimal? volume = null)
    {
        Color = GetColorForChange(_previousPrice, newPrice);
        _previousPrice = Price;
        Price = newPrice;
        Volume = volume;
    }

    private static IBrush GetColorForChange(decimal? previousValue, decimal newValue)
    {
        if (!previousValue.HasValue)
            return BrushHelper.PriceNeutral;

        if (newValue > previousValue.Value)
            return BrushHelper.PriceUp;

        if (newValue < previousValue.Value)
            return BrushHelper.PriceDown;

        return BrushHelper.PriceNeutral;
    }
}
