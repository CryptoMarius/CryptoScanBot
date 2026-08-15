using CryptoScanner.Core.Const;

namespace CryptoScanner.Core.Core;

/// <summary>
/// An exclusive claim on a data folder, held for as long as the process lives.
/// <para>
/// A data folder carries the databases, the settings and the user state, and two scanners in the
/// same one fight over all three: they overwrite each other's settings on shutdown, they keep
/// separate in-memory caches of the same database, and they both act on the same signals.
/// </para>
/// <para>
/// The claim is a file handle opened with <see cref="FileShare.None"/>, so the operating system
/// releases it the moment the process ends - a crash or a kill can never leave a folder behind
/// that nothing is allowed to open. On Windows the second open fails with a sharing violation;
/// on Linux and macOS .NET turns the same request into an exclusive flock.
/// </para>
/// <para>
/// This is a voluntary guard inside our own applications: an older build without this code walks
/// straight past it.
/// </para>
/// </summary>
public static class DataFolderLock
{
    private static FileStream? _stream;

    /// <summary>The folder currently claimed by this process, or null when nothing is held.</summary>
    public static string? Folder { get; private set; }

    /// <summary>
    /// Who owns the folder that the last <see cref="TryAcquire"/> was refused for. Empty after a
    /// successful claim.
    /// </summary>
    public static string HolderDescription { get; private set; } = string.Empty;

    private static string LockFile(string folder) => Path.Combine(folder, $"{Constants.AppName}.lock");

    private static string InfoFile(string folder) => LockFile(folder) + ".info";

    /// <summary>
    /// Claim the folder for this process. Returns false when another process already owns it (or
    /// when the file cannot be opened at all), with <see cref="HolderDescription"/> filled in.
    /// <para>
    /// Claiming a second folder is allowed: the new claim is taken first and only then is the old
    /// one released, so a refusal leaves the current folder untouched. That is what the emulator's
    /// "change database" needs.
    /// </para>
    /// </summary>
    public static bool TryAcquire(string folder)
    {
        HolderDescription = string.Empty;

        // Paths are compared the way the file system does it: only Linux tells "Data" and "data" apart
        var comparison = OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        folder = Path.GetFullPath(folder);
        if (_stream != null && string.Equals(Folder, folder, comparison))
            return true;

        FileStream stream;
        try
        {
            Directory.CreateDirectory(folder);
            stream = new FileStream(LockFile(folder), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException error)
        {
            HolderDescription = ReadHolder(folder);
            ScannerLog.Logger.Error(error, $"DataFolderLock({folder} is in use by {HolderDescription})");
            return false;
        }
        catch (UnauthorizedAccessException error)
        {
            HolderDescription = $"another process (the lock file could not be opened: {error.Message})";
            ScannerLog.Logger.Error(error, $"DataFolderLock({folder} could not be opened)");
            return false;
        }

        // The claim itself cannot be read while it is held, so who holds it goes into a second file.
        // Purely so a refused start can name the process that is in the way.
        try
        {
            File.WriteAllText(InfoFile(folder),
                $"{Environment.ProcessId}|{Environment.MachineName}|{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }
        catch (Exception error)
        {
            // Cosmetic only - the claim above is what does the work
            ScannerLog.Logger.Error(error, "DataFolderLock(writing the info file)");
        }

        Release();
        _stream = stream;
        Folder = folder;
        return true;
    }

    /// <summary>
    /// Give up the current claim. Not needed on shutdown - the operating system closes the handle
    /// anyway - but <see cref="TryAcquire"/> uses it when the process moves to another folder.
    /// </summary>
    public static void Release()
    {
        try
        {
            _stream?.Dispose();
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "DataFolderLock(releasing)");
        }
        _stream = null;
        Folder = null;
    }

    /// <summary>
    /// The message to show the user when a folder is refused. Composed here so all three hosts
    /// (scanner, Photino, emulator) say the same thing.
    /// </summary>
    public static string ConflictMessage(string folder)
    {
        return $"The data folder is already in use by {HolderDescription}." + Environment.NewLine + Environment.NewLine +
            folder + Environment.NewLine + Environment.NewLine +
            "Two applications in one data folder overwrite each other's settings and databases, so this one stops here. " +
            "Close the other one first, or use a data folder of its own (the -f parameter for the scanner, " +
            "the setup dialog for the emulator).";
    }

    /// <summary>Read back the info file the holder left behind; it is written outside the claim.</summary>
    private static string ReadHolder(string folder)
    {
        try
        {
            string[] parts = File.ReadAllText(InfoFile(folder)).Split('|');
            if (parts.Length == 3)
                return $"process {parts[0]} on {parts[1]} (started {parts[2]})";
        }
        catch
        {
            // No info file, or it is being rewritten at this very moment - fall through
        }
        return "another process";
    }
}
