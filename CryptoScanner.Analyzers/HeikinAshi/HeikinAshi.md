# Heikin Ashi trend (HeikinAshi)

## Overview

The **HeikinAshi** strategy trades the colour change of the Heikin Ashi candle in the direction of
the trend of a higher interval. A long opens on the first green Heikin Ashi candle after a red one
while the last closed Heikin Ashi candle of the higher interval (1w by default) is green; a short the
other way round. The position is closed on the first Heikin Ashi candle of the other colour, or with
a wick on the other side.

Like PsarCross it implements `SignalCreateBase.IsExitSignal`, which the position monitor asks on
every close of the position's own interval. Stop loss and take profit keep working next to that exit.

There is no chart overlay of its own.

## Background

A Heikin Ashi candle is an average of the real candles:

- close = (open + high + low + close) / 4 of the real candle
- open = (open + close) / 2 of the previous Heikin Ashi candle
- high = the highest of the real high, the Heikin Ashi open and close
- low = the lowest of the real low, the Heikin Ashi open and close

That smooths the price: in a trend the Heikin Ashi candles keep one colour, and a strong trend shows
candles without a wick on the side against the trend (no lower wick in a rising market). A wick on
that side is the first sign the trend weakens; a candle of the other colour is the turn.

Every candle depends on all candles before it. The strategy calculates the Heikin Ashi candles from
the last 60 real candles of the interval and needs at least 10; the seed of the first candle halves
its weight with every candle, so it has no influence on the last one.

## How it works

Per closed candle of the signal interval, for the side being evaluated, cheapest test first:

1. The last Heikin Ashi candle has the colour of the trade (green for a long, red for a short).
2. The Heikin Ashi candle before it had the other colour, and lies exactly one interval earlier.
3. When `HigherInterval` is set: the last Heikin Ashi candle of that interval that had closed when
   the signal candle closed has the colour of the trade.

The entry follows the trader's entry settings (a market order at the next open in the measurement).

The stop, when `StopBeyondSignalCandle` is on: the distance from the close of the signal candle to its
real low (to its real high for a short), as a percentage, handed to the trader in place of the stop
of the trading settings.

The exit, asked by the position monitor on every close of the position's interval:

1. `ExitOnSignal` has to be on.
2. The last closed Heikin Ashi candle has the other colour - or, with `ExitOnOppositeWick`, has a
   wick on the other side longer than `WickMinimumPercentage` of its range (under the body for a long,
   above it for a short).

## Settings

| Setting | Default | Meaning |
|---|---|---|
| Higher interval | 1w | The last closed Heikin Ashi candle of this interval has to have the colour of the trade (empty = off) |
| Exit on the Heikin Ashi candle | on | Close the position on the first Heikin Ashi candle of the other colour |
| Also on a wick on the other side | on | Also close on a candle with a wick on the other side |
| Minimum wick % | 5 | How long a wick has to be, as a percentage of the candle's range |
| Stop beyond the signal candle | on | Stop under the low (short: above the high) of the signal candle |

The intervals are chosen in the interval box of the strategy; empty follows the side. The rule was
measured on 1d.

## Results

Measured offline (09-11-2026, not in the emulator): daily candles, 1w as the higher interval, entry at
the next open, exit at the open after the exit candle, no take profit, no DCA's, no funding.

- The bare colour change on 1h, 2h, 4h and 1d loses money; the weekly candle on the same colour and
  the exit on the opposite wick are both needed. A random entry with the same weekly filter and exit
  loses money, so the colour change itself carries the timing.
- HyperLiquid Perpetual, 2,000 USDT, 25 USDT per position, 2024-12-01 to 2026-10-01: +713 USDT over
  11,143 trades (57% losers), largest dip 1,258 USDT. With the stop beyond the signal candle +681 USDT,
  dip 1,058 USDT.
- 2026-01-01 to 2026-10-01 on the same account: +389 USDT, with the stop +410 USDT.
- Lower intervals (4h, 2h, 1h) lose money, also with filters.

Emulator results: not measured yet.
