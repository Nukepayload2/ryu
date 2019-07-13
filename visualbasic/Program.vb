Friend Module Program
    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Sub Main(args() As String)
        Dim value As String = DoubleToString(88314.3116932511 / 28.26676, 22)
        Console.WriteLine(value)
    End Sub
End Module
