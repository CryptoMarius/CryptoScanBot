namespace CryptoSbmScanner.Model;

public class CryptoExchange
{
    public string Name { get; set; }

    public DateTime ExchangeInfoLastTime { get; set; } = DateTime.MinValue;

    public SemaphoreSlim AssetListSemaphore { get; set; } = new(1);
    public SortedList<string, CryptoAsset> AssetList { get; } = new();
    //De basecoins geindexeerd op naam 
    public SortedList<string, CryptoSymbol> SymbolListName { get; } = new();
}