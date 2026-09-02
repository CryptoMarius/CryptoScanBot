# Finds out WHERE the memory of a running scanner goes, by comparing two heap snapshots.
#
# The memory sampling next to this script (sample-process.ps1) says HOW FAST a process grows. It
# cannot say WHAT grows, so a slope of 27 megabyte an hour stays a mystery. This script answers that:
# one snapshot at the start of the night, one at the end, and a table of the types that gained the
# most bytes in between.
#
# Two modes, because the two moments are hours apart and each belongs to a different .cmd file:
#
#   -Mode Snapshot   collect one heap dump and remember it   (called from "3 Start all scanners.cmd")
#   -Mode Compare    collect the second and print the diff   (called from "4 Stop all scanners.cmd")
#
# Why over the WHOLE night and not over 45 minutes: the slope we are chasing is 20 to 27 megabyte an
# hour. Over 45 minutes that is 20 megabyte, which drowns in the noise of a garbage collection that
# happens to fall inside the window. Over eleven hours it is a quarter of a gigabyte and whatever
# caused it stands out on the first line of the table.
#
# Why ONE exchange and not all of them: a heap dump is roughly the working set of the process, and
# these run to 1.6 gigabyte. Twenty exchanges times two snapshots is sixty gigabyte of disk for a
# question you are asking about one of them. Set EXCHANGE in the .cmd file to whichever one you are
# investigating.
#
# It reads only; it never writes to the scanner or its data folder. Needs dotnet-dump
# (dotnet tool install --global dotnet-dump).

[CmdletBinding()]
param(
    [ValidateSet("Snapshot", "Compare")]
    [string] $Mode = "Snapshot",

    # Which scanner to look at. Matched against the command line, so "Okx Perpetual" finds the
    # process started with -e "Okx Perpetual". Ignored when ProcessId is given.
    [string] $Exchange,

    [int] $ProcessId = 0,

    [string] $OutputFolder = "$env:TEMP\heap-diff",

    # Minutes to let the scanner warm up before the first snapshot. The first half hour is spent
    # filling the candle history, and counting that as growth would point at the candles every time.
    [int] $WarmupMinutes = 30,

    # Types gaining less than this are left out of the table; they are the long tail.
    [int] $MinimumGrowthKb = 256
)

$ErrorActionPreference = "Stop"

function Get-ScannerProcessId {
    param([string] $Exchange)

    # Match on the command line, not on the process name: every scanner has the same executable name
    # and there are twenty of them. Get-CimInstance is the only way to read the command line here.
    #
    # BOTH user interfaces, the same way sample-process.ps1 does it: the Avalonia scanner is
    # CryptoScanBot.exe and the Photino one is CryptoScanBot.Photino.exe, and those are different
    # process names - a filter on CryptoScanBot.exe alone finds nothing at all on a Photino night.
    # That is exactly what happened up to 02-09-2026: the snapshot was aimed at a process name that
    # was not running, so no comparison ever came out of it.
    #
    # The dump covers the MANAGED heap of the host process. Under Photino the window itself lives in
    # separate msedgewebview2.exe processes, and their memory is not in here - see sample-process.ps1,
    # which measures those separately. So this answers "what does the scanner hold on to", not "why
    # is the total working set of this exchange what it is".
    $candidates = @(Get-CimInstance Win32_Process `
            -Filter "Name = 'CryptoScanBot.exe' OR Name = 'CryptoScanBot.Photino.exe'" |
        Where-Object { $_.CommandLine -and $_.CommandLine -like "*$Exchange*" })

    if ($candidates.Count -eq 0) {
        throw "No running CryptoScanBot.exe or CryptoScanBot.Photino.exe with '$Exchange' in its command line."
    }
    if ($candidates.Count -gt 1) {
        $list = ($candidates | ForEach-Object { "  $($_.ProcessId)  $($_.CommandLine)" }) -join "`n"
        throw "More than one match, pass -ProcessId for the one you mean:`n$list"
    }
    return [int] $candidates[0].ProcessId
}

function Get-HeapStatistics {
    param([string] $DumpPath)

    # dumpheap -stat gives one line per type: MT, count, total bytes, name. The header and the
    # totals are filtered out by requiring the byte column to be a number.
    #
    # THOUSAND SEPARATORS ALLOWED in both number columns. dotnet-dump formats anything over 999 with
    # them ("527,942 38,011,824 SortedSet<...>+Node"), so a pattern of bare digits matches only the
    # types below a thousand bytes - the long tail - and silently drops every type the comparison is
    # actually about. It still matched enough lines to get past the guard below, so the script
    # reported a diff that looked complete and was missing its whole top end. Found on 02-09-2026 by
    # reading an existing dump by hand: the largest type in it held 38 MB and was nowhere in the
    # parsed output.
    $output = & dotnet-dump analyze $DumpPath -c "dumpheap -stat" -c "exit" 2>&1

    # KEYED ON THE METHOD TABLE, not on the type name. The name is what you want to read, but it is
    # not stable between two dumps of the same process: the second one regularly resolves fewer of
    # them and falls back to "<unknown_type_7ffb...>" or to an unspecialised "SortedSet<T1>". Diffing
    # on the name then reports the same type as a whole block gone and an equally large block added
    # - measured on 02-09-2026, where the candle storage showed up as -90,8 MB and +92,1 MB in one
    # table while it had actually grown by 1,3 MB. The method table is the same number in both dumps
    # (same process, same modules): 4551 of 4551 types matched on it.
    $statistics = @{}
    foreach ($line in $output) {
        $text = [string] $line
        if ($text -match '^\s*([0-9a-fA-F]{8,16})\s+([\d,]+)\s+([\d,]+)\s+(.+?)\s*$') {
            $name = $Matches[4].Trim()
            if ($name -eq "Total" -or $name -like "Free*") { continue }
            $statistics[$Matches[1].ToLower()] = [PSCustomObject]@{
                Count = [long] ($Matches[2] -replace ',', '')
                Bytes = [long] ($Matches[3] -replace ',', '')
                Name  = $name
            }
        }
    }
    if ($statistics.Count -eq 0) {
        throw "dotnet-dump analyze returned no type statistics for $DumpPath."
    }
    return $statistics
}

New-Item -ItemType Directory -Force -Path $OutputFolder | Out-Null
$firstDump = Join-Path $OutputFolder "heap-first.dmp"
$secondDump = Join-Path $OutputFolder "heap-second.dmp"

# ------------------------------------------------------------------------------------------------
if ($Mode -eq "Snapshot") {
    if ($WarmupMinutes -gt 0) {
        Write-Host "Waiting $WarmupMinutes minutes for the scanner to fill its candle history ..." -ForegroundColor Cyan
        Start-Sleep -Seconds ($WarmupMinutes * 60)
    }

    if ($ProcessId -eq 0) {
        if (-not $Exchange) { throw "Pass -Exchange or -ProcessId." }
        $ProcessId = Get-ScannerProcessId -Exchange $Exchange
    }

    # An old pair would silently be compared against tonight's second snapshot, which produces a
    # diff over the wrong period. Start clean.
    Remove-Item $firstDump, $secondDump -ErrorAction SilentlyContinue

    Write-Host "First snapshot of process $ProcessId ..." -ForegroundColor Cyan
    & dotnet-dump collect --process-id $ProcessId --output $firstDump --type Heap
    if ($LASTEXITCODE -ne 0) { throw "dotnet-dump collect failed." }

    Write-Host ""
    Write-Host "Done. The stop script takes the second one and prints the difference." -ForegroundColor Green
    exit 0
}

# ------------------------------------------------------------------------------------------------
if (-not (Test-Path $firstDump)) {
    Write-Host "No first snapshot in $OutputFolder, so there is nothing to compare." -ForegroundColor Yellow
    Write-Host "The start script takes that one; this run is skipped." -ForegroundColor Yellow
    exit 0
}

if ($ProcessId -eq 0) {
    if (-not $Exchange) { throw "Pass -Exchange or -ProcessId." }
    $ProcessId = Get-ScannerProcessId -Exchange $Exchange
}

Write-Host "Second snapshot of process $ProcessId ..." -ForegroundColor Cyan
& dotnet-dump collect --process-id $ProcessId --output $secondDump --type Heap
if ($LASTEXITCODE -ne 0) { throw "dotnet-dump collect failed." }

$hours = [math]::Round(((Get-Item $secondDump).LastWriteTime - (Get-Item $firstDump).LastWriteTime).TotalHours, 1)
Write-Host "Reading both heaps (a minute per dump) ..." -ForegroundColor Cyan
$before = Get-HeapStatistics -DumpPath $firstDump
$after = Get-HeapStatistics -DumpPath $secondDump

$rows = foreach ($key in $after.Keys) {
    $bytesBefore = 0
    $countBefore = 0
    # The readable name, taken from whichever dump managed to resolve it - see Get-HeapStatistics.
    $typeName = $after[$key].Name
    if ($before.ContainsKey($key)) {
        $bytesBefore = $before[$key].Bytes
        $countBefore = $before[$key].Count
        if ($typeName -like "<unknown_type_*" -and $before[$key].Name -notlike "<unknown_type_*") {
            $typeName = $before[$key].Name
        }
    }
    [PSCustomObject]@{
        Type        = $typeName
        GrowthKb    = [math]::Round(($after[$key].Bytes - $bytesBefore) / 1KB, 0)
        GrowthCount = $after[$key].Count - $countBefore
        TotalKb     = [math]::Round($after[$key].Bytes / 1KB, 0)
    }
}

$interesting = $rows | Where-Object { $_.GrowthKb -ge $MinimumGrowthKb } | Sort-Object GrowthKb -Descending

Write-Host ""
Write-Host "Growth over $hours hours, largest first:" -ForegroundColor Green
$interesting | Select-Object -First 30 | Format-Table -AutoSize

$totalGrowthKb = ($rows | Measure-Object GrowthKb -Sum).Sum
if ($hours -gt 0) {
    Write-Host ("Total heap growth: {0:N0} KB over {1} hours ({2:N1} MB per hour)" -f `
        $totalGrowthKb, $hours, (($totalGrowthKb / 1KB) / $hours)) -ForegroundColor Green
}
Write-Host ""
Write-Host "Dumps: $firstDump"
Write-Host "       $secondDump"
Write-Host "They are large; delete them once you have read the table."
