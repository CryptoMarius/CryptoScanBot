using CryptoScanner.Core.Core;

using System.Globalization;

namespace CryptoScanner.Emulator.Engine;


/// <summary>
/// The Queue folder next to the data: queue files waiting to run, with Done and Failed underneath
/// for the ones that have been dealt with. The rules that matter live here, away from the view
/// model, so they can be tested against a temporary folder.
/// </summary>
public static class EmulatorQueueFolder
{
    public const string FolderName = "Queue";
    public const string DoneFolderName = "Done";
    public const string FailedFolderName = "Failed";

    /// <summary>How old a file's last write has to be before it is picked up, so one that is still being saved is left alone.</summary>
    public static readonly TimeSpan SettleTime = TimeSpan.FromSeconds(10);

    /// <summary>How long the folder queue waits between two looks into an empty folder.</summary>
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    public static string Folder => Path.Combine(GlobalData.AppDataFolder, FolderName);
    public static string DoneFolder => Path.Combine(Folder, DoneFolderName);
    public static string FailedFolder => Path.Combine(Folder, FailedFolderName);


    /// <summary>
    /// The next file to run: the alphabetically first .json in the folder whose last write is at
    /// least <paramref name="settleTime"/> before <paramref name="utcNow"/>, or null when there is
    /// none. Ordinal order, so "01-..." runs before "02-..." on every machine and a number in front
    /// of the name is enough to decide the order.
    /// </summary>
    public static string? PickNext(string folder, DateTime utcNow, TimeSpan settleTime)
    {
        Directory.CreateDirectory(folder);
        return Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly)
            .Where(f => Path.GetExtension(f).Equals(".json", StringComparison.OrdinalIgnoreCase))
            .Where(f => File.GetLastWriteTimeUtc(f) <= utcNow - settleTime)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .FirstOrDefault();
    }


    /// <summary>
    /// Moves a dealt-with file into <paramref name="targetFolder"/> with the time in front of its
    /// name ("20260907-0815 01-current.json"), so the Done folder reads as a log of what ran when
    /// and a file that is dropped in twice does not overwrite its earlier self. Returns the new path.
    /// </summary>
    public static string MoveTo(string file, string targetFolder, DateTime localNow)
    {
        Directory.CreateDirectory(targetFolder);
        string stamp = localNow.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        string name = Path.GetFileName(file);
        string target = Path.Combine(targetFolder, $"{stamp} {name}");
        for (int i = 1; File.Exists(target); i++)
            target = Path.Combine(targetFolder, $"{stamp}-{i} {name}");
        File.Move(file, target);
        return target;
    }
}
