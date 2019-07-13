Imports System.Runtime.InteropServices

Public Module D2s
    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Function mulShiftAll(m As ULong, mul As Span(Of ULong), j As Integer, ByRef vp As ULong, ByRef vm As ULong, mmShift As UInteger) As ULong
        m <<= 1
        ' m is maximum 55 bits
        Dim tmp As ULong = Nothing
        Dim lo As ULong = umul128(m, mul(0), tmp)
        Dim hi As ULong = Nothing
        Dim mid As ULong = tmp + umul128(m, mul(1), hi)
        hi += Convert.ToUInt64(mid < tmp) ' overflow into hi

        Dim lo2 As ULong = lo + mul(0)
        Dim mid2 As ULong = mid + mul(1) + Convert.ToUInt64(lo2 < lo)
        Dim hi2 As ULong = hi + Convert.ToUInt64(mid2 < mid)
        vp = shiftright128(mid2, hi2, (j - 64 - 1))

        If mmShift = 1 Then
            Dim lo3 As ULong = lo - mul(0)
            Dim mid3 As ULong = mid - mul(1) - Convert.ToUInt64(lo3 > lo)
            Dim hi3 As ULong = hi - Convert.ToUInt64(mid3 > mid)
            vm = shiftright128(mid3, hi3, (j - 64 - 1))
        Else
            Dim lo3 As ULong = lo + lo
            Dim mid3 As ULong = mid + mid + Convert.ToUInt64(lo3 < lo)
            Dim hi3 As ULong = hi + hi + Convert.ToUInt64(mid3 < mid)
            Dim lo4 As ULong = lo3 - mul(0)
            Dim mid4 As ULong = mid3 - mul(1) - Convert.ToUInt64(lo4 > lo3)
            Dim hi4 As ULong = hi3 - Convert.ToUInt64(mid4 > mid3)
            vm = shiftright128(mid4, hi4, (j - 64))
        End If

        Return shiftright128(mid, hi, (j - 64 - 1))
    End Function

    Private Function decimalLength17(v As ULong) As Integer
        ' This is slightly faster than a loop.
        ' The average output length is 16.38 digits, so we check high-to-low.
        ' Function precondition: v is not an 18, 19, or 20-digit number.
        ' (17 digits are sufficient for round-tripping.)
        Debug.Assert(v < 100000000000000000L)
        If v >= 10000000000000000L Then
            Return 17
        End If
        If v >= 1000000000000000L Then
            Return 16
        End If
        If v >= 100000000000000L Then
            Return 15
        End If
        If v >= 10000000000000L Then
            Return 14
        End If
        If v >= 1000000000000L Then
            Return 13
        End If
        If v >= 100000000000L Then
            Return 12
        End If
        If v >= 10000000000L Then
            Return 11
        End If
        If v >= 1000000000L Then
            Return 10
        End If
        If v >= 100000000L Then
            Return 9
        End If
        If v >= 10000000L Then
            Return 8
        End If
        If v >= 1000000L Then
            Return 7
        End If
        If v >= 100000L Then
            Return 6
        End If
        If v >= 10000L Then
            Return 5
        End If
        If v >= 1000L Then
            Return 4
        End If
        If v >= 100L Then
            Return 3
        End If
        If v >= 10L Then
            Return 2
        End If
        Return 1
    End Function

    ' A floating decimal representing m * 10^e.
    Private Structure floating_decimal_64
        Public mantissa As ULong
        ' Decimal exponent's range is -324 to 308
        ' inclusive, and can fit in a short if needed.
        Public exponent As Integer
    End Structure

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Function d2d(ieeeMantissa As ULong, ieeeExponent As UInteger) As floating_decimal_64
        Dim e2 As Integer
        Dim m2 As ULong
        If ieeeExponent = 0 Then
            ' We subtract 2 so that the bounds computation has 2 additional bits.
            e2 = 1 - DOUBLE_BIAS - DOUBLE_MANTISSA_BITS - 2
            m2 = ieeeMantissa
        Else
            e2 = CInt(ieeeExponent) - DOUBLE_BIAS - DOUBLE_MANTISSA_BITS - 2
            m2 = (1UL << DOUBLE_MANTISSA_BITS) Or ieeeMantissa
        End If
        Dim even As Boolean = (m2 And 1UL) = 0
        Dim acceptBounds As Boolean = even

        ' Step 2: Determine the interval of valid decimal representations.
        Dim mv As ULong = 4UL * m2
        ' Implicit bool -> int conversion. True is 1, false is 0.
        Dim mmShift As UInteger = Convert.ToUInt32(ieeeMantissa <> 0 OrElse ieeeExponent <= 1)
        ' We would compute mp and mm like this:
        ' ulong mp = 4 * m2 + 2;
        ' ulong mm = mv - 1 - mmShift;

        ' Step 3: Convert to a decimal power base using 128-bit arithmetic.
        Dim vr, vp, vm As ULong
        Dim e10 As Integer
        Dim vmIsTrailingZeros As Boolean = False
        Dim vrIsTrailingZeros As Boolean = False
        If e2 >= 0 Then
            ' I tried special-casing q == 0, but there was no effect on performance.
            ' This expression is slightly faster than max(0, log10Pow2(e2) - 1).
            Dim q As UInteger = log10Pow2(e2) - Convert.ToUInt32(e2 > 3)
            e10 = CInt(q)
            Dim k As Integer = DOUBLE_POW5_INV_BITCOUNT + pow5bits(CInt(q)) - 1
            Dim i As Integer = -e2 + CInt(q) + k

            vr = mulShiftAll(m2, DOUBLE_POW5_INV_SPLIT(CInt(q)), i, vp, vm, mmShift)

            If q <= 21 Then
                ' This should use q <= 22, but I think 21 is also safe. Smaller values
                ' may still be safe, but it's more difficult to reason about them.
                ' Only one of mp, mv, and mm can be a multiple of 5, if any.
                Dim mvMod5 As UInteger = (CUInt(mv)) - 5UI * (CUInt(div5(mv)))
                If mvMod5 = 0 Then
                    vrIsTrailingZeros = multipleOfPowerOf5(mv, q)
                ElseIf acceptBounds Then
                    ' Same as min(e2 + (~mm & 1), pow5Factor(mm)) >= q
                    ' <=> e2 + (~mm & 1) >= q && pow5Factor(mm) >= q
                    ' <=> true && pow5Factor(mm) >= q, since e2 >= q.
                    vmIsTrailingZeros = multipleOfPowerOf5(mv - 1UL - mmShift, q)
                Else
                    ' Same as min(e2 + 1, pow5Factor(mp)) >= q.
                    vp -= Convert.ToUInt64(multipleOfPowerOf5(mv + 2UL, q))
                End If
            End If
        Else
            ' This expression is slightly faster than max(0, log10Pow5(-e2) - 1).
            Dim q As UInteger = log10Pow5(-e2) - Convert.ToUInt32(-e2 > 1)
            e10 = CInt(q) + e2
            Dim i As Integer = -e2 - CInt(q)
            Dim k As Integer = pow5bits(i) - DOUBLE_POW5_BITCOUNT
            Dim j As Integer = CInt(q) - k

            vr = mulShiftAll(m2, DOUBLE_POW5_SPLIT(i), j, vp, vm, mmShift)

            If q <= 1 Then
                ' {vr,vp,vm} is trailing zeros if {mv,mp,mm} has at least q trailing 0 bits.
                ' mv = 4 * m2, so it always has at least two trailing 0 bits.
                vrIsTrailingZeros = True
                If acceptBounds Then
                    ' mm = mv - 1 - mmShift, so it has 1 trailing 0 bit iff mmShift == 1.
                    vmIsTrailingZeros = mmShift = 1
                Else
                    ' mp = mv + 2, so it always has at least one trailing 0 bit.
                    vp -= 1UL
                End If
            ElseIf q < 63 Then ' TODO(ulfjack): Use a tighter bound here.
                ' We want to know if the full product has at least q trailing zeros.
                ' We need to compute min(p2(mv), p5(mv) - e2) >= q
                ' <=> p2(mv) >= q && p5(mv) - e2 >= q
                ' <=> p2(mv) >= q (because -e2 >= q)
                vrIsTrailingZeros = multipleOfPowerOf2(mv, CInt(q))

            End If
        End If

        ' Step 4: Find the shortest decimal representation in the interval of valid representations.
        Dim removed As Integer = 0
        Dim lastRemovedDigit As Byte = 0
        Dim output As ULong
        ' On average, we remove ~2 digits.
        If vmIsTrailingZeros OrElse vrIsTrailingZeros Then
            ' General case, which happens rarely (~0.7%).
            Do
                Dim vpDiv10 As ULong = div10(vp)
                Dim vmDiv10 As ULong = div10(vm)
                If vpDiv10 <= vmDiv10 Then
                    Exit Do
                End If
                Dim vmMod10 As UInteger = (CUInt(vm)) - 10UI * (CUInt(vmDiv10))
                Dim vrDiv10 As ULong = div10(vr)
                Dim vrMod10 As UInteger = (CUInt(vr)) - 10UI * (CUInt(vrDiv10))
                vmIsTrailingZeros = vmIsTrailingZeros And vmMod10 = 0
                vrIsTrailingZeros = vrIsTrailingZeros And lastRemovedDigit = 0
                lastRemovedDigit = CByte(vrMod10)
                vr = vrDiv10
                vp = vpDiv10
                vm = vmDiv10
                removed += 1
            Loop

            If vmIsTrailingZeros Then
                Do
                    Dim vmDiv10 As ULong = div10(vm)
                    Dim vmMod10 As UInteger = (CUInt(vm)) - 10UI * (CUInt(vmDiv10))
                    If vmMod10 <> 0 Then
                        Exit Do
                    End If
                    Dim vpDiv10 As ULong = div10(vp)
                    Dim vrDiv10 As ULong = div10(vr)
                    Dim vrMod10 As UInteger = (CUInt(vr)) - 10UI * (CUInt(vrDiv10))
                    vrIsTrailingZeros = vrIsTrailingZeros And lastRemovedDigit = 0
                    lastRemovedDigit = CByte(vrMod10)
                    vr = vrDiv10
                    vp = vpDiv10
                    vm = vmDiv10
                    removed += 1
                Loop
            End If

            If vrIsTrailingZeros AndAlso lastRemovedDigit = 5 AndAlso vr Mod 2 = 0 Then
                ' Round even if the exact number is .....50..0.
                lastRemovedDigit = 4
            End If
            ' We need to take vr + 1 if vr is outside bounds or we need to round up.
            output = vr + Convert.ToUInt64((vr = vm AndAlso (Not acceptBounds OrElse Not vmIsTrailingZeros)) OrElse lastRemovedDigit >= 5)
        Else
            ' Specialized for the common case (~99.3%). Percentages below are relative to this.
            Dim roundUp As Boolean = False
            Dim vpDiv100 As ULong = div100(vp)
            Dim vmDiv100 As ULong = div100(vm)
            If vpDiv100 > vmDiv100 Then ' Optimization: remove two digits at a time (~86.2%).
                Dim vrDiv100 As ULong = div100(vr)
                Dim vrMod100 As UInteger = (CUInt(vr)) - 100UI * (CUInt(vrDiv100))
                roundUp = vrMod100 >= 50
                vr = vrDiv100
                vp = vpDiv100
                vm = vmDiv100
                removed += 2
            End If
            ' Loop iterations below (approximately), without optimization above:
            ' 0: 0.03%, 1: 13.8%, 2: 70.6%, 3: 14.0%, 4: 1.40%, 5: 0.14%, 6+: 0.02%
            ' Loop iterations below (approximately), with optimization above:
            ' 0: 70.6%, 1: 27.8%, 2: 1.40%, 3: 0.14%, 4+: 0.02%
            Do
                Dim vpDiv10 As ULong = div10(vp)
                Dim vmDiv10 As ULong = div10(vm)
                If vpDiv10 <= vmDiv10 Then
                    Exit Do
                End If
                Dim vrDiv10 As ULong = div10(vr)
                Dim vrMod10 As UInteger = (CUInt(vr)) - 10UI * (CUInt(vrDiv10))
                roundUp = vrMod10 >= 5
                vr = vrDiv10
                vp = vpDiv10
                vm = vmDiv10
                removed += 1
            Loop

            ' We need to take vr + 1 if vr is outside bounds or we need to round up.
            output = vr + Convert.ToUInt64(vr = vm OrElse roundUp)
        End If
        Dim exp As Integer = e10 + removed

        Dim fd As floating_decimal_64
        fd.exponent = exp
        fd.mantissa = output
        Return fd
    End Function

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Function to_chars(v As floating_decimal_64, sign As Boolean, result As Span(Of Char)) As Integer
        ' Step 5: Print the decimal representation.
        Dim index As Integer = 0
        If sign Then
            'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
            'ORIGINAL LINE: result[index++] = "-"c;
            result(index) = "-"c
            index += 1
        End If

        Dim output As ULong = v.mantissa
        Dim olength As Integer = decimalLength17(output)

        ' Print the decimal digits.
        ' The following code is equivalent to:
        ' for (uint i = 0; i < olength - 1; ++i) {
        '    uint c = output % 10; output /= 10;
        '   result[index + olength - i] = (char) ('0' + c);
        ' }
        ' result[index] = '0' + output % 10;

        Dim i As Integer = 0
        ' We prefer 32-bit operations, even on 64-bit platforms.
        ' We have at most 17 digits, and uint can store 9 digits.
        ' If output doesn't fit into uint, we cut off 8 digits,
        ' so the rest will fit into uint.
        Dim output2 As UInteger
        If (output >> 32) <> 0 Then
            ' Expensive 64-bit division.
            Dim q As ULong = div1e8(output)
            output2 = (CUInt(output)) - 100000000UI * (CUInt(q))
            output = q

            Dim c As UInteger = output2 Mod 10000UI
            output2 \= 10000UI
            Dim d As UInteger = output2 Mod 10000UI
            Dim c0 As UInteger = (c Mod 100UI) << 1UI
            Dim c1 As UInteger = (c \ 100UI) << 1UI
            Dim d0 As UInteger = (d Mod 100UI) << 1UI
            Dim d1 As UInteger = (d \ 100UI) << 1UI
            memcpy(result.Slice(index + olength - i - 1), DIGIT_TABLE, c0, 2)
            memcpy(result.Slice(index + olength - i - 3), DIGIT_TABLE, c1, 2)
            memcpy(result.Slice(index + olength - i - 5), DIGIT_TABLE, d0, 2)
            memcpy(result.Slice(index + olength - i - 7), DIGIT_TABLE, d1, 2)
            i += 8
        End If
        output2 = CUInt(output)
        Do While output2 >= 10000
            Dim c As UInteger = output2 Mod 10000UI
            output2 \= 10000UI
            Dim c0 As UInteger = (c Mod 100UI) << 1UI
            Dim c1 As UInteger = (c \ 100UI) << 1UI
            memcpy(result.Slice(index + olength - i - 1), DIGIT_TABLE, c0, 2)
            memcpy(result.Slice(index + olength - i - 3), DIGIT_TABLE, c1, 2)
            i += 4
        Loop
        If output2 >= 100 Then
            Dim c As UInteger = (output2 Mod 100UI) << 1UI
            output2 \= 100UI
            memcpy(result.Slice(index + olength - i - 1), DIGIT_TABLE, c, 2)
            i += 2
        End If
        If output2 >= 10 Then
            Dim c As UInteger = output2 << 1
            ' We can't use memcpy here: the decimal dot goes between these two digits.
            result(index + olength - i) = DIGIT_TABLE(CInt(c + 1UI))
            result(index) = DIGIT_TABLE(CInt(c))
        Else
            result(index) = ChrW(AscW("0"c) + CInt(output2))
        End If

        ' Print decimal point if needed.
        If olength > 1 Then
            result(index + 1) = "."c
            index += olength + 1
        Else
            index += 1
        End If

        ' Print the exponent.
        'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
        'ORIGINAL LINE: result[index++] = "E"c;
        result(index) = "E"c
        index += 1
        Dim exp As Integer = v.exponent + CInt(olength) - 1
        If exp < 0 Then
            'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
            'ORIGINAL LINE: result[index++] = "-"c;
            result(index) = "-"c
            index += 1
            exp = -exp
        End If

        If exp >= 100 Then
            Dim c As Integer = exp Mod 10
            memcpy(result.Slice(index), DIGIT_TABLE, 2 * (exp \ 10), 2)
            result(index + 2) = ChrW(AscW("0"c) + c)
            index += 3
        ElseIf exp >= 10 Then
            memcpy(result.Slice(index), DIGIT_TABLE, 2 * exp, 2)
            index += 2
        Else
            'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
            'ORIGINAL LINE: result[index++] = (char)("0"c + exp);
            result(index) = ChrW(AscW("0"c) + exp)
            index += 1
        End If

        Return index
    End Function

    Private Function d2d_small_int(ieeeMantissa As ULong, ieeeExponent As UInteger, ByRef v As floating_decimal_64) As Boolean
        Dim m2 As ULong = (1UL << DOUBLE_MANTISSA_BITS) Or ieeeMantissa
        Dim e2 As Integer = CInt(ieeeExponent) - DOUBLE_BIAS - DOUBLE_MANTISSA_BITS

        If e2 > 0 Then
            ' f = m2 * 2^e2 >= 2^53 is an integer.
            ' Ignore this case for now.
            Return False
        End If

        If e2 < -52 Then
            ' f < 1.
            Return False
        End If

        ' Since 2^52 <= m2 < 2^53 and 0 <= -e2 <= 52: 1 <= f = m2 / 2^-e2 < 2^53.
        ' Test if the lower -e2 bits of the significand are 0, i.e. whether the fraction is 0.
        Dim mask As ULong = (1UL << -e2) - 1UL
        Dim fraction As ULong = m2 And mask
        If fraction <> 0 Then
            Return False
        End If

        ' f is an integer in the range [1, 2^53).
        ' Note: mantissa might contain trailing (decimal) 0's.
        ' Note: since 2^53 < 10^16, there is no need to adjust decimalLength17().
        v.mantissa = m2 >> -e2
        v.exponent = 0
        Return True
    End Function

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Function d2s_buffered_n(f As Double, result As Span(Of Char)) As Integer
        ' Step 1: Decode the floating-point number, and unify normalized and subnormal cases.
        Dim bits As ULong = double_to_bits(f)

        ' Decode bits into sign, mantissa, and exponent.
        Dim ieeeSign As Boolean = ((bits >> (DOUBLE_MANTISSA_BITS + DOUBLE_EXPONENT_BITS)) And 1UL) <> 0
        Dim ieeeMantissa As ULong = bits And ((1UL << DOUBLE_MANTISSA_BITS) - 1UL)
        Dim ieeeExponent As UInteger = CUInt((bits >> DOUBLE_MANTISSA_BITS) And ((1UI << DOUBLE_EXPONENT_BITS) - 1UL))
        ' Case distinction; exit early for the easy cases.
        If ieeeExponent = ((1UI << DOUBLE_EXPONENT_BITS) - 1UI) OrElse (ieeeExponent = 0 AndAlso ieeeMantissa = 0) Then
            Return copy_special_str(result, ieeeSign, Convert.ToBoolean(ieeeExponent), Convert.ToBoolean(ieeeMantissa))
        End If

        Dim v As floating_decimal_64 = Nothing
        Dim isSmallInt As Boolean = d2d_small_int(ieeeMantissa, ieeeExponent, v)
        If isSmallInt Then
            ' For small integers in the range [1, 2^53), v.mantissa might contain trailing (decimal) zeros.
            ' For scientific notation we need to move these zeros into the exponent.
            ' (This is not needed for fixed-point notation, so it might be beneficial to trim
            ' trailing zeros in to_chars only if needed - once fixed-point notation output is implemented.)
            Do
                Dim q As ULong = div10(v.mantissa)
                Dim r As UInteger = (CUInt(v.mantissa)) - 10UI * (CUInt(q))
                If r <> 0 Then
                    Exit Do
                End If
                v.mantissa = q
                v.exponent += 1
            Loop
        Else
            v = d2d(ieeeMantissa, ieeeExponent)
        End If

        Return to_chars(v, ieeeSign, result)
    End Function

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Sub d2s_buffered(f As Double, result As Span(Of Char))
        Dim index As Integer = d2s_buffered_n(f, result)

        ' Terminate the string.
        result(index) = Nothing
    End Sub

    <StructLayout(LayoutKind.Sequential, Size:=24 * 2)>
    Private Structure StackAllocChar24
        Dim FirstValue As Char
    End Structure

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Public Function ConvertDoubleToString(f As Double) As String
        'INSTANT VB TODO TASK: There is no equivalent to 'stackalloc' in VB:
        Dim allocated As StackAllocChar24
        Dim result As Span(Of Char) = MemoryMarshal.CreateSpan(allocated.FirstValue, 24)
        Dim index As Integer = d2s_buffered_n(f, result)

        Return New String(result.Slice(0, index))
    End Function
End Module
