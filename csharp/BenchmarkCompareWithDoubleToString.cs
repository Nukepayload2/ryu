using BenchmarkDotNet.Attributes;
using System;

namespace Ryu
{
    [CoreJob]
    [RPlotExporter, MarkdownExporterAttribute.GitHub, RankColumn]
    public class BenchmarkCompareWithDoubleToString
    {
        [Params(Math.PI, Math.E, 0.25, 1.0 / 3.0, 1230000000000.0, 4.56E-21)]
        public double Number;

        [Params(100_0000)]
        public int LoopCount;

        [Benchmark]
        public void DoubleToStringWithRyu()
        {
            for (var i = 0; i < LoopCount; i++)
            {
                Global.DoubleToString(Number);
            }
        }

        [Benchmark]
        public void DoubleToStringWithToString()
        {
            for (var i = 0; i < LoopCount; i++)
            {
                Number.ToString();
            }
        }
    }
}
