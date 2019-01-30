Module ConversionHelper
    Function BooleanToInt32(value As Boolean) As Integer
        Return If(value, 1, 0)
    End Function

    Function BooleanToUInt32(value As Boolean) As UInteger
        Return If(value, 1UI, 0UI)
    End Function

    Function BooleanToUInt64(value As Boolean) As ULong
        Return If(value, 1UL, 0UL)
    End Function

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Sub memcpy(dest As Span(Of Char), src() As Char, srcOffset As UInteger, len As Integer)
        src.AsSpan().Slice(CInt(srcOffset), len).CopyTo(dest)
    End Sub

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Sub memcpy(dest As Span(Of Char), src() As Char, srcOffset As Integer, len As Integer)
        src.AsSpan().Slice(srcOffset, len).CopyTo(dest)
    End Sub
End Module
