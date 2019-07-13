﻿Imports System.Runtime.InteropServices

Public Module F2s
    Private Const FLOAT_MANTISSA_BITS As Integer = 23
    Private Const FLOAT_EXPONENT_BITS As Integer = 8
    Private Const FLOAT_BIAS As Integer = 127

    ' This table is generated by PrintFloatLookupTable.
    Private Const FLOAT_POW5_INV_BITCOUNT As Integer = 59
    Private FLOAT_POW5_INV_SPLIT() As ULong = {576460752303423489UL, 461168601842738791UL, 368934881474191033UL, 295147905179352826UL, 472236648286964522UL, 377789318629571618UL, 302231454903657294UL, 483570327845851670UL, 386856262276681336UL, 309485009821345069UL, 495176015714152110UL, 396140812571321688UL, 316912650057057351UL, 507060240091291761UL, 405648192073033409UL, 324518553658426727UL, 519229685853482763UL, 415383748682786211UL, 332306998946228969UL, 531691198313966350UL, 425352958651173080UL, 340282366920938464UL, 544451787073501542UL, 435561429658801234UL, 348449143727040987UL, 557518629963265579UL, 446014903970612463UL, 356811923176489971UL, 570899077082383953UL, 456719261665907162UL, 365375409332725730UL}
    Private Const FLOAT_POW5_BITCOUNT As Integer = 61
    Private FLOAT_POW5_SPLIT() As ULong = {1152921504606846976UL, 1441151880758558720UL, 1801439850948198400UL, 2251799813685248000UL, 1407374883553280000UL, 1759218604441600000UL, 2199023255552000000UL, 1374389534720000000UL, 1717986918400000000UL, 2147483648000000000UL, 1342177280000000000UL, 1677721600000000000UL, 2097152000000000000UL, 1310720000000000000UL, 1638400000000000000UL, 2048000000000000000UL, 1280000000000000000UL, 1600000000000000000UL, 2000000000000000000UL, 1250000000000000000UL, 1562500000000000000UL, 1953125000000000000UL, 1220703125000000000UL, 1525878906250000000UL, 1907348632812500000UL, 1192092895507812500UL, 1490116119384765625UL, 1862645149230957031UL, 1164153218269348144UL, 1455191522836685180UL, 1818989403545856475UL, 2273736754432320594UL, 1421085471520200371UL, 1776356839400250464UL, 2220446049250313080UL, 1387778780781445675UL, 1734723475976807094UL, 2168404344971008868UL, 1355252715606880542UL, 1694065894508600678UL, 2117582368135750847UL, 1323488980084844279UL, 1654361225106055349UL, 2067951531382569187UL, 1292469707114105741UL, 1615587133892632177UL, 2019483917365790221UL}

    Private Function pow5Factor(value As UInteger) As UInteger
        Dim count As UInteger = 0
        Do
            Debug.Assert(value <> Nothing)
            Dim q As UInteger = value \ 5UI
            Dim r As UInteger = value Mod 5UI
            If r <> 0 Then
                Exit Do
            End If
            value = q
            count += 1UI
        Loop
        Return count
    End Function

    ' Returns true if value is divisible by 5^p.
    Private Function multipleOfPowerOf5(value As UInteger, p As UInteger) As Boolean
        Return pow5Factor(value) >= p
    End Function

    ' Returns true if value is divisible by 2^p.
    Private Function multipleOfPowerOf2(value As UInteger, p As UInteger) As Boolean
        ' return __builtin_ctz(value) >= p;
        Return (value And ((1UI << CInt(p)) - 1)) = 0
    End Function

    ' It seems to be slightly faster to avoid uint128_t here, although the
    ' generated code for uint128_t looks slightly nicer.
    Private Function mulShift(m As UInteger, factor As ULong, shift As Integer) As UInteger
        Debug.Assert(shift > 32)

        ' The casts here help MSVC to avoid calls to the __allmul library
        ' function.
        Dim factorLo As UInteger = CUInt(factor)
        Dim factorHi As UInteger = CUInt(factor >> 32)
        Dim bits0 As ULong = CULng(m) * factorLo
        Dim bits1 As ULong = CULng(m) * factorHi

        Dim sum As ULong = (bits0 >> 32) + bits1
        Dim shiftedSum As ULong = sum >> (shift - 32)
        Debug.Assert(shiftedSum <= UInteger.MaxValue)
        Return CUInt(shiftedSum)
    End Function

    Private Function mulPow5InvDivPow2(m As UInteger, q As UInteger, j As Integer) As UInteger
        Return mulShift(m, FLOAT_POW5_INV_SPLIT(CInt(q)), j)
    End Function

    Private Function mulPow5divPow2(m As UInteger, i As UInteger, j As Integer) As UInteger
        Return mulShift(m, FLOAT_POW5_SPLIT(CInt(i)), j)
    End Function

    ' A floating decimal representing m * 10^e.
    Private Structure floating_decimal_32
        Public mantissa As UInteger
        ' Decimal exponent's range is -45 to 38
        ' inclusive, and can fit in a short if needed.
        Public exponent As Integer
    End Structure

    Private Function f2d(ieeeMantissa As UInteger, ieeeExponent As UInteger) As floating_decimal_32
        Dim e2 As Integer
        Dim m2 As UInteger
        If ieeeExponent = 0 Then
            ' We subtract 2 so that the bounds computation has 2 additional bits.
            e2 = 1 - FLOAT_BIAS - FLOAT_MANTISSA_BITS - 2
            m2 = ieeeMantissa
        Else
            e2 = CInt(ieeeExponent) - FLOAT_BIAS - FLOAT_MANTISSA_BITS - 2
            m2 = (1UI << FLOAT_MANTISSA_BITS) Or ieeeMantissa
        End If
        Dim even As Boolean = (m2 And 1) = 0
        Dim acceptBounds As Boolean = even

        ' Step 2: Determine the interval of valid decimal representations.
        Dim mv As UInteger = 4UI * m2
        Dim mp As UInteger = 4UI * m2 + 2UI
        ' Implicit bool -> int conversion. True is 1, false is 0.
        Dim mmShift As UInteger = Convert.ToUInt32(ieeeMantissa <> 0 OrElse ieeeExponent <= 1)
        Dim mm As UInteger = 4UI * m2 - 1UI - mmShift

        ' Step 3: Convert to a decimal power base using 64-bit arithmetic.
        Dim vr, vp, vm As UInteger
        Dim e10 As Integer
        Dim vmIsTrailingZeros As Boolean = False
        Dim vrIsTrailingZeros As Boolean = False
        Dim lastRemovedDigit As Byte = 0
        If e2 >= 0 Then
            Dim q As UInteger = log10Pow2(e2)
            e10 = CInt(q)
            Dim k As Integer = FLOAT_POW5_INV_BITCOUNT + pow5bits(CInt(q)) - 1
            Dim i As Integer = -e2 + CInt(q) + k
            vr = mulPow5InvDivPow2(mv, q, i)
            vp = mulPow5InvDivPow2(mp, q, i)
            vm = mulPow5InvDivPow2(mm, q, i)

            If q <> 0 AndAlso (vp - 1) \ 10 <= vm \ 10 Then
                ' We need to know one removed digit even if we are not going to loop below. We could use
                ' q = X - 1 above, except that would require 33 bits for the result, and we've found that
                ' 32-bit arithmetic is faster even on 64-bit machines.
                Dim l As Integer = FLOAT_POW5_INV_BITCOUNT + pow5bits(CInt(q - 1)) - 1
                lastRemovedDigit = CByte(mulPow5InvDivPow2(mv, q - 1UI, -e2 + CInt(q) - 1 + l) Mod 10)
            End If
            If q <= 9 Then
                ' The largest power of 5 that fits in 24 bits is 5^10, but q <= 9 seems to be safe as well.
                ' Only one of mp, mv, and mm can be a multiple of 5, if any.
                If mv Mod 5 = 0 Then
                    vrIsTrailingZeros = multipleOfPowerOf5(mv, q)
                ElseIf acceptBounds Then
                    vmIsTrailingZeros = multipleOfPowerOf5(mm, q)
                Else
                    vp -= Convert.ToUInt32(multipleOfPowerOf5(mp, q))
                End If
            End If
        Else
            Dim q As UInteger = log10Pow5(-e2)
            e10 = CInt(q) + e2
            Dim i As Integer = -e2 - CInt(q)
            Dim k As Integer = pow5bits(i) - FLOAT_POW5_BITCOUNT
            Dim j As Integer = CInt(q) - k
            vr = mulPow5divPow2(mv, CUInt(i), j)
            vp = mulPow5divPow2(mp, CUInt(i), j)
            vm = mulPow5divPow2(mm, CUInt(i), j)

            If q <> 0 AndAlso (vp - 1) \ 10 <= vm \ 10 Then
                j = CInt(q) - 1 - (pow5bits(i + 1) - FLOAT_POW5_BITCOUNT)
                lastRemovedDigit = CByte(mulPow5divPow2(mv, CUInt(i + 1), j) Mod 10)
            End If
            If q <= 1 Then
                ' {vr,vp,vm} is trailing zeros if {mv,mp,mm} has at least q trailing 0 bits.
                ' mv = 4 * m2, so it always has at least two trailing 0 bits.
                vrIsTrailingZeros = True
                If acceptBounds Then
                    ' mm = mv - 1 - mmShift, so it has 1 trailing 0 bit iff mmShift == 1.
                    vmIsTrailingZeros = mmShift = 1
                Else
                    ' mp = mv + 2, so it always has at least one trailing 0 bit.
                    vp -= 1UI
                End If
            ElseIf q < 31 Then ' TODO(ulfjack): Use a tighter bound here.
                vrIsTrailingZeros = multipleOfPowerOf2(mv, q - 1UI)

            End If
        End If

        ' Step 4: Find the shortest decimal representation in the interval of valid representations.
        Dim removed As Integer = 0
        Dim output As UInteger
        If vmIsTrailingZeros OrElse vrIsTrailingZeros Then
            ' General case, which happens rarely (~4.0%).
            Do While vp \ 10 > vm \ 10
                vmIsTrailingZeros = vmIsTrailingZeros And vm Mod 10 = 0
                vrIsTrailingZeros = vrIsTrailingZeros And lastRemovedDigit = 0
                lastRemovedDigit = CByte(vr Mod 10)
                vr \= 10UI
                vp \= 10UI
                vm \= 10UI
                removed += 1
            Loop

            If vmIsTrailingZeros Then
                Do While vm Mod 10 = 0
                    vrIsTrailingZeros = vrIsTrailingZeros And lastRemovedDigit = 0
                    lastRemovedDigit = CByte(vr Mod 10)
                    vr \= 10UI
                    vp \= 10UI
                    vm \= 10UI
                    removed += 1
                Loop
            End If
            If vrIsTrailingZeros AndAlso lastRemovedDigit = 5 AndAlso vr Mod 2 = 0 Then
                ' Round even if the exact number is .....50..0.
                lastRemovedDigit = 4
            End If
            ' We need to take vr + 1 if vr is outside bounds or we need to round up.
            output = vr + Convert.ToUInt32((vr = vm AndAlso (Not acceptBounds OrElse Not vmIsTrailingZeros)) OrElse lastRemovedDigit >= 5)
        Else
            ' Specialized for the common case (~96.0%). Percentages below are relative to this.
            ' Loop iterations below (approximately):
            ' 0: 13.6%, 1: 70.7%, 2: 14.1%, 3: 1.39%, 4: 0.14%, 5+: 0.01%
            Do While vp \ 10 > vm \ 10
                lastRemovedDigit = CByte(vr Mod 10)
                vr \= 10UI
                vp \= 10UI
                vm \= 10UI
                removed += 1
            Loop
            ' We need to take vr + 1 if vr is outside bounds or we need to round up.
            output = vr + Convert.ToUInt32(vr = vm OrElse lastRemovedDigit >= 5)
        End If
        Dim exp As Integer = e10 + removed

        Dim fd As floating_decimal_32
        fd.exponent = exp
        fd.mantissa = output
        Return fd
    End Function

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Function to_chars(v As floating_decimal_32, sign As Boolean, result As Span(Of Char)) As Integer
        ' Step 5: Print the decimal representation.
        Dim index As Integer = 0
        If sign Then
            'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
            'ORIGINAL LINE: result[index++] = "-"c;
            result(index) = "-"c
            index += 1
        End If

        Dim output As UInteger = v.mantissa
        Dim olength As Integer = decimalLength9(output)

        ' Print the decimal digits.
        ' The following code is equivalent to:
        ' for (uint i = 0; i < olength - 1; ++i) {
        '    uint c = output % 10; output /= 10;
        '   result[index + olength - i] = (char) ('0' + c);
        ' }
        ' result[index] = '0' + output % 10;
        Dim i As Integer = 0
        Do While output >= 10000
            Dim c As UInteger = output Mod 10000UI
            output \= 10000UI
            Dim c0 As UInteger = (c Mod 100UI) << 1UI
            Dim c1 As UInteger = (c \ 100UI) << 1UI
            memcpy(result.Slice(index + olength - i - 1), DIGIT_TABLE, c0, 2)
            memcpy(result.Slice(index + olength - i - 3), DIGIT_TABLE, c1, 2)
            i += 4
        Loop
        If output >= 100 Then
            Dim c As UInteger = (output Mod 100UI) << 1UI
            output \= 100UI
            memcpy(result.Slice(index + olength - i - 1), DIGIT_TABLE, c, 2)
            i += 2
        End If
        If output >= 10 Then
            Dim c As UInteger = output << 1
            ' We can't use memcpy here: the decimal dot goes between these two digits.
            result(index + olength - i) = DIGIT_TABLE(CInt(c + 1UI))
            result(index) = DIGIT_TABLE(CInt(c))
        Else
            result(index) = ChrW(AscW("0"c) + CInt(output))
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

        If exp >= 10 Then
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

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Function f2s_buffered_n(f As Single, result As Span(Of Char)) As Integer
        ' Step 1: Decode the floating-point number, and unify normalized and subnormal cases.
        Dim bits As UInteger = float_to_bits(f)

        ' Decode bits into sign, mantissa, and exponent.
        Dim ieeeSign As Boolean = ((bits >> (FLOAT_MANTISSA_BITS + FLOAT_EXPONENT_BITS)) And 1) <> 0
        Dim ieeeMantissa As UInteger = bits And ((1UI << FLOAT_MANTISSA_BITS) - 1UI)
        Dim ieeeExponent As UInteger = (bits >> FLOAT_MANTISSA_BITS) And ((1UI << FLOAT_EXPONENT_BITS) - 1UI)

        ' Case distinction; exit early for the easy cases.
        If ieeeExponent = ((1UI << FLOAT_EXPONENT_BITS) - 1UI) OrElse (ieeeExponent = 0 AndAlso ieeeMantissa = Nothing) Then
            Return copy_special_str(result, ieeeSign, Convert.ToBoolean(ieeeExponent), Convert.ToBoolean(ieeeMantissa))
        End If

        Dim v As floating_decimal_32 = f2d(ieeeMantissa, ieeeExponent)
        Return to_chars(v, ieeeSign, result)
    End Function

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Sub f2s_buffered(f As Single, result As Span(Of Char))
        Dim index As Integer = f2s_buffered_n(f, result)

        ' Terminate the string.
        result(index) = Nothing
    End Sub

    <StructLayout(LayoutKind.Sequential, Size:=16 * 2)>
    Private Structure StackAllocChar16
        Dim FirstValue As Char
    End Structure

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Public Function ConvertSingleToString(f As Single) As String
        Dim allocated As StackAllocChar16
        Dim result As Span(Of Char) = MemoryMarshal.CreateSpan(allocated.FirstValue, 16)
        Dim index As Integer = f2s_buffered_n(f, result)

        Return New String(result.Slice(0, index))
    End Function
End Module
