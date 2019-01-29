using System;
using System.Diagnostics;

namespace Ryu
{
    class Program
    {
        private const int LoopCount = 500_0000;
        static void Main(string[] args)
        {
            // Converted from c version.

            string ryuResult = null, expected = null;
            Stopwatch timer = new Stopwatch();

            foreach (var testValue in new[] { Math.PI, 1.2E+233, -2.456E+22, 1.0 / 3.0 })
            {
                timer.Restart();
                for (var i = 1; i <= LoopCount; i++)
                {
                    ryuResult = Global.DoubleToString(testValue);
                }
                timer.Stop();
                Console.WriteLine($"Ryu CStr(Double) {ryuResult} Time {timer.ElapsedMilliseconds}ms");
                timer.Restart();
                for (var i = 1; i <= LoopCount; i++)
                    expected = testValue.ToString();
                timer.Stop();
                Console.WriteLine($"Expected(Double) {expected} Time {timer.ElapsedMilliseconds}ms");
            }

            string ryuResultF = null;
            string expectedF = null;
            foreach (var testValueF in new[] { MathF.PI, 1.2E+14F, -2.456E+22F, 1.0F / 3.0F })
            {
                timer.Restart();
                for (var i = 1; i <= LoopCount; i++)
                    ryuResultF = Global.SingleToString(testValueF);
                timer.Stop();
                Console.WriteLine($"Ryu CStr(Single) {ryuResultF} Time {timer.ElapsedMilliseconds}ms");
                timer.Restart();
                for (var i = 1; i <= LoopCount; i++)
                    expectedF = testValueF.ToString();
                timer.Stop();
                Console.WriteLine($"Expected(Single) {expectedF} Time {timer.ElapsedMilliseconds}ms");
            }

        }

    }
}
