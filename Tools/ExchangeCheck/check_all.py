#!/usr/bin/env python3
"""
Runs check_exchange.py over every scanner data folder under one base folder.

Meant to be started by "Make reports.cmd" (double click), not typed by hand: it finds the data
folders itself, matches each one to its memory samples, writes a report per folder and prints one
overview line per exchange.

A data folder is any folder holding a CryptoScanBot.db - so both
    %APPDATA%\\CryptoScanBot\\Data\\Binance\\Futures
    %APPDATA%\\CryptoScanBot-KRTest
are found without having to list them anywhere.
"""

import argparse
import json
import os
import subprocess
import sys
import time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

MAXIMUM_DEPTH = 3          # how deep below the base folder to look
# Emulator sessions live in the same folder tree and look like a data folder, but they hold a
# backtest, not a night run: no live stream, no log of subscriptions, and a Position table with
# hundreds of thousands of rows. Checking them answers nothing and costs minutes.
EXCLUDED_PARTS = ("emulator",)
WORKERS = 4                # checks run in parallel; the work is disk bound, so more is not faster
VERDICT_ORDER = {"bad": 0, "attention": 1, "unknown": 2, "good": 3}


def find_data_folders(base, depth=MAXIMUM_DEPTH):
    """Every folder with a main database in it, deepest match wins (no nested duplicates)."""
    found = []
    if not base.is_dir():
        return found
    for path in sorted(base.iterdir()):
        if not path.is_dir():
            continue
        if any(part in path.name.lower() for part in EXCLUDED_PARTS):
            continue
        if any(child.name.lower().startswith("cryptoscanbot") and child.suffix.lower() == ".db"
               for child in path.iterdir() if child.is_file()):
            found.append(path)
        elif depth > 1:
            found.extend(find_data_folders(path, depth - 1))
    return found


def last_activity(folder):
    """Newest log file timestamp, or None when the folder has no log at all."""
    log_folder = folder / "Log"
    if not log_folder.is_dir():
        return None
    stamps = [path.stat().st_mtime for path in log_folder.glob("*.log")]
    return max(stamps) if stamps else None


def sample_name(folder):
    """
    Same naming as sample-process.ps1: the last two parts of the data folder joined with a dash,
    so "...\\Data\\Binance\\Futures" pairs with "Binance-Futures-memory.csv".
    """
    parts = [part for part in folder.parts if part not in ("\\", "/")]
    if len(parts) >= 2:
        return "{}-{}".format(parts[-2], parts[-1])
    return folder.name


def archive_previous_reports(output):
    """
    Move whatever is already in the report folder into Older/<date it was written>.

    Two things went wrong without this, both on 20-08-2026:

      - The folder held Coinbase-Futures-report.html from a crashed run of 15-08. Nothing had
        refreshed it since (that data folder is gone), so it sat between the reports of last night
        looking exactly as current as they did, full of "not measured" - and it was read as a report
        about last night.

      - Every night overwrote the *-facts.json of the night before. Those files exist to be able to
        compare night four against night one, and there was never more than one night on disk.

    Moving, not deleting: a report is cheap to keep and the user decides when it goes.
    """
    keep = [path for path in output.glob("*")
            if path.is_file() and (path.name.endswith("-report.html")
                                   or path.name.endswith("-report.md")
                                   or path.name.endswith("-facts.json"))]
    if not keep:
        return 0

    moved = 0
    for path in keep:
        stamp = time.strftime("%Y-%m-%d", time.localtime(path.stat().st_mtime))
        target_folder = output / "Older" / stamp
        target_folder.mkdir(parents=True, exist_ok=True)
        target = target_folder / path.name
        # A second run on the same day would collide with the first; number those instead of
        # silently throwing one of them away.
        if target.exists():
            index = 2
            while (target_folder / "{} ({}){}".format(target.stem, index, target.suffix)).exists():
                index += 1
            target = target_folder / "{} ({}){}".format(target.stem, index, target.suffix)
        path.replace(target)
        moved += 1
    return moved


def main():
    parser = argparse.ArgumentParser(description="Check every scanner data folder under one base.")
    parser.add_argument("--base", required=True, help="Folder holding the data folders.")
    parser.add_argument("--memory", help="Folder with the *-memory.csv files (optional).")
    parser.add_argument("--out", required=True, help="Where the reports are written.")
    parser.add_argument("--format", choices=("html", "md"), default="html",
                        help="html (default) opens with a double click and carries the colours and "
                             "the table of contents; md is the plain text version.")
    parser.add_argument("--max-age-days", type=float, default=3.0,
                        help="Skip folders whose newest log is older than this (default 3). Use 0 "
                             "to check every folder found.")
    parser.add_argument("--deep", action="store_true",
                        help="Pass --deep to every check (whole candle history instead of the run).")
    arguments = parser.parse_args()

    base = Path(os.path.expandvars(arguments.base))
    output = Path(os.path.expandvars(arguments.out))
    memory = Path(os.path.expandvars(arguments.memory)) if arguments.memory else None
    output.mkdir(parents=True, exist_ok=True)

    folders = find_data_folders(base)
    if not folders:
        print("No scanner data folder found under {}".format(base))
        print("A data folder is one that holds CryptoScanBot.db.")
        return 1

    # A folder whose log has not been touched for days did not run last night, and checking it only
    # buries the folders that did.
    skipped = []
    if arguments.max_age_days > 0:
        cutoff = time.time() - arguments.max_age_days * 86400
        fresh = []
        for folder in folders:
            stamp = last_activity(folder)
            if stamp is None or stamp < cutoff:
                skipped.append(folder)
            else:
                fresh.append(folder)
        folders = fresh

    archived = archive_previous_reports(output)
    if archived:
        print("Moved {} report(s) from an earlier run to {}".format(archived, output / "Older"))
        print("The folder now holds THIS run only, so nothing older can be mistaken for it.")
        print()

    print("Found {} data folder(s) under {}".format(len(folders) + len(skipped), base))
    if skipped:
        print("Skipping {} without a log from the last {:.0f} day(s): {}".format(
            len(skipped), arguments.max_age_days,
            ", ".join(sample_name(folder) for folder in skipped)))
    if not folders:
        print("Nothing recent to check. Use --max-age-days 0 to check everything anyway.")
        return 1
    print()

    script = Path(__file__).with_name("check_exchange.py")

    def check_one(folder):
        label = sample_name(folder)
        report_path = output / "{}-report.{}".format(label, arguments.format)
        facts_path = output / "{}-facts.json".format(label)

        command = [sys.executable, str(script), "--folder", str(folder),
                   "--out", str(report_path), "--json", str(facts_path)]
        if arguments.deep:
            command.append("--deep")

        csv_path = None
        if memory:
            candidate = memory / "{}-memory.csv".format(label)
            if candidate.is_file():
                csv_path = candidate
                command.extend(["--memory-csv", str(candidate)])

        try:
            completed = subprocess.run(command, capture_output=True, text=True)
        except Exception as error:
            print("   {} could not run: {}".format(label, error), flush=True)
            return (label, "unknown", "could not run", report_path)

        if completed.returncode not in (0, 1, 2):
            print("   {} failed: {}".format(label, (completed.stderr or "").strip()[:200]),
                  flush=True)
            return (label, "unknown", "check failed", report_path)

        verdict, summary = "unknown", ""
        if facts_path.is_file():
            try:
                facts = json.loads(facts_path.read_text(encoding="utf-8"))
                verdict = facts.get("overall", "unknown")
                bad = [section["title"] for section in facts.get("sections", [])
                       if section.get("verdict") in ("bad", "attention")]
                summary = ", ".join(bad) if bad else "nothing to report"
            except Exception:
                pass
        if not csv_path:
            summary = (summary + "; no memory samples").lstrip("; ")
        print("   {} done ({})".format(label, verdict), flush=True)
        return (label, verdict, summary, report_path)

    started = time.time()
    print("Checking {} folder(s), {} at a time ...".format(len(folders), WORKERS))
    with ThreadPoolExecutor(max_workers=WORKERS) as pool:
        results = list(pool.map(check_one, folders))

    results.sort(key=lambda row: (VERDICT_ORDER.get(row[1], 9), row[0]))

    print()
    print("=" * 78)
    print("{:<24} {:<10} {}".format("EXCHANGE", "VERDICT", "SUBJECTS NEEDING A LOOK"))
    print("=" * 78)
    for label, verdict, summary, _ in results:
        print("{:<24} {:<10} {}".format(label, verdict, summary))
    print("=" * 78)
    print()
    print("Reports written to: {}".format(output))
    print("Worst first, so start at the top. Took {:.0f} seconds.".format(time.time() - started))
    return 0


if __name__ == "__main__":
    sys.exit(main())
