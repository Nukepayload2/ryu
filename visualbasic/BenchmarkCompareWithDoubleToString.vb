Imports BenchmarkDotNet.Attributes

<CoreJob>
<RPlotExporter, MarkdownExporterAttribute.GitHub, RankColumn>
Public Class BenchmarkCompareWithDoubleToString
    <Params(Math.PI, Math.E, 0.25, 1.0 / 3.0, 1230000000000.0, 4.56E-21)>
    Public Number As Double

    <Params(100_0000)>
    Public LoopCount As Integer

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    <Benchmark>
    Public Sub DoubleToStringWithRyu()
        For i = 0 To LoopCount - 1
            ConvertDoubleToString(Number)
        Next i
    End Sub

    <Benchmark>
    Public Sub DoubleToStringWithToString()
        For i = 0 To LoopCount - 1
            Number.ToString()
        Next i
    End Sub
End Class
