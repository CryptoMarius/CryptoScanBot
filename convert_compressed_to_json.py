"""
Converts CryptoScanBot .compressed candle files (LZ4 + binary) to JSON test data files.

Binary format (version 2):
  [int32]  version = 2
  Per interval section (repeats until EOF):
    [int32]  marker = 1234567890
    [int32]  intervalPeriod  (CryptoIntervalPeriod enum, 0-15)
    [uint32] lastCandleSynchronized
    [int32]  candleCount
    Per candle (29 bytes each):
      [uint32] OpenTime    minutes since 2010-01-04 00:00:00 UTC
      [byte]   TickDecimals
      [int32]  OpenTicks
      [int32]  HighTicks
      [int32]  LowTicks
      [int32]  CloseTicks
      [double] Volume

Price = ticks * 10^(-TickDecimals)

Output JSON format (matches LoadCandleDataFromDisk / CryptoCandleList):
{
  "12345": { "OpenTime": 12345, "Open": 0.7456, "High": ..., "Low": ..., "Close": ..., "Volume": ... },
  ...
}
"""

import struct
import json
import os
import io
from datetime import datetime, timezone

import lz4.frame

# CryptoIntervalPeriod enum (0-15)
INTERVAL_NAMES = {
    0: "1m", 1: "2m", 2: "3m", 3: "5m", 4: "10m",
    5: "15m", 6: "30m", 7: "1h", 8: "2h", 9: "3h",
    10: "4h", 11: "6h", 12: "8h", 13: "12h", 14: "1d", 15: "1w"
}

MARKER = 1234567890
EPOCH = datetime(2010, 1, 4, 0, 0, 0, tzinfo=timezone.utc)


def candle_time_to_dt(minutes: int) -> datetime:
    return datetime.fromtimestamp(
        EPOCH.timestamp() + minutes * 60, tz=timezone.utc
    )


def convert_file(input_path: str, output_dir: str, symbol: str, interval_name: str):
    """
    Decompress and parse one .compressed candle file.

    File format (this export variant, version 2):
      [int32]  version = 2
      [int32]  marker  = 1234567890
      [int32]  candleCount
      [candleCount * 29 bytes] candles

    Per candle (29 bytes, matches CryptoCandle.LoadVersion3):
      [uint32] OpenTime      minutes since 2010-01-04 UTC
      [byte]   TickDecimals
      [int32]  OpenTicks
      [int32]  HighTicks
      [int32]  LowTicks
      [int32]  CloseTicks
      [double] Volume

    Returns the output file path, or None on error.
    """
    with open(input_path, "rb") as f:
        raw = f.read()

    dec = lz4.frame.decompress(raw)

    version = struct.unpack_from("<i", dec, 0)[0]
    if version != 2:
        raise ValueError(f"Unsupported file version: {version}")

    marker = struct.unpack_from("<i", dec, 4)[0]
    if marker != MARKER:
        raise ValueError(f"Bad marker: {marker}")

    count = struct.unpack_from("<i", dec, 8)[0]

    expected = 12 + count * 29
    if len(dec) != expected:
        raise ValueError(f"Size mismatch: expected {expected} bytes, got {len(dec)}")

    candles = {}
    offset = 12
    for _ in range(count):
        ot  = struct.unpack_from("<I", dec, offset)[0]      # uint32
        td  = struct.unpack_from("<B", dec, offset + 4)[0]  # byte
        op  = struct.unpack_from("<i", dec, offset + 5)[0]  # int32
        hi  = struct.unpack_from("<i", dec, offset + 9)[0]
        lo  = struct.unpack_from("<i", dec, offset + 13)[0]
        cl  = struct.unpack_from("<i", dec, offset + 17)[0]
        vol = struct.unpack_from("<d", dec, offset + 21)[0]  # double
        offset += 29

        ts = 10 ** (-td)
        candles[str(ot)] = {
            "OpenTime": ot,
            "Open":     round(op  * ts, td),
            "High":     round(hi  * ts, td),
            "Low":      round(lo  * ts, td),
            "Close":    round(cl  * ts, td),
            "Volume":   round(vol, 8),
        }

    if not candles:
        print(f"  [{interval_name}] 0 candles — skipped")
        return None

    os.makedirs(output_dir, exist_ok=True)

    out_path = os.path.join(output_dir, f"{symbol}-{interval_name}.json")
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(candles, f, indent=2)  # already in ascending order (scanner writes sorted)

    times = list(candles.values())
    first_dt = candle_time_to_dt(times[0]["OpenTime"])
    last_dt  = candle_time_to_dt(times[-1]["OpenTime"])
    print(f"  [{interval_name:>3}]  {count:>6} candles  "
          f"{first_dt.strftime('%Y-%m-%d %H:%M')} - {last_dt.strftime('%Y-%m-%d %H:%M')} UTC  -> {out_path}")
    return out_path


# ─── main ────────────────────────────────────────────────────────────────────

SOURCE_DIR = r"E:\CryptoScanBot\Binance\FuturesAvalonia.DataGrid.Minutes\Binance Futures\usdt"
OUTPUT_DIR = r"E:\Projects\CryptoScanBot.Avalonia.DataGrid\CryptoScanner.CoreTests\Signal\Bbma"

# Which symbols to convert (lowercase = filename prefix, uppercase = output symbol name)
SYMBOLS = {
    "ada": "ADAUSDT",
    # Add more here, e.g.: "btc": "BTCUSDT",
}

total_files = 0

for prefix, symbol in SYMBOLS.items():
    # Find all .compressed files for this symbol
    pattern_files = [
        f for f in os.listdir(SOURCE_DIR)
        if f.startswith(f"{prefix}-") and f.endswith(".compressed")
    ]

    if not pattern_files:
        print(f"No .compressed files found for '{prefix}' in {SOURCE_DIR}")
        continue

    out_dir = os.path.join(OUTPUT_DIR, symbol)
    print(f"\n=== {symbol} -> {out_dir} ===")

    for filename in sorted(pattern_files):
        # Extract interval name from filename: "ada-5m.compressed" -> "5m"
        interval_name = filename[len(prefix) + 1:].replace(".compressed", "")
        input_path = os.path.join(SOURCE_DIR, filename)
        try:
            out = convert_file(input_path, out_dir, symbol, interval_name)
            if out:
                total_files += 1
        except Exception as e:
            print(f"  [{interval_name}] ERROR: {e}")

print(f"\nDone — {total_files} JSON file(s) written.")
