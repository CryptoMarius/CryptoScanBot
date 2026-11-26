# Impulse box

A strong move (the impulse), then a sideways box of bodies right after it. The trade goes
**against the impulse**: either the box breaks the other way (counter), or a break with the impulse
fails and closes back inside the box (fake break). The exit is the one of the trading settings
(take profit, DCA's, stop); the strategy has no exit of its own.

## Rules

Everything is measured on closes and bodies; wicks do not count.

1. **Impulse up** - inside the last `Impulse window` candles, the highest close is the end of the
   impulse and the lowest body (min of open and close) before it is the start. The height (end -
   start) must be at least `Impulse size` x ATR(14), and no close inside it may fall back more than
   `Max pullback` % of the height from the best close so far. An impulse down is the mirror image.
2. **Box** - the candles after the impulse. While the box forms, a close beyond the impulse end
   measures the impulse again (it ends at that new close, the box starts over); when no impulse
   qualifies anymore, the search starts over. The box high and low are the highest and lowest body
   of its candles. A box taller than `Max box height` % of the impulse is dropped.
3. The box is **valid** after `Box length` candles. A valid box that is not broken by a close within
   `Box expiry` candles is dropped.
4. **Break** - the first close outside a valid box:
   - against the impulse (below the box after an impulse up): **counter** - short after an impulse
     up, long after an impulse down;
   - with the impulse: **continuation**. When a candle closes back inside the box within
     `Fake break window` candles, that close is a **fake break**, traded against the impulse.
5. After a break the search for the next impulse starts on the next candle. Up and down impulses are
   tracked separately.

Only the kinds that are switched on open a position; when several fire on one candle the order is
counter, fake break, continuation.

## Settings

| setting | default | meaning |
|---|---|---|
| Impulse size (x ATR) | 3.0 | minimum impulse height in ATR(14) |
| Impulse window (candles) | 16 | candles in which the impulse is looked for |
| Max pullback in the impulse % | 38.2 | largest fall back of a close inside the impulse |
| Box length (candles) | 24 | candles after the impulse before the box is valid |
| Max box height % | 50 | box height as a percentage of the impulse |
| Box expiry (candles) | 192 | a valid box not broken within this many candles is dropped |
| Counter trade | on | trade the break against the impulse |
| Fake break | on | trade a failed break with the impulse |
| Fake break window (candles) | 8 | candles after the break in which a close back inside counts |
| Continuation | off | follow the break with the impulse |

Interval: 15m.

## Chart

The box is drawn from its first candle to the break or expiry: green after an impulse up, red after
an impulse down, with a grey dashed line at the impulse start. Breaks of the enabled kinds get a
label.

## Dry run before the build (21-11-2026)

Binance perpetuals, 121 symbols, 15m, 2026-01-01 to 2026-10-01, entry 100 USDT, DCA -2% x200 and
-4% x400, take profit 7.5% from the average price, stop 8% from the entry, fee 0.05% per side. Every
row is compared with a blind entry on the same candles and side. Profit in USDT.

Reference setting (3.0 / 16 / 24 / 50):

| setup | side | trades | profit | without best 10 | blind |
|---|---|---|---|---|---|
| counter after impulse down | long | 2075 | +3121.84 | | +1976.60 |
| fake break after impulse down | long | 1163 | +3245.57 | +2727.83 | +653.82 |
| counter after impulse up | short | 1793 | -59.04 | | -2977.83 |
| fake break after impulse up | short | 746 | -3301.57 | | -1284.64 |
| continuation (both) | | | worse than blind | | |

Neighbour settings, the two long setups:

| setting | counter long | blind | fake break long | blind |
|---|---|---|---|---|
| size 2.0 | +2993.12 | +1620.38 | +3316.47 | +657.72 |
| size 4.0 | +2640.33 | +1192.25 | +2985.59 | +720.35 |
| box 12 | +5473.89 | +6227.70 | -930.05 | +3076.57 |
| box 48 | +769.21 | +163.56 | +993.91 | +449.57 |
| box height 35 | +2703.27 | +676.19 | +645.71 | -300.53 |
| box height 70 | +1520.32 | +5159.61 | +4545.94 | +378.76 |

The long side beats the blind entry in most neighbours, not in all (box 12, box height 70 for the
counter). The short side loses, but loses less than a blind short in the same period. Not measured
in the emulator yet.

## Emulator (22-11-2026)

Session2, Binance perpetuals, 121 symbols, 15m, 2026-01-01 to 2026-10-01, start capital 100000 USDT,
stop 4, take profit 7.5, DCA 200 at 2% and 400 at 4%, no MA200. Profit in USDT, closed positions.

| run | setup | start | end | profit | trades | without best 10 | long | short |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| 2107 | reference: failedbreakout | 100000.00 | 92559.10 | -7440.90 | 9351 | -8007.80 | -3268.71 | -4172.19 |
| 2108 | defaults (counter and fake break) | 100000.00 | 101488.48 | +1488.48 | 2468 | +937.86 | +1828.63 | -340.15 |
| 2109 | counter only | 100000.00 | 101555.39 | +1555.39 | 1830 | +1008.27 | +845.74 | +709.65 |
| 2110 | fake break only | 100000.00 | 99928.56 | -71.44 | 996 | -614.21 | +1591.70 | -1663.14 |
| 2111 | defaults, long only | 100000.00 | 102845.61 | +2845.61 | 1584 | +2288.20 | +2845.61 | |

The long side earns; the fake break short loses. The neighbours of the box and the impulse size
were not measured in the emulator. Unregistered on 24-11-2026; the code stays.
