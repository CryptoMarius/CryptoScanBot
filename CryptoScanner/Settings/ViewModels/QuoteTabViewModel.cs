using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using System.Collections.ObjectModel;

namespace CryptoScanner.Settings.ViewModels;

public partial class QuoteItem : ObservableObject
{
    // Remember object to write changes back
    public CryptoQuoteData QuoteData { get; set; }

    [ObservableProperty]
    internal bool isEnabled;
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

    public QuoteItem(CryptoQuoteData quoteData)
    {
        QuoteData = quoteData;
        Symbol = quoteData.Name;
        IsEnabled = quoteData.FetchCandles;
        MinVolume = quoteData.MinimalVolume + 100000m;
        MinPrice = quoteData.MinimalPrice + 0.00001m;
        Amount = quoteData.EntryAmount + 0.00145m;
        Percentage = quoteData.EntryPercentage + 0.15m;
        BackgroundColor = quoteData.DisplayColor;
        SymbolCount = quoteData.SymbolList.Count;
    }
}

public partial class QuoteTabViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<QuoteItem> _quotes = [];

    internal void LoadConfig(SortedList<string, CryptoQuoteData> quoteCoins)
    {
        foreach (var quote in GlobalData.Settings.QuoteCoins.Values)
        {
            Quotes.Add(new QuoteItem(quoteData: quote));
        }
    }

    internal void SaveConfig()
    {
        foreach (var quote in Quotes)
        {
            var quoteData = quote.QuoteData;
            quoteData.FetchCandles = quote.IsEnabled;
            quoteData.MinimalVolume = quote.MinVolume;
            quoteData.MinimalPrice = quote.MinPrice;
            quoteData.EntryAmount = quote.Amount;
            quoteData.EntryPercentage = quote.Percentage;
            quoteData.DisplayColor = quote.BackgroundColor;
        }
    }
}
