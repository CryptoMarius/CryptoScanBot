using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Exchange;

public class CryptoExternalUrl
{
    // Only HyperTrader uses an execute link
    public CryptoExternalUrlType Execute { get; set; } = CryptoExternalUrlType.External;
    public string Url { get; set; } = "";
    public string? Telegram { get; set; }
}

public class CryptoExternalUrlAltrady : CryptoExternalUrl
{
    public string? Code { get; set; }
}

public class CryptoExternalUrls
{
    public CryptoExternalUrlAltrady? Altrady { get; set; }
    public CryptoExternalUrl? HyperTrader { get; set; }
    public CryptoExternalUrl? TradingView { get; set; }
    public CryptoExternalUrl? ExchangeUrl { get; set; }

    /// <summary>
    /// Addresses that differ per product, for a market that carries more than one. Keyed on
    /// <see cref="Model.CryptoSymbol.Product"/>.
    /// <para>
    /// Okx Perpetual is the reason: it holds the swaps and the X-Perps, and the outside world does
    /// not name those the same way. TradingView calls the swap OKX:BTCUSDT.P and the X-Perp
    /// OKX:BTCUSD.UM, and the exchange itself serves them from trade-swap and trade-futures.
    /// </para>
    /// <para>
    /// Only what actually differs belongs here. An app the override does not mention falls back to
    /// the address above it, so a market states one exception rather than a whole second set.
    /// </para>
    /// </summary>
    public Dictionary<string, CryptoExternalUrls> PerProduct { get; set; } = [];

    /// <summary>
    /// The addresses of every market an outside party deployed on this exchange - a product that is
    /// not one of ours (<see cref="Model.CryptoProduct.IsReserved"/>) and has no entry of its own in
    /// <see cref="PerProduct"/>. HyperLiquid is the reason: it carries ten such markets and the
    /// outside world names all of them the same way, with the deployer as a suffix or a prefix.
    /// Altrady writes HYPERLIQUIDF_USDC_TSLA_XYZ, TradingView HIP3XYZ:TSLAUSDC.P - so one template
    /// with {PRODUCT} covers them all, where a PerProduct entry per deployer would have to be kept
    /// up with every market that appears. Falls back to the addresses above it like PerProduct does.
    /// </summary>
    public CryptoExternalUrls? Deployed { get; set; }
}
