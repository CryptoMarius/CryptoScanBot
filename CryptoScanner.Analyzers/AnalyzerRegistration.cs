using CryptoScanner.Analyzers.AtrRb;
using CryptoScanner.Analyzers.Baba;
using CryptoScanner.Analyzers.Bre;
//using CryptoScanner.Analyzers.Storsi;
using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers;

/// <summary>
/// Entry point for the Analyzers project. Call <see cref="RegisterAll"/> once
/// at startup to register all analyzer strategies into the PluginManager.
/// </summary>
public static class AnalyzerRegistration
{
    public static void RegisterAll()
    {
        PluginManager.Register(new AtrRbPlugin());
        PluginManager.Register(new BabaPlugin());
        PluginManager.Register(new BrePlugin());

        //PluginManager.Register(new StoRsiPlugin());
    }
}
