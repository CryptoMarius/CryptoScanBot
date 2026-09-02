using CryptoScanner.Core.Core;
using CryptoScanner.Emulator.Engine;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The backup the emulator makes of its queue before a batch starts. The rule that matters is that
/// a batch restarted on an unchanged queue does NOT leave a second copy: the backups used to be made
/// by hand and 62 of them piled up beside the file the emulator reads, which is what this replaces.
/// </summary>
[DoNotParallelize]
[TestClass]
public class EmulatorQueueArchiveTests : TestBase
{
    private string _queue = "";

    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        _queue = EmulatorQueueFile.FilePath;
        if (Directory.Exists(EmulatorQueueFile.ArchiveFolder))
            Directory.Delete(EmulatorQueueFile.ArchiveFolder, recursive: true);
    }

    [TestCleanup]
    public void Restore()
    {
        if (Directory.Exists(EmulatorQueueFile.ArchiveFolder))
            Directory.Delete(EmulatorQueueFile.ArchiveFolder, recursive: true);
        if (File.Exists(_queue))
            File.Delete(_queue);
    }

    private static int ArchivedFiles()
        => Directory.GetFiles(EmulatorQueueFile.ArchiveFolder, "Queue-*.json").Length;


    [TestMethod]
    public void AQueueIsCopiedIntoTheArchiveFolder()
    {
        File.WriteAllText(_queue, "[]");

        string? written = EmulatorQueueFile.ArchiveBeforeRun();

        Assert.IsNotNull(written);
        Assert.AreEqual(1, ArchivedFiles());
        Assert.AreEqual("[]", File.ReadAllText(written));
    }


    [TestMethod]
    public void AnUnchangedQueue_DoesNotLeaveASecondCopy()
    {
        File.WriteAllText(_queue, "[]");

        string? first = EmulatorQueueFile.ArchiveBeforeRun();
        string? second = EmulatorQueueFile.ArchiveBeforeRun();

        Assert.AreEqual(first, second);
        Assert.AreEqual(1, ArchivedFiles());
    }


    /// <summary>
    /// A changed queue gets its own copy even when the restart is inside the same minute - the file
    /// name only counts to minutes, so without the counter behind it the second batch of a busy
    /// afternoon would archive itself over the first.
    /// </summary>
    [TestMethod]
    public void AChangedQueue_GetsACopyOfItsOwn()
    {
        File.WriteAllText(_queue, "[]");
        string? first = EmulatorQueueFile.ArchiveBeforeRun();

        File.WriteAllText(_queue, "[{\"Label\": \"iets anders\"}]");
        string? second = EmulatorQueueFile.ArchiveBeforeRun();

        Assert.AreNotEqual(first, second);
        Assert.AreEqual(2, ArchivedFiles());
    }


    [TestMethod]
    public void WithoutAQueueFile_ItSaysNothingWasCopied()
    {
        if (File.Exists(_queue))
            File.Delete(_queue);

        Assert.IsNull(EmulatorQueueFile.ArchiveBeforeRun());
    }
}
