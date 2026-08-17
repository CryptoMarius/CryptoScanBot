using System.Reflection;

namespace CryptoScanner.Core.Core;

/// <summary>
/// The commit this build was made from, stamped in by the StampGitCommit target in
/// Directory.Build.props and read back here so every host shows the same thing.
/// <para>
/// A version number does not identify a build - 2.6.0 covers weeks of commits - so a report about an
/// overnight run cannot say which code produced it. The commit can.
/// </para>
/// </summary>
public static class BuildInfo
{
    /// <summary>Short commit hash, with "-dirty" appended when the build was made from a working
    /// tree with uncommitted changes. Empty when the build was not stamped (no git available).</summary>
    public static string Commit { get; } = ReadMetadata("GitCommit");

    /// <summary>Date of that commit as yyyy-MM-dd, or empty. A hash on its own says nothing about
    /// how old a build is, which is exactly what you want to know when reading a night report.</summary>
    public static string CommitDate { get; } = ReadMetadata("GitCommitDate");

    /// <summary>True when the build was made from edited sources, so it is NOT the commit it
    /// names.</summary>
    public static bool IsDirty => Commit.EndsWith("-dirty", StringComparison.Ordinal);

    /// <summary>
    /// One line for the about box and the log: "e69536b1 (2026-08-17)", with the date left out when
    /// it is not known. Empty when there is no stamp at all - the caller then simply shows nothing
    /// rather than a placeholder that looks like a version.
    /// </summary>
    public static string Description
    {
        get
        {
            if (Commit == "")
                return "";
            if (CommitDate == "")
                return Commit;
            return $"{Commit} ({CommitDate})";
        }
    }

    /// <summary>
    /// The entry assembly is the executable that was started, which is the build the user is asking
    /// about. Under a test host there is none, so this assembly is the fallback; both carry the same
    /// stamp because Directory.Build.props applies to every project.
    /// </summary>
    private static string ReadMetadata(string key)
    {
        Assembly assembly = Assembly.GetEntryAssembly() ?? typeof(BuildInfo).Assembly;
        foreach (var attribute in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (attribute.Key == key)
                return attribute.Value ?? "";
        }
        return "";
    }
}
