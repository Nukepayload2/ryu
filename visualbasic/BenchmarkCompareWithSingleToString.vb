Imports BenchmarkDotNet.Attributes

<CoreJob>
<RPlotExporter, MarkdownExporterAttribute.GitHub, RankColumn>
Public Class BenchmarkCompareWithSingleToString
    <Params(MathF.PI, MathF.E, 0.25F, 1.0F / 3.0F, 1.23E+12F, 4.56E-21F)>
    Public Number As Single

    <Params(100_0000)>
    Public LoopCount As Integer

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    <Benchmark>
    Public Sub SingleToStringWithRyu()
        For i = 0 To LoopCount - 1
            ConvertSingleToString(Number)
        Next i
    End Sub

    <Benchmark>
    Public Sub SingleToStringWithToString()
        For i = 0 To LoopCount - 1
            Number.ToString()
        Next i
    End Sub
End Class
