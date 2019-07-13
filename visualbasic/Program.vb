Friend Module Program

    Sub Main(args() As String)
        For i = 0 To 49
            Dim value As String = D2Fixed.DoubleToString(88314.3116932511 / 28.26676, i)
            Console.WriteLine(value)
        Next
    End Sub
End Module
