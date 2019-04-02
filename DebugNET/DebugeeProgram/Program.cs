using System;
using System.Threading;

namespace DebugeeProgram {
    class Program {
        private static unsafe void Main(string[] args) {

            Random random = new Random();
            int value = random.Next(0, 255);
            IntPtr address = (IntPtr)(&value);

            do {

                Console.Clear();
                int v = value;
                Console.Write($"{ v } ({ address.ToString("X8") })");
                Thread.Sleep(1000);

            } while (true);
        }
    }
}
