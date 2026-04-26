"""
Converts an XLS candle file to multiple JSON files.
One JSON file per sheet (excluding 'Information'), named ADAUSDT-{sheet}.json.
Uses xlrd directly (1.2.0) to support the legacy .xls OLE2 format.
"""

import json
import os
import xlrd
from datetime import datetime, timezone, timedelta

# --- Configuration ---
INPUT_FILE = r"E:\CryptoScanBot\Binance\FuturesAvalonia.DataGrid.Minutes\Excel\ADAUSDT Candles.xls"
OUTPUT_DIR = r"E:\Projects\CryptoScanBot.Avalonia.DataGrid\CryptoScanner.CoreTests\Signal\Bbma\ADAUSDT"
SYMBOL = "ADAUSDT"
SKIP_SHEETS = {"Information"}

# Epoch: 2010-01-04 00:00:00 UTC
EPOCH = datetime(2010, 1, 4, 0, 0, 0, tzinfo=timezone.utc)


def candle_time_from_open_time(open_time: datetime) -> int:
    """
    Calculate CandleTime as integer minutes since epoch.
    OpenTime values in the Excel are treated as UTC.
    """
    # Ensure UTC — Excel datetimes have no tz info, treat as UTC
    if open_time.tzinfo is None:
        open_time = open_time.replace(tzinfo=timezone.utc)
    delta = open_time - EPOCH
    return int(delta.total_seconds() / 60)


def xlrd_datetime_to_python(cell, datemode: int) -> datetime:
    """Convert an xlrd date cell value to a Python datetime (UTC, naive)."""
    # xlrd stores dates as floating-point day counts from a workbook epoch
    tup = xlrd.xldate_as_tuple(cell.value, datemode)
    return datetime(*tup)  # naive — treated as UTC per spec


def process_sheet(sheet, datemode: int) -> dict:
    """Read an xlrd Sheet and return the target JSON dict structure."""
    # First row = header
    headers = [sheet.cell_value(0, col) for col in range(sheet.ncols)]
    col_idx = {name: idx for idx, name in enumerate(headers)}

    required = {"OpenTime", "CloseTime", "Open", "High", "Low", "Close", "Volume"}
    missing = required - set(col_idx.keys())
    if missing:
        raise ValueError(f"Missing columns: {missing}")

    result = {}

    for row_idx in range(1, sheet.nrows):
        open_cell = sheet.cell(row_idx, col_idx["OpenTime"])
        open_time_dt = xlrd_datetime_to_python(open_cell, datemode)
        candle_time = candle_time_from_open_time(open_time_dt)
        key = str(candle_time)

        def fval(col_name):
            return round(float(sheet.cell_value(row_idx, col_idx[col_name])), 8)

        result[key] = {
            "OpenTime": candle_time,
            "Open":   fval("Open"),
            "High":   fval("High"),
            "Low":    fval("Low"),
            "Close":  fval("Close"),
            "Volume": fval("Volume"),
        }

    # Sort by OpenTime ascending
    result = dict(sorted(result.items(), key=lambda x: int(x[0])))
    return result


def candle_time_to_dt(candle_time: int) -> datetime:
    """Convert CandleTime back to UTC datetime for display."""
    return EPOCH.replace(tzinfo=None) + timedelta(minutes=candle_time)


def main():
    # Create output directory if needed
    os.makedirs(OUTPUT_DIR, exist_ok=True)

    print(f"Reading: {INPUT_FILE}")
    workbook = xlrd.open_workbook(INPUT_FILE)
    sheet_names = workbook.sheet_names()
    datemode = workbook.datemode
    print(f"Sheets found: {sheet_names}")
    print(f"Date mode   : {datemode} ({'1904' if datemode else '1900'} epoch)\n")

    for sheet_name in sheet_names:
        if sheet_name in SKIP_SHEETS:
            print(f"  Skipping sheet: {sheet_name}")
            continue

        sheet = workbook.sheet_by_name(sheet_name)

        if sheet.nrows <= 1:
            print(f"  WARNING: Sheet '{sheet_name}' is empty — skipped.")
            continue

        try:
            data = process_sheet(sheet, datemode)
        except ValueError as e:
            print(f"  WARNING: Sheet '{sheet_name}': {e} — skipped.")
            continue

        output_filename = f"{SYMBOL}-{sheet_name}.json"
        output_path = os.path.join(OUTPUT_DIR, output_filename)

        with open(output_path, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2)

        # Summary info
        keys = list(data.keys())
        first_key = int(keys[0])
        last_key  = int(keys[-1])
        first_dt  = candle_time_to_dt(first_key)
        last_dt   = candle_time_to_dt(last_key)

        print(f"  Written: {output_filename}")
        print(f"    Rows       : {len(data)}")
        print(f"    First entry: CandleTime={first_key}  ({first_dt.strftime('%Y-%m-%d %H:%M')} UTC)")
        print(f"    Last entry : CandleTime={last_key}  ({last_dt.strftime('%Y-%m-%d %H:%M')} UTC)")
        print()

    print("Done.")


if __name__ == "__main__":
    main()
