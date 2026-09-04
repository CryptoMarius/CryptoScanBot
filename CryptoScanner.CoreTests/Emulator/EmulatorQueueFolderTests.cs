using CryptoScanner.Emulator.Engine;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The Queue folder the emulator works through on its own. What is worth testing is the order the
/// files are taken in, that a file still being written is left alone, and that a finished file
/// keeps its name behind the time stamp - because those three rules are what someone relies on when
/// dropping a file into the folder while nobody is at the machine.
/// </summary>
[TestClass]
public class EmulatorQueueFolderTests
{
    private string _folder = "";

    [TestInitialize]
    public void Setup()
    {
        _folder = Path.Combine(Path.GetTempPath(), "CryptoScanBot-QueueFolderTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }


    private string Put(string name, DateTime lastWriteUtc)
    {
        string path = Path.Combine(_folder, name);
        File.WriteAllText(path, "[]");
        File.SetLastWriteTimeUtc(path, lastWriteUtc);
        return path;
    }


    [TestMethod]
    public void PickNext_TakesTheAlphabeticallyFirstJsonFile()
    {
        DateTime old = new(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc);
        Put("02-halves.json", old);
        string first = Put("01-current.json", old);
        Put("00-notes.txt", old);

        Assert.AreEqual(first, EmulatorQueueFolder.PickNext(_folder, old.AddMinutes(1), TimeSpan.FromSeconds(10)));
    }


    [TestMethod]
    public void PickNext_LeavesAFileThatIsStillBeingWritten()
    {
        DateTime now = new(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc);
        Put("01-current.json", now.AddSeconds(-3));

        Assert.IsNull(EmulatorQueueFolder.PickNext(_folder, now, TimeSpan.FromSeconds(10)),
            "a file written three seconds ago may still be half-saved");
        Assert.IsNotNull(EmulatorQueueFolder.PickNext(_folder, now.AddSeconds(10), TimeSpan.FromSeconds(10)));
    }


    [TestMethod]
    public void PickNext_ReturnsNullOnAnEmptyOrMissingFolder()
    {
        Assert.IsNull(EmulatorQueueFolder.PickNext(_folder, DateTime.UtcNow, TimeSpan.Zero));

        string missing = Path.Combine(_folder, "not-there-yet");
        Assert.IsNull(EmulatorQueueFolder.PickNext(missing, DateTime.UtcNow, TimeSpan.Zero));
        Assert.IsTrue(Directory.Exists(missing), "the folder is created so a file can be dropped in");
    }


    [TestMethod]
    public void MoveTo_PutsTheTimeInFrontAndKeepsTheName()
    {
        string file = Put("01-current.json", DateTime.UtcNow);
        string done = Path.Combine(_folder, "Done");
        DateTime at = new(2026, 9, 7, 8, 15, 0);

        string moved = EmulatorQueueFolder.MoveTo(file, done, at);

        Assert.AreEqual(Path.Combine(done, "20260907-0815 01-current.json"), moved);
        Assert.IsTrue(File.Exists(moved));
        Assert.IsFalse(File.Exists(file));
    }


    [TestMethod]
    public void MoveTo_DoesNotOverwriteAFileDoneInTheSameMinute()
    {
        string done = Path.Combine(_folder, "Done");
        DateTime at = new(2026, 9, 7, 8, 15, 0);

        string first = EmulatorQueueFolder.MoveTo(Put("01-current.json", DateTime.UtcNow), done, at);
        string second = EmulatorQueueFolder.MoveTo(Put("01-current.json", DateTime.UtcNow), done, at);

        Assert.AreNotEqual(first, second);
        Assert.IsTrue(File.Exists(first));
        Assert.IsTrue(File.Exists(second));
        StringAssert.EndsWith(second, "20260907-0815-1 01-current.json");
    }
}
