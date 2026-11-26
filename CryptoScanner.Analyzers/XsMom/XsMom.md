# XsMom - cross-sectional momentum

A portfolio rule rather than a signal per coin: once a week all coins of a quote are ranked on how
much they rose or fell, the strongest are bought and the weakest are sold short, and what drops out
is closed at the next rebalance.

## Rules

1. **Rebalance moment**: 00:00 UTC on the rebalance day (Monday), every `RebalanceEveryWeeks` weeks.
   Any interval whose candle closes at that moment gives the same answer; 1d is the natural one.
2. **Universe**: every symbol of the exchange with the same quote, barometer symbols excluded, TradFi
   perpetuals (shares, commodities, indices) excluded unless `SkipTradFi` is off.
3. **Candidates**: a coin needs a daily close on the day before the rebalance and on the day
   `LookbackDays` before that. Only bars that closed before the rebalance moment are read.
4. **Top by volume**: the `TopByVolume` candidates with the highest average quote volume (base volume
   x close) over the `VolumeDays` days before the rebalance; a missing day counts as zero.
5. **Ranking**: on the return close(day before) / close(LookbackDays earlier) - 1. Ties on the name.
6. **Selection**: long the strongest `CoinsPerSide`, short the weakest `CoinsPerSide`. With
   `KeepWithinFactor` above 1 a coin that was selected last time stays while it is within
   `KeepWithinFactor x CoinsPerSide` of its end of the ranking; new coins fill up to `CoinsPerSide`.
7. **Entry**: a signal on the rebalance moment for every selected coin on its side. A coin that stays
   selected already has its position and keeps it.
8. **Exit**: `IsExitSignal` at the rebalance moment when the coin is no longer on the side of its
   position. Between rebalance moments the strategy never exits.
9. **No stop and no DCA's** by design: set the stop-loss to 0, no DCA's, and the take profit so far
   away that it is never reached. Those are trading settings per side, so a run with this strategy
   should not share a side with other strategies.

## Where the ranking comes from

- **Emulator**: the 1d candles of every symbol of the exchange from the candle database, read once
  per run. The ranking therefore does not depend on which coins the run replays nor on the order of
  the symbol threads. A coin is only traded when the run replays it, so the queue file lists every
  coin that is ever selected (about 210 for 14 days / top 100 / 20 per side on Binance Perpetual).
- **Live**: the 1d candles in memory. A selection is cached once the rebalance moment is two minutes
  behind, because the last daily candles of the other coins may still be arriving.

## Funding

Funding is booked on every open position at every settlement (`FundingTools`, emulator:
`FundingReplay`): quantity x mark price x rate, paid by the long and received by the short when the
rate is positive. It is kept in `Position.Funding`, counts in the profit and moves the paper quote
balance; it does not move the break-even price. The settlements are fetched with the candles
(`fapi/v1/fundingRate`, Binance Perpetual only for now). Another exchange needs only an `IFunding`
implementation on its perpetual `Api`; the table, the booking and the columns are not tied to an
exchange. Without it a run has no funding and the log says so.

## Settings

| setting | default | meaning |
|---|---|---|
| `LookbackDays` | 14 | days the return is measured over |
| `TopByVolume` | 100 | coins with the highest average volume that take part |
| `VolumeDays` | 30 | days the average volume is taken over |
| `SkipTradFi` | true | leave the TradFi perpetuals out |
| `CoinsPerSide` | 20 | coins long and coins short |
| `KeepWithinFactor` | 1 | 1 = off; above 1 a held coin stays while within this many times `CoinsPerSide` of its end |
| `RebalanceDay` | Monday | day of the week of the rebalance, 00:00 UTC |
| `RebalanceEveryWeeks` | 1 | rebalance every this many weeks |

## Measured

Offline on Binance Perpetual daily closes, 2026-01-05 .. 2026-09-30, 100 USDT per position, fee
0.05% on the traded notional, real funding:

- 14 days / top 100 / 20 per side: +2,466 USDT (long +1,144, short +1,408, fees -87; funding +610 is
  inside both sides), largest drawdown 260, 8 of 9 months positive, 4,000 USDT capital in use.
- All 18 core variants (14/21/28 days x top 100/200 x 5/10/20 per side) positive with 0.2% slippage
  per side; weekly rebalancing beats every 3 days; the weekday matters a lot.
- Most of the profit came in weeks with a falling BTC (+2,269 against +199).

Not measured in the emulator yet.
