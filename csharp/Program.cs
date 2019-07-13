using System;

namespace Ryu
{
    class Program
    {
        static void Main(string[] args)
        {
            for (int i = 0; i < 50; i++)
            {
                string value = Global.DoubleToString(88314.3116932511 / 28.26676, i);
                Console.WriteLine(value);
            }
        }
    }
}
