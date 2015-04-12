using System;
using System.Threading;

namespace nora.clara {

    class Reader {

        private static Thread inputThread;
        private static AutoResetEvent getInput, gotInput;
        private static string input;

        static Reader() {
            inputThread = new Thread(reader);
            inputThread.IsBackground = true;
            inputThread.Start();
            getInput = new AutoResetEvent(false);
            gotInput = new AutoResetEvent(false);
        }

        private static void reader() {
            var stream = Console.OpenStandardInput();
            while (stream.CanRead) {
                getInput.WaitOne();
                input = Console.ReadLine();
                gotInput.Set();
            }
        }

        public static string ReadLine(TimeSpan time) {
            getInput.Set();
            bool success = gotInput.WaitOne(time);
            if (success) {
                return input;
            } else {
                return null;
            }
        }
    }
}
