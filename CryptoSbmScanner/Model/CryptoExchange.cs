namespace CryptoSbmScanner.Model;

public class CryptoExchange
{
    public string Name { get; set; }

    public DateTime ExchangeInfoLastTime { get; set; } = DateTime.MinValue;

    //De basecoins geindexeerd op naam 
    public SortedList<string, CryptoSymbol> SymbolListName { get; } = new SortedList<string, CryptoSymbol>();
}