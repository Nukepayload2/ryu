Module EmbeddedCRuntime
    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Sub memcpy(dest As Span(Of Char), src() As Char, srcOffset As UInteger, len As Integer)
        src.AsSpan().Slice(CInt(srcOffset), len).CopyTo(dest)
    End Sub

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Sub memcpy(dest As Span(Of Char), src As String, len As Integer)
        src.AsSpan().Slice(0, len).CopyTo(dest)
    End Sub

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Sub memcpy(dest As Span(Of Char), src() As Char, srcOffset As Integer, len As Integer)
        src.AsSpan().Slice(srcOffset, len).CopyTo(dest)
    End Sub

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Sub memset(str As Span(Of Char), value As Char, count As Integer)
        For i As Integer = 0 To count - 1
            str(i) = value
        Next i
    End Sub
End Module
