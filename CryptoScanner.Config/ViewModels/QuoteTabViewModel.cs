using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Model;

using System.Collections.ObjectModel;

namespace CryptoScanner.Config.ViewModels;

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
    // Zero means "not filled in": the start capital from the trader settings is used instead
    [ObservableProperty]
    private decimal startCapital;
    [ObservableProperty]
    private Color backgroundColor;
    [ObservableProperty]
    private int symbolCount;

    public QuoteItem(CryptoQuoteData quoteData)
    {
        QuoteData = quoteData;
        Symbol = quoteData.Name;
        IsEnabled = quoteData.FetchCandles;
        MinVolume = (decimal)quoteData.MinimalVolume;
        MinPrice = quoteData.MinimalPrice;
        Amount = quoteData.EntryAmount;
        Percentage = (decimal)quoteData.EntryPercentage;
        StartCapital = quoteData.StartCapital;
        BackgroundColor = quoteData.DisplayColor.ToAvaloniaColor();
        SymbolCount = quoteData.SymbolList.Count;
    }
}

/// <summary>
/// One row of the products table under the quote coins: the code behind the dot in a symbol name
/// (PERP, XYZ) and whether its symbols are fetched at all. See CryptoProductData.
/// </summary>
public partial class ProductItem : ObservableObject
{
    // Remember object to write changes back
    public CryptoProductData ProductData { get; set; }

    [ObservableProperty]
    internal bool isEnabled;
    [ObservableProperty]
    private string product = string.Empty;
    [ObservableProperty]
    private int symbolCount;

    public ProductItem(CryptoProductData productData, int symbolCount)
    {
        ProductData = productData;
        Product = productData.Name;
        IsEnabled = productData.Active;
        SymbolCount = symbolCount;
    }
}

public partial class QuoteTabViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<QuoteItem> _quotes = [];

    [ObservableProperty]
    private ObservableCollection<ProductItem> _products = [];

    internal void LoadConfig(SortedList<string, CryptoQuoteData> quoteCoins)
    {
        foreach (var quote in quoteCoins.Values)
        {
            Quotes.Add(new QuoteItem(quoteData: quote));
        }
    }

    /// <summary>
    /// The products, with the number of active symbols each one has on the active exchange. A
    /// product that is switched off shows zero after the next refresh, which is the confirmation
    /// the user is looking for.
    /// </summary>
    internal void LoadConfig(SortedList<string, CryptoProductData> products, Core.Model.CryptoExchange? exchange)
    {
        foreach (var product in products.Values)
        {
            int count = 0;
            if (exchange != null)
                count = exchange.SymbolListName.Values.Count(s => s.Status == 1 && s.Product == product.Name);
            Products.Add(new ProductItem(product, count));
        }
    }

    internal void SaveConfig()
    {
        foreach (var quote in Quotes)
        {
            var quoteData = quote.QuoteData;
            quoteData.FetchCandles = quote.IsEnabled;
            quoteData.MinimalVolume = (double)quote.MinVolume;
            quoteData.MinimalPrice = quote.MinPrice;
            quoteData.EntryAmount = quote.Amount;
            quoteData.EntryPercentage = (float)quote.Percentage;
            quoteData.StartCapital = quote.StartCapital;
            quoteData.DisplayColor = quote.BackgroundColor.ToCoreColor();
        }

        foreach (var product in Products)
        {
            product.ProductData.Active = product.IsEnabled;
        }
    }
}
