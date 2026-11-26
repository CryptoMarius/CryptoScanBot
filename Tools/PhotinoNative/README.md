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

## The second patch: the Invoke race

`photino-native-invoke-race.patch` fixes a crash, not a leak. `Photino::Invoke` (called by
`PhotinoWindow.Invoke` from any thread other than the UI thread) puts its wait record, a mutex
protected flag plus a `std::condition_variable`, on the stack of the calling thread, posts
`WM_USER_INVOKE` and waits. The window procedure runs the callback, sets the flag under the lock,
releases the lock and only then calls `notify_one`. In that gap the calling thread can see the
flag, return and reuse its stack, so `notify_one` reads and writes whatever lives there by then.
The patch moves `notify_one` inside the lock (Windows and Linux code paths both had it).

How it was found: the crash dump of HyperLiquid Perpetual of 21-11-2026 has the UI thread in
`ntdll!RtlpWakeConditionVariable` called from `WM_USER_INVOKE`, on a condition variable address in
the stack of a thread that had already started its next `Photino_Invoke`; the slot held a coreclr
code address. The coreclr frames on top are only the failing exception handling.

Proof: `E:\Claude\claudeTestPhotinoInvoke` (not part of the repository) is a small Photino window
with eight threads calling `Invoke` with an empty callback as fast as they can, each scribbling over
its own stack after every call. With the package dll it dies with an access violation on the UI
thread; see the results below.

Results on 21-11-2026 (8 threads, stack scribbling on):

| Dll | Run | Invokes | Outcome |
|---|---|---|---|
| package 4.0.22 | 1 | 4.7 million | access violation after 20 s |
| package 4.0.22 | 2 | 11.2 million | no crash in 120 s |
| package 4.0.22 | 3 | 10.3 million | access violation after 105 s |
| package 4.0.22 | 4 | 0.5 million | access violation after 5 s |
| Invoke race patch only | 1 | 24.2 million | no crash in 300 s |
| both patches (the dll next to this file) | 1 | 25.1 million | no crash in 300 s |

The package dll crashed about once per 9 million calls; at that rate 24 million calls without a
crash happens by luck in about 7 percent of runs, so two clean runs in a row is about 0.5 percent.
Without the stack scribbling the package dll survived 11.8 million calls: the crash needs the freed
stack slot to hold a nonzero value, which in the scanner is whatever code ran on that thread next.

The patched dll built on 21-11-2026 has md5 039ed4d8509e8e86cce9c331262dfa06 (the leak-only build
of 03-09-2026 was 06963d84f168bca6663a723912f631d2).
