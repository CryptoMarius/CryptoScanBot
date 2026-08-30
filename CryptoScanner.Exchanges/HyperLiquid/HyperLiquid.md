# HyperLiquid — rate limits and why a start takes three minutes

Written 30-08-2026. Every number below is either quoted from the exchange documentation or measured
on the live scanner; the source is named in each case. The constants that follow from it live in
[HyperLiquidLimits.cs](HyperLiquidLimits.cs).

## 1. What the exchange allows

Source: <https://hyperliquid.gitbook.io/hyperliquid-docs/for-developers/api/rate-limits-and-user-limits>
(checked 30-08-2026).

### REST, per IP address

| What | Limit |
| --- | --- |
| All REST requests together | **1200 weight per minute** |
| An ordinary info request, `candleSnapshot` included | **20 weight** |
| `l2Book`, `allMids`, `clearinghouseState`, `orderStatus`, `spotClearinghouseState`, `exchangeStatus` | 2 weight |
| `userRole` | 60 weight |
| `candleSnapshot` surcharge | **+1 weight per 60 candles in the answer** |
| An order request | 1 + floor(batch length / 40) |
| Explorer requests | 40 weight |

Two lines carry everything else in this document. Line 2: you pay 20 for **being allowed to ask**.
Line 5: the data itself is nearly free. A request that returns 9 candles costs 20, a request that
returns the maximum of 5000 costs 20 + 84 = 104. Asking is expensive, data is cheap.

`1150 / 20 = 57` requests per minute was therefore the whole ceiling, whatever those requests carry.
That is the number every measurement in section 2 was taken against. Since 30-08-2026 the ceiling is
a setting with a default of 3000 - see section 4c and the comment on `WeightPerMinute` in
[HyperLiquidLimits.cs](HyperLiquidLimits.cs).

The budget is **per IP address, not per market**. HyperLiquid Spot next to HyperLiquid Perpetual on
one machine spends it twice.

**The 1200 in that table is what the documentation says, and it is not what the exchange enforces.**
Measured on 30-08-2026 the address was allowed about 3730 weight per minute, three times as much,
with the weight model itself confirmed exactly. Section 4b has the measurement and 4c what was
changed because of it; everything between here and there is arithmetic on the 1150 the scanner ran
on until that day.

### WebSocket, per address

| What | Limit |
| --- | --- |
| Connections | 10 |
| New connections per minute | 30 |
| Subscriptions | 1000 |
| Messages sent to HyperLiquid per minute, over all connections | **2000** |
| Simultaneous inflight post messages, over all connections | 100 |

The documentation does **not** say whether websocket post messages also consume the 1200 REST
weight. Section 4 measures it: they do.

### Address-based limits (trading only)

Starting buffer of 10000 requests, then 1 request per 1 USDC traded cumulatively. Irrelevant to the
scanner, which only reads.

## 2. What a start actually costs

Measured on the live scanner, `HyperLiquid/Perpetual/Log/CryptoScanBot.log`, 30-08-2026. Four
consecutive starts of the same day:

| Start | Symbols | Requests | Candles received | Weight | Measured | weight / 1150 predicts |
| --- | --- | --- | --- | --- | --- | --- |
| 09:19:06 | 113 | 130 | 7534 | 2822 | 124 s | 147 s |
| 10:32:32 | 112 | 115 | 243 | 2404 | 123 s | 125 s |
| 11:22:30 | 115 | 177 | 34746 | 4234 | 187 s | 221 s |
| 12:23:51 | 117 | 157 | 20323 | 3586 | 186 s | 187 s |

The measured time follows the weight and nothing else. Compare rows 2 and 4: **243 candles took
123 seconds, 20323 candles took 186 seconds**. Eighty times the data, one and a half times the
time.

The start of 12:23, in full:

```
CryptoScanBot start 12:23:22 .. ready 12:26:56          total 3 min 34 s
├── 00:02  plugins and exchange defaults
├── 00:08  symbols and tickers from HyperLiquid
├── 00:05  read candles.db (119 symbols, 978,866 candles)
├── 00:12  open websocket subscriptions (117 symbols over 4 bundles)
└── 03:05  fetch candles over REST                      <-- 86% of the time
    ├── 00:03  43 requests, budget spent
    ├── 01:00  waiting for the next minute window
    ├── 01:00  waiting for the next minute window
    └── 01:00  waiting for the next minute window
```

The log shows the three waits directly, one per minute:
`RateLimitTriggered HyperLiquid Perpetual 1150 HyperLiquid REST Limit of 1150 per 00:01:00 ... Wait`
at 12:23:53, 12:24:54 and 12:25:56.

Arithmetic for that start: `157 x 20 = 3140` flat, plus 446 candle surcharge, is 3586 weight.
`3586 / 1150 = 3 min 7 s` predicted against 3 min 5 s measured.

Where the 3586 goes:

- **88% is the flat 20 per request**, 12% is the candles.
- 134 of the 157 requests were for 1m. The other eleven intervals together needed 23, because
  `CandleBase.GetCandlesForIntervalAsync` derives the higher intervals from 1m
  (`BulkCalculateCandles`) before their own turn comes up.
- 100 symbols needed one 1m request, 17 needed two, and 2 symbols — newly admitted on volume, with
  no history at all — did all twelve intervals.
- **20 of the 157 requests came back empty** (13%), costing 400 weight = 21 seconds.

## 3. Why it cannot be faster as it stands

1. HyperLiquid has no request that covers more than one coin. `candleSnapshot` takes one coin and
   one interval; `allMids`, `allDexsAssetCtxs` and `fastAssetCtxs` do cover every coin at once but
   carry no candles. 117 symbols is therefore 117 requests, minimum.
2. `117 x 20 = 2340` weight, at the 1150 per minute the scanner is set to, is **2 minutes
   2 seconds**. That is the floor as long as that setting stands.
3. Every restart has to catch up the minutes the websocket was down — the run of 12:23 had been off
   since 12:14:47 — so that floor is paid on every start.
4. Raising the ceiling from 1150 to the documented 1200 saves 8 seconds and removes the slack that
   keeps the exchange from refusing. Not worth it — but the ceiling itself turns out to be the wrong
   number entirely, see section 4b.

## 4. The same request over the websocket — measured, and it does not help

**Settled on 30-08-2026 by `Tools/HyperLiquidRateTest`: a websocket post is charged against the same
budget as a REST request.** The reasoning that led there is kept below because it is what the package
still suggests, but the answer is no.

How it was settled: a cheap `allMids` request (2 weight) was fired over REST once a second,
throughout. It was answered during the twenty seconds before the burst, refused with
`Server rate limit exceeded` seven times **while 150 candle requests went out over the websocket**,
and answered again afterwards. A REST burst as positive control refused the same probe the same way,
so the instrument is known to work.

Two more findings, both against the socket path:

- Over the socket, being over the limit arrives as **no answer at all**. Sixteen requests sat there
  until the 20 second timeout instead of returning an error `RetryAfterRateLimitAsync` could act on.
- It is far slower: 200 requests over REST took 4.1 seconds, 150 over the socket took 43.9.

### What the package suggests, and why it is not proof

`HyperLiquid.Net` 5.7.0 exposes `GetKlinesAsync` twice, with an identical signature:

- `IHyperLiquidRestClientExchangeData.GetKlinesAsync(string, KlineInterval, DateTime, DateTime, CancellationToken)`
- `IHyperLiquidSocketClientApiExchangeData.GetKlinesAsync(string, KlineInterval, DateTime, DateTime, CancellationToken)`

The second sends the same `candleSnapshot` as a websocket **post request** — the documented
mechanism at
<https://hyperliquid.gitbook.io/hyperliquid-docs/for-developers/api/websocket/post-requests> —
instead of over HTTP.

The package books the two against **separate gates**, which is why our ceiling does not touch the
second one:

| Gate | Guard |
| --- | --- |
| `HyperLiquidRest` | `RateLimitGuard(PerHost, [], 1200, 60s, Sliding)` |
| `HyperLiquidSocket` | `RateLimitGuard(PerHost, LimitItemTypeFilter(Request), 2000, 60s)` |

`LibraryRateLimit.Lower` only adds a guard to `HyperLiquidRest`, so socket requests run on the
package's 2000 per minute. In that accounting a candle request is **one message instead of 20
weight**: 117 symbols would fit inside a few seconds rather than two minutes.

The exchange documentation lists the websocket limits separately and never says post messages are
free of the 1200 REST weight. The separate "100 simultaneous inflight post messages" limit only makes
sense as a budget of its own, and the package author modelled it that way - but the measurement above
shows the exchange does not agree. The two gates are the package's opinion, not the exchange's.

## 4b. What the same measurement found instead: the ceiling is three times too low

The positive control was supposed to start refusing at request 57, because `1200 / 21` is 57. It did
not refuse until 116, and after a fully quiet minute not until 167. So the measurement was turned
around: keep asking without pause, and count what gets through per minute.

| Window per request | Candles per answer | Weight per request | Accepted per minute | Implied budget |
| --- | --- | --- | --- | --- |
| 10 minutes | 11 | 20 + 1 = 21 | **177** | 3717 per minute |
| 5000 minutes | 5000 | 20 + 84 = 104 | **36** | 3744 per minute |

Two request sizes five times apart in weight, both landing on the same budget within 0.7%. That
confirms the documented weight model exactly - an ordinary info request weighs 20, `candleSnapshot`
adds one per 60 candles rounded up, which is what `HyperLiquidLimits.BookCandleWeightAsync` books -
while the ceiling itself is **about 3730 weight per minute and not the documented 1200**. On top of
that there is a burst allowance of roughly one extra minute's worth: 352 small requests went through
in the first minute against 177 in each of the two minutes after it.

What that means for section 2: the start of 12:23 needed 3586 weight. That is less than one minute of
the measured budget and inside the burst allowance, so **the whole candle catch-up could finish in
seconds instead of three minutes.**

Three things to hold on to before acting on it:

1. It is one address on one afternoon, and the documentation says 1200. Running at three times the
   documented budget is a deliberate choice against the documentation, not a bug fix.
2. `WeightPerMinute` is the budget of the whole IP address. Two HyperLiquid markets on one machine
   share whatever it is set to.
3. A refusal is not free: `CandleBase.RetryAfterRateLimitAsync` waits five seconds on the first
   attempt, so a ceiling set right at the edge costs more than it gains.

**What does not change.** The websocket connection count stays at 10 per address, and the
subscriptions we already hold (117 symbols over 4 bundles) share those connections.

## 4c. What was changed on 30-08-2026

`HyperLiquidLimits.WeightPerMinute` was a constant of 1150 and is now the setting
`SettingsGeneral.HyperLiquidWeightPerMinute`, default **3000**, clamped to 200..3600. It is on the
Exchange tab of both configuration screens as "HyperLiquid weight per minute", and it takes effect
when the exchange is activated - so on a restart or an exchange switch.

It has to be a setting rather than a constant because the right value depends on something the code
cannot see: **how many scanners on this machine are drawing from the same address**. One scanner may
have 3000; two HyperLiquid markets side by side have to be set to 1500 each. Nothing enforces that
across processes, so the setting is the only place where the division is stated.

### Raising it needed a second change

Guards on a `CryptoExchange.Net` rate limit gate are conditions that all have to pass, so the guard
`LibraryRateLimit` adds could only ever make the ceiling **stricter**. HyperLiquid's own guard of
1200 was still in place next to it, which means a setting of 3000 would have run at 1200 without
saying so. That was harmless as long as every value we ever passed sat below the documented budget,
and stopped being harmless the moment the budget itself turned out to be measured too low.

`LibraryRateLimit.Lower` therefore no longer adds a guard but **replaces** them: the gate's private
`_guards` bag is cleared by reflection and one guard of ours is put back. Verified against a real
`RateLimitGate` before the scanner was made to lean on it - two guards in, cleared to zero, one guard
of 3000 back. Two consequences worth knowing:

- Anything else the package had on that gate is gone as well, which is why this is only called from
  `ExchangeDefaults`, when no request is in flight and no retry-after guard can be pending.
- When the bag cannot be reached the call falls back to adding a guard, which still holds for any
  value below what the package allows, and says so in the log rather than pretending.

### What to watch after changing it

The line `delay needed because of rate limits` in the scanner log, and only that line: it appears
when the **exchange** refused, never for a client-side wait. `RateLimitTriggered` is the opposite -
that is our own ceiling holding a request back, and during a start it is expected.

## 5. Cheaper without changing the transport

Ordered by what they cost on the start of 12:23:

1. **The 20 empty requests, 21 seconds.** Illiquid symbols where the first request reaches only as
   far as the last trade — 12:19 while the clock says 12:23 — after which the loop in
   `CandleBase.GetCandlesForIntervalAsync` asks once more and gets nothing. A request that came back
   short of what was asked for has already told us there is nothing beyond it.
2. **The 1h interval dragged to 3000 candles, part of 39 seconds.** Two symbols with no history did
   all twelve intervals; 1h alone was 6002 candles. That depth exists only because 6h is built from
   3h which is built from 1h, and HyperLiquid supports neither 3h nor 6h.
3. **Deriving instead of fetching.** 3m, 2h and 4h can be built from intervals already being
   fetched, which turns 12 requests per fresh symbol into 9.

Together roughly one minute of the three. The other two are the floor from section 3.

## 6. What does not help

- **The S3 archive.** `s3://hyperliquid-archive/market_data/...` holds L2 book snapshots and asset
  contexts, uploaded about once a month, requester-pays. The documentation states plainly that
  candles are not among the data sets it carries, and a monthly upload could not close a nine-minute
  gap anyway. <https://hyperliquid.gitbook.io/hyperliquid-docs/historical-data>
- **Asking for more candles per request.** The window is capped at 5000 and the surcharge is small,
  so a bigger window is nearly free — but on a start we need about ten candles per coin, so there is
  nothing there to win. It matters only for a cold start with no database.
- **A multi-coin candle request.** There is none.
