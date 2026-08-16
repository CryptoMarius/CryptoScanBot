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

    With six scanners running, point -Out at a FOLDER and every matching process gets its own csv,
    named after the data folder from its command line - which is exactly the name the report needs.

.PARAMETER Name
    Process name without extension (default CryptoScanBot). Every running process with this name is
    sampled unless -Id narrows it down.

.PARAMETER Id
    One or more process ids. Takes precedence over -Name.

.PARAMETER Out
    Csv file, or a folder when more than one process is sampled. Existing files are appended to, so
    restarting this script does not lose the earlier samples.

.PARAMETER IntervalSeconds
    Seconds between samples. 300 (five minutes) is plenty for a night; sampling faster does not make
    the slope better, only the file bigger.

.EXAMPLE
    .\sample-process.ps1 -Out "D:\runs"

    Samples every running CryptoScanBot into D:\runs\<data folder>-memory.csv.

.EXAMPLE
    .\sample-process.ps1 -Id 12345 -Out "D:\runs\kraken-memory.csv" -IntervalSeconds 60
#>
[CmdletBinding()]
param(
    [string] $Name = 'CryptoScanBot',
    [int[]] $Id,
    [Parameter(Mandatory = $true)][string] $Out,
    [int] $IntervalSeconds = 300
)

$ErrorActionPreference = 'Stop'

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

function Resolve-Targets {
    param([string] $Name, [int[]] $Id)

    if ($Id) {
        return @($Id | ForEach-Object { Get-Process -Id $_ })
    }
    # Not $matches - that is an automatic variable that the regex operators overwrite.
    $found = @(Get-Process -Name $Name -ErrorAction SilentlyContinue)
    if ($found.Count -eq 0) {
        throw "No running process named '$Name'. Start the scanner first, or pass -Id."
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

    if (-not (Test-Path -LiteralPath $path)) {
        # No byte order mark: the reader on the other side is Python, not PowerShell.
        [System.IO.File]::WriteAllText($path, "timestamp;pid;workingSetMb;privateMb;threads;handles;cpuSeconds`r`n")
    }

    $sampled += [pscustomobject]@{ Process = $process; Path = $path }
    Write-Host ("Sampling {0} ({1}) -> {2}" -f $process.Id, $process.ProcessName, $path)
}

Write-Host "Interval: $IntervalSeconds seconds. Press Ctrl-C to stop; the script ends when the last process exits."

while ($sampled.Count -gt 0) {
    $stamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $alive = @()

    foreach ($item in $sampled) {
        try {
            $process = $item.Process
            $process.Refresh()
            if ($process.HasExited) {
                Write-Host "Process $($process.Id) has exited."
                continue
            }

            $row = '{0};{1};{2};{3};{4};{5};{6}' -f `
                $stamp,
                $process.Id,
                [math]::Round($process.WorkingSet64 / 1MB, 1),
                [math]::Round($process.PrivateMemorySize64 / 1MB, 1),
                $process.Threads.Count,
                $process.HandleCount,
                [math]::Round($process.TotalProcessorTime.TotalSeconds, 1)

            Add-Content -LiteralPath $item.Path -Value $row
            $alive += $item
        }
        catch {
            Write-Warning "Sample for $($item.Process.Id) failed: $($_.Exception.Message)"
        }
    }

    $sampled = $alive
    if ($sampled.Count -eq 0) {
        break
    }
    Start-Sleep -Seconds $IntervalSeconds
}

Write-Host "Done."
