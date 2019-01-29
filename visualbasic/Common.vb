Imports System.Runtime.CompilerServices
Imports System.Text

Module Common
    ' Returns e == 0 ? 1 : ceil(log_2(5^e)).
    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Function Pow5bits(e As Integer) As Integer
        ' This approximation works up to the point that the multiplication overflows at e = 3529.
        ' If the multiplication were done in 64 bits, it would fail at 5^4004 which is just greater
        ' than 2^9297.
        Return CInt(((CUInt(e) * 1217359UI) >> 19) + 1UI)
    End Function

    ' Returns floor(log_10(2^e)).
    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Function Log10Pow2(e As Integer) As UInteger
        ' The first value this approximation fails for is 2^1651 which is just greater than 10^297.
        Return (CUInt(e) * 78913UI) >> 18
    End Function

    ' Returns floor(log_10(5^e)).
    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Function Log10Pow5(e As Integer) As UInteger
        ' The first value this approximation fails for is 5^2621 which is just greater than 10^1832.
        Return (CUInt(e) * 732923UI) >> 20
    End Function

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Function CopySpecialString(result As Span(Of SByte), sign As Boolean, exponent As Boolean, mantissa As Boolean) As Integer
        If mantissa Then
            result(0) = AscW("N"c)
            result(1) = AscW("a"c)
            result(2) = AscW("N"c)
            Return 3
        End If
        If sign Then
            result(0) = AscW("-"c)
        End If
        Dim signI As Integer = BooleanToInt32(sign)
        If exponent Then
            result(0) = AscW("I"c)
            result(1) = AscW("n"c)
            result(2) = AscW("f"c)
            result(3) = AscW("i"c)
            result(4) = AscW("n"c)
            result(5) = AscW("i"c)
            result(6) = AscW("t"c)
            result(7) = AscW("y"c)
            Return signI + 8
        Else
            result(0) = AscW("0"c)
            result(1) = AscW("E"c)
            result(2) = AscW("0"c)
            Return signI + 3
        End If
    End Function

    <ThreadStatic>
    Private t_NumberFormatterSharedStringBuilder As StringBuilder

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Function CopyAsciiSpanToNewString(result As Span(Of SByte), strLen As Integer) As String
        If t_NumberFormatterSharedStringBuilder Is Nothing Then
            t_NumberFormatterSharedStringBuilder = New StringBuilder()
        Else
            t_NumberFormatterSharedStringBuilder.Clear()
        End If
        Dim sb = t_NumberFormatterSharedStringBuilder
        For i As Integer = 0 To strLen - 1
            Dim ch = result(i)
            sb.Append(Convert.ToChar(ch))
        Next i
        Return sb.ToString()
    End Function
End Module
