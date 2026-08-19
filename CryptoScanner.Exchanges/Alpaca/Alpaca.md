# Alpaca

Alpaca is not a crypto exchange but a **US stock broker**. It is plugged into the scanner as
a regular exchange, so the existing analyzers, zones and emulator can run on US equities
(AAPL, MSFT, TSLA, ...) instead of coins.

Everything runs against the **paper trading** environment, so no real money is involved.
An API key and secret are still mandatory, even for reading market data only.

## Links

| What | URL |
| --- | --- |
| Main site | https://alpaca.markets |
| Dashboard (API key + secret) | https://app.alpaca.markets |
| Trade page per symbol | https://app.alpaca.markets/trade/{BASE} |
| Documentation | https://docs.alpaca.markets |
| REST API reference | https://docs.alpaca.markets/reference |
| .NET SDK (`Alpaca.Markets`) | https://github.com/alpacahq/alpaca-trade-api-csharp |

Endpoints, handled by the SDK and listed for reference only:

| Environment | URL |
| --- | --- |
| Paper trading (used here) | https://paper-api.alpaca.markets |
| Live trading | https://api.alpaca.markets |
| Market data | https://data.alpaca.markets |

## Getting started

1. Register a free account at https://alpaca.markets (paper trading is enough).
2. Open the dashboard at https://app.alpaca.markets and generate an API key and secret.
3. Enter both in the scanner under Settings -> API keys -> Alpaca.
4. Activate the Alpaca exchange; symbols are fetched on the next refresh.

Without a key nothing works: `Api.GetClient()` throws an `InvalidOperationException`,
because Alpaca authenticates every request, including market data.

## How it differs from the other exchanges

* **Not CryptoExchange.Net.** All other exchange implementations are built on the
  CryptoExchange.Net library. Alpaca uses the official `Alpaca.Markets` NuGet package
  (currently version 7.2.2, see `CryptoScanner.Core.csproj`). That is why
  `SubscriptionKLineTicker` overrides `StartAsync`/`StopAsync` instead of implementing
  the usual `Subscribe()` pattern, and why `Symbol.GetSymbolsAsync` passes `null` for
  the `api` parameter.
* **Spot only.** There is no futures variant; `ExchangeHelper` throws for
  `CryptoTradingType.Futures`.
* **One asset per symbol.** The ticker is used as both exchange name and base, with USD
  as the fixed quote. This mirrors how HyperLiquid handles single-asset instruments.
* **Market hours.** US equities do not trade 24/7. Gaps in the candle series over nights,
  weekends and holidays are normal, not missing data.

## Files

| File | Purpose |
| --- | --- |
| `Spot/Api.cs` | Exchange entry point, defaults, order placement, external links |
| `Spot/Symbol.cs` | Fetches tradable US equity assets and the volume snapshots |
| `Spot/Candle.cs` | Historical bars via `GetHistoricalBarsAsync` |
| `Spot/Interval.cs` | Maps `CryptoIntervalPeriod` to Alpaca `BarTimeFrame` |
| `Spot/SubscriptionKLineTicker.cs` | Real-time minute bars via the streaming client |
| `Spot/LimitRate.cs` | Rate limiter, see below |

## Exchange defaults

Set in `Api.ExchangeDefaults()`:

* quote currency `USD`
* 1000 bars per request
* maximum 100 symbols per WebSocket group

Symbol precision is set in `Symbol.GetSymbolsAsync()`: price tick size 0.01 (cent
precision) and quantity tick size 0.000001, because Alpaca supports fractional shares.

## Rate limits

The free tier allows 200 calls per minute. `LimitRate` uses a 20 second sliding window
with a conservative ceiling of 50 calls, and sleeps 2.5 seconds whenever that is reached.

## Intervals

Supported: 1m, 2m, 3m, 5m, 10m, 15m, 30m, 1h, 2h, 3h, 4h, 6h, 8h, 12h, 1d, 1w.
Anything else returns `null` from `Interval.GetExchangeInterval` and is rejected by
`ExchangeHelper.IsIntervalSupported`.

## Trading

Real trading is not implemented. `PlaceOrder` and `Cancel` build a complete `TradeParams`
so paper trading and the emulator can create position steps, but throw when
`Settings.Trading.TradeVia` is `RealTrading`.
