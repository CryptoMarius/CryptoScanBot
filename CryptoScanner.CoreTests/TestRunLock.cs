using System.Diagnostics;
using System.Globalization;

namespace CryptoScanner.CoreTests;

/// <summary>
/// Keeps two test runs off the same test database. Every test in this assembly works against one
/// file - AppDataFolder/CryptoScanBot.db, set in <see cref="TestAssemblySetup"/> - and TestBase
/// empties the Asset, Position, PositionPart, PositionStep, Order and Trade tables without a filter
/// before a test arranges its own rows. That is harmless while one process owns the database and
/// destructive the moment a second one starts, because the delete of one run lands in the middle of
/// the arrange of the other.
/// <para>
/// It is not a theory. Several sessions work in this repository at the same time and each of them
/// runs the suite; three full runs on 31-08-2026 with nothing changed in between gave 1, 0 and 2
/// failures. Two of them were traced to exactly this: AssetManagementSwitchTests lost a Position
/// row under an insert of its PositionPart and answered with "SQLite Error 19: FOREIGN KEY
/// constraint failed", and AssetAdjustmentTests counted one day instead of two because the other
/// run had just emptied the Asset table. A build of the same moment failed with "The file is locked
/// by: testhost", which is the second process saying so out loud.
/// </para>
/// <para>
/// So the run takes an exclusive lock on a file beside that database and waits for its turn when
/// somebody else holds it. The lock is the OPEN HANDLE and not the contents of the file: a handle
/// is released by the operating system when the process ends, however it ends, so a run that
/// crashes or is killed cannot leave a lock behind that blocks everybody else. Others may still
/// read the file, so its contents say who is holding it and since when.
/// </para>
/// <para>
/// What this does NOT cover: the BUILD. Two sessions building at the same time still collide on the
/// assemblies themselves ("CryptoScanner.Analyzers.pdb because it is being used by another
/// process"). This guards the run, not the compiler.
/// </para>
/// </summary>
internal static class TestRunLock
{
    /// <summary>Name of the lock file, beside the test database it protects.</summary>
    private const string FileName = "testrun.lock";

    /// <summary>
    /// How long to wait for the other run before giving up. A full suite takes some three and a half
    /// minutes, so this is roughly four turns of it - long enough that an ordinary queue never hits
    /// it, short enough that a run does not appear to hang forever.
    /// </summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(15);

    /// <summary>How often to try again while another run holds the lock.</summary>
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(500);

    private static FileStream? _handle;


    /// <summary>
    /// Waits until no other test run is using the database and then takes it. Answers with the
    /// number of seconds spent waiting, so the caller can say so in the run output - zero when the
    /// lock was free straight away.
    /// </summary>
    /// <exception cref="TimeoutException">The other run held on past <see cref="Timeout"/>.</exception>
    internal static double Acquire(string folder)
    {
        string path = Path.Combine(folder, FileName);
        Stopwatch waiting = Stopwatch.StartNew();
        bool reported = false;

        while (true)
        {
            try
            {
                // FileShare.Read and not None: a second run asks for write access and is refused,
                // which is the whole point, while a person or a script may still read the file to
                // see who is holding it.
                FileStream handle = new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                Describe(handle);
                _handle = handle;
                return waiting.Elapsed.TotalSeconds;
            }
            catch (IOException)
            {
                // Held by another test run. Anything else - a missing folder, no rights - is not an
                // IOException we can wait out, and is left to travel up.
                if (!reported)
                {
                    reported = true;
                    Console.WriteLine($"Another test run is using {path}, waiting for it to finish...");
                }

                if (waiting.Elapsed > Timeout)
                {
                    throw new TimeoutException(
                        $"Waited {Timeout.TotalMinutes:N0} minutes for the other test run to release {path}. " +
                        $"Either that run is stuck, or it is a very long one - check which process holds the file.");
                }

                Thread.Sleep(RetryDelay);
            }
        }
    }


    /// <summary>Hands the lock back. Safe to call when it was never taken.</summary>
    internal static void Release()
    {
        _handle?.Dispose();
        _handle = null;
    }


    /// <summary>
    /// Writes who is holding the lock and since when. Only readable by someone else while the lock
    /// is held (the holder keeps the write access), and it is what stays behind afterwards, so a
    /// question about a run that already finished can still be answered.
    /// </summary>
    private static void Describe(FileStream handle)
    {
        using Process self = Process.GetCurrentProcess();
        string text = string.Format(CultureInfo.InvariantCulture,
            "process {0} ({1}) since {2:yyyy-MM-dd HH:mm:ss} utc{3}",
            self.Id, self.ProcessName, DateTime.UtcNow, Environment.NewLine);

        handle.SetLength(0);
        using StreamWriter writer = new(handle, leaveOpen: true);
        writer.Write(text);
        writer.Flush();
        handle.Flush(true);
    }
}
