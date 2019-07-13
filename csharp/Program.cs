using System;

namespace Ryu
{
    class Program
    {
        static void Main(string[] args)
        {
            string value = Global.DoubleToString(88314.3116932511 / 28.26676, 22);
            Console.WriteLine(value);
        }
    }
}
