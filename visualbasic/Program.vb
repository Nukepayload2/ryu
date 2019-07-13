Friend Module Program
    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Sub Main(args() As String)
        For i = 0 To 49
            Dim value As String = DoubleToString(88314.3116932511 / 28.26676, i)
            Console.WriteLine(value)
        Next
    End Sub
End Module
