Partial Friend Module Common
    ' Copyright 2018 Ulf Adams
    '
    ' The contents of this file may be used under the terms of the Apache License,
    ' Version 2.0.
    '
    '    (See accompanying file LICENSE-Apache or copy at
    '     http://www.apache.org/licenses/LICENSE-2.0)
    '
    ' Alternatively, the contents of this file may be used under the terms of
    ' the Boost Software License, Version 1.0.
    '    (See accompanying file LICENSE-Boost or copy at
    '     https://www.boost.org/LICENSE_1_0.txt)
    '
    ' Unless required by applicable law or agreed to in writing, this software
    ' is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
    ' KIND, either express or implied.

    Function decimalLength9(v As UInteger) As Integer
        ' Function precondition: v is not a 10-digit number.
        ' (f2s: 9 digits are sufficient for round-tripping.)
        ' (d2fixed: We print 9-digit blocks.)
        Debug.Assert(v < 1000000000)
        If v >= 100000000 Then
            Return 9
        End If
        If v >= 10000000 Then
            Return 8
        End If
        If v >= 1000000 Then
            Return 7
        End If
        If v >= 100000 Then
            Return 6
        End If
        If v >= 10000 Then
            Return 5
        End If
        If v >= 1000 Then
            Return 4
        End If
        If v >= 100 Then
            Return 3
        End If
        If v >= 10 Then
            Return 2
        End If
        Return 1
    End Function

    ' Returns e == 0 ? 1 : ceil(log_2(5^e)).
    Function pow5bits(e As Integer) As Integer
        ' This approximation works up to the point that the multiplication overflows at e = 3529.
        ' If the multiplication were done in 64 bits, it would fail at 5^4004 which is just greater
        ' than 2^9297.
        Debug.Assert(e >= 0)
        Debug.Assert(e <= 3528)
        Return CInt((((CUInt(e)) * 1217359) >> 19) + 1)
    End Function

    ' Returns floor(log_10(2^e)).
    Function log10Pow2(e As Integer) As UInteger
        ' The first value this approximation fails for is 2^1651 which is just greater than 10^297.
        Debug.Assert(e >= 0)
        Debug.Assert(e <= 1650)
        Return ((CUInt(e)) * 78913UI) >> 18
    End Function

    ' Returns floor(log_10(5^e)).
    Function log10Pow5(e As Integer) As UInteger
        ' The first value this approximation fails for is 5^2621 which is just greater than 10^1832.
        Debug.Assert(e >= 0)
        Debug.Assert(e <= 2620)
        Return ((CUInt(e)) * 732923UI) >> 20
    End Function

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Function copy_special_str(result As Span(Of Char), sign As Boolean, exponent As Boolean, mantissa As Boolean) As Integer
        If mantissa Then
            result(0) = "N"c
            result(1) = "a"c
            result(2) = "N"c
            Return 3
        End If
        If sign Then
            result(0) = "-"c
        End If
        If exponent Then
            result(0) = "∞"c
            Return Convert.ToInt32(sign) + 1
        Else
            result(0) = "0"c
            result(1) = "E"c
            result(2) = "0"c
            Return Convert.ToInt32(sign) + 3
        End If
    End Function

    Function float_to_bits(f As Single) As UInteger
        Return CUInt(Math.Truncate(BitConverter.SingleToInt32Bits(f)))
    End Function

    Function double_to_bits(d As Double) As ULong
        Return CULng(BitConverter.DoubleToInt64Bits(d))
    End Function
End Module
