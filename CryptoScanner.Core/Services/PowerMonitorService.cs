using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace CryptoScanner.Core.Services;

/// <summary>
/// Cross-platform power status monitor voor Windows, Linux en macOS
/// </summary>
public class PowerMonitorService : IDisposable
{
    private Timer? _pollingTimer;
    private PowerStatus _lastStatus;
    private bool _disposed;

    public event EventHandler<PowerStatusChangedEventArgs>? PowerStatusChanged;
    public event EventHandler<PowerModeEventArgs>? PowerModeChanged;

    public PowerStatus CurrentStatus => _lastStatus;

    public PowerMonitorService()
    {
        _lastStatus = GetCurrentPowerStatus();

        if (OperatingSystem.IsWindows())
        {
            InitializeWindows();
        }
        else if (OperatingSystem.IsLinux())
        {
            InitializeLinux();
        }
        else if (OperatingSystem.IsMacOS())
        {
            InitializeMacOS();
        }
        else
        {
            // Fallback voor onbekende platforms
            StartPolling(TimeSpan.FromSeconds(60));
        }
    }

    #region Windows Implementation

    [SupportedOSPlatform("windows")]
    private void InitializeWindows()
    {
        try
        {
            Microsoft.Win32.SystemEvents.PowerModeChanged += OnWindowsPowerModeChanged;

            // Aanvullende polling voor battery percentage (SystemEvents geeft dit niet)
            StartPolling(TimeSpan.FromMinutes(1));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Windows power events niet beschikbaar: {ex.Message}");
            // Fallback naar polling
            StartPolling(TimeSpan.FromSeconds(30));
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnWindowsPowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e)
    {
        var mode = e.Mode switch
        {
            Microsoft.Win32.PowerModes.Resume => PowerMode.Resume,
            Microsoft.Win32.PowerModes.Suspend => PowerMode.Suspend,
            Microsoft.Win32.PowerModes.StatusChange => PowerMode.StatusChange,
            _ => PowerMode.Unknown
        };

        PowerModeChanged?.Invoke(this, new PowerModeEventArgs(mode));

        // Check status change
        CheckForStatusChange();
    }

    #endregion

    #region Linux Implementation

    private void InitializeLinux()
    {
        // Linux heeft geen event system voor power, dus we pollen
        StartPolling(TimeSpan.FromSeconds(10));
    }

    private PowerStatus GetLinuxPowerStatus()
    {
        var status = new PowerStatus();

        try
        {
            // Check AC power status
            var acPaths = new[]
            {
                "/sys/class/power_supply/AC/online",
                "/sys/class/power_supply/AC0/online",
                "/sys/class/power_supply/ACAD/online"
            };

            foreach (var path in acPaths)
            {
                if (File.Exists(path))
                {
                    var content = File.ReadAllText(path).Trim();
                    status.IsPluggedIn = content == "1";
                    break;
                }
            }

            // Check battery status en percentage
            var batteryPaths = new[]
            {
                "/sys/class/power_supply/BAT0",
                "/sys/class/power_supply/BAT1",
                "/sys/class/power_supply/battery"
            };

            foreach (var basePath in batteryPaths)
            {
                if (Directory.Exists(basePath))
                {
                    // Battery percentage
                    var capacityFile = Path.Combine(basePath, "capacity");
                    if (File.Exists(capacityFile))
                    {
                        var capacityText = File.ReadAllText(capacityFile).Trim();
                        if (int.TryParse(capacityText, out int capacity))
                        {
                            status.BatteryPercentage = capacity;
                        }
                    }

                    // Battery status (Charging, Discharging, Full, etc.)
                    var statusFile = Path.Combine(basePath, "status");
                    if (File.Exists(statusFile))
                    {
                        var batteryStatus = File.ReadAllText(statusFile).Trim();
                        status.IsCharging = batteryStatus.Equals("Charging", StringComparison.OrdinalIgnoreCase);
                        status.BatteryStatus = batteryStatus;
                    }

                    // We hebben battery info gevonden
                    status.HasBattery = true;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Linux power status error: {ex.Message}");
        }

        return status;
    }

    #endregion

    #region macOS Implementation

    private void InitializeMacOS()
    {
        // macOS heeft ook geen simpel event system, we pollen
        StartPolling(TimeSpan.FromSeconds(10));
    }

    private PowerStatus GetMacOSPowerStatus()
    {
        var status = new PowerStatus();

        try
        {
            // Gebruik pmset -g batt om battery info te krijgen
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "pmset",
                    Arguments = "-g batt",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Output format:
            // Now drawing from 'AC Power'
            // -InternalBattery-0 (id=123456)	95%; charging; 1:23 remaining present: true

            status.IsPluggedIn = output.Contains("'AC Power'") || output.Contains("AC attached");
            status.IsCharging = output.Contains("charging");

            // Parse battery percentage
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("InternalBattery") || line.Contains("Battery"))
                {
                    status.HasBattery = true;

                    // Percentage is tussen het begin en %;
                    var percentIndex = line.IndexOf('%');
                    if (percentIndex > 0)
                    {
                        // Ga terug vanaf % om het getal te vinden
                        var numStart = percentIndex - 1;
                        while (numStart > 0 && (char.IsDigit(line[numStart]) || line[numStart] == ' ' || line[numStart] == '\t'))
                        {
                            numStart--;
                        }

                        var percentStr = line.Substring(numStart + 1, percentIndex - numStart - 1).Trim();
                        if (int.TryParse(percentStr, out int percentage))
                        {
                            status.BatteryPercentage = percentage;
                        }
                    }

                    // Status bepalen
                    if (line.Contains("charged"))
                        status.BatteryStatus = "Full";
                    else if (line.Contains("charging"))
                        status.BatteryStatus = "Charging";
                    else if (line.Contains("discharging"))
                        status.BatteryStatus = "Discharging";
                    else
                        status.BatteryStatus = "Unknown";

                    break;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"macOS power status error: {ex.Message}");
        }

        return status;
    }

    #endregion

    #region Common Polling Logic

    private void StartPolling(TimeSpan interval)
    {
        _pollingTimer = new Timer(OnPollingTick, null, TimeSpan.Zero, interval);
    }

    private void OnPollingTick(object? state)
    {
        if (_disposed) return;

        CheckForStatusChange();
    }

    private void CheckForStatusChange()
    {
        try
        {
            var newStatus = GetCurrentPowerStatus();

            // Check of er iets veranderd is
            if (HasStatusChanged(_lastStatus, newStatus))
            {
                var oldStatus = _lastStatus;
                _lastStatus = newStatus;

                PowerStatusChanged?.Invoke(this, new PowerStatusChangedEventArgs(oldStatus, newStatus));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Power status check error: {ex.Message}");
        }
    }

    private bool HasStatusChanged(PowerStatus old, PowerStatus newStatus)
    {
        return old.IsPluggedIn != newStatus.IsPluggedIn ||
               old.IsCharging != newStatus.IsCharging ||
               Math.Abs(old.BatteryPercentage - newStatus.BatteryPercentage) >= 5; // 5% verschil
    }

    #endregion

    #region Get Current Status

    private PowerStatus GetCurrentPowerStatus()
    {
        if (OperatingSystem.IsWindows())
        {
            return GetWindowsPowerStatus();
        }
        else if (OperatingSystem.IsLinux())
        {
            return GetLinuxPowerStatus();
        }
        else if (OperatingSystem.IsMacOS())
        {
            return GetMacOSPowerStatus();
        }
        else
        {
            return new PowerStatus(); // Unknown platform
        }
    }

    [SupportedOSPlatform("windows")]
    private PowerStatus GetWindowsPowerStatus()
    {
        var status = new PowerStatus();

        try
        {
            // P/Invoke naar GetSystemPowerStatus
            if (GetSystemPowerStatus(out SYSTEM_POWER_STATUS sps))
            {
                status.IsPluggedIn = sps.ACLineStatus == 1;
                status.IsCharging = sps.BatteryFlag == 8; // Charging flag
                status.BatteryPercentage = sps.BatteryLifePercent == 255 ? -1 : sps.BatteryLifePercent;
                status.HasBattery = sps.BatteryFlag != 128; // 128 = No system battery

                // Bepaal battery status
                if (sps.BatteryFlag == 128)
                    status.BatteryStatus = "NoBattery";
                else if (sps.BatteryFlag == 8)
                    status.BatteryStatus = "Charging";
                else if (sps.BatteryFlag == 4)
                    status.BatteryStatus = "Critical";
                else if (sps.BatteryFlag == 2)
                    status.BatteryStatus = "Low";
                else if (sps.BatteryFlag == 1)
                    status.BatteryStatus = "High";
                else
                    status.BatteryStatus = "Unknown";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Windows power status error: {ex.Message}");
        }

        return status;
    }

    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll")]
    private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

    [SupportedOSPlatform("windows")]
    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;          // 0 = Offline, 1 = Online, 255 = Unknown
        public byte BatteryFlag;           // Battery status flags
        public byte BatteryLifePercent;    // 0-100, 255 = Unknown
        public byte SystemStatusFlag;      // Reserved
        public int BatteryLifeTime;        // Seconds remaining (-1 = Unknown)
        public int BatteryFullLifeTime;    // Seconds when full (-1 = Unknown)
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        if (OperatingSystem.IsWindows())
        {
            UnregisterWindowsEvents();
        }

        _pollingTimer?.Dispose();
        _pollingTimer = null;

        GC.SuppressFinalize(this);
    }

    [SupportedOSPlatform("windows")]
    private void UnregisterWindowsEvents()
    {
        try
        {
            Microsoft.Win32.SystemEvents.PowerModeChanged -= OnWindowsPowerModeChanged;
        }
        catch
        {
            // Ignore errors during cleanup
        }
    }

    #endregion
}

#region Event Args

public class PowerStatusChangedEventArgs : EventArgs
{
    public PowerStatus OldStatus { get; }
    public PowerStatus NewStatus { get; }

    public PowerStatusChangedEventArgs(PowerStatus oldStatus, PowerStatus newStatus)
    {
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }
}

public class PowerModeEventArgs : EventArgs
{
    public PowerMode Mode { get; }

    public PowerModeEventArgs(PowerMode mode)
    {
        Mode = mode;
    }
}

#endregion

#region Data Classes

public class PowerStatus
{
    public bool IsPluggedIn { get; set; }
    public bool IsCharging { get; set; }
    public int BatteryPercentage { get; set; } = -1;  // -1 = Unknown
    public string BatteryStatus { get; set; } = "Unknown";
    public bool HasBattery { get; set; }

    public override string ToString()
    {
        if (!HasBattery)
            return "No battery detected";

        var plugged = IsPluggedIn ? "Plugged in" : "On battery";
        var charging = IsCharging ? ", Charging" : "";
        var percentage = BatteryPercentage >= 0 ? $", {BatteryPercentage}%" : "";

        return $"{plugged}{charging}{percentage} ({BatteryStatus})";
    }
}

public enum PowerMode
{
    Unknown,
    Resume,      // System komt uit sleep/hibernate
    Suspend,     // System gaat naar sleep/hibernate
    StatusChange // Power status veranderd (AC/Battery switch)
}

#endregion
