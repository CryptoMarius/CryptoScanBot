"""
Entry-timing measurement for the VBS (VWAP band) strategy.

The question this answers: after a VBS band break, HOW FAR and for HOW LONG does price keep
running the wrong way before it turns? That number decides whether waiting for a reversal can work
at all, and how long a signal has to stay alive while it waits.

What it does per symbol on one interval:
  1. recompute the indicators the scanner uses (verified against the C# values, see
     verify_indicators.py)
  2. flag every candle where the VBS signal conditions hold (band break + RSI + the Bollinger
     conditions from VbsSignalShort/VbsSignalLong)
  3. group consecutive flags into one event - the trader enters on the FIRST one and a newer
     signal supersedes the older, so the first flag is today's entry moment
  4. look forward `horizon` candles and record where the extreme against the position was, how far
     it went, and whether the fixed stop-loss / take-profit would have been hit first

Two outputs:
  events.csv   one row per event: indicators at the signal candle plus the forward measurements
  panel.csv.gz one row per event x offset (0..horizon) with prices and indicators, so entry rules
               can be tried out afterwards without recomputing anything

All distances are expressed twice: in percent, and in units of ACS (Average Candle Size) so that a
quiet market and a wild one can be compared on the same scale.

Usage:
    python measure_entry_timing.py --candles "<path>\\Binance Futures.db" --interval 15m \
                                   --out ..\\..\\..\\out --horizon 48 --panel
"""

import argparse
import os
import sys
import time

import io

import numpy as np
import pandas as pd

import candledb
import indicators

# Defaults, overwritten by load_settings() from the scanner's own CryptoScanBot-settings.json so a
# measurement never quietly uses a threshold the scanner does not have.
RSI_LENGTH = 14
RSI_OVERBOUGHT = 70.0
RSI_OVERSOLD = 30.0
STOCH_LENGTH, STOCH_K, STOCH_D = 14, 3, 3
STOCH_OVERBOUGHT, STOCH_OVERSOLD = 80.0, 20.0
BB_LENGTH, BB_DEVIATION = 20, 2.0


def load_settings(path):
    """Take RSI / Stochastic / Bollinger settings from the scanner's settings file.

    Returns a short description of what was loaded, so a run can print which file it used.
    """
    global RSI_LENGTH, RSI_OVERBOUGHT, RSI_OVERSOLD
    global STOCH_LENGTH, STOCH_K, STOCH_D, STOCH_OVERBOUGHT, STOCH_OVERSOLD
    global BB_LENGTH, BB_DEVIATION

    import json
    with io.open(path, encoding="utf-8-sig") as handle:
        general = json.load(handle).get("General", {})

    rsi = general.get("SettingsRsi", {})
    RSI_LENGTH = int(rsi.get("Length", RSI_LENGTH))
    RSI_OVERBOUGHT = float(rsi.get("Overbought", RSI_OVERBOUGHT))
    RSI_OVERSOLD = float(rsi.get("Oversold", RSI_OVERSOLD))

    stoch = general.get("SettingsStoch", {})
    STOCH_LENGTH = int(stoch.get("Length", STOCH_LENGTH))
    STOCH_K = int(stoch.get("SmoothingK", STOCH_K))
    STOCH_D = int(stoch.get("SmoothingD", STOCH_D))
    STOCH_OVERBOUGHT = float(stoch.get("Overbought", STOCH_OVERBOUGHT))
    STOCH_OVERSOLD = float(stoch.get("Oversold", STOCH_OVERSOLD))

    bollinger = general.get("SettingsBb", {})
    BB_LENGTH = int(bollinger.get("Length", BB_LENGTH))
    BB_DEVIATION = float(bollinger.get("Deviation", BB_DEVIATION))

    return (f"rsi {RSI_LENGTH} ({RSI_OVERSOLD:.0f}/{RSI_OVERBOUGHT:.0f}) | "
            f"stoch {STOCH_LENGTH}/{STOCH_K}/{STOCH_D} ({STOCH_OVERSOLD:.0f}/{STOCH_OVERBOUGHT:.0f}) | "
            f"bollinger {BB_LENGTH}/{BB_DEVIATION}")
BB_WIDTH_MINIMUM = 1.50           # VbsSettings.BBMinPercentage
VBS_LENGTH, VBS_MULTIPLIER = 50, 2.5
ACS_LENGTH, ACS_FACTOR = 50, 2.17
WARMUP = 260


def compute(frame):
    high = frame["high"].to_numpy()
    low = frame["low"].to_numpy()
    close = frame["close"].to_numpy()
    openp = frame["open"].to_numpy()
    volume = frame["volume"].to_numpy()

    out = pd.DataFrame({"opentime": frame["opentime"].to_numpy(),
                        "open": openp, "high": high, "low": low, "close": close})
    out["rsi"] = indicators.rsi(close, RSI_LENGTH)
    out["stoch_k"], out["stoch_d"] = indicators.stochastic(high, low, close, STOCH_LENGTH, STOCH_K, STOCH_D)
    out["macd"], out["macd_signal"], out["macd_hist"] = indicators.macd(close, 12, 26, 9)
    bb_basis, bb_upper, bb_lower, bb_width, bb_pct = indicators.bollinger(close, BB_LENGTH, BB_DEVIATION)
    out["bb_upper"], out["bb_lower"], out["bb_width"], out["bb_pct"] = bb_upper, bb_lower, bb_width, bb_pct
    out["psar"], out["psar_rising"] = indicators.parabolic_sar(high, low, 0.02, 0.2)
    basis, vbs_upper, vbs_lower, vw_stdev = indicators.vbs_bands(high, low, close, volume, VBS_LENGTH, VBS_MULTIPLIER)
    out["vbs_basis"], out["vbs_upper"], out["vbs_lower"] = basis, vbs_upper, vbs_lower
    out["acs"] = indicators.acs_percentage(high, low, close, ACS_LENGTH, ACS_FACTOR)

    keltner_centre, keltner_upper, keltner_lower = indicators.keltner(high, low, close, 20, 10, 2.0)
    out["kc_upper"], out["kc_lower"], out["kc_centre"] = keltner_upper, keltner_lower, keltner_centre

    # Where the close sits inside its own candle: 100 = closed on the high, 0 = on the low.
    span = high - low
    with np.errstate(divide="ignore", invalid="ignore"):
        out["close_in_range"] = np.where(span > 0, (close - low) / span * 100.0, 50.0)
        # Position between the VBS basis and the band: 100 = on the band, above 100 = outside it.
        out["vbs_pos_upper"] = (close - basis) / (vbs_upper - basis) * 100.0
        out["vbs_pos_lower"] = (basis - close) / (basis - vbs_lower) * 100.0
    return out


def signal_mask(data, side, use_bb_inside=True, bb_width_minimum=None):
    """The VBS entry conditions, mirroring VbsSignalShort.IsSignal / VbsSignalLong.IsSignal.

    use_bb_inside switches off the "both bollinger bands inside the vbs bands" comparison
    (VbsSignalShort lines 77 and 82). With it on, the signal only fires while the bollinger bands
    are the narrower pair, which is close to what the STOBB strategy already flags.
    """
    minimum = BB_WIDTH_MINIMUM if bb_width_minimum is None else bb_width_minimum
    wide_enough = data["bb_width"].to_numpy() > minimum
    if use_bb_inside:
        bb_inside = ((data["bb_lower"].to_numpy() > data["vbs_lower"].to_numpy())
                     & (data["bb_upper"].to_numpy() < data["vbs_upper"].to_numpy()))
    else:
        bb_inside = np.ones(len(data), dtype=bool)
    if side == "short":
        breaks = ((data["high"].to_numpy() > data["vbs_upper"].to_numpy())
                  | (data["close"].to_numpy() > data["vbs_upper"].to_numpy()))
        confluence = data["rsi"].to_numpy() >= RSI_OVERBOUGHT
    else:
        breaks = ((data["low"].to_numpy() < data["vbs_lower"].to_numpy())
                  | (data["close"].to_numpy() < data["vbs_lower"].to_numpy()))
        confluence = data["rsi"].to_numpy() <= RSI_OVERSOLD
    mask = wide_enough & bb_inside & breaks & confluence
    mask[:WARMUP] = False
    return np.nan_to_num(mask, nan=False).astype(bool)


def cluster_starts(mask, max_gap):
    """First flag of every burst of signals. A newer VBS signal supersedes the older one, so a run
    of consecutive band breaks is one trade opportunity - and the trader takes the first."""
    indexes = np.flatnonzero(mask)
    if not len(indexes):
        return []
    starts = [indexes[0]]
    for previous, current in zip(indexes, indexes[1:]):
        if current - previous > max_gap:
            starts.append(current)
    return starts


def measure_symbol(symbol_name, data, contiguous, side, horizon, cluster_gap, collect_panel):
    high = data["high"].to_numpy()
    low = data["low"].to_numpy()
    close = data["close"].to_numpy()
    vbs_upper = data["vbs_upper"].to_numpy()
    vbs_lower = data["vbs_lower"].to_numpy()
    acs = data["acs"].to_numpy()
    opentime = data["opentime"].to_numpy()
    is_short = side == "short"

    events, panel = [], []
    for index in cluster_starts(signal_mask(data, side), cluster_gap):
        stop = index + horizon
        if stop >= len(data):
            continue
        if not contiguous[index + 1:stop + 1].all():
            continue                       # a hole in the candle history: forward window unusable
        acs_pct = acs[index]
        if not np.isfinite(acs_pct) or acs_pct <= 0:
            continue

        # Entry as the signal computes it: the more extreme of the close and the band.
        entry = max(close[index], vbs_upper[index]) if is_short else min(close[index], vbs_lower[index])
        if not np.isfinite(entry) or entry <= 0:
            continue

        window = slice(index + 1, stop + 1)
        # Adverse = the direction the position does NOT want; favourable = towards the take-profit.
        adverse = (high[window] - entry) / entry * 100.0 if is_short else (entry - low[window]) / entry * 100.0
        favourable = (entry - low[window]) / entry * 100.0 if is_short else (high[window] - entry) / entry * 100.0

        worst_offset = int(np.argmax(adverse)) + 1
        worst_pct = float(adverse.max())
        best_offset = int(np.argmax(favourable)) + 1
        best_pct = float(favourable.max())

        # After the extreme: how much did it give back? That is what waiting could have captured.
        after = favourable[worst_offset - 1:]
        recovery_pct = float(after.max()) if len(after) else float("nan")

        # Outcome under today's rules: stop-loss and take-profit both at 1 x ACS from the entry.
        stop_hit = np.flatnonzero(adverse >= acs_pct)
        target_hit = np.flatnonzero(favourable >= acs_pct)
        first_stop = int(stop_hit[0]) + 1 if len(stop_hit) else None
        first_target = int(target_hit[0]) + 1 if len(target_hit) else None
        if first_target is None and first_stop is None:
            outcome = "open"
        elif first_stop is None:
            outcome = "target"
        elif first_target is None:
            outcome = "stop"
        else:
            # Same candle: assume the adverse side is touched first (conservative).
            outcome = "stop" if first_stop <= first_target else "target"

        row = data.iloc[index]
        events.append({
            "symbol": symbol_name,
            "side": side,
            "opentime": int(opentime[index]),
            "date": candledb.minutes_to_datetime(opentime[index]).strftime("%Y-%m-%d %H:%M"),
            "acs_pct": acs_pct,
            "rsi": row["rsi"],
            "stoch_k": row["stoch_k"],
            "stoch_d": row["stoch_d"],
            "bb_width": row["bb_width"],
            "bb_pct": row["bb_pct"],
            "macd_hist_pct": row["macd_hist"] / entry * 100.0,
            "psar_rising": bool(row["psar_rising"]),
            "close_in_range": row["close_in_range"],
            "vbs_pos": row["vbs_pos_upper"] if is_short else row["vbs_pos_lower"],
            "worst_offset": worst_offset,
            "worst_pct": worst_pct,
            "worst_acs": worst_pct / acs_pct,
            "best_offset": best_offset,
            "best_pct": best_pct,
            "best_acs": best_pct / acs_pct,
            "recovery_pct": recovery_pct,
            "recovery_acs": recovery_pct / acs_pct,
            "outcome": outcome,
            "stop_offset": first_stop if first_stop is not None else -1,
            "target_offset": first_target if first_target is not None else -1,
        })

        if collect_panel:
            event_id = f"{symbol_name}|{side}|{int(opentime[index])}"
            for offset in range(0, horizon + 1):
                at = index + offset
                current = data.iloc[at]
                panel.append({
                    "event_id": event_id,
                    "offset": offset,
                    "adverse_pct": ((high[at] - entry) / entry * 100.0) if is_short else ((entry - low[at]) / entry * 100.0),
                    "favourable_pct": ((entry - low[at]) / entry * 100.0) if is_short else ((high[at] - entry) / entry * 100.0),
                    "close_pct": ((entry - close[at]) / entry * 100.0) if is_short else ((close[at] - entry) / entry * 100.0),
                    # Everything below is normalised to the direction of the POSITION, so one rule
                    # covers both sides: a high value always means "the move that triggered the
                    # signal is still going", a low or negative one means "it is turning our way".
                    "rsi_extreme": current["rsi"] if is_short else 100.0 - current["rsi"],
                    "stoch_extreme": current["stoch_k"] if is_short else 100.0 - current["stoch_k"],
                    "stoch_turn": (current["stoch_k"] - current["stoch_d"]) * (1.0 if is_short else -1.0),
                    "macd_turn": current["macd_hist"] / entry * 100.0 * (1.0 if is_short else -1.0),
                    # True when the parabolic SAR sits on the side that opposes the move, i.e. it
                    # flipped in favour of the position.
                    "psar_against": (not bool(current["psar_rising"])) if is_short else bool(current["psar_rising"]),
                    # 100 = on the band, above 100 = outside it, on the side the signal fired.
                    "bb_out": current["bb_pct"] if is_short else 100.0 - current["bb_pct"],
                    "bb_width": current["bb_width"],
                    # 100 = closed at the extreme of the move (no rejection), 0 = full rejection.
                    "close_in_range": current["close_in_range"] if is_short else 100.0 - current["close_in_range"],
                    "vbs_pos": current["vbs_pos_upper"] if is_short else current["vbs_pos_lower"],
                    "kc_break": bool(current["high"] > current["kc_upper"]) if is_short else bool(current["low"] < current["kc_lower"]),
                    "acs_pct": current["acs"],
                    # The band and the VWAP basis of THIS candle, relative to the original entry.
                    # A limit order rides the band, so its price moves with every candle.
                    "band_pct": (current["vbs_upper"] - entry) / entry * 100.0 if is_short
                                else (entry - current["vbs_lower"]) / entry * 100.0,
                    "basis_pct": (current["vbs_basis"] - entry) / entry * 100.0 if is_short
                                 else (entry - current["vbs_basis"]) / entry * 100.0,
                })
    return events, panel


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--candles", required=True)
    parser.add_argument("--interval", default="15m")
    parser.add_argument("--horizon", type=int, default=48, help="candles to look forward")
    parser.add_argument("--cluster-gap", type=int, default=5,
                        help="consecutive signals within this many candles count as one event "
                             "(EntryRemoveTime)")
    parser.add_argument("--symbols", default="", help="comma separated subset, default all")
    parser.add_argument("--out", required=True, help="output directory")
    parser.add_argument("--panel", action="store_true", help="also write the per-offset panel")
    parser.add_argument("--mirror", action="store_true",
                        help="run on mirrored prices (1/price, high and low swapped). Every rise "
                             "becomes a fall, so a correct measurement must swap the long and short "
                             "results. Any difference that survives the mirror is in the code.")
    args = parser.parse_args()

    interval_id = candledb.INTERVAL_IDS[args.interval]
    interval_minutes = candledb.INTERVAL_MINUTES[args.interval]
    connection = candledb.open_readonly(args.candles)
    wanted = {s.strip().upper() for s in args.symbols.split(",") if s.strip()}

    symbols = candledb.list_symbols(connection, interval_id, minimum_candles=WARMUP + args.horizon + 50)
    if wanted:
        symbols = [s for s in symbols if s[1].upper() in wanted]
    print(f"{len(symbols)} symbols on {args.interval}")

    os.makedirs(args.out, exist_ok=True)
    all_events, all_panel = [], []
    started = time.time()

    for number, (symbol_id, name, count) in enumerate(symbols, 1):
        frame = candledb.load_candles(connection, symbol_id, interval_id)
        if len(frame) < WARMUP + args.horizon + 50:
            continue
        if args.mirror:
            frame = frame.assign(open=1.0 / frame["open"], close=1.0 / frame["close"],
                                 high=1.0 / frame["low"], low=1.0 / frame["high"])
        data = compute(frame)
        contiguous = candledb.gap_mask(frame, interval_minutes)
        for side in ("short", "long"):
            events, panel = measure_symbol(name, data, contiguous, side, args.horizon,
                                           args.cluster_gap, args.panel)
            all_events.extend(events)
            all_panel.extend(panel)
        print(f"  [{number}/{len(symbols)}] {name:<16} {count:>7} candles  "
              f"events so far: {len(all_events)}", flush=True)

    events_frame = pd.DataFrame(all_events)
    events_path = os.path.join(args.out, "events.csv")
    events_frame.to_csv(events_path, index=False)
    print(f"\n{len(events_frame)} events -> {events_path}")

    if args.panel and all_panel:
        panel_path = os.path.join(args.out, "panel.csv.gz")
        pd.DataFrame(all_panel).to_csv(panel_path, index=False, compression="gzip")
        print(f"{len(all_panel)} panel rows -> {panel_path}")

    print(f"took {time.time() - started:.0f}s")


if __name__ == "__main__":
    sys.exit(main())
