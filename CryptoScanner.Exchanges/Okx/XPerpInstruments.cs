using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

using Microsoft.Data.Sqlite;

using OKX.Net.Enums;
using OKX.Net.Interfaces.Clients.UnifiedApi;
using OKX.Net.Objects.Public;

namespace CryptoScanner.Core.Exchange.Okx;

/// <summary>
/// The X-Perp instrument mapping. Kept apart from the rest of the Perpetual market because it
/// answers a different question: InstrumentType.Futures holds three products at once and only the
/// xperp ruleType belongs here, next to the swaps that the same market fetches separately.
/// <para>
/// It used to serve a standalone XPerp market as well. That market is gone since 28-08-2026 - these
/// contracts sit in the Perpetual market now, told apart by their product - so this is one caller.
/// </para>
/// </summary>
static internal class XPerpInstruments
{
    /// <summary>
    /// The quote of an X-Perp under the name the coin has that actually moves. Okx states USD as the
    /// settlement asset of every one of them, because these contracts settle in USD VALUE rather than
    /// in one fixed coin. Under Okx Europe that value is paid in USDC or USDG, with USDC as the
    /// default when the account makes no choice of its own, so fees, funding, margin and pnl all run
    /// in USDC. The exchange interface and Altrady both name these markets after that coin
    /// (AAVE/USDC), and the scanner follows them: USD becomes USDC, every other settlement asset
    /// (the inverse contracts, which settle in the base coin) is left alone.
    /// </summary>
    static internal string ScannerQuote(string? settlementAsset)
    {
        string asset = (settlementAsset ?? "").ToUpper();
        if (asset == "USD")
            return "USDC";
        return asset;
    }


    /// <summary>
    /// Maps the X-Perps out of one InstrumentType.Futures answer into the given market. Answers
    /// with the number of instruments the tickers had no volume for.
    /// <para>
    /// InstrumentType.Futures holds three products at once and only one of them belongs here
    /// (counted on 27-08-2026): 155 X-Perps with ruleType xperp, 16 dated USD_UM contracts that
    /// expire and roll to a new name, and 16 dated inverse contracts that settle in the base coin.
    /// The filter runs BEFORE IsSymbolAccepted, the way the Binance and Bybit markets do it: a
    /// dated USD_UM contract carries the same contract value and settlement asset as its X-Perp
    /// (BTC-USD_UM-260828 next to BTC-USD_UM_XPERP-310404), so both parse to the same pair. Four
    /// pairs are shared that way today (BTCUSDC, ETHUSDC, SOLUSDC, XAUUSDC); the rejected names go
    /// into <paramref name="rejectedSymbols"/> so the caller can hand them to
    /// <see cref="SymbolBase.RegisterAmbiguousSymbolNames"/> after its loop. The rejected name goes
    /// through <see cref="ScannerQuote"/> as well, otherwise the two sides no longer match and the
    /// check silently records nothing.
    /// </para>
    /// </summary>
    static internal int AddInstruments(
        Model.CryptoExchange exchange,
        IOKXRestClientUnifiedApi api,
        IEnumerable<OKXInstrument> instruments,
        SortedList<string, decimal> volumeTicker,
        string product,
        CryptoDatabase database,
        SqliteTransaction transaction,
        List<CryptoSymbol> cache,
        SortedList<string, CryptoSymbol> activeSymbols,
        List<string> rejectedSymbols)
    {
        int withoutVolume = 0;

        foreach (OKXInstrument symbolData in instruments)
        {
            if (symbolData.RuleType != SymbolRuleType.Perp)
            {
                rejectedSymbols.Add((symbolData.ContractValueAsset ?? "").ToUpper() + ScannerQuote(symbolData.SettlementAsset));
                continue;
            }

            // Only take instruments that are actually tradable (skip PreTrading, PostTrading or
            // Halt). One that was live before is deactivated by the caller, because it is missing
            // from activeSymbols.
            if (symbolData.State != InstrumentState.Live)
                continue;

            // These contracts report no base and no quote asset, and their instrument family
            // ("BTC-USD_UM_XPERP") carries the product name rather than the quote. The pair is in
            // the two asset fields instead: the contract value asset is what the contract is on,
            // the settlement asset is what it pays out in (see ScannerQuote for the USD -> USDC
            // step that makes the scanner name BTCUSDC).
            string baseAsset = (symbolData.ContractValueAsset ?? "").ToUpper();
            string quoteAsset = ScannerQuote(symbolData.SettlementAsset);
            if (baseAsset.Length == 0 || quoteAsset.Length == 0)
                continue;

            SymbolBase.SymbolInfo info = SymbolBase.ParseSymbol(symbolData.Symbol, baseAsset, quoteAsset, product);
            if (!SymbolBase.IsSymbolAccepted(exchange, info, api, TradingMode.PerpetualLinear, out CryptoSymbol? symbol))
                continue;

            // An order quantity is expressed in contracts, ctVal tells how much of the base asset
            // one contract represents. Multiply them to get the step in base amount.
            if (symbolData.LotSize.HasValue && symbolData.ContractValue.HasValue)
                symbol.QuantityTickSize = symbolData.LotSize.Value * symbolData.ContractValue.Value;
            else if (symbolData.LotSize.HasValue)
                symbol.QuantityTickSize = symbolData.LotSize.Value;

            if (symbolData.TickSize.HasValue)
                symbol.PriceTickSize = symbolData.TickSize.Value;

            // volume from the tickers (indexed on the instrument id, not the scanner name)
            if (volumeTicker.TryGetValue(symbol.ExchangeName, out decimal volume))
                symbol.Volume = (double)volume;
            else
            {
                symbol.Volume = 0;
                withoutVolume++;
            }

            // Only live instruments reach this point
            symbol.Status = 1;

            if (symbol.Id == 0)
            {
                database.Connection.Insert(symbol, transaction);
                cache.Add(symbol);
            }
            else
                database.Connection.Update(symbol, transaction);
            activeSymbols[symbol.Name] = symbol;
        }

        return withoutVolume;
    }
}
