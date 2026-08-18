# Finds out WHERE the memory of a running scanner goes, by comparing two heap snapshots.
#
# The memory sampling next to this script (sample-process.ps1) says how fast a process grows. It
# cannot say what grows, so a slope of 27 megabyte an hour stays a mystery until something looks
# inside the heap. That is what this does: two snapshots with a wait in between, then a table of the
# types that gained the most bytes.
#
# It reads only; it never writes to the scanner or its data folder. The dumps land in the output
# folder and can be deleted afterwards - they are large (roughly the working set of the process).
#
# Usage:
#   .\heap-diff.ps1 -Exchange "Binance Futures"
#   .\heap-diff.ps1 -ProcessId 12345 -WaitMinutes 45
#
# Needs dotnet-dump (dotnet tool install --global dotnet-dump). Run the window as the same user that
# started the scanner, otherwise attaching to the process is refused.

[CmdletBinding()]
param(
    # Which scanner to look at. Matched against the command line, so "Binance Futures" finds the
    # process started with -e "Binance Futures". Ignored when ProcessId is given.
    [string] $Exchange,

    [int] $ProcessId = 0,

    # How long to wait between the two snapshots. Long enough that the growth is larger than the
    # noise: at 27 megabyte an hour, 45 minutes is about 20 megabyte and that stands out fine.
    [int] $WaitMinutes = 45,

    [string] $OutputFolder = "$env:TEMP\heap-diff",

    # Types gaining less than this are left out of the table; they are the long tail.
    [int] $MinimumGrowthKb = 256
)

$ErrorActionPreference = "Stop"

function Get-ScannerProcessId {
    param([string] $Exchange)

    # Match on the command line, not on the process name: every scanner is CryptoScanBot.exe and
    # there are twenty of them. Get-CimInstance is the only way to read the command line here.
    $candidates = Get-CimInstance Win32_Process -Filter "Name = 'CryptoScanBot.exe'" |
        Where-Object { $_.CommandLine -and $_.CommandLine -like "*$Exchange*" }

    if (-not $candidates) {
        throw "No running CryptoScanBot.exe with '$Exchange' in its command line. Start the scanner first, or pass -ProcessId."
    }
    if ($candidates.Count -gt 1) {
        $list = ($candidates | ForEach-Object { "  $($_.ProcessId)  $($_.CommandLine)" }) -join "`n"
        throw "More than one match, pass -ProcessId for the one you mean:`n$list"
    }
    return [int] $candidates.ProcessId
}

function Get-HeapStatistics {
    param([string] $DumpPath)

    # dumpheap -stat gives one line per type: MT, count, total bytes, name. The header and the
    # totals are filtered out by requiring the byte column to be a number.
    $output = & dotnet-dump analyze $DumpPath -c "dumpheap -stat" -c "exit" 2>&1

    $statistics = @{}
    foreach ($line in $output) {
        $text = [string] $line
        if ($text -match '^\s*[0-9a-fA-F]{8,16}\s+(\d+)\s+(\d+)\s+(.+?)\s*$') {
            $name = $Matches[3].Trim()
            if ($name -eq "Total" -or $name -like "Free*") { continue }
            $statistics[$name] = [PSCustomObject]@{
                Count = [long] $Matches[1]
                Bytes = [long] $Matches[2]
            }
        }
    }
    if ($statistics.Count -eq 0) {
        throw "dotnet-dump analyze returned no type statistics for $DumpPath. Is dotnet-dump installed and is the dump complete?"
    }
    return $statistics
}

if ($ProcessId -eq 0) {
    if (-not $Exchange) { throw "Pass -Exchange or -ProcessId." }
    $ProcessId = Get-ScannerProcessId -Exchange $Exchange
}

$commandLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $ProcessId").CommandLine
Write-Host "Process $ProcessId" -ForegroundColor Cyan
Write-Host "  $commandLine"
Write-Host ""

New-Item -ItemType Directory -Force -Path $OutputFolder | Out-Null
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$firstDump  = Join-Path $OutputFolder "heap-$ProcessId-$stamp-1.dmp"
$secondDump = Join-Path $OutputFolder "heap-$ProcessId-$stamp-2.dmp"

Write-Host "Snapshot 1 ..." -ForegroundColor Cyan
& dotnet-dump collect --process-id $ProcessId --output $firstDump --type Heap
if ($LASTEXITCODE -ne 0) { throw "dotnet-dump collect failed for the first snapshot." }

Write-Host "Waiting $WaitMinutes minutes. Leave the scanner running." -ForegroundColor Cyan
Start-Sleep -Seconds ($WaitMinutes * 60)

Write-Host "Snapshot 2 ..." -ForegroundColor Cyan
& dotnet-dump collect --process-id $ProcessId --output $secondDump --type Heap
if ($LASTEXITCODE -ne 0) { throw "dotnet-dump collect failed for the second snapshot." }

Write-Host "Reading the heaps (this takes a minute per dump) ..." -ForegroundColor Cyan
$before = Get-HeapStatistics -DumpPath $firstDump
$after  = Get-HeapStatistics -DumpPath $secondDump

$rows = foreach ($name in $after.Keys) {
    $bytesAfter  = $after[$name].Bytes
    $countAfter  = $after[$name].Count
    $bytesBefore = 0
    $countBefore = 0
    if ($before.ContainsKey($name)) {
        $bytesBefore = $before[$name].Bytes
        $countBefore = $before[$name].Count
    }
    [PSCustomObject]@{
        Type         = $name
        GrowthKb     = [math]::Round(($bytesAfter - $bytesBefore) / 1KB, 0)
        GrowthCount  = $countAfter - $countBefore
        TotalKb      = [math]::Round($bytesAfter / 1KB, 0)
    }
}

$interesting = $rows | Where-Object { $_.GrowthKb -ge $MinimumGrowthKb } | Sort-Object GrowthKb -Descending

Write-Host ""
Write-Host "Growth over $WaitMinutes minutes, largest first:" -ForegroundColor Green
$interesting | Select-Object -First 30 | Format-Table -AutoSize

$totalGrowthKb = ($rows | Measure-Object GrowthKb -Sum).Sum
Write-Host ("Total heap growth: {0:N0} KB over {1} minutes ({2:N1} MB per hour)" -f `
    $totalGrowthKb, $WaitMinutes, ($totalGrowthKb / 1KB) * (60 / $WaitMinutes)) -ForegroundColor Green
Write-Host ""
Write-Host "Dumps: $firstDump"
Write-Host "       $secondDump"
Write-Host "Delete them when you are done, they are large."
