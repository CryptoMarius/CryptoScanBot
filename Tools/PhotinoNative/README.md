# Patched Photino.Native

## The problem

Photino.Native 4.0.22 (the version Photino.NET 4.0.16 / Photino.Blazor 4.0.13 ship) converts every
string it receives from .NET with `ToUTF16String`, which allocates a `std::wstring` with `new` and
never deletes it. `SendWebMessage` runs through that function, and Blazor calls `SendWebMessage`
for every render batch. So every repaint of the window leaves one UTF-16 copy of the batch in the
native heap of the scanner process, for as long as the process lives.

Measured on 03-09-2026 on Binance Perpetual, read-only scan of the private memory of the running
scanner for the marker `__bwv:["RenderBatch"`:

| Time | Copies | Of which .NET strings | Of which raw native buffers |
|---|---|---|---|
| 08:07 | 2,193 | | |
| 08:22 | 4,682 | 90 | 4,592 |

That is 30 to 50 MB per hour per scanner window over a night, growing all night, independent of
symbols, signals and trading. Upstream: tryphotino/photino.Native issue 165 (open since
September 2025); the master branch still has the leak.

The 09-08-2026 leak (render batches of over 100 KB per dashboard tick) was the same mechanism.
That fix made the batches small; this one stops the leak itself.

## What the patch does

`photino-native-webmessage-leak.patch` gives `Photino.Windows.cpp` a conversion helper that lives
on the stack of the caller and uses it in the hot paths: `SendWebMessage`, `NavigateToString`,
`NavigateToUrl` and `SetTitle`. The one-off conversions at window creation (title, user agent,
temporary files path, custom schemes, icon file) keep the original function: they run once and
several callers keep the returned pointer, so freeing it there is not safe without a larger change.

## Building it

Needs Visual Studio with the "Desktop development with C++" workload (MSVC toolset and a Windows
10/11 SDK). This machine had Visual Studio 18 Community without that workload on 03-09-2026; add
it with:

```
"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\setup.exe" modify --installPath "F:\Microsoft Visual Studio\18\Community" --add Microsoft.VisualStudio.Workload.NativeDesktop --includeRecommended --passive
```

Then double click `build.cmd`. It clones the upstream source into `%TEMP%\photino.Native`, applies
the patch, restores the WebView2 SDK and WIL packages, builds x64 Release and copies
`Photino.Native.dll` next to this file.

Built on 03-09-2026 from upstream commit 3986d60 with MSVC 14.51 (toolset v145) and Windows SDK
10.0.26100: the `Photino.Native.dll` next to this file, 319 KB, same 64 exports as the package
version. The patch touches nothing in the export table, so Photino.NET 4.0.16 loads it unchanged.

## How it gets into the scanner

`CryptoScanner.Photino.csproj` has two targets that copy `Tools\PhotinoNative\Photino.Native.dll`
over the package's copy after every build and publish, as long as the file exists. Without the
file the build is unchanged. A running scanner keeps the dll it started with; restart it after
publishing.

## Checking that it worked

`sample-process.ps1` next door (in `Tools\ExchangeCheck`) shows the slope. For the direct proof,
count the marker copies in a running scanner twice, ten minutes apart; with the patch the number
stays flat. The scan script used on 03-09-2026 is `scan-private.ps1` in the Claude session
scratchpad; it is a read-only P/Invoke walk over `VirtualQueryEx` and `ReadProcessMemory`.

## The other half: fewer messages

The leak is per message, so the size of the stream matters too. On 03-09-2026 the stream of
Binance Perpetual was, over 35 minutes:

| Batch size | Count | Total |
|---|---|---|
| under 2 KB | 6,453 | 13.3 MB |
| 10 KB to 100 KB | 202 | 20.1 MB |
| 100 KB to 1 MB | 17 | 5.6 MB |
| over 1 MB (the first render) | 1 | 6.5 MB |

The 10 KB to 100 KB group is the symbol sidebar: every 15 seconds the distance of every row is
reset and the whole table repaints. `MainLayout.razor` now skips that when the sidebar is
collapsed or the Distance column is hidden. The small ones are the dashboard tick (2 s), the log
pump (2 s) and the live data pump (3 s), the same intervals the Avalonia scanner uses; they were
left alone.
