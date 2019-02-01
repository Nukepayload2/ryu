using BenchmarkDotNet.Attributes;
using System;

namespace Ryu
{
    [CoreJob]
    [RPlotExporter, MarkdownExporterAttribute.GitHub, RankColumn]
    public class BenchmarkCompareWithSingleToString
    {
        [Params(MathF.PI, MathF.E, 0.25F, 1.0F / (double)3.0F, 1.23E+12F, 4.56E-21F)]
        public float Number;

        [Params(100_0000)]
        public int LoopCount;

        [Benchmark]
        public void SingleToStringWithRyu()
        {
            for (var i = 0; i < LoopCount; i++)
            {
                Global.SingleToString(Number);
            }
        }

        [Benchmark]
        public void SingleToStringWithToString()
        {
            for (var i = 0; i < LoopCount; i++)
            {
                Number.ToString();
            }
        }
    }
}
