Module Program
    Sub Main(args As String())
        ' Converted from Java version.
        ' This sample is not ready for performance tests, because it uses BigInteger, which is slow.
        ' BigInteger is preserved for better readability.
        ' If you need better performance, see the CSharp version. 
        Dim f As Single = 0.330078125F
        Dim result As String = RyuFloat.floatToString(f, RoundingMode.ROUND_EVEN)
        Console.WriteLine(result & " " & f)
    End Sub

End Module
