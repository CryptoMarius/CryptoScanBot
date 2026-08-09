using CryptoScanner.Core.Core;
using CryptoScanner.Core.Diagnostics;

namespace CryptoScanner.Commands;

public class CommandShowMemoryObjects : CommandBase
{
    public override async void Execute(object? parameter)
    {
        _ = Task.Run(() => { DumpSomething(); });
    }

    public static void DumpSomething()
    {
        System.Diagnostics.Debug.WriteLine($"Show memory");

        // The dump itself lives in the Core so the Avalonia host and the Photino host produce
        // exactly the same files (see MemoryDump.Execute).
        string folder = MemoryDump.Execute();
        GlobalData.AddTextToLogTab($"Memory dump saved to {folder}");

        //// does not work..
        //var app = Application.Current;
        //if (app?.Styles == null)
        //    return;

        //foreach (var style in app.Styles)
        //{
        //    if (style is IResourceDictionary rd) //IResourceProvider
        //    {
        //        foreach (var key in rd.Keys)
        //        {
        //            if (rd.TryGetValue(key, out var val))
        //            {
        //                if (val is IBrush)
        //                    log.AppendLine($"Resource key={key} type={val.GetType().Name}");
        //                //else if (val is IColor)
        //                //    log.AppendLine($"Resource key={key} type={val.GetType().Name}");
        //            }
        //        }

        //        // ThemeDictionaries (Light/Dark) if present
        //        if (rd.ThemeDictionaries != null)
        //        {
        //            foreach (var kv in rd.ThemeDictionaries)
        //            {
        //                log.AppendLine($"Theme variant={kv.Key}");
        //                if (kv.Value is IResourceDictionary trd)
        //                {
        //                    foreach (var k in trd.Keys)
        //                    {
        //                        if (trd.TryGetValue(k, out var v) && v is IBrush)
        //                            log.AppendLine($"  {k} => {v.GetType().Name}");
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}
        //log.AppendLine($"");
        //log.AppendLine($"");
    }
}
