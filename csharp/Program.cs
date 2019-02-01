using BenchmarkDotNet.Running;
using System.Reflection;

namespace Ryu
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run(Assembly.GetExecutingAssembly());
        }
    }
}
