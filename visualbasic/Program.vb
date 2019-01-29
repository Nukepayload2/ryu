Module Program
    Private Const LoopCount As Integer = 500_0000

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Sub Main(args() As String)
        ' Converted from c version.

        Dim ryuResult As String = Nothing
        Dim expected As String = Nothing
        Dim timer As New Stopwatch

        For Each testValue In {Math.PI, 1.2E+233, -2.456E+22, 1 / 3}
            timer.Restart()
            For i = 1 To LoopCount
                ryuResult = ConvertDoubleToString(testValue)
            Next
            timer.Stop()
            Console.WriteLine($"Ryu CStr(Double) {ryuResult} Time {timer.ElapsedMilliseconds}ms")
            timer.Restart()
            For i = 1 To LoopCount
                expected = testValue.ToString
            Next
            timer.Stop()
            Console.WriteLine($"Expected(Double) {expected} Time {timer.ElapsedMilliseconds}ms")
        Next

        Dim ryuResultF As String = Nothing
        Dim expectedF As String = Nothing
        For Each testValueF In {MathF.PI, 1.2E+14F, -2.456E+22F, 1.0F / 3.0F}
            timer.Restart()
            For i = 1 To LoopCount
                ryuResultF = ConvertSingleToString(testValueF)
            Next
            timer.Stop()
            Console.WriteLine($"Ryu CStr(Single) {ryuResultF} Time {timer.ElapsedMilliseconds}ms")
            timer.Restart()
            For i = 1 To LoopCount
                expectedF = testValueF.ToString
            Next
            timer.Stop()
            Console.WriteLine($"Expected(Single) {expectedF} Time {timer.ElapsedMilliseconds}ms")
        Next

    End Sub
End Module
