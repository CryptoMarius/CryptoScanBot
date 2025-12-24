using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings;

using System.Collections.ObjectModel;

namespace CryptoScanner.Settings.ViewModels;

public partial class QuoteItem : ObservableObject
{
    [ObservableProperty] 
    private bool isEnabled;
    [ObservableProperty] 
    private string symbol = string.Empty;
    [ObservableProperty] 
    private decimal minVolume;
    [ObservableProperty] 
    private decimal minPrice;
    [ObservableProperty] 
    private decimal amount;
    [ObservableProperty] 
    private decimal percentage;
    [ObservableProperty] 
    private Color backgroundColor;
    [ObservableProperty] 
    private int symbolCount;
}

public partial class QuotesViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<QuoteItem> _quotes = [];

    public QuotesViewModel()
    {
        foreach (var quote in GlobalData.Settings.QuoteCoins.Values)
        {
            Quotes.Add(new QuoteItem { 
                Symbol = quote.Name,
                IsEnabled = quote.FetchCandles,
                MinVolume = quote.MinimalVolume + 100000,
                MinPrice = quote.MinimalPrice + 0.00001m,
                Amount = quote.EntryAmount + 0.00145m,
                Percentage = quote.EntryPercentage + 0.15m,
                BackgroundColor = quote.DisplayColor,
                SymbolCount = quote.SymbolList.Count,
            });
        }
    }

}
