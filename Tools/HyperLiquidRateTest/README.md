# HyperLiquidRateTest

Measures what HyperLiquid actually charges an IP address, instead of taking the documentation's word
for it. Built on 30-08-2026 to answer one question — can the candle catch-up move to the websocket
and escape the REST budget — and answered a bigger one on the way.

Deliberately outside the solution and referencing only `HyperLiquid.Net`: a reference to
`CryptoScanner.Core` would drag the scanner's own 1150 weight ceiling in, and that ceiling is exactly
what is being questioned.

```
dotnet run -- --transport none  --warmup 60 --cooldown 20        # is the address quiet?
dotnet run -- --transport rest  --count 400 --concurrency 16     # positive control
dotnet run -- --transport socket --count 150 --concurrency 16    # the question
dotnet run -- --transport none  --sustain 180 --warmup 90        # requests per minute
dotnet run -- --transport none  --sustain 150 --warmup 90 --minutes 5000   # same, biggest window
```

## How it measures

A cheap REST request — `allMids`, 2 weight — is fired every second from beginning to end. That probe
is the instrument: while the address has budget it is answered, and the moment the address runs out
it comes back with `Server rate limit exceeded`. Around it:

| Phase | What happens | What it is for |
| --- | --- | --- |
| `warmup` | probes only | Refusals here mean something else is spending this address's budget — a scanner on the same machine — and the run has to be thrown away. |
| `burst` | `--count` candle requests over `--transport`, `--concurrency` at a time | The question. |
| `sustain` | candle requests without pause for `--sustain` seconds, counted per ten seconds | How much the exchange hands back per minute once the burst allowance is gone. |
| `cooldown` | probes only | Shows the address recovering. |

The client-side rate limiter is switched off (`RateLimiterEnabled = false`). Left on, the package
holds the requests back itself and the exchange never gets the chance to answer.

The first version of this tool had no warmup control, and its first run was worthless because of it:
the live scanner happened to be doing a zone catch-up at that moment, so a stall during the burst
could not be attributed to the burst. Any run whose warmup shows a refusal has to be discarded.

**This spends the budget of the whole IP address.** A scanner running on HyperLiquid at the same
moment shares it. Over the runs below the live HyperLiquid Perpetual scanner was refused 8 times,
retried all 8 successfully, and lost no candles — but that is the order of the disturbance.

## What it measured, 30-08-2026

### 1. A websocket post is not a free lane

`HyperLiquid.Net` books REST and socket requests against separate gates (`HyperLiquidRest` at 1200
per minute, `HyperLiquidSocket` at 2000), which suggested the socket path might escape the REST
budget. It does not.

| Run | Burst | Accepted | `allMids` probe |
| --- | --- | --- | --- |
| `--transport rest --count 400` | REST | 133 of 400, first failure at 116 | refused during the burst, never before |
| `--transport socket --count 150` | websocket post | 118 of 150, first failure at 59 | refused 7 times during the burst, never before |

The probe travels over REST. It being refused **while the socket burst runs and never during the
warmup** is the whole finding: the two share one budget.

Two more things the socket run showed, both against it:

- Over the socket, being over the limit arrives as **no answer at all** — sixteen requests sat there
  until the 20 second timeout — instead of an error that can be recognised and retried.
- It is far slower. 200 requests over REST took 4.1 seconds; 150 over the socket took 43.9.

### 2. The enforced budget is about three times the documented one

The positive control was supposed to fail at request 57 (`1200 / 21`). It did not fail until 116,
and after a fully quiet minute not until 167. So the `sustain` mode was added, which simply keeps
asking and counts what gets through:

| Window per request | Candles per answer | Weight per request | Accepted per minute | Implied budget |
| --- | --- | --- | --- | --- |
| 10 minutes | 11 | 20 + 1 = 21 | **177** | 3717 per minute |
| 5000 minutes | 5000 | 20 + 84 = 104 | **36** | 3744 per minute |

Two request sizes five times apart in weight, and both land on the same budget within 0.7%. That
confirms the documented weight model exactly — an info request weighs 20 and `candleSnapshot` adds
one per 60 candles, rounded up, which is what `HyperLiquidLimits.BookCandleWeightAsync` books — while
the ceiling itself is **about 3730 weight per minute and not the documented 1200**.

The first minute allows more still: 352 small requests in minute 0 against 177 in minutes 1 and 2, so
there is a burst allowance of roughly one extra minute's worth on top.

Measured on one address on one afternoon, with the live scanner spending from the same budget, so the
real figure is a little higher. It is reproducible: the pattern of "a batch of about 177, then
silence until the next minute" repeated over three minutes in both runs.

## Files

Each run writes a CSV of every single request — `kind,phase,ordinal,symbol,atMs,durationMs,success,candles,error`
— so the raw evidence survives the console. The runs of 30-08-2026 are next to this file.
