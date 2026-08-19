"""
Account simulation without a measuring window.

The earlier scripts looked a fixed number of candles ahead, which forced a choice between closing
a position that was still running (booking profit that was never taken) or letting it block a slot
forever. Neither is what happens. This one walks the candles themselves and follows every position
until its stop-loss or take-profit is actually hit, however long that takes.

The trader's rules, as they are in the code:
  - entry: limit order on the VBS band (EntryOrderType = Limit), cancelled after EntryRemoveTime
    candles if it never fills
  - DCA: every level placed at once, at a fixed percentage from the entry anchor, sized by Factor
  - stop-loss: beyond the furthest DCA (Altrady: entry x (1 - (deepest DCA% + stop%)/100))
  - take-profit: target% from TpGridAnchorPrice - the average fill price moved against the position
    by the fee paid and the fee still to pay, so the fee sits IN the target, and the anchor moves
    every time a DCA fills
  - fee: Exchange.FeeRate per transaction (0.1 in the database), charged on the way in and out

The account then applies its own limits: one position per symbol, a maximum number of long and
short slots, a fixed amount of money divided over those slots, and the exchange minimum per order.
Profit is only added to the capital when a position actually closes. Whatever is still running at
the end is reported separately, as an open result that has not been banked.

Usage:
    python account.py --candles "<db>" --capital 500 --slots-long 15 --slots-short 15
"""

import argparse
import sys
import time

import numpy as np
import pandas as pd

import band_index
import candledb
import indicators
import measure_entry_timing as met

MAXIMUM_HOLD = 2880          # 30 days on 15m: a backstop, not a rule


ENTRY_RULES = [
    "now",              # enter on the signal candle itself (current behaviour)
    "stoch %k under %d",   # %K SITS on the favourable side of %D (a state, true on many candles)
    "stoch %k crosses %d", # %K CROSSES %D on this candle (a moment, rare)
    "lower high",       # no new extreme against the position
    "wick rejection",   # candle closed in the third of its range away from the extreme
    "psar",             # parabolic SAR flipped in favour of the position
    "inside vbs band",  # close came back inside the VBS band
    "inside bollinger", # close came back inside the bollinger band
    "inside keltner",   # close came back inside the keltner channel
    "macd histo recovering",  # macd histogram moved in favour of the position
    "macd histo flip",  # macd histogram changed sign
    "rsi leaves os/ob", # rsi came back out of the oversold / overbought zone
    "stoch %k leaves os/ob",  # stochastic %K came back out of the zone
    "stoch %d leaves os/ob",  # stochastic %D came back out of the zone
    "rsi leaves os/ob + lower high",
    "stoch %k leaves os/ob + lower high",
    "lower high + stoch %k under %d",
    "lower high + psar",
    "lower high + inside vbs band",
    "wick rejection + inside vbs band",
    "two inside vbs band",  # two closes in a row back inside the VBS band
]


def arm_offset(data, index, side, rule, window):
    """First candle within the window on which the rule says 'place the order'. -1 = never.

    Every test is written in the direction of the POSITION: something is 'against us' when it
    continues the move that produced the signal, so one expression covers long and short.
    """
    if rule == "now":
        return index

    high = data["high"].to_numpy()
    low = data["low"].to_numpy()
    close = data["close"].to_numpy()
    stoch_k = data["stoch_k"].to_numpy()
    stoch_d = data["stoch_d"].to_numpy()
    rsi = data["rsi"].to_numpy()
    psar = data["psar"].to_numpy()
    bb_upper = data["bb_upper"].to_numpy()
    bb_lower = data["bb_lower"].to_numpy()
    kc_upper = data["kc_upper"].to_numpy()
    kc_lower = data["kc_lower"].to_numpy()
    macd_hist = data["macd_hist"].to_numpy()
    vbs_upper = data["vbs_upper"].to_numpy()
    vbs_lower = data["vbs_lower"].to_numpy()
    is_short = side == "short"
    sign = 1.0 if is_short else -1.0
    extreme = high if is_short else low
    band = vbs_upper if is_short else vbs_lower

    inside_previous = False
    for j in range(index + 1, min(index + window, len(data) - 1) + 1):
        span = high[j] - low[j]
        if span <= 0:
            continue
        # where the close sits inside its own candle: 100 = closed at the extreme of the move
        place = (close[j] - low[j]) / span * 100.0
        if not is_short:
            place = 100.0 - place

        no_new_extreme = (extreme[j] <= extreme[j - 1]) if is_short else (extreme[j] >= extreme[j - 1])
        # state: %K is on the side of %D that the position wants
        turned = (stoch_k[j] - stoch_d[j]) * sign < 0
        # moment: it was on the other side one candle ago, so it crosses here
        crossed = turned and ((stoch_k[j - 1] - stoch_d[j - 1]) * sign >= 0)
        sar_against = (psar[j] > close[j]) if is_short else (psar[j] < close[j])
        inside_band = (close[j] < band[j]) if is_short else (close[j] > band[j])
        inside_bb = (close[j] < bb_upper[j]) if is_short else (close[j] > bb_lower[j])
        inside_kc = (close[j] < kc_upper[j]) if is_short else (close[j] > kc_lower[j])
        # macd histogram moving in the direction the position wants, and changing sign outright
        macd_turn = (macd_hist[j] - macd_hist[j - 1]) * sign < 0
        macd_flip = (macd_hist[j] * sign < 0) and (macd_hist[j - 1] * sign >= 0)

        # Leaving the extreme zone: overbought for a short, oversold for a long. The candle before
        # was still inside it, this one is not - that is the moment the extreme lets go.
        if is_short:
            rsi_leaves = rsi[j] < met.RSI_OVERBOUGHT <= rsi[j - 1]
            k_leaves = stoch_k[j] < met.STOCH_OVERBOUGHT <= stoch_k[j - 1]
            d_leaves = stoch_d[j] < met.STOCH_OVERBOUGHT <= stoch_d[j - 1]
        else:
            rsi_leaves = rsi[j] > met.RSI_OVERSOLD >= rsi[j - 1]
            k_leaves = stoch_k[j] > met.STOCH_OVERSOLD >= stoch_k[j - 1]
            d_leaves = stoch_d[j] > met.STOCH_OVERSOLD >= stoch_d[j - 1]
        rejection = place < 33.0

        hit = {
            "stoch %k under %d": turned,
            "stoch %k crosses %d": crossed,
            "lower high": no_new_extreme,
            "wick rejection": rejection,
            "psar": sar_against,
            "inside vbs band": inside_band,
            "inside bollinger": inside_bb,
            "inside keltner": inside_kc,
            "macd histo recovering": macd_turn,
            "macd histo flip": macd_flip,
            "rsi leaves os/ob": rsi_leaves,
            "stoch %k leaves os/ob": k_leaves,
            "stoch %d leaves os/ob": d_leaves,
            "rsi leaves os/ob + lower high": rsi_leaves and no_new_extreme,
            "stoch %k leaves os/ob + lower high": k_leaves and no_new_extreme,
            "lower high + stoch %k under %d": no_new_extreme and turned,
            "lower high + psar": no_new_extreme and sar_against,
            "lower high + inside vbs band": no_new_extreme and inside_band,
            "wick rejection + inside vbs band": rejection and inside_band,
            "two inside vbs band": inside_band and inside_previous,
        }[rule]
        inside_previous = inside_band
        if hit:
            return j
    return -1


def find_positions(symbol, data, contiguous, side, rule, window, dca_levels, stop_pct,
                   target_pct, fee_rate, cluster_gap, interval_minutes,
                   minimum_band_index=0.0, tick_size=1.0, use_bb_inside=True,
                   bb_width_minimum=None, entry_order="limit"):
    """Every position this symbol would have produced, each followed until it really closes."""
    high = data["high"].to_numpy()
    low = data["low"].to_numpy()
    close = data["close"].to_numpy()
    vbs_upper = data["vbs_upper"].to_numpy()
    vbs_lower = data["vbs_lower"].to_numpy()
    stoch_k = data["stoch_k"].to_numpy()
    stoch_d = data["stoch_d"].to_numpy()
    opentime = data["opentime"].to_numpy()
    is_short = side == "short"
    sign = 1.0 if is_short else -1.0
    band = vbs_upper if is_short else vbs_lower
    deepest = max((p for p, _ in dca_levels), default=0.0)
    ladder = 1.0 + sum(f / 100.0 for _, f in dca_levels)

    band_idx = data["band_index"].to_numpy()
    bb_width = data["bb_width"].to_numpy()

    out = []
    for index in met.cluster_starts(
            met.signal_mask(data, side, use_bb_inside, bb_width_minimum), cluster_gap):
        if minimum_band_index > 0:
            value = band_idx[index]
            if not np.isfinite(value) or value < minimum_band_index:
                continue

        arm = arm_offset(data, index, side, rule, window)
        if arm < 0:
            continue

        if entry_order == "market":
            # A market order is filled straight away, at the close of the candle the rule fired on.
            # No waiting for price to come back, so no signal is ever lost - but the price is
            # whatever the market gives.
            if arm + 1 > len(data) - 1 or not contiguous[arm]:
                continue
            fill = arm
            entry = close[arm]
            if not np.isfinite(entry) or entry <= 0:
                continue
        else:
            # --- the limit order, on the band, valid for the rest of the window ---
            limit = max(close[arm], band[arm]) if is_short else min(close[arm], band[arm])
            if not np.isfinite(limit) or limit <= 0:
                continue
            fill = -1
            for j in range(arm + 1, min(index + window, len(data) - 1) + 1):
                if not contiguous[j]:
                    break
                if (high[j] >= limit) if is_short else (low[j] <= limit):
                    fill = j
                    break
            if fill < 0:
                continue
            entry = limit
        pending = [(entry * (1.0 + sign * p / 100.0), f / 100.0) for p, f in dca_levels]
        stop_price = entry * (1.0 + sign * (deepest + stop_pct) / 100.0)

        quantity, invested, biggest = 1.0, entry, 1.0
        outcome, exit_price, closed_at = "open", np.nan, -1

        for j in range(fill + 1, min(fill + MAXIMUM_HOLD, len(data))):
            if not contiguous[j]:
                break
            still = []
            for level_price, size in pending:
                if (high[j] >= level_price) if is_short else (low[j] <= level_price):
                    quantity += size
                    invested += size * level_price
                    biggest = max(biggest, quantity)
                else:
                    still.append((level_price, size))
            pending = still

            average = invested / quantity
            # TpGridAnchorPrice (TradeTools.cs): the commission is ADDED to the anchor for a long
            # and SUBTRACTED for a short, so in both cases the target ends up further away and the
            # position nets the full target percentage after fees.
            anchor = average * (1.0 - sign * 2.0 * fee_rate / 100.0)
            target_price = anchor * (1.0 - sign * target_pct / 100.0)

            if (high[j] >= stop_price) if is_short else (low[j] <= stop_price):
                outcome, exit_price, closed_at = "stop", stop_price, j
                break
            if (low[j] <= target_price) if is_short else (high[j] >= target_price):
                outcome, exit_price, closed_at = "target", target_price, j
                break

        if closed_at < 0:
            closed_at = min(fill + MAXIMUM_HOLD, len(data)) - 1
            exit_price = close[closed_at]

        average = invested / quantity
        gross = (exit_price / average - 1.0) * -sign * 100.0
        net = gross - 2.0 * fee_rate            # percent of the money actually put in

        dca_prices = [entry * (1.0 + sign * q / 100.0) for q, _ in dca_levels]
        out.append({
            "symbol": symbol, "side": side,
            "signal_date": candledb.minutes_to_datetime(opentime[index]).strftime("%Y-%m-%d %H:%M"),
            "entry_date": candledb.minutes_to_datetime(opentime[fill]).strftime("%Y-%m-%d %H:%M"),
            "close_date": candledb.minutes_to_datetime(opentime[closed_at]).strftime("%Y-%m-%d %H:%M"),
            "open_time": int(opentime[fill]),
            "close_time": int(opentime[closed_at]),
            "hold_candles": closed_at - fill,
            # real prices, so a position can be looked up in the chart and checked by hand
            "entry_price": entry * tick_size,
            "dca_prices": " ".join("%.8g" % (q * tick_size) for q in dca_prices),
            "stop_price": stop_price * tick_size,
            "avg_price": average * tick_size,
            "exit_price": exit_price * tick_size,
            "band_index": band_idx[index],
            "bb_width": bb_width[index],
            "units": biggest, "ladder": ladder,
            "outcome": outcome, "gross_pct": gross, "net_pct": net,
        })
    return out


def run_account(positions, capital, slots_long, slots_short, minimum_order, ladder):
    positions = positions.sort_values("open_time").reset_index(drop=True)
    equity, free, realised = capital, capital, 0.0
    live, busy = [], set()
    counts = {"long": 0, "short": 0}
    taken, skipped = 0, {"munt al bezet": 0, "geen slot vrij": 0, "te weinig geld": 0}
    opened = []                  # the positions the account actually took, for the report
    curve = []

    def settle(until):
        nonlocal equity, free, realised
        rest = []
        for pos in live:
            if pos["close_time"] <= until:
                profit = pos["stake"] * pos["net_pct"] / 100.0
                equity += profit
                realised += profit
                free += pos["reserved"] + profit
                busy.discard(pos["symbol"])
                counts[pos["side"]] -= 1
                curve.append((pos["close_time"], equity))
            else:
                rest.append(pos)
        live[:] = rest

    reserved = capital / max(slots_long + slots_short, 1)
    for row in positions.itertuples():
        settle(row.open_time)
        if row.symbol in busy:
            skipped["munt al bezet"] += 1
            continue
        if counts[row.side] >= (slots_long if row.side == "long" else slots_short):
            skipped["geen slot vrij"] += 1
            continue
        if reserved / ladder < minimum_order or free < reserved:
            skipped["te weinig geld"] += 1
            continue
        free -= reserved
        busy.add(row.symbol)
        counts[row.side] += 1
        live.append({"close_time": row.close_time, "symbol": row.symbol, "side": row.side,
                     "reserved": reserved, "stake": reserved / ladder * row.units,
                     "net_pct": row.net_pct})
        opened.append(row.Index)
        taken += 1

    settle(10 ** 12)          # everything that closed before the end of the data
    unrealised = sum(p["stake"] * p["net_pct"] / 100.0 for p in live)
    return {"capital": capital, "equity": equity, "realised": realised,
            "unrealised": unrealised, "open": len(live),
            "stuck": sum(p["reserved"] for p in live), "taken": taken, "skipped": skipped,
            "opened": positions.loc[opened] if opened else positions.iloc[0:0],
            "curve": pd.DataFrame(curve, columns=["time", "equity"])}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--candles", required=True)
    parser.add_argument("--interval", default="15m")
    parser.add_argument("--capital", type=float, default=500.0)
    parser.add_argument("--slots-long", type=int, default=15)
    parser.add_argument("--slots-short", type=int, default=15)
    parser.add_argument("--minimum-order", type=float, default=5.0)
    parser.add_argument("--window", type=int, default=9, help="EntryRemoveTime in candles")
    parser.add_argument("--dca", default="5:200")
    parser.add_argument("--stop", type=float, default=2.5)
    parser.add_argument("--target", type=float, default=1.8)
    parser.add_argument("--fee", type=float, default=0.1, help="Exchange.FeeRate per transaction")
    parser.add_argument("--rule", default="now", choices=ENTRY_RULES)
    parser.add_argument("--entry-order", default="limit", choices=["limit", "market"],
                        help="limit = order on the vbs band (EntryOrderType = Limit), "
                             "market = fill at the close of the candle the rule fired on")
    parser.add_argument("--no-bb-inside", action="store_true",
                        help="drop the 'bollinger bands inside the vbs bands' condition")
    parser.add_argument("--bb-width", type=float, default=None,
                        help="minimum bollinger width; default is VbsSettings.BBMinPercentage")
    parser.add_argument("--min-band-index", type=float, default=0.0,
                        help="only take signals whose band range index is at or above this")
    parser.add_argument("--sides", default="both", choices=["both", "long", "short"])
    parser.add_argument("--out", default="", help="csv with every position, for checking by hand")
    parser.add_argument("--settings", default="",
                        help="CryptoScanBot-settings.json, for the rsi/stoch/bollinger thresholds")
    parser.add_argument("--symbol-db", default="",
                        help="CryptoScanBot.db, to turn tick prices into real prices in the export")
    args = parser.parse_args()

    dca_levels = [tuple(float(v) for v in part.split(":")) for part in args.dca.split(",") if part]
    ladder = 1.0 + sum(f / 100.0 for _, f in dca_levels)
    interval_id = candledb.INTERVAL_IDS[args.interval]
    interval_minutes = candledb.INTERVAL_MINUTES[args.interval]
    connection = candledb.open_readonly(args.candles)

    loaded_settings = "standaardwaarden (geen settings-bestand opgegeven)"
    if args.settings:
        loaded_settings = met.load_settings(args.settings)

    tick_sizes = {}
    if args.symbol_db:
        symbol_connection = candledb.open_readonly(args.symbol_db)
        tick_sizes = {n: float(t) for n, t in
                      symbol_connection.execute("select Name, PriceTickSize from Symbol").fetchall()
                      if t}

    started = time.time()
    rows = []
    symbols = candledb.list_symbols(connection, interval_id, met.WARMUP + 100)
    for number, (symbol_id, name, count) in enumerate(symbols, 1):
        frame = candledb.load_candles(connection, symbol_id, interval_id)
        if len(frame) < met.WARMUP + 100:
            continue
        data = met.compute(frame)
        contiguous = candledb.gap_mask(frame, interval_minutes)
        for side in ("short", "long"):
            if args.sides != "both" and side != args.sides:
                continue
            rows.extend(find_positions(name, data, contiguous, side, args.rule, args.window,
                                       dca_levels, args.stop, args.target, args.fee, 5,
                                       interval_minutes, args.min_band_index,
                                       tick_sizes.get(name, 1.0), not args.no_bb_inside,
                                       args.bb_width, args.entry_order))
        print(f"  [{number}/{len(symbols)}] {name:<16} posities tot nu toe: {len(rows)}", flush=True)

    positions = pd.DataFrame(rows)
    print(f"\n{len(positions)} posities gevonden in {time.time()-started:.0f}s")
    if args.out:
        positions.to_csv(args.out, index=False)

    slots_long = args.slots_long if args.sides in ("both", "long") else 0
    slots_short = args.slots_short if args.sides in ("both", "short") else 0
    result = run_account(positions, args.capital, slots_long, slots_short,
                         args.minimum_order, ladder)

    print()
    print("instellingen van deze meting")
    print(f"  instapregel                : {args.rule}")
    print(f"  order bij instap           : " + (
        "limietorder op de vbs-band (EntryOrderType = Limit), vervalt na het wachtvenster"
        if args.entry_order == "limit" else
        "marktorder op de close van de candle waar de regel afgaat (altijd gevuld)"))
    print(f"  wachtvenster               : {args.window} candles (EntryRemoveTime)")
    print(f"  interval                   : {args.interval}")
    print(f"  signaal                    : vbs-bandbreuk + rsi {met.RSI_OVERSOLD:.0f}/{met.RSI_OVERBOUGHT:.0f}"
          f" + bollinger binnen de vbs-band")
    print(f"  bollinger breedte minimaal : "
          f"{met.BB_WIDTH_MINIMUM if args.bb_width is None else args.bb_width}%")
    print(f"  bollinger binnen vbs-band  : "
          f"{'nee (uitgezet)' if args.no_bb_inside else 'ja (zoals VbsSignalShort regel 77/82)'}")
    print(f"  band range index minimaal  : "
          f"{args.min_band_index if args.min_band_index > 0 else 'geen filter'}")
    print(f"  dca                        : {args.dca}")
    print(f"  stop-loss                  : {args.stop}% voorbij de verste dca")
    print(f"  doel                       : {args.target}% vanaf het anker (de fee zit erin)")
    print(f"  fee                        : {args.fee}% per transactie")
    print(f"  drempels uit de settings   : {loaded_settings}")
    print(f"  slots                      : {slots_long} long / {slots_short} short")
    reserved = args.capital / max(slots_long + slots_short, 1)
    print(f"per positie gereserveerd {reserved:.2f} -> eerste instap {reserved/ladder:.2f}\n")
    print(f"  kansen                                       : {len(positions):7}")
    print(f"  daadwerkelijk geopend                        : {result['taken']:7}")
    for reden, aantal in result["skipped"].items():
        print(f"    overgeslagen, {reden:<16}         : {aantal:7}")
    print()
    print(f"  startkapitaal                                : {result['capital']:10.2f}")
    print(f"  geincasseerd op gesloten posities            : {result['realised']:+10.2f}")
    print(f"  ---------------------------------------------------------")
    print(f"  eindkapitaal                                 : {result['equity']:10.2f}   "
          f"({100*(result['equity']/result['capital']-1):+.1f}%)")
    print()
    print(f"  nog open aan het eind                        : {result['open']:7} posities")
    print(f"  openstaande winst/verlies (niet geincasseerd) : {result['unrealised']:+10.2f}")
    curve = result["curve"]
    if len(curve):
        top = curve["equity"].cummax()
        print(f"  grootste terugval onderweg                   : "
              f"{100*((curve['equity']-top)/top).min():9.1f}%")
    if len(positions):
        print()
        print(f"  afloop van de posities: "
              f"{dict(positions['outcome'].value_counts())}")
        print(f"  looptijd in candles: mediaan {positions['hold_candles'].median():.0f}, "
              f"gemiddeld {positions['hold_candles'].mean():.0f}, "
              f"langste {positions['hold_candles'].max():.0f}")


if __name__ == "__main__":
    sys.exit(main())
