using System;

namespace Ryu
{
    class Program
    {
        static void Main(string[] args)
        {
            // Converted from c version.

            var testValue = Math.PI;
            var ryuResult = Global.DoubleToString(testValue);
            var expected = testValue.ToString();
            Console.WriteLine($"Ryu      {ryuResult}");
            Console.WriteLine($"Expected {expected}");

            var testValueF = MathF.PI;
            var ryuResultF = Global.SingleToString(testValueF);
            var expectedF = testValueF.ToString();
            Console.WriteLine($"RyuF      {ryuResultF}");
            Console.WriteLine($"ExpectedF {expectedF}");
        }
    }
}
