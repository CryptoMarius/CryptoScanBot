using CryptoScanBot.Core.Enums;
using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Model;

[Table("TradeAccount")]
public class CryptoTradeAccount
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string Short { get; set; }

    public int ExchangeId { get; set; }
    [Computed]
    public virtual CryptoExchange Exchange { get; set; }

    public CryptoAccountType AccountType { get; set; }
    public CryptoTradeAccountType TradeAccountType { get; set; }

    public bool CanTrade { get; set; }

    // Instellingen:
    // Eventueel aangepaste instellingen per accountType voor bijvoorbeeld backtest project?
    //[Computed]
    // public ConfigWhatever Config { get; set; } 

    // Assets + locking
    [Computed]
    public SemaphoreSlim AssetListSemaphore { get; set; } = new(1);
    [Computed]
    public DateTime? LastRefreshAssets { get; set; } = null;
    [Computed]
    public SortedList<string, CryptoAsset> AssetList { get; } = [];


    [Computed]
    // Key = symbolName
    public SortedList<string, CryptoPosition> PositionList { get; } = [];

    [Computed]
    // Key = (symbolName, TradeId), value = trade
    public CryptoTradeList TradeList { get; } = [];

    [Computed]
    // Key = (symbolName, OrderId), value = order
    public CryptoOrderList OrderList { get; } = [];

    // Posities + locking
    //[Computed]
    //public SemaphoreSlim PositionListSemaphore { get; set; } = new(1);


    /// <summary>
    /// Clear cached information (after change of exchange), assets, orders, trades and positions
    /// </summary>
    public void Clear()
    {
        AssetList.Clear();
        OrderList.Clear();
        TradeList.Clear();
        PositionList.Clear();
    }

}
