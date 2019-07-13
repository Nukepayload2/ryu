using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Ryu
{
    partial class Global
    {
        static ulong mulShiftAll(ulong m, Span<ulong> mul, int j,
          out ulong vp, out ulong vm, uint mmShift)
        {
            m <<= 1;
            // m is maximum 55 bits
            ulong tmp;
            ulong lo = umul128(m, mul[0], out tmp);
            ulong hi;
            ulong mid = tmp + umul128(m, mul[1], out hi);
            hi += Convert.ToUInt64(mid < tmp); // overflow into hi

            ulong lo2 = lo + mul[0];
            ulong mid2 = mid + mul[1] + Convert.ToUInt64(lo2 < lo);
            ulong hi2 = hi + Convert.ToUInt64(mid2 < mid);
            vp = shiftright128(mid2, hi2, (j - 64 - 1));

            if (mmShift == 1)
            {
                ulong lo3 = lo - mul[0];
                ulong mid3 = mid - mul[1] - Convert.ToUInt64(lo3 > lo);
                ulong hi3 = hi - Convert.ToUInt64(mid3 > mid);
                vm = shiftright128(mid3, hi3, (j - 64 - 1));
            }
            else
            {
                ulong lo3 = lo + lo;
                ulong mid3 = mid + mid + Convert.ToUInt64(lo3 < lo);
                ulong hi3 = hi + hi + Convert.ToUInt64(mid3 < mid);
                ulong lo4 = lo3 - mul[0];
                ulong mid4 = mid3 - mul[1] - Convert.ToUInt64(lo4 > lo3);
                ulong hi4 = hi3 - Convert.ToUInt64(mid4 > mid3);
                vm = shiftright128(mid4, hi4, (j - 64));
            }

            return shiftright128(mid, hi, (j - 64 - 1));
        }

        static int decimalLength17(ulong v)
        {
            // This is slightly faster than a loop.
            // The average output length is 16.38 digits, so we check high-to-low.
            // Function precondition: v is not an 18, 19, or 20-digit number.
            // (17 digits are sufficient for round-tripping.)
            Debug.Assert(v < 100000000000000000L);
            if (v >= 10000000000000000L) { return 17; }
            if (v >= 1000000000000000L) { return 16; }
            if (v >= 100000000000000L) { return 15; }
            if (v >= 10000000000000L) { return 14; }
            if (v >= 1000000000000L) { return 13; }
            if (v >= 100000000000L) { return 12; }
            if (v >= 10000000000L) { return 11; }
            if (v >= 1000000000L) { return 10; }
            if (v >= 100000000L) { return 9; }
            if (v >= 10000000L) { return 8; }
            if (v >= 1000000L) { return 7; }
            if (v >= 100000L) { return 6; }
            if (v >= 10000L) { return 5; }
            if (v >= 1000L) { return 4; }
            if (v >= 100L) { return 3; }
            if (v >= 10L) { return 2; }
            return 1;
        }

        // A floating decimal representing m * 10^e.
        struct floating_decimal_64
        {
            public ulong mantissa;
            // Decimal exponent's range is -324 to 308
            // inclusive, and can fit in a short if needed.
            public int exponent;
        }

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

            // Step 2: Determine the interval of valid decimal representations.
            ulong mv = 4 * m2;
            // Implicit bool -> int conversion. True is 1, false is 0.
            uint mmShift = Convert.ToUInt32(ieeeMantissa != 0 || ieeeExponent <= 1);
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
                uint q = log10Pow2(e2) - Convert.ToUInt32(e2 > 3);
                e10 = (int)q;
                int k = DOUBLE_POW5_INV_BITCOUNT + pow5bits((int)q) - 1;
                int i = -e2 + (int)q + k;

                vr = mulShiftAll(m2, DOUBLE_POW5_INV_SPLIT[q], i, out vp, out vm, mmShift);

                if (q <= 21)
                {
                    // This should use q <= 22, but I think 21 is also safe. Smaller values
                    // may still be safe, but it's more difficult to reason about them.
                    // Only one of mp, mv, and mm can be a multiple of 5, if any.
                    uint mvMod5 = ((uint)mv) - 5 * ((uint)div5(mv));
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
                        vp -= Convert.ToUInt64(multipleOfPowerOf5(mv + 2, q));
                    }
                }
            }
            else
            {
                // This expression is slightly faster than max(0, log10Pow5(-e2) - 1).
                uint q = log10Pow5(-e2) - Convert.ToUInt32(-e2 > 1);
                e10 = (int)q + e2;
                int i = -e2 - (int)q;
                int k = pow5bits(i) - DOUBLE_POW5_BITCOUNT;
                int j = (int)q - k;

                vr = mulShiftAll(m2, DOUBLE_POW5_SPLIT[i], j, out vp, out vm, mmShift);

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
                  // We want to know if the full product has at least q trailing zeros.
                  // We need to compute min(p2(mv), p5(mv) - e2) >= q
                  // <=> p2(mv) >= q && p5(mv) - e2 >= q
                  // <=> p2(mv) >= q (because -e2 >= q)
                    vrIsTrailingZeros = multipleOfPowerOf2(mv, (int)q);

                }
            }

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
                    uint vmMod10 = ((uint)vm) - 10 * ((uint)vmDiv10);
                    ulong vrDiv10 = div10(vr);
                    uint vrMod10 = ((uint)vr) - 10 * ((uint)vrDiv10);
                    vmIsTrailingZeros &= vmMod10 == 0;
                    vrIsTrailingZeros &= lastRemovedDigit == 0;
                    lastRemovedDigit = (byte)vrMod10;
                    vr = vrDiv10;
                    vp = vpDiv10;
                    vm = vmDiv10;
                    ++removed;
                }

                if (vmIsTrailingZeros)
                {
                    for (; ; )
                    {
                        ulong vmDiv10 = div10(vm);
                        uint vmMod10 = ((uint)vm) - 10 * ((uint)vmDiv10);
                        if (vmMod10 != 0)
                        {
                            break;
                        }
                        ulong vpDiv10 = div10(vp);
                        ulong vrDiv10 = div10(vr);
                        uint vrMod10 = ((uint)vr) - 10 * ((uint)vrDiv10);
                        vrIsTrailingZeros &= lastRemovedDigit == 0;
                        lastRemovedDigit = (byte)vrMod10;
                        vr = vrDiv10;
                        vp = vpDiv10;
                        vm = vmDiv10;
                        ++removed;
                    }
                }

                if (vrIsTrailingZeros && lastRemovedDigit == 5 && vr % 2 == 0)
                {
                    // Round even if the exact number is .....50..0.
                    lastRemovedDigit = 4;
                }
                // We need to take vr + 1 if vr is outside bounds or we need to round up.
                output = vr + Convert.ToUInt64((vr == vm && (!acceptBounds || !vmIsTrailingZeros)) || lastRemovedDigit >= 5);
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
                    uint vrMod100 = ((uint)vr) - 100 * ((uint)vrDiv100);
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
                    uint vrMod10 = ((uint)vr) - 10 * ((uint)vrDiv10);
                    roundUp = vrMod10 >= 5;
                    vr = vrDiv10;
                    vp = vpDiv10;
                    vm = vmDiv10;
                    ++removed;
                }

                // We need to take vr + 1 if vr is outside bounds or we need to round up.
                output = vr + Convert.ToUInt64(vr == vm || roundUp);
            }
            int exp = e10 + removed;

            floating_decimal_64 fd;
            fd.exponent = exp;
            fd.mantissa = output;
            return fd;
        }

        static int to_chars(floating_decimal_64 v, bool sign, Span<char> result)
        {
            // Step 5: Print the decimal representation.
            int index = 0;
            if (sign)
            {
                result[index++] = '-';
            }

            ulong output = v.mantissa;
            int olength = decimalLength17(output);

            // Print the decimal digits.
            // The following code is equivalent to:
            // for (uint i = 0; i < olength - 1; ++i) {
            //    uint c = output % 10; output /= 10;
            //   result[index + olength - i] = (char) ('0' + c);
            // }
            // result[index] = '0' + output % 10;

            int i = 0;
            // We prefer 32-bit operations, even on 64-bit platforms.
            // We have at most 17 digits, and uint can store 9 digits.
            // If output doesn't fit into uint, we cut off 8 digits,
            // so the rest will fit into uint.
            uint output2;
            if ((output >> 32) != 0)
            {
                // Expensive 64-bit division.
                ulong q = div1e8(output);
                output2 = ((uint)output) - 100000000 * ((uint)q);
                output = q;

                uint c = output2 % 10000;
                output2 /= 10000;
                uint d = output2 % 10000;
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
            output2 = (uint)output;
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
                result[index] = (char)('0' + output2);
            }

            // Print decimal point if needed.
            if (olength > 1)
            {
                result[index + 1] = '.';
                index += olength + 1;
            }
            else
            {
                ++index;
            }

            // Print the exponent.
            result[index++] = 'E';
            int exp = v.exponent + (int)olength - 1;
            if (exp < 0)
            {
                result[index++] = '-';
                exp = -exp;
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

        static bool d2d_small_int(ulong ieeeMantissa, uint ieeeExponent,
          ref floating_decimal_64 v)
        {
            ulong m2 = (1ul << DOUBLE_MANTISSA_BITS) | ieeeMantissa;
            int e2 = (int)ieeeExponent - DOUBLE_BIAS - DOUBLE_MANTISSA_BITS;

            if (e2 > 0)
            {
                // f = m2 * 2^e2 >= 2^53 is an integer.
                // Ignore this case for now.
                return false;
            }

            if (e2 < -52)
            {
                // f < 1.
                return false;
            }

            // Since 2^52 <= m2 < 2^53 and 0 <= -e2 <= 52: 1 <= f = m2 / 2^-e2 < 2^53.
            // Test if the lower -e2 bits of the significand are 0, i.e. whether the fraction is 0.
            ulong mask = (1ul << -e2) - 1;
            ulong fraction = m2 & mask;
            if (fraction != 0)
            {
                return false;
            }

            // f is an integer in the range [1, 2^53).
            // Note: mantissa might contain trailing (decimal) 0's.
            // Note: since 2^53 < 10^16, there is no need to adjust decimalLength17().
            v.mantissa = m2 >> -e2;
            v.exponent = 0;
            return true;
        }

        static int d2s_buffered_n(double f, Span<char> result)
        {
            // Step 1: Decode the floating-point number, and unify normalized and subnormal cases.
            ulong bits = double_to_bits(f);

            // Decode bits into sign, mantissa, and exponent.
            bool ieeeSign = ((bits >> (DOUBLE_MANTISSA_BITS + DOUBLE_EXPONENT_BITS)) & 1) != 0;
            ulong ieeeMantissa = bits & ((1ul << DOUBLE_MANTISSA_BITS) - 1);
            uint ieeeExponent = (uint)((bits >> DOUBLE_MANTISSA_BITS) & ((1u << DOUBLE_EXPONENT_BITS) - 1));
            // Case distinction; exit early for the easy cases.
            if (ieeeExponent == ((1u << DOUBLE_EXPONENT_BITS) - 1u) || (ieeeExponent == 0 && ieeeMantissa == 0))
            {
                return copy_special_str(result, ieeeSign, Convert.ToBoolean(ieeeExponent), Convert.ToBoolean(ieeeMantissa));
            }

            floating_decimal_64 v = default(floating_decimal_64);
            bool isSmallInt = d2d_small_int(ieeeMantissa, ieeeExponent, ref v);
            if (isSmallInt)
            {
                // For small integers in the range [1, 2^53), v.mantissa might contain trailing (decimal) zeros.
                // For scientific notation we need to move these zeros into the exponent.
                // (This is not needed for fixed-point notation, so it might be beneficial to trim
                // trailing zeros in to_chars only if needed - once fixed-point notation output is implemented.)
                for (; ; )
                {
                    ulong q = div10(v.mantissa);
                    uint r = ((uint)v.mantissa) - 10 * ((uint)q);
                    if (r != 0)
                    {
                        break;
                    }
                    v.mantissa = q;
                    ++v.exponent;
                }
            }
            else
            {
                v = d2d(ieeeMantissa, ieeeExponent);
            }

            return to_chars(v, ieeeSign, result);
        }

        void d2s_buffered(double f, Span<char> result)
        {
            int index = d2s_buffered_n(f, result);

            // Terminate the string.
            result[index] = '\0';
        }

        public static string DoubleToString(double f)
        {
            Span<char> result = stackalloc char[24];
            int index = d2s_buffered_n(f, result);

            return new string(result.Slice(0, index));
        }
    }
}
