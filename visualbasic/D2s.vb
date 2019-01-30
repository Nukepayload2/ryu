Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Module DoubleToString ' D2s
    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Private Function Pow5Factor(value As ULong) As UInteger
        Dim count As UInteger = 0
        Do
            Dim q As ULong = div5(value)
            Dim r As UInteger = CUInt(value - 5UL * q)
            If r <> 0UI Then
                Exit Do
            End If
            value = q
            count += 1UI
        Loop
        Return count
    End Function

    ' Returns true if value is divisible by 5^p.
    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Private Function MultipleOfPowerOf5(value As ULong, p As UInteger) As Boolean
        ' I tried a case distinction on p, but there was no performance difference.
        Return Pow5Factor(value) >= p
    End Function

    ' Returns true if value is divisible by 2^p.
    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Private Function MultipleOfPowerOf2(value As ULong, p As Integer) As Boolean
        ' return __builtin_ctzll(value) >= p;
        Return (value And ((1UL << p) - 1UL)) = 0UL
    End Function

    ' We need a 64x128-bit multiplication and a subsequent 128-bit shift.
    ' Multiplication:
    '   The 64-bit factor is variable and passed in, the 128-bit factor comes
    '   from a lookup table. We know that the 64-bit factor only has 55
    '   significant bits (i.e., the 9 topmost bits are zeros). The 128-bit
    '   factor only has 124 significant bits (i.e., the 4 topmost bits are
    '   zeros).
    ' Shift:
    '   In principle, the multiplication result requires 55 + 124 = 179 bits to
    '   represent. However, we then shift this value to the right by j, which is
    '   at least j >= 115, so the result is guaranteed to fit into 179 - 115 = 64
    '   bits. This means that we only need the topmost 64 significant bits of
    '   the 64x128-bit multiplication.
    '
    ' There are several ways to do this:
    ' 1. Best case: the compiler exposes a 128-bit type.
    '    We perform two 64x64-bit multiplications, add the higher 64 bits of the
    '    lower result to the higher result, and shift by j - 64 bits.
    '
    '    We explicitly cast from 64-bit to 128-bit, so the compiler can tell
    '    that these are only 64-bit inputs, and can map these to the best
    '    possible sequence of assembly instructions.
    '    x64 machines happen to have matching assembly instructions for
    '    64x64-bit multiplications and 128-bit shifts.
    '
    ' 2. Second best case: the compiler exposes intrinsics for the x64 assembly
    '    instructions mentioned in 1.
    '
    ' 3. We only have 64x64 bit instructions that return the lower 64 bits of
    '    the result, i.e., we have to use plain C.
    '    Our inputs are less than the full width, so we have three options:
    '    a. Ignore this fact and just implement the intrinsics manually.
    '    b. Split both into 31-bit pieces, which guarantees no internal overflow,
    '       but requires extra work upfront (unless we change the lookup table).
    '    c. Split only the first factor into 31-bit pieces, which also guarantees
    '       no internal overflow, but requires extra work since the intermediate
    '       results are not perfectly aligned.
#If HAS_UINT128 Then

' Best case: use 128-bit type.
Private inline Function mulShift(m As ULong, mul As ULong*, j As Integer) As ULong
  Dim b0 As uint128_t = (CType(m, uint128_t)) * mul(0)
  Dim b2 As uint128_t = (CType(m, uint128_t)) * mul(1)
  Return CULng(((b0 >> 64) + b2) >> (j - 64))
End Function

Private inline Function mulShiftAll(m As ULong, mul As ULong*, j As Integer, vp As ULong*, vm As ULong*, mmShift As UInteger) As ULong
'  m <<= 2;
'  uint128_t b0 = ((uint128_t) m) * mul[0]; // 0
'  uint128_t b2 = ((uint128_t) m) * mul[1]; // 64
'
'  uint128_t hi = (b0 >> 64) + b2;
'  uint128_t lo = b0 & 0xffffffffffffffffull;
'  uint128_t factor = (((uint128_t) mul[1]) << 64) + mul[0];
'  uint128_t vpLo = lo + (factor << 1);
'  *vp = (ulong) ((hi + (vpLo >> 64)) >> (j - 64));
'  uint128_t vmLo = lo - (factor << mmShift);
'  *vm = (ulong) ((hi + (vmLo >> 64) - (((uint128_t) 1ull) << 64)) >> (j - 64));
'  return (ulong) (hi >> (j - 64));
  *vp = mulShift(4 * m + 2, mul, j)
  *vm = mulShift(4 * m - 1 - mmShift, mul, j)
  Return mulShift(4 * m, mul, j)
End Function

#ElseIf HAS_64_BIT_INTRINSICS Then

Private inline Function mulShift(m As ULong, mul As ULong*, j As Integer) As ULong
  ' m is maximum 55 bits
  Dim high1 As ULong ' 128
  Dim low1 As ULong = umul128(m, mul(1), &high1) ' 64
  Dim high0 As ULong ' 64
  umul128(m, mul(0), &high0) ' 0
  Dim sum As ULong = high0 + low1
  If sum < high0 Then
	high1 += 1 ' overflow into high1
  End If
  Return shiftright128(sum, high1, j - 64)
End Function

Private inline Function mulShiftAll(m As ULong, mul As ULong*, j As Integer, vp As ULong*, vm As ULong*, mmShift As UInteger) As ULong
  *vp = mulShift(4 * m + 2, mul, j)
  *vm = mulShift(4 * m - 1 - mmShift, mul, j)
  Return mulShift(4 * m, mul, j)
End Function

#Else

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Private Function MulShiftAll(m As ULong, mul() As ULong, j As Integer, ByRef vp As ULong, ByRef vm As ULong, mmShift As UInteger) As ULong
        m <<= 1
        ' m is maximum 55 bits
        Dim tmp As ULong = Nothing
        Dim lo As ULong = umul128(m, mul(0), tmp)
        Dim hi As ULong = Nothing
        Dim mid As ULong = tmp + umul128(m, mul(1), hi)
        hi += BooleanToUInt64(mid < tmp) ' overflow into hi

        Dim lo2 As ULong = lo + mul(0)
        Dim mid2 As ULong = mid + mul(1) + BooleanToUInt64(lo2 < lo)
        Dim hi2 As ULong = hi + BooleanToUInt64(mid2 < mid)
        vp = shiftright128(mid2, hi2, j - 64 - 1)

        If mmShift = 1 Then
            Dim lo3 As ULong = lo - mul(0)
            Dim mid3 As ULong = mid - mul(1) - BooleanToUInt64(lo3 > lo)
            Dim hi3 As ULong = hi - BooleanToUInt64(mid3 > mid)
            vm = shiftright128(mid3, hi3, j - 64 - 1)
        Else
            Dim lo3 As ULong = lo + lo
            Dim mid3 As ULong = mid + mid + BooleanToUInt64(lo3 < lo)
            Dim hi3 As ULong = hi + hi + BooleanToUInt64(mid3 < mid)
            Dim lo4 As ULong = lo3 - mul(0)
            Dim mid4 As ULong = mid3 - mul(1) - BooleanToUInt64(lo4 > lo3)
            Dim hi4 As ULong = hi3 - BooleanToUInt64(mid4 > mid3)
            vm = shiftright128(mid4, hi4, j - 64)
        End If

        Return shiftright128(mid, hi, j - 64 - 1)
    End Function

#End If ' HAS_64_BIT_INTRINSICS

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Private Function DecimalLength(v As ULong) As Integer
        ' This is slightly faster than a loop.
        ' The average output length is 16.38 digits, so we check high-to-low.
        ' Function precondition: v is not an 18, 19, or 20-digit number.
        ' (17 digits are sufficient for round-tripping.)

        If v >= 100000000UL Then
            If v >= 10000000000000UL Then
                If v >= 1000000000000000UL Then
                    If v >= 10000000000000000UL Then
                        Return 17
                    End If
                    Return 16
                Else
                    If v >= 100000000000000UL Then
                        Return 15
                    End If
                    Return 14
                End If
            Else
                If v >= 10000000000UL Then
                    If v >= 1000000000000UL Then
                        Return 13
                    End If
                    If v >= 100000000000UL Then
                        Return 12
                    End If
                    Return 11
                Else
                    If v >= 1000000000UL Then
                        Return 10
                    End If
                    Return 9
                End If
            End If
        Else
            If v >= 10000UL Then
                If v >= 1000000UL Then
                    If v >= 10000000UL Then
                        Return 8
                    End If
                    Return 7
                Else
                    If v >= 100000UL Then
                        Return 6
                    End If
                    Return 5
                End If
            Else
                If v >= 100UL Then
                    If v >= 1000UL Then
                        Return 4
                    End If
                    Return 3
                Else
                    If v >= 10UL Then
                        Return 2
                    End If
                    Return 1
                End If
            End If
        End If
    End Function

    ' A floating decimal representing m * 10^e.
    Private Structure FloatingDecimal64
        Public Mantissa As ULong
        Public Exponent As Integer
    End Structure

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Private Function CreateFloatingDecimal64(ieeeMantissa As ULong, ieeeExponent As UInteger) As FloatingDecimal64
        Dim e2 As Integer
        Dim m2 As ULong
        If ieeeExponent = 0UI Then
            ' We subtract 2 so that the bounds computation has 2 additional bits.
            e2 = 1I - DOUBLE_BIAS - DOUBLE_MANTISSA_BITS - 2I
            m2 = ieeeMantissa
        Else
            e2 = CInt(ieeeExponent) - DOUBLE_BIAS - DOUBLE_MANTISSA_BITS - 2I
            m2 = (1UL << DOUBLE_MANTISSA_BITS) Or ieeeMantissa
        End If
        Dim even As Boolean = (m2 And 1UL) = 0UL
        Dim acceptBounds As Boolean = even

#If RYU_DEBUG Then
			printf("-> %" PRIu64 " * 2^%d" & vbLf, m2, e2 + 2)
#End If

        ' Step 2: Determine the interval of valid decimal representations.
        Dim mv As ULong = 4UL * m2
        ' Implicit bool -> int conversion. True is 1, false is 0.
        Dim mmShift As UInteger = BooleanToUInt32(ieeeMantissa <> 0UL OrElse ieeeExponent <= 1UI)
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
            Dim q As UInteger = Log10Pow2(e2) - BooleanToUInt32(e2 > 3)
            e10 = CInt(q)
            Dim k As Integer = CInt(DOUBLE_POW5_INV_BITCOUNT + Pow5bits(CInt(q)) - 1UI)
            Dim i As Integer = -e2 + CInt(q) + k
#If RYU_OPTIMIZE_SIZE Then
	ULong pow5(2)
	double_computeInvPow5(q, pow5)
	vr = mulShiftAll(m2, pow5, i, &vp, &vm, mmShift)
#Else
            vr = MulShiftAll(m2, DOUBLE_POW5_INV_SPLIT(CInt(q)), i, vp, vm, mmShift)
#End If
#If RYU_DEBUG Then
				printf("%" PRIu64 " * 2^%d / 10^%u" & vbLf, mv, e2, q)
				printf("V+=%" PRIu64 vbLf & "V =%" PRIu64 vbLf & "V-=%" PRIu64 vbLf, vp, vr, vm)
#End If
            If q <= 21UI Then
                ' This should use q <= 22, but I think 21 is also safe. Smaller values
                ' may still be safe, but it's more difficult to reason about them.
                ' Only one of mp, mv, and mm can be a multiple of 5, if any.
                Dim mvMod5 As UInteger = CUInt(mv - 5UL * div5(mv))
                If mvMod5 = 0UI Then
                    vrIsTrailingZeros = MultipleOfPowerOf5(mv, q)
                ElseIf acceptBounds Then
                    ' Same as min(e2 + (~mm & 1), pow5Factor(mm)) >= q
                    ' <=> e2 + (~mm & 1) >= q && pow5Factor(mm) >= q
                    ' <=> true && pow5Factor(mm) >= q, since e2 >= q.
                    vmIsTrailingZeros = MultipleOfPowerOf5(mv - 1UL - mmShift, q)
                Else
                    ' Same as min(e2 + 1, pow5Factor(mp)) >= q.
                    vp -= BooleanToUInt64(MultipleOfPowerOf5(mv + 2UL, q))
                End If
            End If
        Else
            ' This expression is slightly faster than max(0, log10Pow5(-e2) - 1).
            Dim q As UInteger = Log10Pow5(-e2) - BooleanToUInt32(-e2 > 1I)
            e10 = CInt(q) + e2
            Dim i As Integer = -e2 - CInt(q)
            Dim k As Integer = Pow5bits(i) - DOUBLE_POW5_BITCOUNT
            Dim j As Integer = CInt(q) - k
#If RYU_OPTIMIZE_SIZE Then
	ULong pow5(2)
	double_computePow5(i, pow5)
	vr = mulShiftAll(m2, pow5, j, &vp, &vm, mmShift)
#Else
            vr = MulShiftAll(m2, DOUBLE_POW5_SPLIT(i), j, vp, vm, mmShift)
#End If
#If RYU_DEBUG Then
				printf("%" PRIu64 " * 5^%d / 10^%u" & vbLf, mv, -e2, q)
				printf("%u %d %d %d" & vbLf, q, i, k, j)
				printf("V+=%" PRIu64 vbLf & "V =%" PRIu64 vbLf & "V-=%" PRIu64 vbLf, vp, vr, vm)
#End If
            If q <= 1UI Then
                ' {vr,vp,vm} is trailing zeros if {mv,mp,mm} has at least q trailing 0 bits.
                ' mv = 4 * m2, so it always has at least two trailing 0 bits.
                vrIsTrailingZeros = True
                If acceptBounds Then
                    ' mm = mv - 1 - mmShift, so it has 1 trailing 0 bit iff mmShift == 1.
                    vmIsTrailingZeros = mmShift = 1UI
                Else
                    ' mp = mv + 2, so it always has at least one trailing 0 bit.
                    vp -= 1UL
                End If
            ElseIf q < 63 Then ' TODO(ulfjack): Use a tighter bound here.
                ' We need to compute min(ntz(mv), pow5Factor(mv) - e2) >= q - 1
                ' <=> ntz(mv) >= q - 1 && pow5Factor(mv) - e2 >= q - 1
                ' <=> ntz(mv) >= q - 1 (e2 is negative and -e2 >= q)
                ' <=> (mv & ((1 << (q - 1)) - 1)) == 0
                ' We also need to make sure that the left shift does not overflow.
                vrIsTrailingZeros = MultipleOfPowerOf2(mv, CInt(q) - 1)
#If RYU_DEBUG Then
					printf("vr is trailing zeros=%s" & vbLf,If(vrIsTrailingZeros, "true", "false"))
#End If
            End If
        End If
#If RYU_DEBUG Then
			printf("e10=%d" & vbLf, e10)
			printf("V+=%" PRIu64 vbLf & "V =%" PRIu64 vbLf & "V-=%" PRIu64 vbLf, vp, vr, vm)
			printf("vm is trailing zeros=%s" & vbLf,If(vmIsTrailingZeros, "true", "false"))
			printf("vr is trailing zeros=%s" & vbLf,If(vrIsTrailingZeros, "true", "false"))
#End If

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
                Dim vmMod10 As UInteger = CUInt(vm - 10UL * vmDiv10)
                Dim vrDiv10 As ULong = div10(vr)
                Dim vrMod10 As UInteger = CUInt(vr - 10UL * vrDiv10)
                vmIsTrailingZeros = vmIsTrailingZeros And vmMod10 = 0UI
                vrIsTrailingZeros = vrIsTrailingZeros And lastRemovedDigit = 0
                lastRemovedDigit = CByte(vrMod10)
                vr = vrDiv10
                vp = vpDiv10
                vm = vmDiv10
                removed += 1
            Loop
#If RYU_DEBUG Then
				printf("V+=%" PRIu64 vbLf & "V =%" PRIu64 vbLf & "V-=%" PRIu64 vbLf, vp, vr, vm)
				printf("d-10=%s" & vbLf,If(vmIsTrailingZeros, "true", "false"))
#End If
            If vmIsTrailingZeros Then
                Do
                    Dim vmDiv10 As ULong = div10(vm)
                    Dim vmMod10 As UInteger = CUInt(vm - 10UL * vmDiv10)
                    If vmMod10 <> 0UI Then
                        Exit Do
                    End If
                    Dim vpDiv10 As ULong = div10(vp)
                    Dim vrDiv10 As ULong = div10(vr)
                    Dim vrMod10 As UInteger = CUInt(vr - 10UL * vrDiv10)
                    vrIsTrailingZeros = vrIsTrailingZeros And lastRemovedDigit = 0I
                    lastRemovedDigit = CByte(vrMod10)
                    vr = vrDiv10
                    vp = vpDiv10
                    vm = vmDiv10
                    removed += 1
                Loop
            End If
#If RYU_DEBUG Then
				printf("%" PRIu64 " %d" & vbLf, vr, lastRemovedDigit)
				printf("vr is trailing zeros=%s" & vbLf,If(vrIsTrailingZeros, "true", "false"))
#End If
            If vrIsTrailingZeros AndAlso lastRemovedDigit = 5I AndAlso vr Mod 2UL = 0UL Then
                ' Round even if the exact number is .....50..0.
                lastRemovedDigit = 4I
            End If
            ' We need to take vr + 1 if vr is outside bounds or we need to round up.
            output = vr + BooleanToUInt64((vr = vm AndAlso (Not acceptBounds OrElse Not vmIsTrailingZeros)) OrElse lastRemovedDigit >= 5I)
        Else
            ' Specialized for the common case (~99.3%). Percentages below are relative to this.
            Dim roundUp As Boolean = False
            Dim vpDiv100 As ULong = div100(vp)
            Dim vmDiv100 As ULong = div100(vm)
            If vpDiv100 > vmDiv100 Then ' Optimization: remove two digits at a time (~86.2%).
                Dim vrDiv100 As ULong = div100(vr)
                Dim vrMod100 As UInteger = CUInt(vr - 100UL * vrDiv100)
                roundUp = vrMod100 >= 50UI
                vr = vrDiv100
                vp = vpDiv100
                vm = vmDiv100
                removed += 2I
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
                Dim vrMod10 As UInteger = CUInt(vr - 10UL * vrDiv10)
                roundUp = vrMod10 >= 5UI
                vr = vrDiv10
                vp = vpDiv10
                vm = vmDiv10
                removed += 1I
            Loop
#If RYU_DEBUG Then
				printf("%" PRIu64 " roundUp=%s" & vbLf, vr,If(roundUp, "true", "false"))
				printf("vr is trailing zeros=%s" & vbLf,If(vrIsTrailingZeros, "true", "false"))
#End If
            ' We need to take vr + 1 if vr is outside bounds or we need to round up.
            output = vr + BooleanToUInt64(vr = vm OrElse roundUp)
        End If
        Dim exp As Integer = e10 + removed

#If RYU_DEBUG Then
			printf("V+=%" PRIu64 vbLf & "V =%" PRIu64 vbLf & "V-=%" PRIu64 vbLf, vp, vr, vm)
			printf("O=%" PRIu64 vbLf, output)
			printf("EXP=%d" & vbLf, exp)
#End If

        Dim fd As FloatingDecimal64
        fd.Exponent = exp
        fd.Mantissa = output
        Return fd
    End Function

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Private Function WriteToSBytes(v As FloatingDecimal64, sign As Boolean, result As Span(Of Char)) As Integer
        ' Step 5: Print the decimal representation.
        Dim index As Integer = 0
        If sign Then
            'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
            'ORIGINAL LINE: result[index++] = (sbyte)"-"c;
            result(index) = "-"c
            index += 1I
        End If

        Dim output As ULong = v.Mantissa
        Dim olength As Integer = DecimalLength(output)

#If RYU_DEBUG Then
			printf("DIGITS=%" PRIu64 vbLf, v.mantissa)
			printf("OLEN=%u" & vbLf, olength)
			printf("EXP=%u" & vbLf, v.exponent + olength)
#End If

        ' Print the decimal digits.
        ' The following code is equivalent to:
        ' for (uint i = 0; i < olength - 1; ++i) {
        '   uint c = output % 10; output /= 10;
        '   result[index + olength - i] = (sbyte) ('0' + c);
        ' }
        ' result[index] = '0' + output % 10;

        Dim i As Integer = 0
        ' We prefer 32-bit operations, even on 64-bit platforms.
        ' We have at most 17 digits, and uint can store 9 digits.
        ' If output doesn't fit into uint, we cut off 8 digits,
        ' so the rest will fit into uint.
        If (output >> 32) <> 0UL Then
            ' Expensive 64-bit division.
            Dim q As ULong = div1e8(output)
            Dim output3 As UInteger = CUInt(output - 100000000UL * q)
            output = q

            Dim c As UInteger = output3 Mod 10000UI
            output3 \= 10000UI
            Dim d As UInteger = output3 Mod 10000UI
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
        Dim output2 As UInteger = CUInt(output)
        Do While output2 >= 10000UI
            Dim c As UInteger = output2 Mod 10000UI

            output2 \= 10000UI
            Dim c0 As UInteger = (c Mod 100UI) << 1UI
            Dim c1 As UInteger = (c \ 100UI) << 1UI
            memcpy(result.Slice(index + olength - i - 1), DIGIT_TABLE, c0, 2)
            memcpy(result.Slice(index + olength - i - 3), DIGIT_TABLE, c1, 2)
            i += 4
        Loop
        If output2 >= 100UI Then
            Dim c As UInteger = (output2 Mod 100UI) << 1UI
            output2 \= 100UI
            memcpy(result.Slice(index + olength - i - 1), DIGIT_TABLE, c, 2)
            i += 2
        End If
        If output2 >= 10UI Then
            Dim c As UInteger = output2 << 1UI
            ' We can't use memcpy here: the decimal dot goes between these two digits.
            result(index + olength - i) = DIGIT_TABLE(CInt(c + 1UI))
            result(index) = DIGIT_TABLE(CInt(c))
        Else
            result(index) = Convert.ToChar(AscW("0"c) + CInt(output2))
        End If

        ' Print decimal point if needed.
        If olength > 1 Then
            result(index + 1) = "."c
            index += olength + 1
        Else
            index += 1
        End If

        ' Print the exponent.
        Dim exp As Integer = v.Exponent + olength - 1

        'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
        'ORIGINAL LINE: result[index++] = (sbyte)"E"c;
        result(index) = "E"c
        index += 1
        If exp < 0 Then
            'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
            'ORIGINAL LINE: result[index++] = (sbyte)"-"c;
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
            result(index + 2) = Convert.ToChar(AscW("0"c) + c)
            index += 3
        ElseIf exp >= 10 Then
            memcpy(result.Slice(index), DIGIT_TABLE, 2 * exp, 2)
            index += 2
        Else
            'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
            'ORIGINAL LINE: result[index++] = (sbyte)("0"c + exp);
            result(index) = Convert.ToChar(AscW("0"c) + exp)
            index += 1
        End If

        Return index
    End Function

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Private Function DoubleToSByteBuffer(f As Double, result As Span(Of Char)) As Integer
        ' Step 1: Decode the floating-point number, and unify normalized and subnormal cases.
        Dim bits As ULong = CULng(BitConverter.DoubleToInt64Bits(f))

#If RYU_DEBUG Then
			printf("IN=")
			For bit As Integer = 63 To 0 Step -1
				printf("%d", CInt((bits >> bit) And 1))
			Next bit
			printf(vbLf)
#End If

        ' Decode bits into sign, mantissa, and exponent.
        Dim ieeeSign As Boolean = ((bits >> (DOUBLE_MANTISSA_BITS + DOUBLE_EXPONENT_BITS)) And 1UL) <> 0UL
        Dim ieeeMantissa As ULong = bits And ((1UL << DOUBLE_MANTISSA_BITS) - 1UL)
        Dim ieeeExponent As UInteger = CUInt((bits >> DOUBLE_MANTISSA_BITS) And ((1UI << DOUBLE_EXPONENT_BITS) - 1UI))
        ' Case distinction; exit early for the easy cases.
        If ieeeExponent = ((1UI << DOUBLE_EXPONENT_BITS) - 1UI) OrElse (ieeeExponent = 0UI AndAlso ieeeMantissa = 0UL) Then
            Return CopySpecialString(result, ieeeSign, ieeeExponent <> 0UI, ieeeMantissa <> 0UL)
        End If

        Dim v As FloatingDecimal64 = CreateFloatingDecimal64(ieeeMantissa, ieeeExponent)
        Return WriteToSBytes(v, ieeeSign, result)
    End Function

    <StructLayout(LayoutKind.Sequential, Size:=24 * 2)>
    Private Structure StackAllocChar24
        Dim FirstValue As Char
    End Structure

    <Obsolete("Types with embedded references are not supported in this version of your compiler.")>
    Public Function ConvertDoubleToString(f As Double) As String
        'INSTANT VB TODO TASK: There is no equivalent to 'stackalloc' in VB:
        Dim allocated As StackAllocChar24
        Dim result As Span(Of Char) = MemoryMarshal.CreateSpan(allocated.FirstValue, 24)
        Dim index As Integer = DoubleToSByteBuffer(f, result)

        Return New String(result.Slice(0, index))
    End Function
End Module
