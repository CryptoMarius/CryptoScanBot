#if JustAnExample

using CryptoScanner.Core;
using System;
using System.Threading.Tasks;

namespace CryptoScanner.Examples;

/// <summary>
/// Voorbeelden van hoe je PowerMonitor gebruikt in je applicatie
/// </summary>
public class PowerMonitorUsageExamples
{
    private PowerMonitor? _powerMonitor;

    #region Voorbeeld 1: Basis gebruik

    public void BasicUsage()
    {
        // Maak een PowerMonitor instance
        _powerMonitor = new PowerMonitor();

        // Subscribe op events
        _powerMonitor.PowerStatusChanged += OnPowerStatusChanged;
        _powerMonitor.PowerModeChanged += OnPowerModeChanged;

        // Check huidige status
        var status = _powerMonitor.CurrentStatus;
        Console.WriteLine($"Current power status: {status}");
    }

    private void OnPowerStatusChanged(object? sender, PowerStatusChangedEventArgs e)
    {
        Console.WriteLine($"Power status changed:");
        Console.WriteLine($"  Old: {e.OldStatus}");
        Console.WriteLine($"  New: {e.NewStatus}");

        // Voorbeeld acties:
        if (!e.NewStatus.IsPluggedIn && e.OldStatus.IsPluggedIn)
        {
            Console.WriteLine("⚠️ Switched to battery power!");
        }

        if (e.NewStatus.BatteryPercentage < 20 && !e.NewStatus.IsCharging)
        {
            Console.WriteLine("⚠️ Low battery warning!");
        }
    }

    private void OnPowerModeChanged(object? sender, PowerModeEventArgs e)
    {
        Console.WriteLine($"Power mode changed: {e.Mode}");

        switch (e.Mode)
        {
            case PowerMode.Suspend:
                Console.WriteLine("💤 System is going to sleep");
                break;

            case PowerMode.Resume:
                Console.WriteLine("☀️ System woke up from sleep");
                break;

            case PowerMode.StatusChange:
                Console.WriteLine("🔌 Power status changed (AC/Battery)");
                break;
        }
    }

    #endregion

    #region Voorbeeld 2: Gebruik in Crypto Scanner (disconnect op sleep)

    public class CryptoScannerWithPowerManagement
    {
        private PowerMonitor? _powerMonitor;
        private bool _wasConnectedBeforeSleep;

        public void Initialize()
        {
            _powerMonitor = new PowerMonitor();
            _powerMonitor.PowerModeChanged += OnPowerModeChanged;
            _powerMonitor.PowerStatusChanged += OnPowerStatusChanged;
        }

        private async void OnPowerModeChanged(object? sender, PowerModeEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerMode.Suspend:
                    // Systeem gaat slapen - disconnect netjes
                    _wasConnectedBeforeSleep = IsConnected();
                    if (_wasConnectedBeforeSleep)
                    {
                        Console.WriteLine("System going to sleep - disconnecting...");
                        await DisconnectAsync();
                    }
                    break;

                case PowerMode.Resume:
                    // Systeem komt uit sleep - reconnect
                    if (_wasConnectedBeforeSleep)
                    {
                        Console.WriteLine("System resumed - reconnecting...");
                        await Task.Delay(2000); // Wacht even voor netwerk
                        await ReconnectAsync();
                    }
                    break;
            }
        }

        private void OnPowerStatusChanged(object? sender, PowerStatusChangedEventArgs e)
        {
            // Optioneel: Verlaag update frequentie op batterij
            if (!e.NewStatus.IsPluggedIn && e.OldStatus.IsPluggedIn)
            {
                Console.WriteLine("Switched to battery - reducing update frequency");
                SetUpdateInterval(TimeSpan.FromSeconds(5)); // Langzamer
            }
            else if (e.NewStatus.IsPluggedIn && !e.OldStatus.IsPluggedIn)
            {
                Console.WriteLine("Plugged in - restoring update frequency");
                SetUpdateInterval(TimeSpan.FromSeconds(1)); // Normaal
            }

            // Waarschuwing bij lage batterij
            if (e.NewStatus.BatteryPercentage < 15 && !e.NewStatus.IsCharging)
            {
                ShowLowBatteryWarning();
            }
        }

        private bool IsConnected() => true; // Jouw logica
        private Task DisconnectAsync() => Task.CompletedTask; // Jouw logica
        private Task ReconnectAsync() => Task.CompletedTask; // Jouw logica
        private void SetUpdateInterval(TimeSpan interval) { } // Jouw logica
        private void ShowLowBatteryWarning() { } // Jouw logica

        public void Cleanup()
        {
            _powerMonitor?.Dispose();
        }
    }

    #endregion

    #region Voorbeeld 3: Gebruik in Avalonia Window

    public class MainWindowWithPowerMonitoring
    {
        private PowerMonitor? _powerMonitor;

        public void InitializeWindow()
        {
            _powerMonitor = new PowerMonitor();
            _powerMonitor.PowerStatusChanged += OnPowerStatusChanged;

            // Toon huidige status in UI
            UpdatePowerStatusUI(_powerMonitor.CurrentStatus);
        }

        private void OnPowerStatusChanged(object? sender, PowerStatusChangedEventArgs e)
        {
            // Update UI op UI thread (in Avalonia gebruik Dispatcher)
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdatePowerStatusUI(e.NewStatus);
            });
        }

        private void UpdatePowerStatusUI(PowerStatus status)
        {
            // Voorbeeld: Update statusbar
            var statusText = status.ToString();
            var batteryIcon = GetBatteryIcon(status);
            
            Console.WriteLine($"UI Update: {batteryIcon} {statusText}");
        }

        private string GetBatteryIcon(PowerStatus status)
        {
            if (!status.HasBattery)
                return "🔌";

            if (status.IsCharging)
                return "🔋⚡";

            return status.BatteryPercentage switch
            {
                >= 80 => "🔋",
                >= 50 => "🔋",
                >= 20 => "🪫",
                _ => "🪫⚠️"
            };
        }

        public void OnWindowClosing()
        {
            _powerMonitor?.Dispose();
        }
    }

    #endregion

    #region Voorbeeld 4: Logging en Monitoring

    public class PowerMonitorWithLogging
    {
        private PowerMonitor? _powerMonitor;

        public void Start()
        {
            _powerMonitor = new PowerMonitor();
            _powerMonitor.PowerStatusChanged += LogPowerStatusChange;
            _powerMonitor.PowerModeChanged += LogPowerModeChange;

            // Log initiële status
            LogCurrentStatus();
        }

        private void LogCurrentStatus()
        {
            var status = _powerMonitor!.CurrentStatus;
            
            Console.WriteLine("=== Power Status ===");
            Console.WriteLine($"Has Battery: {status.HasBattery}");
            Console.WriteLine($"Plugged In: {status.IsPluggedIn}");
            Console.WriteLine($"Charging: {status.IsCharging}");
            Console.WriteLine($"Battery %: {status.BatteryPercentage}");
            Console.WriteLine($"Status: {status.BatteryStatus}");
            Console.WriteLine($"Platform: {GetPlatformName()}");
            Console.WriteLine("===================");
        }

        private void LogPowerStatusChange(object? sender, PowerStatusChangedEventArgs e)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            Console.WriteLine($"[{timestamp}] Power Status Changed");
            
            if (e.OldStatus.IsPluggedIn != e.NewStatus.IsPluggedIn)
            {
                var change = e.NewStatus.IsPluggedIn ? "AC Power" : "Battery";
                Console.WriteLine($"  → Switched to: {change}");
            }

            if (e.OldStatus.BatteryPercentage != e.NewStatus.BatteryPercentage)
            {
                Console.WriteLine($"  → Battery: {e.OldStatus.BatteryPercentage}% → {e.NewStatus.BatteryPercentage}%");
            }
        }

        private void LogPowerModeChange(object? sender, PowerModeEventArgs e)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Console.WriteLine($"[{timestamp}] Power Mode: {e.Mode}");
        }

        private string GetPlatformName()
        {
            if (OperatingSystem.IsWindows()) return "Windows";
            if (OperatingSystem.IsLinux()) return "Linux";
            if (OperatingSystem.IsMacOS()) return "macOS";
            return "Unknown";
        }
    }

    #endregion

    #region Cleanup

    public void Dispose()
    {
        _powerMonitor?.Dispose();
    }

    #endregion
}

#endif