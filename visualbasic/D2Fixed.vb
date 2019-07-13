Imports System.Buffers
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Module D2Fixed
    Private Const POW10_ADDITIONAL_BITS As Integer = 120

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Function mulShift_mod1e9(m As ULong, mul As Span(Of ULong), j As Integer) As UInteger
        Dim high0 As ULong = Nothing ' 64
        Dim low0 As ULong = umul128(m, mul(0), high0) ' 0
        Dim high1 As ULong = Nothing ' 128
        Dim low1 As ULong = umul128(m, mul(1), high1) ' 64
        Dim high2 As ULong = Nothing ' 192
        Dim low2 As ULong = umul128(m, mul(2), high2) ' 128
        Dim s0low As ULong = low0 ' 0
        Dim s0high As ULong = low1 + high0 ' 64
        Dim c1 As UInteger = Convert.ToUInt32(s0high < low1)
        Dim s1low As ULong = low2 + high1 + c1 ' 128
        Dim c2 As UInteger = Convert.ToUInt32(s1low < low2) ' high1 + c1 can't overflow, so compare against low2
        Dim s1high As ULong = high2 + c2 ' 192

        Debug.Assert(j >= 128)
        Debug.Assert(j <= 180)

        If j < 160 Then ' j: [128, 160)
            Dim r0 As ULong = mod1e9(s1high)
            Dim r1 As ULong = mod1e9((r0 << 32) Or (s1low >> 32))
            Dim r2 As ULong = ((r1 << 32) Or (s1low And &HFFFFFFFFUI))
            Return mod1e9(r2 >> (j - 128))
        Else ' j: [160, 192)
            Dim r0 As ULong = mod1e9(s1high)
            Dim r1 As ULong = ((r0 << 32) Or (s1low >> 32))
            Return mod1e9(r1 >> (j - 160))
        End If
    End Function

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Sub append_n_digits(olength As Integer, digits As UInteger, result As Span(Of Char))
        Dim i As Integer = 0
        Do While digits >= 10000UI
            Dim c As UInteger = digits Mod 10000UI
            digits \= 10000UI
            Dim c0 As UInteger = (c Mod 100UI) << 1UI
            Dim c1 As UInteger = (c \ 100UI) << 1UI
            memcpy(result.Slice(olength - i - 2), DIGIT_TABLE, c0, 2)
            memcpy(result.Slice(olength - i - 4), DIGIT_TABLE, c1, 2)
            i += 4
        Loop
        If digits >= 100UI Then
            Dim c As UInteger = (digits Mod 100UI) << 1UI
            digits \= 100UI
            memcpy(result.Slice(olength - i - 2), DIGIT_TABLE, c, 2)
            i += 2
        End If
        If digits >= 10UI Then
            Dim c As UInteger = digits << 1
            memcpy(result.Slice(olength - i - 2), DIGIT_TABLE, c, 2)
        Else
            result(0) = ChrW(AscW("0"c) + CInt(digits))
        End If
    End Sub

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Sub append_d_digits(olength As Integer, digits As UInteger, result As Span(Of Char))
        Dim i As Integer = 0
        Do While digits >= 10000
            Dim c As UInteger = digits Mod 10000UI
            digits \= 10000UI
            Dim c0 As UInteger = (c Mod 100UI) << 1UI
            Dim c1 As UInteger = (c \ 100UI) << 1UI
            memcpy(result.Slice(olength + 1 - i - 2), DIGIT_TABLE, c0, 2)
            memcpy(result.Slice(olength + 1 - i - 4), DIGIT_TABLE, c1, 2)
            i += 4
        Loop
        If digits >= 100 Then
            Dim c As UInteger = (digits Mod 100UI) << 1UI
            digits \= 100UI
            memcpy(result.Slice(olength + 1 - i - 2), DIGIT_TABLE, c, 2)
            i += 2
        End If
        If digits >= 10 Then
            Dim c As UInteger = digits << 1
            result(2) = DIGIT_TABLE(CInt(c + 1UI))
            result(1) = "."c
            result(0) = DIGIT_TABLE(CInt(c))
        Else
            result(1) = "."c
            result(0) = ChrW(AscW("0"c) + CInt(digits))
        End If
    End Sub

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Sub append_c_digits(count As Integer, digits As UInteger, result As Span(Of Char))
        Dim i As Integer = 0
        Do While i < count - 1
            Dim c As UInteger = (digits Mod 100UI) << 1UI
            digits \= 100UI
            memcpy(result.Slice(count - i - 2), DIGIT_TABLE, c, 2)
            i += 2
        Loop
        If i < count Then
            Dim c As Char = ChrW(AscW("0"c) + CInt(digits Mod 10UI))
            result(count - i - 1) = c
        End If
    End Sub

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Sub append_nine_digits(digits As UInteger, result As Span(Of Char))
        If digits = Nothing Then
            memset(result, "0"c, 9)
            Return
        End If

        For i As Integer = 0 To 4 Step 4
            Dim c As UInteger = digits Mod 10000UI
            digits \= 10000UI
            Dim c0 As UInteger = (c Mod 100UI) << 1UI
            Dim c1 As UInteger = (c \ 100UI) << 1UI
            memcpy(result.Slice(7 - i), DIGIT_TABLE, c0, 2)
            memcpy(result.Slice(5 - i), DIGIT_TABLE, c1, 2)
        Next i
        result(0) = ChrW(AscW("0"c) + CInt(digits))
    End Sub

    Private Function indexForExponent(e As UInteger) As UInteger
        Return (e + 15UI) \ 16UI
    End Function

    Private Function pow10BitsForIndex(idx As UInteger) As UInteger
        Return 16UI * idx + CUInt(POW10_ADDITIONAL_BITS)
    End Function

    Private Function lengthForIndex(idx As UInteger) As UInteger
        ' +1 for ceil, +16 for mantissa, +8 to round up when dividing by 9
        Return (log10Pow2(16 * CInt(idx)) + 1UI + 16UI + 8UI) \ 9UI
    End Function

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Function copy_special_str_printf(result As Span(Of Char), sign As Boolean, mantissa As ULong) As Integer
        If sign Then
            result(0) = "-"c
        End If
        Dim signI As Integer = Convert.ToInt32(sign)
        If mantissa <> Nothing Then
            memcpy(result.Slice(signI), "nan", 3)
            Return signI + 3
        End If
        memcpy(result.Slice(signI), "Infinity", 8)
        Return signI + 8
    End Function

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Function d2fixed_buffered_n(d As Double, precision As Integer, result As Span(Of Char)) As Integer
        Dim bits As ULong = double_to_bits(d)

        ' Decode bits into sign, mantissa, and exponent.
        Dim ieeeSign As Boolean = ((bits >> (DOUBLE_MANTISSA_BITS + DOUBLE_EXPONENT_BITS)) And 1UI) <> 0UI
        Dim ieeeMantissa As ULong = bits And ((1UL << DOUBLE_MANTISSA_BITS) - 1UL)
        Dim ieeeExponent As UInteger = CUInt((bits >> DOUBLE_MANTISSA_BITS) And ((1UI << DOUBLE_EXPONENT_BITS) - 1UI))

        ' Case distinction; exit early for the easy cases.
        If ieeeExponent = ((1UI << DOUBLE_EXPONENT_BITS) - 1UI) Then
            Return copy_special_str_printf(result, ieeeSign, ieeeMantissa)
        End If

        Dim index As Integer = 0
        If ieeeExponent = Nothing AndAlso ieeeMantissa = Nothing Then
            If ieeeSign Then
                'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
                'ORIGINAL LINE: result[index++] = "-"c;
                result(index) = "-"c
                index += 1
            End If
            'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
            'ORIGINAL LINE: result[index++] = "0"c;
            result(index) = "0"c
            index += 1
            If precision > Nothing Then
                'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
                'ORIGINAL LINE: result[index++] = "."c;
                result(index) = "."c
                index += 1
                memset(result.Slice(index), "0"c, precision)
                index += precision
            End If
            Return index
        End If

        Dim e2 As Integer
        Dim m2 As ULong
        If ieeeExponent = Nothing Then
            e2 = 1 - DOUBLE_BIAS - DOUBLE_MANTISSA_BITS
            m2 = ieeeMantissa
        Else
            e2 = CInt(ieeeExponent) - DOUBLE_BIAS - DOUBLE_MANTISSA_BITS
            m2 = (1UL << DOUBLE_MANTISSA_BITS) Or ieeeMantissa
        End If

        Dim nonzero As Boolean = False
        If ieeeSign Then
            'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
            'ORIGINAL LINE: result[index++] = "-"c;
            result(index) = "-"c
            index += 1
        End If
        If e2 >= -52 Then
            Dim idx As UInteger = If(e2 < 0, 0UI, indexForExponent(CUInt(e2)))
            Dim p10bits As UInteger = pow10BitsForIndex(idx)
            Dim len As Integer = CInt(lengthForIndex(idx))

            For i As Integer = len - 1 To Nothing Step -1
                Dim j As UInteger = p10bits - CUInt(e2)
                ' Temporary: j is usually around 128, and by shifting a bit, we push it to 128 or above, which is
                ' a slightly faster code path in mulShift_mod1e9. Instead, we can just increase the multipliers.
                Dim digits As UInteger = mulShift_mod1e9(m2 << 8, POW10_SPLIT(POW10_OFFSET(CInt(idx)) + i), CInt(j + 8))
                If nonzero Then
                    append_nine_digits(digits, result.Slice(index))
                    index += 9
                ElseIf digits <> Nothing Then
                    Dim olength As Integer = decimalLength9(digits)
                    append_n_digits(olength, digits, result.Slice(index))
                    index += olength
                    nonzero = True
                End If
            Next i
        End If
        If Not nonzero Then
            'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
            'ORIGINAL LINE: result[index++] = "0"c;
            result(index) = "0"c
            index += 1
        End If
        If precision > Nothing Then
            'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
            'ORIGINAL LINE: result[index++] = "."c;
            result(index) = "."c
            index += 1
        End If

        If e2 < Nothing Then
            Dim idx As Integer = -e2 \ 16

            Dim blocks As Integer = (precision \ 9 + 1)
            ' Nothing = don't round up; 1 = round up unconditionally; 2 = round up if odd.
            Dim roundUp As Integer = 0
            Dim i As Integer = 0
            If blocks <= MIN_BLOCK_2(idx) Then
                i = blocks
                memset(result.Slice(index), "0"c, precision)
                index += precision
            ElseIf i < MIN_BLOCK_2(idx) Then
                i = MIN_BLOCK_2(idx)
                memset(result.Slice(index), "0"c, 9 * i)
                index += 9 * i
            End If
            Do While i < blocks
                Dim j As Integer = ADDITIONAL_BITS_2 + (-e2 - 16 * idx)
                Dim p As Integer = POW10_OFFSET_2(idx) + i - MIN_BLOCK_2(idx)
                If p >= POW10_OFFSET_2(idx + 1) Then
                    ' If the remaining digits are all 0, then we might as well use memset.
                    ' No rounding required in this case.
                    Dim fill As Integer = precision - 9 * i
                    memset(result.Slice(index), "0"c, fill)
                    index += fill
                    Exit Do
                End If
                ' Temporary: j is usually around 128, and by shifting a bit, we push it to 128 or above, which is
                ' a slightly faster code path in mulShift_mod1e9. Instead, we can just increase the multipliers.
                Dim digits As UInteger = mulShift_mod1e9(m2 << 8, POW10_SPLIT_2(p), j + 8)

                If i < blocks - 1 Then
                    append_nine_digits(digits, result.Slice(index))
                    index += 9
                Else
                    Dim maximum As Integer = precision - 9 * i
                    Dim lastDigit As UInteger = 0
                    Dim k As UInteger = 0
                    Do While k < 9UI - CUInt(maximum)
                        lastDigit = digits Mod 10UI
                        digits \= 10UI
                        k += 1UI
                    Loop

                    If lastDigit <> 5UI Then
                        roundUp = Convert.ToInt32(lastDigit > 5)
                    Else
                        ' Is m * 10^(additionalDigits + 1) / 2^(-e2) integer?
                        Dim requiredTwos As Integer = -e2 - CInt(precision) - 1
                        Dim trailingZeros As Boolean = requiredTwos <= Nothing OrElse (requiredTwos < 60 AndAlso multipleOfPowerOf2(m2, requiredTwos))
                        roundUp = If(trailingZeros, 2, 1)

                    End If
                    If maximum > Nothing Then
                        append_c_digits(maximum, digits, result.Slice(index))
                        index += maximum
                    End If
                    Exit Do
                End If
                i += 1
            Loop

            If roundUp <> Nothing Then
                Dim roundIndex As Integer = index
                Dim dotIndex As Integer = Nothing ' '.' can't be located at index 0
                Do
                    roundIndex -= 1
                    If roundIndex = -1 Then
                        result(roundIndex + 1) = "1"c
                        If dotIndex > Nothing Then
                            result(dotIndex) = "0"c
                            result(dotIndex + 1) = "."c
                        End If
                        'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
                        'ORIGINAL LINE: result[index++] = "0"c;
                        result(index) = "0"c
                        index += 1
                        Exit Do
                    End If
                    Dim c As Char = result(roundIndex)
                    If c = "-"c Then
                        result(roundIndex + 1) = "1"c
                        If dotIndex > Nothing Then
                            result(dotIndex) = "0"c
                            result(dotIndex + 1) = "."c
                        End If
                        'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
                        'ORIGINAL LINE: result[index++] = "0"c;
                        result(index) = "0"c
                        index += 1
                        Exit Do
                    End If
                    If c = "."c Then
                        dotIndex = roundIndex
                        Continue Do
                    ElseIf c = "9"c Then
                        result(roundIndex) = "0"c
                        roundUp = 1
                        Continue Do
                    Else
                        If roundUp = 2 AndAlso AscW(c) Mod 2 = Nothing Then
                            Exit Do
                        End If
                        result(roundIndex) = Convert.ToChar(AscW(c) + 1)
                        Exit Do
                    End If
                Loop
            End If
        Else
            memset(result.Slice(index), "0"c, precision)
            index += precision
        End If
        Return index
    End Function

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Sub d2fixed_buffered(d As Double, precision As Integer, result As Span(Of Char))
        Dim len As Integer = d2fixed_buffered_n(d, precision, result)
        result(len) = Nothing
    End Sub

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Function d2exp_buffered_n(d As Double, precision As Integer, result As Span(Of Char)) As Integer
        Dim bits As ULong = double_to_bits(d)

        ' Decode bits into sign, mantissa, and exponent.
        Dim ieeeSign As Boolean = ((bits >> (DOUBLE_MANTISSA_BITS + DOUBLE_EXPONENT_BITS)) And 1UL) <> 0UL
        Dim ieeeMantissa As ULong = bits And ((1UL << DOUBLE_MANTISSA_BITS) - 1UL)
        Dim ieeeExponent As UInteger = CUInt((bits >> DOUBLE_MANTISSA_BITS) And ((1UI << DOUBLE_EXPONENT_BITS) - 1UI))

        ' Case distinction; exit early for the easy cases.
        If ieeeExponent = ((1UI << DOUBLE_EXPONENT_BITS) - 1UI) Then
            Return copy_special_str_printf(result, ieeeSign, ieeeMantissa)
        End If
        Dim index As Integer = 0
        If ieeeExponent = Nothing AndAlso ieeeMantissa = Nothing Then
            If ieeeSign Then
                'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
                'ORIGINAL LINE: result[index++] = "-"c;
                result(index) = "-"c
                index += 1
            End If
            'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
            'ORIGINAL LINE: result[index++] = "0"c;
            result(index) = "0"c
            index += 1
            If precision > Nothing Then
                'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
                'ORIGINAL LINE: result[index++] = "."c;
                result(index) = "."c
                index += 1
                memset(result.Slice(index), "0"c, precision)
                index += precision
            End If
            memcpy(result.Slice(index), "e+00", 4)
            index += 4
            Return index
        End If

        Dim e2 As Integer
        Dim m2 As ULong
        If ieeeExponent = Nothing Then
            e2 = 1 - DOUBLE_BIAS - DOUBLE_MANTISSA_BITS
            m2 = ieeeMantissa
        Else
            e2 = CInt(ieeeExponent) - DOUBLE_BIAS - DOUBLE_MANTISSA_BITS
            m2 = (1UL << DOUBLE_MANTISSA_BITS) Or ieeeMantissa
        End If

        Dim printDecimalPoint As Boolean = precision > 0
        precision += 1
        If ieeeSign Then
            'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
            'ORIGINAL LINE: result[index++] = "-"c;
            result(index) = "-"c
            index += 1
        End If
        Dim digits As UInteger = 0
        Dim printedDigits As Integer = 0
        Dim availableDigits As Integer = 0
        Dim exp As Integer = 0
        If e2 >= -52 Then
            Dim idx As UInteger = If(e2 < 0, 0UI, indexForExponent(CUInt(e2)))
            Dim p10bits As UInteger = pow10BitsForIndex(idx)
            Dim len As Integer = CInt(lengthForIndex(idx))

            For i As Integer = len - 1 To Nothing Step -1
                Dim j As Integer = CInt(p10bits) - e2
                ' Temporary: j is usually around 128, and by shifting a bit, we push it to 128 or above, which is
                ' a slightly faster code path in mulShift_mod1e9. Instead, we can just increase the multipliers.
                digits = mulShift_mod1e9(m2 << 8, POW10_SPLIT(POW10_OFFSET(CInt(idx)) + i), CInt(j + 8))
                If printedDigits <> Nothing Then
                    If printedDigits + 9 > precision Then
                        availableDigits = 9
                        Exit For
                    End If
                    append_nine_digits(digits, result.Slice(index))
                    index += 9
                    printedDigits += 9
                ElseIf digits <> Nothing Then
                    availableDigits = decimalLength9(digits)
                    exp = i * 9 + CInt(availableDigits) - 1
                    If availableDigits > precision Then
                        Exit For
                    End If
                    If printDecimalPoint Then
                        append_d_digits(availableDigits, digits, result.Slice(index))
                        index += availableDigits + 1 ' +1 for decimal point
                    Else
                        'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
                        'ORIGINAL LINE: result[index++] = (char)("0"c + digits);
                        result(index) = ChrW(AscW("0"c) + CInt(digits))
                        index += 1
                    End If
                    printedDigits = availableDigits
                    availableDigits = 0
                End If
            Next i
        End If

        If e2 < Nothing AndAlso availableDigits = Nothing Then
            Dim idx As Integer = -e2 \ 16

            For i As Integer = MIN_BLOCK_2(idx) To 199
                Dim j As Integer = ADDITIONAL_BITS_2 + (-e2 - 16 * idx)
                Dim p As UInteger = POW10_OFFSET_2(idx) + CUInt(i) - MIN_BLOCK_2(idx)
                ' Temporary: j is usually around 128, and by shifting a bit, we push it to 128 or above, which is
                ' a slightly faster code path in mulShift_mod1e9. Instead, we can just increase the multipliers.
                digits = If(p >= POW10_OFFSET_2(idx + 1), 0UI, mulShift_mod1e9(m2 << 8, POW10_SPLIT_2(CInt(p)), j + 8))

                If printedDigits <> Nothing Then
                    If printedDigits + 9 > precision Then
                        availableDigits = 9
                        Exit For
                    End If
                    append_nine_digits(digits, result.Slice(index))
                    index += 9
                    printedDigits += 9
                ElseIf digits <> Nothing Then
                    availableDigits = decimalLength9(digits)
                    exp = -(i + 1) * 9 + CInt(availableDigits) - 1
                    If availableDigits > precision Then
                        Exit For
                    End If
                    If printDecimalPoint Then
                        append_d_digits(availableDigits, digits, result.Slice(index))
                        index += availableDigits + 1 ' +1 for decimal point
                    Else
                        'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
                        'ORIGINAL LINE: result[index++] = (char)("0"c + digits);
                        result(index) = ChrW(AscW("0"c) + CInt(digits))
                        index += 1
                    End If
                    printedDigits = availableDigits
                    availableDigits = 0
                End If
            Next i
        End If

        Dim maximum As Integer = precision - printedDigits

        If availableDigits = Nothing Then
            digits = 0
        End If
        Dim lastDigit As UInteger = 0
        If availableDigits > maximum Then
            Dim k As UInteger = 0
            Do While k < availableDigits - maximum
                lastDigit = digits Mod 10UI
                digits \= 10UI
                k += 1UI
            Loop
        End If

        ' Nothing = don't round up; 1 = round up unconditionally; 2 = round up if odd.
        Dim roundUp As Integer = 0
        If lastDigit <> 5 Then
            roundUp = Convert.ToInt32(lastDigit > 5)
        Else
            ' Is m * 2^e2 * 10^(precision + 1 - exp) integer?
            ' precision was already increased by 1, so we don't need to write + 1 here.
            Dim rexp As Integer = CInt(precision) - exp
            Dim requiredTwos As Integer = -e2 - rexp
            Dim trailingZeros As Boolean = requiredTwos <= Nothing OrElse (requiredTwos < 60 AndAlso multipleOfPowerOf2(m2, requiredTwos))
            If rexp < Nothing Then
                Dim requiredFives As Integer = -rexp
                trailingZeros = trailingZeros AndAlso multipleOfPowerOf5(m2, CUInt(requiredFives))
            End If
            roundUp = If(trailingZeros, 2, 1)

        End If
        If printedDigits <> Nothing Then
            If digits = Nothing Then
                memset(result.Slice(index), "0"c, maximum)
            Else
                append_c_digits(maximum, digits, result.Slice(index))
            End If
            index += maximum
        Else
            If printDecimalPoint Then
                append_d_digits(maximum, digits, result.Slice(index))
                index += maximum + 1 ' +1 for decimal point
            Else
                'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
                'ORIGINAL LINE: result[index++] = (char)("0"c + digits);
                result(index) = ChrW(AscW("0"c) + CInt(digits))
                index += 1
            End If
        End If

        If roundUp <> Nothing Then
            Dim roundIndex As Integer = index
            Do
                roundIndex -= 1
                If roundIndex = -1 Then
                    result(roundIndex + 1) = "1"c
                    exp += 1
                    Exit Do
                End If
                Dim c As Char = result(roundIndex)
                If c = "-"c Then
                    result(roundIndex + 1) = "1"c
                    exp += 1
                    Exit Do
                End If
                If c = "."c Then
                    Continue Do
                ElseIf c = "9"c Then
                    result(roundIndex) = "0"c
                    roundUp = 1
                    Continue Do
                Else
                    If roundUp = 2 AndAlso AscW(c) Mod 2 = Nothing Then
                        Exit Do
                    End If
                    result(roundIndex) = Convert.ToChar(AscW(c) + 1)
                    Exit Do
                End If
            Loop
        End If

        result(index) = "e"c
        index += 1
        If exp < Nothing Then
            result(index) = "-"c
            index += 1
            exp = -exp
        Else
            result(index) = "+"c
            index += 1
        End If

        If exp >= 100 Then
            Dim c As Integer = exp Mod 10
            memcpy(result.Slice(index), DIGIT_TABLE, 2 * (exp \ 10), 2)
            result(index + 2) = ChrW(AscW("0"c) + c)
            index += 3
        Else
            memcpy(result.Slice(index), DIGIT_TABLE, 2 * exp, 2)
            index += 2
        End If

        Return index
    End Function

#Disable Warning BC40000
    Public Function DoubleToString(f As Double, precision As Integer) As String
        If precision < 0 Then
            Throw New ArgumentOutOfRangeException(NameOf(precision), "Expected [0, 1000]")
        End If

        If precision > 16 Then
            Return DoubleToStringArrayPool(f, precision)
        Else
            Return DoubleToStringRefStruct(f, precision)
        End If
    End Function
#Enable Warning BC40000

    <StructLayout(LayoutKind.Sequential, Size:=24 * 2)>
    Private Structure StackAllocChar24
        Dim FirstValue As Char
    End Structure

    <MethodImpl(MethodImplOptions.NoInlining)> ' DoubleToString should not always stackalloc 24 chars.
    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Function DoubleToStringRefStruct(f As Double, precision As Integer) As String
        Debug.Assert(precision >= 0)
        Debug.Assert(precision <= 16) ' precision + Len("-1.E+000")
        Dim stackAlloc As New StackAllocChar24
        Dim span = MemoryMarshal.CreateSpan(stackAlloc.FirstValue, 24)
        Dim index As Integer = d2exp_buffered_n(f, precision, span)
        Return New String(span.Slice(0, index))
    End Function

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Function DoubleToStringArrayPool(f As Double, precision As Integer) As String
        Dim pool As ArrayPool(Of Char) = ArrayPool(Of Char).Shared
        Dim rented = pool.Rent(precision + 8)
        Dim rentedSpan = rented.AsSpan
        Dim index As Integer = d2exp_buffered_n(f, precision, rented)
        Dim result As New String(rentedSpan.Slice(0, index))
        pool.Return(rented)
        Return result
    End Function
End Module
