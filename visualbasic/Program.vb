Module Program

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Sub Main(args() As String)
        ' Converted from c version.

        Dim testValue = Math.PI
        Dim ryuResult = ConvertDoubleToString(testValue)
        Dim expected = testValue.ToString()
        Console.WriteLine($"Ryu CStr(Double) {ryuResult}")
        Console.WriteLine($"Expected(Double) {expected}")

        Dim testValueF = MathF.PI
        Dim ryuResultF = ConvertSingleToString(testValueF)
        Dim expectedF = testValueF.ToString()
        Console.WriteLine($"Ryu CStr(Single) {ryuResultF}")
        Console.WriteLine($"Expected(Single) {expectedF}")
    End Sub
End Module
