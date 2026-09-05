using CryptoScanner.Emulator.Engine;

using System.Text;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// Turning "Force": true off in a queue file after the entry ran. What matters is that exactly one
/// word changes and nothing else does - the file is hand-written, one line per entry, sometimes
/// with comments, and shared between sessions - and that the right entry is found even when the
/// file was extended in front of it while the run was going.
/// </summary>
[TestClass]
public class EmulatorQueueForceResetTests
{
    private string _folder = "";

    [TestInitialize]
    public void Setup()
    {
        _folder = Path.Combine(Path.GetTempPath(), "CryptoScanBot-QueueForceResetTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }


    private const string Compact =
        "[\r\n" +
        "  // the reference run\r\n" +
        "  {\"Label\": \"CA1 reference\", \"Algorithm\": \"dbr\", \"Trading\": {\"StopLossPercentage\": 4, \"Force\": true}},\r\n" +
        "  {\"Label\": \"LA2 close within range 50\", \"Algorithm\": \"failedbreakout\", \"BaseInterval\": \"1m\", \"Force\": true, \"SignalOverrides\": {\"failedbreakout\": {\"CloseWithinRangePercentage\": 50}}},\r\n" +
        "  {\"Label\": \"LA3 no label twin\", \"force\":true},\r\n" +
        "]\r\n";


    private string Put(string content, bool bom)
    {
        string path = Path.Combine(_folder, "queue.json");
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: bom);
        File.WriteAllText(path, content, encoding);
        return path;
    }


    [TestMethod]
    public void ResetForce_ChangesOnlyTheOneWordAndKeepsEverythingElseByteForByte()
    {
        string path = Put(Compact, bom: true);
        byte[] before = File.ReadAllBytes(path);

        Assert.IsTrue(EmulatorQueueFile.ResetForce(path, "LA2 close within range 50", 1));

        byte[] after = File.ReadAllBytes(path);
        string expected = Compact.Replace(
            "\"BaseInterval\": \"1m\", \"Force\": true,",
            "\"BaseInterval\": \"1m\", \"Force\": false,");
        CollectionAssert.AreEqual(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(expected)).ToArray(), after);
        Assert.AreEqual(before.Length + 1, after.Length);

        // The nested Force inside the Trading block of the first entry and the lower-case one of the
        // third entry are still there: only the second entry was touched.
        string text = Encoding.UTF8.GetString(after);
        Assert.IsTrue(text.Contains("\"StopLossPercentage\": 4, \"Force\": true}"));
        Assert.IsTrue(text.Contains("\"force\":true"));
    }


    [TestMethod]
    public void ResetForce_FindsTheEntryByLabelWhenTheIndexHasShifted()
    {
        // Another session put an entry in front while the run was going: the index the batch
        // remembers (0) now points at a different entry, the label still finds the right one.
        string path = Put(Compact, bom: false);

        Assert.IsTrue(EmulatorQueueFile.ResetForce(path, "LA3 no label twin", 0));

        string text = File.ReadAllText(path);
        Assert.IsTrue(text.Contains("\"LA3 no label twin\", \"force\":false}"));
        Assert.IsTrue(text.Contains("\"BaseInterval\": \"1m\", \"Force\": true,"));
    }


    [TestMethod]
    public void ResetForce_FallsBackToThePositionForAnEntryWithoutALabel()
    {
        string path = Put("[{\"Force\": true}, {\"Algorithm\": \"dbr\", \"Force\": true}]", bom: false);

        Assert.IsTrue(EmulatorQueueFile.ResetForce(path, null, 1));

        Assert.AreEqual("[{\"Force\": true}, {\"Algorithm\": \"dbr\", \"Force\": false}]", File.ReadAllText(path));
    }


    [TestMethod]
    public void ResetForce_LeavesTheFileAloneWhenTheEntryHasNoForceOrIsNotThere()
    {
        string path = Put(Compact, bom: true);
        byte[] before = File.ReadAllBytes(path);

        // The first entry only has a Force inside its Trading block, which is not the entry's own.
        Assert.IsFalse(EmulatorQueueFile.ResetForce(path, "CA1 reference", 0));
        Assert.IsFalse(EmulatorQueueFile.ResetForce(path, "not in this file", 0));
        Assert.IsFalse(EmulatorQueueFile.ResetForce(Path.Combine(_folder, "missing.json"), "LA2", 0));

        CollectionAssert.AreEqual(before, File.ReadAllBytes(path));
    }


    [TestMethod]
    public void ResetForce_ReturnsFalseOnAFileThatIsNotJson()
    {
        string path = Put("this is not a queue", bom: false);

        Assert.IsFalse(EmulatorQueueFile.ResetForce(path, "LA2", 0));
        Assert.AreEqual("this is not a queue", File.ReadAllText(path));
    }
}
