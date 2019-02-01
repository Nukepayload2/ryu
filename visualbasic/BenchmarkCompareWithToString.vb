Imports BenchmarkDotNet.Attributes

<CoreJob>
<MarkdownExporterAttribute.GitHub, RankColumn>
Public Class BenchmarkCompareWithToString
    <Params(MathF.PI, MathF.E, 0.25F, 1.0F / 3.0F, 1.23E+12F, 4.56E-21F)>
    Public Number As Single

    <Params(100_0000)>
    Public LoopCount As Integer

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    <Benchmark>
    Public Sub SingleToStringWithRyu()
        For i = 1 To LoopCount
            ConvertSingleToString(Number)
        Next
    End Sub

    <Benchmark>
    Public Sub SingleToStringWithToString()
        For i = 1 To LoopCount
            Number.ToString()
        Next
    End Sub
End Class
