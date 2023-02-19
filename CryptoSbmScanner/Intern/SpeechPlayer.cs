using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoSbmScanner
{
    static public class ThreadSpeechPlayer
    {
        private static Thread speechPlayThread;
        private static BlockingCollection<string> speechQueue = new BlockingCollection<string>();
        public static CancellationTokenSource speechCancelToken = new CancellationTokenSource();

        public static void AddToQueue(string filename)
        {
            speechQueue.Add(filename);
            StartSpeechThread();
        }


        private static void StartSpeechThread()
        {
            //Sound Player Loop Thread
            if (speechPlayThread == null || !speechPlayThread.IsAlive)
            {
                speechPlayThread = new Thread(SpeechThreadExecute);
                speechPlayThread.Name = "SpeechPlayer";
                speechPlayThread.IsBackground = true;
                speechPlayThread.Start();
            }
        }

        /// <summary>
        /// Method that the outside thread will use outside the thread of this class
        /// </summary>
        public static void SpeechThreadExecute()
        {
            using (SpeechSynthesizer synthesizer = new SpeechSynthesizer())
            {
                // to change VoiceGender and VoiceAge check out those links below
                synthesizer.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Adult);
                synthesizer.Volume = 100;  // (0 - 100)
                synthesizer.Rate = 0;     // (-10 - 10)

                foreach (string text in speechQueue.GetConsumingEnumerable(speechCancelToken.Token))
                {
                   synthesizer.Speak(text);
                }
            }
        }

    }
}
