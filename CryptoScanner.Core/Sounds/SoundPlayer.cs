using CryptoScanner.Core.Core;

using NAudio.Wave;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CryptoScanner.Core.Sounds;


public static class ThreadSoundPlayer
{
    private static Thread? soundThread = null;
    // Concurrent: AddToQueue is called from multiple threads (signal pipeline, UI). A plain Dictionary
    // here corrupted its internal state under concurrent access ("non-concurrent collections must have
    // exclusive access").
    private static readonly ConcurrentDictionary<string, DateTime> FilesPlayed = new();
    // Not readonly: CompleteAdding and Cancel are both one-shot. StopSoundThread (on suspend and on
    // exit) used to leave them in that state forever, so after the first sleep every AddToQueue threw
    // "the collection has been marked as complete with regards to additions" and no sound was ever
    // played again. They are replaced when the player is started after a stop.
    private static BlockingCollection<string> soundQueue = [];
    // The sound files that were reported as missing, so the report happens once per file name.
    private static readonly ConcurrentDictionary<string, bool> MissingFilesReported = new();
    private static CancellationTokenSource soundCancelToken = new();
    // Guards the queue/token swap against the threads that are queueing sounds at that moment.
    private static readonly object soundThreadLock = new();


    public static void AddToQueue(string soundFile, bool test)
    {
        string fileName;
        if (Path.GetDirectoryName(soundFile) != "")
            fileName = soundFile;
        else
            fileName = Path.Combine(GlobalData.AppPath, "Sounds", soundFile);

        // Als we binnen x seconden hetzelfde bestand afspelen negeren we het (anders een eindeloze reeks met pingeltjes)

        if (test)
        {
            // Alway's play test sounds
            AddFileToQueue(fileName);
        }
        else
        {
            if (!GlobalData.Settings.Signal.SoundsActive)
                return;

            // Ignore recently played sounds. AddOrUpdate is atomic, so the get-old-and-set-new is
            // thread-safe (no read/modify/write race on the dictionary).
            DateTime now = DateTime.Now;
            bool isPlayedRecently = false;
            FilesPlayed.AddOrUpdate(fileName, now, (key, last) =>
            {
                isPlayedRecently = (now - last).TotalSeconds < 15;
                return now;
            });

            if (!isPlayedRecently)
            {
                AddFileToQueue(fileName);
            }
        }
    }


    /// <summary>
    /// Queue a file and make sure there is a player running for it. Both happen under the same lock:
    /// starting the player can replace the queue, and an add to the old queue would be lost.
    /// </summary>
    private static void AddFileToQueue(string fileName)
    {
        lock (soundThreadLock)
        {
            StartSoundThread();
            soundQueue.Add(fileName);
        }
    }


    private static void StartSoundThread()
    {
        lock (soundThreadLock)
        {
            // A previous StopSoundThread cancelled the token and completed the queue, and neither can
            // be undone, so both are replaced before anything is queued again. The old token source is
            // deliberately not disposed: a player that is still running down the old queue holds its
            // token, and reading a disposed token throws.
            if (soundCancelToken.IsCancellationRequested || soundQueue.IsAddingCompleted)
            {
                soundCancelToken = new CancellationTokenSource();
                soundQueue = [];
            }

            // Sound Player Loop Thread
            if (soundThread == null || !soundThread.IsAlive)
            {
                // Hand the thread the queue and token it has to consume: the fields can be replaced by
                // a next stop/start while this thread is still running down its own queue.
                BlockingCollection<string> queue = soundQueue;
                CancellationToken cancelToken = soundCancelToken.Token;
                soundThread = new Thread(() => SoundThreadExecuteAsync(queue, cancelToken).GetAwaiter().GetResult())
                {
                    Name = "SoundPlayer",
                    IsBackground = true
                };
                soundThread.Start();
            }
        }
    }

    public static void StopSoundThread()
    {
        try
        {
            Thread? threadToJoin;
            lock (soundThreadLock)
            {
                soundCancelToken.Cancel();
                soundQueue.CompleteAdding();
                // Forget the thread: it is running down a queue that has just been cancelled. A sound
                // that arrives after this (after a resume, for instance) needs a player of its own.
                threadToJoin = soundThread;
                soundThread = null;
            }
            threadToJoin?.Join(2000); // Wait for the thread to finish
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab(error.ToString());
        }
    }


    /// <summary>
    /// Method that the outside thread will use outside the thread of this class
    /// </summary>
    private static async Task SoundThreadExecuteAsync(BlockingCollection<string> queue, CancellationToken cancelToken)
    {
        try
        {
            foreach (string fileName in queue.GetConsumingEnumerable(cancelToken))
            {

                if (!File.Exists(fileName))
                {
                    System.Diagnostics.Debug.WriteLine($"Sound file not found: {fileName}");
                    // A missing file used to be that Debug line and nothing else, so a sound file that
                    // was renamed or misspelled in the settings was completely invisible: no sound and
                    // no message. Reported once per file name, otherwise a heartbeat of one minute
                    // writes the same line into the log all day long.
                    if (MissingFilesReported.TryAdd(fileName, true))
                        GlobalData.AddErrorToLogTab($"Sound file not found: {fileName}");
                }
                else
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        // Windows: System.Media.SoundPlayer (built-in!)
                        //var player = new System.Media.SoundPlayer(fileName);
                        //player.Play();

                        // Use NAudio for cross-platform audio playback
                        using var reader = new AudioFileReader(fileName);
                        using var output = new WaveOutEvent();

                        output.Init(reader);
                        output.Play();

                        // Wait for playback to finish
                        while (output.PlaybackState == PlaybackState.Playing && !cancelToken.IsCancellationRequested)
                        {
                            await Task.Delay(100);
                        }

                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        // macOS: afplay command
                        Process.Start("afplay", fileName);
                    }
                    else // Linux
                    {
                        // Linux: aplay command
                        Process.Start("aplay", fileName);
                    }

                }
            }
        }
        catch (OperationCanceledException)
        {
            // niets..
            GlobalData.AddTextToLogTab("Soundplayer exit");
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab(error.ToString());
        }
    }
}
