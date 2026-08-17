<#
.SYNOPSIS
    Samples the memory and handle usage of running scanner processes into a csv per process.

.DESCRIPTION
    An overnight run only shows a leak as a slope: one measurement says nothing, a line over eight
    hours says everything. This script writes one row per interval so check_exchange.py can fit that
    slope afterwards (--memory-csv).

    Nothing is attached to the process and no dump is taken, so the scanner is not disturbed. The
    working set is what the machine feels; the managed versus native split needs the "Dump memory
    info" action inside the scanner itself (its output is picked up by the report as well).

    The Photino user interface hosts its window in WebView2, which runs in SEPARATE
    msedgewebview2.exe processes (a browser process plus a renderer and a gpu process). Their memory
    is not part of the working set of CryptoScanBot.Photino.exe, so sampling only the host process
    understates what the machine is really using and hides a leak that sits in the renderer. Every
    round therefore walks the process tree and adds what those children use in their own columns,
    plus a total. For the Avalonia scanner there are no such children and the columns stay zero.

    With six scanners running, point -Out at a FOLDER and every matching process gets its own csv,
    named after the data folder from its command line - which is exactly the name the report needs.

.PARAMETER Name
    One or more process names without extension. The default covers both user interfaces: the
    Avalonia scanner is CryptoScanBot.exe and the Photino one is CryptoScanBot.Photino.exe, which
    are different process names - "CryptoScanBot" on its own does NOT match the Photino process.
    Wildcards are allowed, but "CryptoScanBot*" would also drag the emulator in, which is not a
    scanner and does not belong in an exchange report. Every running process with one of these names
    is sampled unless -Id narrows it down.

.PARAMETER Id
    One or more process ids. Takes precedence over -Name.

.PARAMETER Out
    Csv file, or a folder when more than one process is sampled. Existing files are appended to, so
    restarting this script does not lose the earlier samples. They do not have to be cleared between
    runs either: check_exchange.py cuts the samples to the run's window and to its process id.

.PARAMETER IntervalSeconds
    Seconds between samples. 300 (five minutes) is plenty for a night; sampling faster does not make
    the slope better, only the file bigger.

.EXAMPLE
    .\sample-process.ps1 -Out "D:\runs"

    Samples every running scanner, whichever user interface it runs, into
    D:\runs\<data folder>-memory.csv.

.EXAMPLE
    .\sample-process.ps1 -Id 12345 -Out "D:\runs\kraken-memory.csv" -IntervalSeconds 60
#>
[CmdletBinding()]
param(
    [string[]] $Name = @('CryptoScanBot', 'CryptoScanBot.Photino'),
    [int[]] $Id,
    [Parameter(Mandatory = $true)][string] $Out,
    [int] $IntervalSeconds = 300
)

$ErrorActionPreference = 'Stop'

# The csv the report reads. The last four columns were added when the Photino user interface arrived;
# a csv from before that has the legacy header and keeps it, because appending wider rows to it would
# break every earlier sample in the same file.
$SampleHeader = 'timestamp;pid;workingSetMb;privateMb;threads;handles;cpuSeconds;webviewProcesses;webviewWorkingSetMb;webviewPrivateMb;totalWorkingSetMb'
$LegacySampleHeader = 'timestamp;pid;workingSetMb;privateMb;threads;handles;cpuSeconds'

function Get-DataFolderName {
    <#
        The scanner is started with -f "<data folder>", so the command line says which run this
        process belongs to. That name is what ties the csv to the report afterwards; without it
        six files called "CryptoScanBot-memory.csv" would be useless.
    #>
    param([int] $ProcessId)

    $commandLine = ''
    try {
        $commandLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $ProcessId").CommandLine
    }
    catch { }

    if ($commandLine -match '(?:-f|--folder)\s+"([^"]+)"' -or $commandLine -match '(?:-f|--folder)\s+(\S+)') {
        $folder = $Matches[1].TrimEnd('\', '/')
        # Keep the last two path parts: "Data\Kraken\Spot" becomes "Kraken-Spot", which is readable
        # and still unique between the spot and the futures run of the same exchange.
        $parts = @($folder -split '[\\/]' | Where-Object { $_ -and $_ -ne '.' })
        if ($parts.Count -ge 2) {
            $label = ($parts[-2..-1]) -join '-'
        }
        elseif ($parts.Count -eq 1) {
            $label = $parts[0]
        }
        else {
            $label = "pid$ProcessId"
        }
    }
    else {
        $label = "pid$ProcessId"
    }

    # Whatever ends up in a file name has to survive being a file name.
    foreach ($invalid in [System.IO.Path]::GetInvalidFileNameChars()) {
        $label = $label.Replace($invalid, '_')
    }
    return $label
}

function Get-WebViewMap {
    <#
        One CIM query per round instead of one per process: with six scanners and three WebView2
        children each that is the difference between one query and twenty. Returns a hashtable
        parent process id -> the msedgewebview2 processes that hang under it.

        Only msedgewebview2 processes are put in the map, so a recycled parent process id cannot
        drag an unrelated process into somebody's total.
    #>
    $map = @{}
    $rows = @()
    try {
        $rows = @(Get-CimInstance Win32_Process -Filter "Name = 'msedgewebview2.exe'" -ErrorAction SilentlyContinue)
    }
    catch { }

    foreach ($row in $rows) {
        $parent = [int] $row.ParentProcessId
        if (-not $map.ContainsKey($parent)) {
            $map[$parent] = @()
        }
        $map[$parent] += $row
    }
    return $map
}

function Get-WebViewUsage {
    <#
        What the WebView2 processes under this scanner use together. WebView2 puts its browser
        process under the host application and its renderer and gpu processes under THAT one, so the
        tree has to be walked rather than only the direct children.
    #>
    param([int] $ProcessId, [hashtable] $Map)

    $count = 0
    $workingSet = [long] 0
    $private = [long] 0

    $pending = New-Object System.Collections.Generic.Queue[int]
    $pending.Enqueue($ProcessId)
    $seen = @{ $ProcessId = $true }

    while ($pending.Count -gt 0) {
        $current = $pending.Dequeue()
        if (-not $Map.ContainsKey($current)) {
            continue
        }
        foreach ($child in $Map[$current]) {
            $childId = [int] $child.ProcessId
            if ($seen.ContainsKey($childId)) {
                continue
            }
            $seen[$childId] = $true
            $count++
            $workingSet += [long] $child.WorkingSetSize
            # PrivatePageCount is the private commit, the same quantity as PrivateMemorySize64 on
            # the host process, so the two columns can be read side by side.
            $private += [long] $child.PrivatePageCount
            $pending.Enqueue($childId)
        }
    }

    return [pscustomobject]@{
        Count           = $count
        WorkingSetBytes = $workingSet
        PrivateBytes    = $private
    }
}

function Initialize-SampleFile {
    <#
        Creates the csv when it is new and reports whether the WebView2 columns may be written to it.
        An existing file from before those columns existed keeps its own layout: widening the rows
        halfway would leave the reader with a file in two shapes.
    #>
    param([string] $Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        # No byte order mark: the reader on the other side is Python, not PowerShell.
        [System.IO.File]::WriteAllText($Path, "$SampleHeader`r`n")
        return $true
    }

    $firstLine = ''
    try {
        $firstLine = (Get-Content -LiteralPath $Path -TotalCount 1 -ErrorAction Stop)
    }
    catch { }
    $firstLine = "$firstLine".Trim([char]0xFEFF).Trim()

    if ($firstLine -eq $LegacySampleHeader) {
        Write-Host ("  {0} was written before the WebView2 columns existed; keeping the old layout for it." -f (Split-Path -Leaf $Path))
        return $false
    }
    return $true
}

function Resolve-Targets {
    param([string[]] $Name, [int[]] $Id)

    if ($Id) {
        return @($Id | ForEach-Object { Get-Process -Id $_ })
    }
    # Not $matches - that is an automatic variable that the regex operators overwrite.
    # Several names may resolve to the same process when a wildcard is passed, hence the dedupe.
    $found = @(Get-Process -Name $Name -ErrorAction SilentlyContinue |
        Sort-Object -Property Id -Unique)
    if ($found.Count -eq 0) {
        throw "No running process named $($Name -join ' or '). Start the scanner first, or pass -Id."
    }
    return $found
}

$targets = Resolve-Targets -Name $Name -Id $Id

# One process may go to a file; several need a folder to write into.
$outIsFolder = (Test-Path -LiteralPath $Out -PathType Container) -or
               ($targets.Count -gt 1 -and -not $Out.EndsWith('.csv'))
if ($targets.Count -gt 1 -and -not $outIsFolder) {
    throw "$($targets.Count) processes matched but -Out is a single file. Pass a folder instead."
}
if ($outIsFolder -and -not (Test-Path -LiteralPath $Out)) {
    $null = New-Item -ItemType Directory -Path $Out -Force
}

$sampled = @()
foreach ($process in $targets) {
    $path = if ($outIsFolder) {
        Join-Path $Out ("{0}-memory.csv" -f (Get-DataFolderName -ProcessId $process.Id))
    }
    else {
        $Out
    }

    $extended = Initialize-SampleFile -Path $path

    $sampled += [pscustomobject]@{ Process = $process; Path = $path; Extended = $extended }
    Write-Host ("Sampling {0} ({1}) -> {2}" -f $process.Id, $process.ProcessName, $path)
}

Write-Host "Interval: $IntervalSeconds seconds. Press Ctrl-C to stop; the script ends when the last process exits."

while ($true) {
    $stamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $alive = @()

    # Look for new processes every round instead of only at startup. A scanner that is stopped and
    # started again during the night gets a new process id, and sampling the old one just ends -
    # which is how BitMart and Bitvavo ended up with five of their eleven hours measured. The csv is
    # named after the data folder, so the restarted scanner appends to the same file it had before.
    if (-not $Id) {
        $known = @($sampled | ForEach-Object { $_.Process.Id })
        foreach ($process in @(Get-Process -Name $Name -ErrorAction SilentlyContinue)) {
            if ($known -contains $process.Id) {
                continue
            }
            $path = if ($outIsFolder) {
                Join-Path $Out ("{0}-memory.csv" -f (Get-DataFolderName -ProcessId $process.Id))
            }
            else {
                $Out
            }
            $extended = Initialize-SampleFile -Path $path
            $sampled += [pscustomobject]@{ Process = $process; Path = $path; Extended = $extended }
            Write-Host ("Picked up {0} ({1}) -> {2}" -f $process.Id, $process.ProcessName, $path)
        }
    }

    # Built once per round and shared by every process sampled in it.
    $webViewMap = Get-WebViewMap

    foreach ($item in $sampled) {
        try {
            $process = $item.Process
            $process.Refresh()
            if ($process.HasExited) {
                Write-Host "Process $($process.Id) has exited."
                continue
            }

            $webView = Get-WebViewUsage -ProcessId $process.Id -Map $webViewMap
            $hostWorkingSetMb = [math]::Round($process.WorkingSet64 / 1MB, 1)

            $row = '{0};{1};{2};{3};{4};{5};{6}' -f `
                $stamp,
                $process.Id,
                $hostWorkingSetMb,
                [math]::Round($process.PrivateMemorySize64 / 1MB, 1),
                $process.Threads.Count,
                $process.HandleCount,
                [math]::Round($process.TotalProcessorTime.TotalSeconds, 1)

            if ($item.Extended) {
                $row += ';{0};{1};{2};{3}' -f `
                    $webView.Count,
                    [math]::Round($webView.WorkingSetBytes / 1MB, 1),
                    [math]::Round($webView.PrivateBytes / 1MB, 1),
                    [math]::Round(($process.WorkingSet64 + $webView.WorkingSetBytes) / 1MB, 1)
            }

            Add-Content -LiteralPath $item.Path -Value $row
            $alive += $item
        }
        catch {
            Write-Warning "Sample for $($item.Process.Id) failed: $($_.Exception.Message)"
        }
    }

    $sampled = $alive
    # Not "break when empty" any more: a scanner that is restarting has no process for a round or
    # two, and stopping there is exactly the gap this loop is meant to close. With -Id there is
    # nothing to rediscover, so that case still ends when the last process is gone.
    if ($Id -and $sampled.Count -eq 0) {
        break
    }
    Start-Sleep -Seconds $IntervalSeconds
}

Write-Host "Done."
