using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System.Net;
using System.Net.NetworkInformation;

namespace CryptoScanner.Core.SignalR;

/// <summary>
/// Self-hosted Kestrel server that exposes a SignalR hub for broadcasting crypto signals.
/// Call <see cref="StartAsync"/> after configuration is loaded and <see cref="StopAsync"/> on shutdown.
/// </summary>
public sealed class SignalRService : IDisposable
{
    private WebApplication? _app;
    private IHubContext<CryptoSignalHub>? _hubContext;
    private bool _disposed;

    public bool IsRunning => _app != null;

    public async Task StartAsync()
    {
        if (_app != null)
            return;

        if (!GlobalData.Settings.General.SignalREnabled)
            return;

        int port = GlobalData.Settings.General.SignalRPort;

        if (!IsPortAvailable(port))
        {
            GlobalData.AddTextToLogTab($"SignalR: port {port} is already in use, server not started");
            ScannerLog.Logger.Warn($"SignalR: port {port} is already in use");
            return;
        }

        try
        {
            var builder = WebApplication.CreateBuilder();

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Loopback, port);
            });

            // Suppress ASP.NET Core's own console logging (signals go through our log tab)
            builder.Logging.ClearProviders();

            builder.Services.AddSignalR()
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.PropertyNamingPolicy = null;
                });

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .SetIsOriginAllowed(_ => true)
                        .AllowCredentials();
                });
            });

            _app = builder.Build();

            _app.UseCors();
            _app.MapHub<CryptoSignalHub>("/signalr/signals");

            await _app.StartAsync();

            _hubContext = _app.Services.GetRequiredService<IHubContext<CryptoSignalHub>>();

            GlobalData.AddTextToLogTab($"SignalR server started on http://localhost:{port}/signalr/signals");
            ScannerLog.Logger.Info($"SignalR server started on port {port}");
        }
        catch (Exception ex)
        {
            ScannerLog.Logger.Error(ex, "SignalR server failed to start");
            GlobalData.AddTextToLogTab($"SignalR server failed to start: {ex.Message}");
            _app = null;
            _hubContext = null;
        }
    }

    public async Task StopAsync()
    {
        if (_app != null)
        {
            try
            {
                await _app.StopAsync();
                await _app.DisposeAsync();
            }
            catch (Exception ex)
            {
                ScannerLog.Logger.Error(ex, "SignalR server stop error");
            }
            finally
            {
                _app = null;
                _hubContext = null;
                GlobalData.AddTextToLogTab("SignalR server stopped");
            }
        }
    }

    /// <summary>
    /// Broadcast a signal to all connected clients. Safe to call when the server is not running.
    /// </summary>
    public void BroadcastSignal(CryptoSignal signal)
    {
        if (_hubContext == null || GlobalData.IsEmulatorMode)
            return;

        try
        {
            var dto = CryptoSignalDto.FromSignal(signal);
            _ = _hubContext.Clients.All.SendAsync("ReceiveSignal", dto);
        }
        catch (Exception ex)
        {
            ScannerLog.Logger.Error(ex, "SignalR broadcast error");
        }
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var listeners = properties.GetActiveTcpListeners();
            foreach (var endpoint in listeners)
            {
                if (endpoint.Port == port)
                    return false;
            }
            return true;
        }
        catch
        {
            return true;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            StopAsync().GetAwaiter().GetResult();
        }
    }
}
