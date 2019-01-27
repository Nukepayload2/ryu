Module Program
    Sub Main(args As String())
        Dim f As Single = 0.330078125F
        Dim result As String = RyuFloat.floatToString(f, RoundingMode.ROUND_EVEN)
        Console.WriteLine(result & " " & f)
    End Sub

End Module
