using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ryu
{
    partial class Global // D2s
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint pow5Factor(ulong value)
        {
            uint count = 0;
            for (; ; )
            {
                ulong q = div5(value);
                uint r = (uint)(value - 5 * q);
                if (r != 0)
                {
                    break;
                }
                value = q;
                ++count;
            }
            return count;
        }

        // Returns true if value is divisible by 5^p.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool multipleOfPowerOf5(ulong value, uint p)
        {
            // I tried a case distinction on p, but there was no performance difference.
            return pow5Factor(value) >= p;
        }

        // Returns true if value is divisible by 2^p.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool multipleOfPowerOf2(ulong value, int p)
        {
            // return __builtin_ctzll(value) >= p;
            return (value & ((1ul << p) - 1)) == 0;
        }

        // We need a 64x128-bit multiplication and a subsequent 128-bit shift.
        // Multiplication:
        //   The 64-bit factor is variable and passed in, the 128-bit factor comes
        //   from a lookup table. We know that the 64-bit factor only has 55
        //   significant bits (i.e., the 9 topmost bits are zeros). The 128-bit
        //   factor only has 124 significant bits (i.e., the 4 topmost bits are
        //   zeros).
        // Shift:
        //   In principle, the multiplication result requires 55 + 124 = 179 bits to
        //   represent. However, we then shift this value to the right by j, which is
        //   at least j >= 115, so the result is guaranteed to fit into 179 - 115 = 64
        //   bits. This means that we only need the topmost 64 significant bits of
        //   the 64x128-bit multiplication.
        //
        // There are several ways to do this:
        // 1. Best case: the compiler exposes a 128-bit type.
        //    We perform two 64x64-bit multiplications, add the higher 64 bits of the
        //    lower result to the higher result, and shift by j - 64 bits.
        //
        //    We explicitly cast from 64-bit to 128-bit, so the compiler can tell
        //    that these are only 64-bit inputs, and can map these to the best
        //    possible sequence of assembly instructions.
        //    x64 machines happen to have matching assembly instructions for
        //    64x64-bit multiplications and 128-bit shifts.
        //
        // 2. Second best case: the compiler exposes intrinsics for the x64 assembly
        //    instructions mentioned in 1.
        //
        // 3. We only have 64x64 bit instructions that return the lower 64 bits of
        //    the result, i.e., we have to use plain C.
        //    Our inputs are less than the full width, so we have three options:
        //    a. Ignore this fact and just implement the intrinsics manually.
        //    b. Split both into 31-bit pieces, which guarantees no internal overflow,
        //       but requires extra work upfront (unless we change the lookup table).
        //    c. Split only the first factor into 31-bit pieces, which also guarantees
        //       no internal overflow, but requires extra work since the intermediate
        //       results are not perfectly aligned.
#if HAS_UINT128 

// Best case: use 128-bit type.
static inline ulong mulShift(ulong m, ulong* mul, int j) {
  uint128_t b0 = ((uint128_t) m) * mul[0];
  uint128_t b2 = ((uint128_t) m) * mul[1];
  return (ulong) (((b0 >> 64) + b2) >> (j - 64));
}

static inline ulong mulShiftAll(ulong m, ulong* mul, int j,
  ulong* vp, ulong* vm, uint mmShift) {
//  m <<= 2;
//  uint128_t b0 = ((uint128_t) m) * mul[0]; // 0
//  uint128_t b2 = ((uint128_t) m) * mul[1]; // 64
//
//  uint128_t hi = (b0 >> 64) + b2;
//  uint128_t lo = b0 & 0xffffffffffffffffull;
//  uint128_t factor = (((uint128_t) mul[1]) << 64) + mul[0];
//  uint128_t vpLo = lo + (factor << 1);
//  *vp = (ulong) ((hi + (vpLo >> 64)) >> (j - 64));
//  uint128_t vmLo = lo - (factor << mmShift);
//  *vm = (ulong) ((hi + (vmLo >> 64) - (((uint128_t) 1ull) << 64)) >> (j - 64));
//  return (ulong) (hi >> (j - 64));
  *vp = mulShift(4 * m + 2, mul, j);
  *vm = mulShift(4 * m - 1 - mmShift, mul, j);
  return mulShift(4 * m, mul, j);
}

#elif HAS_64_BIT_INTRINSICS 

static inline ulong mulShift(ulong m, ulong* mul, int j) {
  // m is maximum 55 bits
  ulong high1;                                   // 128
  ulong low1 = umul128(m, mul[1], &high1); // 64
  ulong high0;                                   // 64
  umul128(m, mul[0], &high0);                       // 0
  ulong sum = high0 + low1;
  if (sum < high0) {
    ++high1; // overflow into high1
  }
  return shiftright128(sum, high1, j - 64);
}

static inline ulong mulShiftAll(ulong m, ulong* mul, int j,
  ulong* vp, ulong* vm, uint mmShift) {
  *vp = mulShift(4 * m + 2, mul, j);
  *vm = mulShift(4 * m - 1 - mmShift, mul, j);
  return mulShift(4 * m, mul, j);
}

#else

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong mulShiftAll(ulong m, ulong[] mul, int j,
            out ulong vp, out ulong vm, uint mmShift)
        {
            m <<= 1;
            // m is maximum 55 bits
            ulong tmp;
            ulong lo = umul128(m, mul[0], out tmp);
            ulong hi;
            ulong mid = tmp + umul128(m, mul[1], out hi);
            hi += BooleanToUInt64(mid < tmp); // overflow into hi

            ulong lo2 = lo + mul[0];
            ulong mid2 = mid + mul[1] + BooleanToUInt64(lo2 < lo);
            ulong hi2 = hi + BooleanToUInt64(mid2 < mid);
            vp = shiftright128(mid2, hi2, j - 64 - 1);

            if (mmShift == 1)
            {
                ulong lo3 = lo - mul[0];
                ulong mid3 = mid - mul[1] - BooleanToUInt64(lo3 > lo);
                ulong hi3 = hi - BooleanToUInt64(mid3 > mid);
                vm = shiftright128(mid3, hi3, j - 64 - 1);
            }
            else
            {
                ulong lo3 = lo + lo;
                ulong mid3 = mid + mid + BooleanToUInt64(lo3 < lo);
                ulong hi3 = hi + hi + BooleanToUInt64(mid3 < mid);
                ulong lo4 = lo3 - mul[0];
                ulong mid4 = mid3 - mul[1] - BooleanToUInt64(lo4 > lo3);
                ulong hi4 = hi3 - BooleanToUInt64(mid4 > mid3);
                vm = shiftright128(mid4, hi4, j - 64);
            }

            return shiftright128(mid, hi, j - 64 - 1);
        }

#endif // HAS_64_BIT_INTRINSICS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int decimalLength(ulong v)
        {
            // This is slightly faster than a loop.
            // The average output length is 16.38 digits, so we check high-to-low.
            // Function precondition: v is not an 18, 19, or 20-digit number.
            // (17 digits are sufficient for round-tripping.)

            if (v >= 100000000UL)
            {
                if (v >= 10000000000000UL)
                {
                    if (v >= 1000000000000000UL)
                    {
                        if (v >= 10000000000000000UL)
                            return 17;
                        return 16;
                    }
                    else
                    {
                        if (v >= 100000000000000UL)
                            return 15;
                        return 14;
                    }
                }
                else if (v >= 10000000000UL)
                {
                    if (v >= 1000000000000UL)
                        return 13;
                    if (v >= 100000000000UL)
                        return 12;
                    return 11;
                }
                else
                {
                    if (v >= 1000000000UL)
                        return 10;
                    return 9;
                }
            }
            else if (v >= 10000UL)
            {
                if (v >= 1000000UL)
                {
                    if (v >= 10000000UL)
                        return 8;
                    return 7;
                }
                else
                {
                    if (v >= 100000UL)
                        return 6;
                    return 5;
                }
            }
            else if (v >= 100UL)
            {
                if (v >= 1000UL)
                    return 4;
                return 3;
            }
            else
            {
                if (v >= 10UL)
                    return 2;
                return 1;
            }
        }

        // A floating decimal representing m * 10^e.
        struct floating_decimal_64
        {
            public ulong mantissa;
            public int exponent;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static floating_decimal_64 d2d(ulong ieeeMantissa, uint ieeeExponent)
        {
            int e2;
            ulong m2;
            if (ieeeExponent == 0)
            {
                // We subtract 2 so that the bounds computation has 2 additional bits.
                e2 = 1 - DOUBLE_BIAS - DOUBLE_MANTISSA_BITS - 2;
                m2 = ieeeMantissa;
            }
            else
            {
                e2 = (int)ieeeExponent - DOUBLE_BIAS - DOUBLE_MANTISSA_BITS - 2;
                m2 = (1ul << DOUBLE_MANTISSA_BITS) | ieeeMantissa;
            }
            bool even = (m2 & 1) == 0;
            bool acceptBounds = even;

#if RYU_DEBUG
            printf("-> %" PRIu64 " * 2^%d\n", m2, e2 + 2);
#endif

            // Step 2: Determine the interval of valid decimal representations.
            ulong mv = 4 * m2;
            // Implicit bool -> int conversion. True is 1, false is 0.
            uint mmShift = BooleanToUInt32(ieeeMantissa != 0 || ieeeExponent <= 1);
            // We would compute mp and mm like this:
            // ulong mp = 4 * m2 + 2;
            // ulong mm = mv - 1 - mmShift;

            // Step 3: Convert to a decimal power base using 128-bit arithmetic.
            ulong vr, vp, vm;
            int e10;
            bool vmIsTrailingZeros = false;
            bool vrIsTrailingZeros = false;
            if (e2 >= 0)
            {
                // I tried special-casing q == 0, but there was no effect on performance.
                // This expression is slightly faster than max(0, log10Pow2(e2) - 1).
                uint q = log10Pow2(e2) - BooleanToUInt32(e2 > 3);
                e10 = (int)q;
                int k = (int)(DOUBLE_POW5_INV_BITCOUNT + pow5bits((int)q) - 1u);
                int i = -e2 + (int)q + k;
#if RYU_OPTIMIZE_SIZE
    ulong pow5[2];
    double_computeInvPow5(q, pow5);
    vr = mulShiftAll(m2, pow5, i, &vp, &vm, mmShift);
#else
                vr = mulShiftAll(m2, DOUBLE_POW5_INV_SPLIT[q], i, out vp, out vm, mmShift);
#endif
#if RYU_DEBUG
                printf("%" PRIu64 " * 2^%d / 10^%u\n", mv, e2, q);
                printf("V+=%" PRIu64 "\nV =%" PRIu64 "\nV-=%" PRIu64 "\n", vp, vr, vm);
#endif
                if (q <= 21)
                {
                    // This should use q <= 22, but I think 21 is also safe. Smaller values
                    // may still be safe, but it's more difficult to reason about them.
                    // Only one of mp, mv, and mm can be a multiple of 5, if any.
                    uint mvMod5 = (uint)(mv - 5 * div5(mv));
                    if (mvMod5 == 0)
                    {
                        vrIsTrailingZeros = multipleOfPowerOf5(mv, q);
                    }
                    else if (acceptBounds)
                    {
                        // Same as min(e2 + (~mm & 1), pow5Factor(mm)) >= q
                        // <=> e2 + (~mm & 1) >= q && pow5Factor(mm) >= q
                        // <=> true && pow5Factor(mm) >= q, since e2 >= q.
                        vmIsTrailingZeros = multipleOfPowerOf5(mv - 1 - mmShift, q);
                    }
                    else
                    {
                        // Same as min(e2 + 1, pow5Factor(mp)) >= q.
                        vp -= BooleanToUInt64(multipleOfPowerOf5(mv + 2, q));
                    }
                }
            }
            else
            {
                // This expression is slightly faster than max(0, log10Pow5(-e2) - 1).
                uint q = log10Pow5(-e2) - BooleanToUInt32(-e2 > 1);
                e10 = (int)q + e2;
                int i = -e2 - (int)q;
                int k = pow5bits(i) - DOUBLE_POW5_BITCOUNT;
                int j = (int)q - k;
#if RYU_OPTIMIZE_SIZE
    ulong pow5[2];
    double_computePow5(i, pow5);
    vr = mulShiftAll(m2, pow5, j, &vp, &vm, mmShift);
#else
                vr = mulShiftAll(m2, DOUBLE_POW5_SPLIT[i], j, out vp, out vm, mmShift);
#endif
#if RYU_DEBUG
                printf("%" PRIu64 " * 5^%d / 10^%u\n", mv, -e2, q);
                printf("%u %d %d %d\n", q, i, k, j);
                printf("V+=%" PRIu64 "\nV =%" PRIu64 "\nV-=%" PRIu64 "\n", vp, vr, vm);
#endif
                if (q <= 1)
                {
                    // {vr,vp,vm} is trailing zeros if {mv,mp,mm} has at least q trailing 0 bits.
                    // mv = 4 * m2, so it always has at least two trailing 0 bits.
                    vrIsTrailingZeros = true;
                    if (acceptBounds)
                    {
                        // mm = mv - 1 - mmShift, so it has 1 trailing 0 bit iff mmShift == 1.
                        vmIsTrailingZeros = mmShift == 1;
                    }
                    else
                    {
                        // mp = mv + 2, so it always has at least one trailing 0 bit.
                        --vp;
                    }
                }
                else if (q < 63)
                { // TODO(ulfjack): Use a tighter bound here.
                  // We need to compute min(ntz(mv), pow5Factor(mv) - e2) >= q - 1
                  // <=> ntz(mv) >= q - 1 && pow5Factor(mv) - e2 >= q - 1
                  // <=> ntz(mv) >= q - 1 (e2 is negative and -e2 >= q)
                  // <=> (mv & ((1 << (q - 1)) - 1)) == 0
                  // We also need to make sure that the left shift does not overflow.
                    vrIsTrailingZeros = multipleOfPowerOf2(mv, (int)q - 1);
#if RYU_DEBUG
                    printf("vr is trailing zeros=%s\n", vrIsTrailingZeros ? "true" : "false");
#endif
                }
            }
#if RYU_DEBUG
            printf("e10=%d\n", e10);
            printf("V+=%" PRIu64 "\nV =%" PRIu64 "\nV-=%" PRIu64 "\n", vp, vr, vm);
            printf("vm is trailing zeros=%s\n", vmIsTrailingZeros ? "true" : "false");
            printf("vr is trailing zeros=%s\n", vrIsTrailingZeros ? "true" : "false");
#endif

            // Step 4: Find the shortest decimal representation in the interval of valid representations.
            int removed = 0;
            byte lastRemovedDigit = 0;
            ulong output;
            // On average, we remove ~2 digits.
            if (vmIsTrailingZeros || vrIsTrailingZeros)
            {
                // General case, which happens rarely (~0.7%).
                for (; ; )
                {
                    ulong vpDiv10 = div10(vp);
                    ulong vmDiv10 = div10(vm);
                    if (vpDiv10 <= vmDiv10)
                    {
                        break;
                    }
                    uint vmMod10 = (uint)(vm - 10 * vmDiv10);
                    ulong vrDiv10 = div10(vr);
                    uint vrMod10 = (uint)(vr - 10 * vrDiv10);
                    vmIsTrailingZeros &= vmMod10 == 0;
                    vrIsTrailingZeros &= lastRemovedDigit == 0;
                    lastRemovedDigit = (byte)vrMod10;
                    vr = vrDiv10;
                    vp = vpDiv10;
                    vm = vmDiv10;
                    ++removed;
                }
#if RYU_DEBUG
                printf("V+=%" PRIu64 "\nV =%" PRIu64 "\nV-=%" PRIu64 "\n", vp, vr, vm);
                printf("d-10=%s\n", vmIsTrailingZeros ? "true" : "false");
#endif
                if (vmIsTrailingZeros)
                {
                    for (; ; )
                    {
                        ulong vmDiv10 = div10(vm);
                        uint vmMod10 = (uint)(vm - 10 * vmDiv10);
                        if (vmMod10 != 0)
                        {
                            break;
                        }
                        ulong vpDiv10 = div10(vp);
                        ulong vrDiv10 = div10(vr);
                        uint vrMod10 = (uint)(vr - 10 * vrDiv10);
                        vrIsTrailingZeros &= lastRemovedDigit == 0;
                        lastRemovedDigit = (byte)vrMod10;
                        vr = vrDiv10;
                        vp = vpDiv10;
                        vm = vmDiv10;
                        ++removed;
                    }
                }
#if RYU_DEBUG
                printf("%" PRIu64 " %d\n", vr, lastRemovedDigit);
                printf("vr is trailing zeros=%s\n", vrIsTrailingZeros ? "true" : "false");
#endif
                if (vrIsTrailingZeros && lastRemovedDigit == 5 && vr % 2 == 0)
                {
                    // Round even if the exact number is .....50..0.
                    lastRemovedDigit = 4;
                }
                // We need to take vr + 1 if vr is outside bounds or we need to round up.
                output = vr + BooleanToUInt64((vr == vm && (!acceptBounds || !vmIsTrailingZeros)) || lastRemovedDigit >= 5);
            }
            else
            {
                // Specialized for the common case (~99.3%). Percentages below are relative to this.
                bool roundUp = false;
                ulong vpDiv100 = div100(vp);
                ulong vmDiv100 = div100(vm);
                if (vpDiv100 > vmDiv100)
                { // Optimization: remove two digits at a time (~86.2%).
                    ulong vrDiv100 = div100(vr);
                    uint vrMod100 = (uint)(vr - 100 * vrDiv100);
                    roundUp = vrMod100 >= 50;
                    vr = vrDiv100;
                    vp = vpDiv100;
                    vm = vmDiv100;
                    removed += 2;
                }
                // Loop iterations below (approximately), without optimization above:
                // 0: 0.03%, 1: 13.8%, 2: 70.6%, 3: 14.0%, 4: 1.40%, 5: 0.14%, 6+: 0.02%
                // Loop iterations below (approximately), with optimization above:
                // 0: 70.6%, 1: 27.8%, 2: 1.40%, 3: 0.14%, 4+: 0.02%
                for (; ; )
                {
                    ulong vpDiv10 = div10(vp);
                    ulong vmDiv10 = div10(vm);
                    if (vpDiv10 <= vmDiv10)
                    {
                        break;
                    }
                    ulong vrDiv10 = div10(vr);
                    uint vrMod10 = (uint)(vr - 10 * vrDiv10);
                    roundUp = vrMod10 >= 5;
                    vr = vrDiv10;
                    vp = vpDiv10;
                    vm = vmDiv10;
                    ++removed;
                }
#if RYU_DEBUG
                printf("%" PRIu64 " roundUp=%s\n", vr, roundUp ? "true" : "false");
                printf("vr is trailing zeros=%s\n", vrIsTrailingZeros ? "true" : "false");
#endif
                // We need to take vr + 1 if vr is outside bounds or we need to round up.
                output = vr + BooleanToUInt64(vr == vm || roundUp);
            }
            int exp = e10 + removed;

#if RYU_DEBUG
            printf("V+=%" PRIu64 "\nV =%" PRIu64 "\nV-=%" PRIu64 "\n", vp, vr, vm);
            printf("O=%" PRIu64 "\n", output);
            printf("EXP=%d\n", exp);
#endif

            floating_decimal_64 fd;
            fd.exponent = exp;
            fd.mantissa = output;
            return fd;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int to_sbytes(floating_decimal_64 v, bool sign, Span<char> result)
        {
            // Step 5: Print the decimal representation.
            int index = 0;
            if (sign)
            {
                result[index++] = '-';
            }

            ulong output = v.mantissa;
            int olength = decimalLength(output);

#if RYU_DEBUG
            printf("DIGITS=%" PRIu64 "\n", v.mantissa);
            printf("OLEN=%u\n", olength);
            printf("EXP=%u\n", v.exponent + olength);
#endif

            // Print the decimal digits.
            // The following code is equivalent to:
            // for (uint i = 0; i < olength - 1; ++i) {
            //   uint c = output % 10; output /= 10;
            //   result[index + olength - i] = (sbyte) ('0' + c);
            // }
            // result[index] = '0' + output % 10;

            int i = 0;
            // We prefer 32-bit operations, even on 64-bit platforms.
            // We have at most 17 digits, and uint can store 9 digits.
            // If output doesn't fit into uint, we cut off 8 digits,
            // so the rest will fit into uint.
            if ((output >> 32) != 0)
            {
                // Expensive 64-bit division.
                ulong q = div1e8(output);
                uint output3 = (uint)(output - 100000000 * q);
                output = q;

                uint c = output3 % 10000;
                output3 /= 10000;
                uint d = output3 % 10000;
                uint c0 = (c % 100) << 1;
                uint c1 = (c / 100) << 1;
                uint d0 = (d % 100) << 1;
                uint d1 = (d / 100) << 1;
                memcpy(result.Slice(index + olength - i - 1), DIGIT_TABLE, c0, 2);
                memcpy(result.Slice(index + olength - i - 3), DIGIT_TABLE, c1, 2);
                memcpy(result.Slice(index + olength - i - 5), DIGIT_TABLE, d0, 2);
                memcpy(result.Slice(index + olength - i - 7), DIGIT_TABLE, d1, 2);
                i += 8;
            }
            uint output2 = (uint)output;
            while (output2 >= 10000)
            {
                uint c = output2 % 10000;

                output2 /= 10000;
                uint c0 = (c % 100) << 1;
                uint c1 = (c / 100) << 1;
                memcpy(result.Slice(index + olength - i - 1), DIGIT_TABLE, c0, 2);
                memcpy(result.Slice(index + olength - i - 3), DIGIT_TABLE, c1, 2);
                i += 4;
            }
            if (output2 >= 100)
            {
                uint c = (output2 % 100) << 1;
                output2 /= 100;
                memcpy(result.Slice(index + olength - i - 1), DIGIT_TABLE, c, 2);
                i += 2;
            }
            if (output2 >= 10)
            {
                uint c = output2 << 1;
                // We can't use memcpy here: the decimal dot goes between these two digits.
                result[index + olength - i] = DIGIT_TABLE[c + 1];
                result[index] = DIGIT_TABLE[c];
            }
            else
            {
                result[index] = (char)((int)'0' + output2);
            }

            // Print decimal point if needed.
            if (olength > 1)
            {
                result[index + 1] = '.';
                index += (int)olength + 1;
            }
            else
            {
                ++index;
            }

            // Print the exponent.
            int exp = v.exponent + (int)olength - 1;

            result[index++] = 'E';
            if (exp < 0)
            {
                result[index++] = '-';
                exp = -exp;
            }
            else
            {
                result[index++] = '+';
            }

            if (exp >= 100)
            {
                int c = exp % 10;
                memcpy(result.Slice(index), DIGIT_TABLE, 2 * (exp / 10), 2);
                result[index + 2] = (char)('0' + c);
                index += 3;
            }
            else if (exp >= 10)
            {
                memcpy(result.Slice(index), DIGIT_TABLE, 2 * exp, 2);
                index += 2;
            }
            else
            {
                result[index++] = (char)('0' + exp);
            }

            return index;
        }

        static int d2s_buffered_n(double f, Span<char> result)
        {
            // Step 1: Decode the floating-point number, and unify normalized and subnormal cases.
            ulong bits = (ulong)BitConverter.DoubleToInt64Bits(f);

#if RYU_DEBUG
            printf("IN=");
            for (int bit = 63; bit >= 0; --bit)
            {
                printf("%d", (int)((bits >> bit) & 1));
            }
            printf("\n");
#endif

            // Decode bits into sign, mantissa, and exponent.
            bool ieeeSign = ((bits >> (DOUBLE_MANTISSA_BITS + DOUBLE_EXPONENT_BITS)) & 1) != 0;
            ulong ieeeMantissa = bits & ((1ul << DOUBLE_MANTISSA_BITS) - 1);
            uint ieeeExponent = (uint)((bits >> DOUBLE_MANTISSA_BITS) & ((1u << DOUBLE_EXPONENT_BITS) - 1));
            // Case distinction; exit early for the easy cases.
            if (ieeeExponent == ((1u << DOUBLE_EXPONENT_BITS) - 1u) || (ieeeExponent == 0 && ieeeMantissa == 0))
            {
                return copy_special_str(result, ieeeSign, ieeeExponent != 0, ieeeMantissa != 0);
            }

            floating_decimal_64 v = d2d(ieeeMantissa, ieeeExponent);
            return to_sbytes(v, ieeeSign, result);
        }

        public static string DoubleToString(double f)
        {
            Span<char> result = stackalloc char[24];
            int index = d2s_buffered_n(f, result);

            return new string(result.Slice(0, index));
        }
    }
}
