﻿using System;
using System.Runtime.CompilerServices;

namespace Ryu
{
    partial class Global // F2s
    {

        const int FLOAT_MANTISSA_BITS = 23;
        const int FLOAT_EXPONENT_BITS = 8;
        const int FLOAT_BIAS = 127;

        // This table is generated by PrintFloatLookupTable.
        const int FLOAT_POW5_INV_BITCOUNT = 59;
        static readonly ulong[] FLOAT_POW5_INV_SPLIT = {
  576460752303423489u, 461168601842738791u, 368934881474191033u, 295147905179352826u,
  472236648286964522u, 377789318629571618u, 302231454903657294u, 483570327845851670u,
  386856262276681336u, 309485009821345069u, 495176015714152110u, 396140812571321688u,
  316912650057057351u, 507060240091291761u, 405648192073033409u, 324518553658426727u,
  519229685853482763u, 415383748682786211u, 332306998946228969u, 531691198313966350u,
  425352958651173080u, 340282366920938464u, 544451787073501542u, 435561429658801234u,
  348449143727040987u, 557518629963265579u, 446014903970612463u, 356811923176489971u,
  570899077082383953u, 456719261665907162u, 365375409332725730u
};
        const int FLOAT_POW5_BITCOUNT = 61;

        static readonly ulong[] FLOAT_POW5_SPLIT = {
  1152921504606846976u, 1441151880758558720u, 1801439850948198400u, 2251799813685248000u,
  1407374883553280000u, 1759218604441600000u, 2199023255552000000u, 1374389534720000000u,
  1717986918400000000u, 2147483648000000000u, 1342177280000000000u, 1677721600000000000u,
  2097152000000000000u, 1310720000000000000u, 1638400000000000000u, 2048000000000000000u,
  1280000000000000000u, 1600000000000000000u, 2000000000000000000u, 1250000000000000000u,
  1562500000000000000u, 1953125000000000000u, 1220703125000000000u, 1525878906250000000u,
  1907348632812500000u, 1192092895507812500u, 1490116119384765625u, 1862645149230957031u,
  1164153218269348144u, 1455191522836685180u, 1818989403545856475u, 2273736754432320594u,
  1421085471520200371u, 1776356839400250464u, 2220446049250313080u, 1387778780781445675u,
  1734723475976807094u, 2168404344971008868u, 1355252715606880542u, 1694065894508600678u,
  2117582368135750847u, 1323488980084844279u, 1654361225106055349u, 2067951531382569187u,
  1292469707114105741u, 1615587133892632177u, 2019483917365790221u
};

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint pow5Factor(uint value)
        {
            uint count = 0;
            for (; ; )
            {
                uint q = value / 5;
                uint r = value % 5;
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
        static bool multipleOfPowerOf5(uint value, uint p)
        {
            return pow5Factor(value) >= p;
        }

        // Returns true if value is divisible by 2^p.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool multipleOfPowerOf2(uint value, int p)
        {
            // return __builtin_ctz(value) >= p;
            return (value & ((1u << p) - 1)) == 0;
        }

        // It seems to be slightly faster to avoid uint128_t here, although the
        // generated code for uint128_t looks slightly nicer.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint mulShift(uint m, ulong factor, int shift)
        {
            // The casts here help MSVC to avoid calls to the __allmul library
            // function.
            uint factorLo = (uint)(factor);
            uint factorHi = (uint)(factor >> 32);
            ulong bits0 = (ulong)m * factorLo;
            ulong bits1 = (ulong)m * factorHi;

#if RYU_32_BIT_PLATFORM
        // On 32-bit platforms we can avoid a 64-bit shift-right since we only
        // need the upper 32 bits of the result and the shift value is > 32.
        uint bits0Hi = (uint)(bits0 >> 32);
        uint bits1Lo = (uint)(bits1);
        uint bits1Hi = (uint)(bits1 >> 32);
        bits1Lo += bits0Hi;
        bits1Hi += (bits1Lo < bits0Hi);
        int s = shift - 32;
        return (bits1Hi << (32 - s)) | (bits1Lo >> s);
#else // RYU_32_BIT_PLATFORM
            ulong sum = (bits0 >> 32) + bits1;
            ulong shiftedSum = sum >> (shift - 32);
            return (uint)shiftedSum;
#endif // RYU_32_BIT_PLATFORM
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint mulPow5InvDivPow2(uint m, uint q, int j)
        {
            return mulShift(m, FLOAT_POW5_INV_SPLIT[q], j);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint mulPow5divPow2(uint m, uint i, int j)
        {
            return mulShift(m, FLOAT_POW5_SPLIT[i], j);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int decimalLength(uint v)
        {
            // Function precondition: v is not a 10-digit number.
            // (9 digits are sufficient for round-tripping.)
            if (v >= 100000000) { return 9; }
            if (v >= 10000000) { return 8; }
            if (v >= 1000000) { return 7; }
            if (v >= 100000) { return 6; }
            if (v >= 10000) { return 5; }
            if (v >= 1000) { return 4; }
            if (v >= 100) { return 3; }
            if (v >= 10) { return 2; }
            return 1;
        }

        // A floating decimal representing m * 10^e.
        struct floating_decimal_32
        {
            public uint mantissa;
            public int exponent;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static floating_decimal_32 f2d(uint ieeeMantissa, uint ieeeExponent)
        {
            int e2;
            uint m2;
            if (ieeeExponent == 0)
            {
                // We subtract 2 so that the bounds computation has 2 additional bits.
                e2 = 1 - FLOAT_BIAS - FLOAT_MANTISSA_BITS - 2;
                m2 = ieeeMantissa;
            }
            else
            {
                e2 = (int)ieeeExponent - FLOAT_BIAS - FLOAT_MANTISSA_BITS - 2;
                m2 = (1u << FLOAT_MANTISSA_BITS) | ieeeMantissa;
            }
            bool even = (m2 & 1) == 0;
            bool acceptBounds = even;

#if RYU_DEBUG
        printf("-> %u * 2^%d\n", m2, e2 + 2);
#endif

            // Step 2: Determine the interval of valid decimal representations.
            uint mv = 4 * m2;
            uint mp = 4 * m2 + 2;
            // Implicit bool -> int conversion. True is 1, false is 0.
            uint mmShift = BooleanToUInt32(ieeeMantissa != 0 || ieeeExponent <= 1);
            uint mm = 4 * m2 - 1 - mmShift;

            // Step 3: Convert to a decimal power base using 64-bit arithmetic.
            uint vr, vp, vm;
            int e10;
            bool vmIsTrailingZeros = false;
            bool vrIsTrailingZeros = false;
            byte lastRemovedDigit = 0;
            if (e2 >= 0)
            {
                uint q = log10Pow2(e2);
                e10 = (int)q;
                int k = FLOAT_POW5_INV_BITCOUNT + pow5bits((int)q) - 1;
                int i = -e2 + (int)q + k;
                vr = mulPow5InvDivPow2(mv, q, i);
                vp = mulPow5InvDivPow2(mp, q, i);
                vm = mulPow5InvDivPow2(mm, q, i);
#if RYU_DEBUG
            printf("%u * 2^%d / 10^%u\n", mv, e2, q);
            printf("V+=%u\nV =%u\nV-=%u\n", vp, vr, vm);
#endif
                if (q != 0 && (vp - 1) / 10 <= vm / 10)
                {
                    // We need to know one removed digit even if we are not going to loop below. We could use
                    // q = X - 1 above, except that would require 33 bits for the result, and we've found that
                    // 32-bit arithmetic is faster even on 64-bit machines.
                    int l = FLOAT_POW5_INV_BITCOUNT + pow5bits((int)(q - 1)) - 1;
                    lastRemovedDigit = (byte)(mulPow5InvDivPow2(mv, q - 1, -e2 + (int)q - 1 + l) % 10);
                }
                if (q <= 9)
                {
                    // The largest power of 5 that fits in 24 bits is 5^10, but q <= 9 seems to be safe as well.
                    // Only one of mp, mv, and mm can be a multiple of 5, if any.
                    if (mv % 5 == 0)
                    {
                        vrIsTrailingZeros = multipleOfPowerOf5(mv, q);
                    }
                    else if (acceptBounds)
                    {
                        vmIsTrailingZeros = multipleOfPowerOf5(mm, q);
                    }
                    else
                    {
                        vp -= BooleanToUInt32(multipleOfPowerOf5(mp, q));
                    }
                }
            }
            else
            {
                uint q = log10Pow5(-e2);
                e10 = (int)q + e2;
                int i = -e2 - (int)q;
                int k = pow5bits(i) - FLOAT_POW5_BITCOUNT;
                int j = (int)q - k;
                vr = mulPow5divPow2(mv, (uint)i, j);
                vp = mulPow5divPow2(mp, (uint)i, j);
                vm = mulPow5divPow2(mm, (uint)i, j);
#if RYU_DEBUG
            printf("%u * 5^%d / 10^%u\n", mv, -e2, q);
            printf("%u %d %d %d\n", q, i, k, j);
            printf("V+=%u\nV =%u\nV-=%u\n", vp, vr, vm);
#endif
                if (q != 0 && (vp - 1) / 10 <= vm / 10)
                {
                    j = (int)q - 1 - (pow5bits(i + 1) - FLOAT_POW5_BITCOUNT);
                    lastRemovedDigit = (byte)(mulPow5divPow2(mv, (uint)(i + 1), j) % 10);
                }
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
                else if (q < 31)
                { // TODO(ulfjack): Use a tighter bound here.
                    vrIsTrailingZeros = multipleOfPowerOf2(mv, (int)q - 1);
#if RYU_DEBUG
                printf("vr is trailing zeros=%s\n", vrIsTrailingZeros ? "true" : "false");
#endif
                }
            }
#if RYU_DEBUG
        printf("e10=%d\n", e10);
        printf("V+=%u\nV =%u\nV-=%u\n", vp, vr, vm);
        printf("vm is trailing zeros=%s\n", vmIsTrailingZeros ? "true" : "false");
        printf("vr is trailing zeros=%s\n", vrIsTrailingZeros ? "true" : "false");
#endif

            // Step 4: Find the shortest decimal representation in the interval of valid representations.
            int removed = 0;
            uint output;
            if (vmIsTrailingZeros || vrIsTrailingZeros)
            {
                // General case, which happens rarely (~4.0%).
                while (vp / 10 > vm / 10)
                {
                    vmIsTrailingZeros &= vm % 10 == 0;
                    vrIsTrailingZeros &= lastRemovedDigit == 0;
                    lastRemovedDigit = (byte)(vr % 10);
                    vr /= 10;
                    vp /= 10;
                    vm /= 10;
                    ++removed;
                }
#if RYU_DEBUG
            printf("V+=%u\nV =%u\nV-=%u\n", vp, vr, vm);
            printf("d-10=%s\n", vmIsTrailingZeros ? "true" : "false");
#endif
                if (vmIsTrailingZeros)
                {
                    while (vm % 10 == 0)
                    {
                        vrIsTrailingZeros &= lastRemovedDigit == 0;
                        lastRemovedDigit = (byte)(vr % 10);
                        vr /= 10;
                        vp /= 10;
                        vm /= 10;
                        ++removed;
                    }
                }
#if RYU_DEBUG
            printf("%u %d\n", vr, lastRemovedDigit);
            printf("vr is trailing zeros=%s\n", vrIsTrailingZeros ? "true" : "false");
#endif
                if (vrIsTrailingZeros && lastRemovedDigit == 5 && vr % 2 == 0)
                {
                    // Round even if the exact number is .....50..0.
                    lastRemovedDigit = 4;
                }
                // We need to take vr + 1 if vr is outside bounds or we need to round up.
                output = vr + BooleanToUInt32((vr == vm && (!acceptBounds || !vmIsTrailingZeros)) || lastRemovedDigit >= 5);
            }
            else
            {
                // Specialized for the common case (~96.0%). Percentages below are relative to this.
                // Loop iterations below (approximately):
                // 0: 13.6%, 1: 70.7%, 2: 14.1%, 3: 1.39%, 4: 0.14%, 5+: 0.01%
                while (vp / 10 > vm / 10)
                {
                    lastRemovedDigit = (byte)(vr % 10);
                    vr /= 10;
                    vp /= 10;
                    vm /= 10;
                    ++removed;
                }
#if RYU_DEBUG
            printf("%u %d\n", vr, lastRemovedDigit);
            printf("vr is trailing zeros=%s\n", vrIsTrailingZeros ? "true" : "false");
#endif
                // We need to take vr + 1 if vr is outside bounds or we need to round up.
                output = vr + BooleanToUInt32(vr == vm || lastRemovedDigit >= 5);
            }
            int exp = e10 + removed;

#if RYU_DEBUG
        printf("V+=%u\nV =%u\nV-=%u\n", vp, vr, vm);
        printf("O=%u\n", output);
        printf("EXP=%d\n", exp);
#endif

            floating_decimal_32 fd;
            fd.exponent = exp;
            fd.mantissa = output;
            return fd;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int to_sbytes(floating_decimal_32 v, bool sign, Span<sbyte> result)
        {
            // Step 5: Print the decimal representation.
            int index = 0;
            if (sign)
            {
                result[index++] = (sbyte)'-';
            }

            uint output = v.mantissa;
            int olength = decimalLength(output);

#if RYU_DEBUG
        printf("DIGITS=%u\n", v.mantissa);
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
            while (output >= 10000)
            {
                uint c = output % 10000;

                output /= 10000;
                uint c0 = (c % 100) << 1;
                uint c1 = (c / 100) << 1;
                memcpy(result.Slice(index + olength - i - 1), DIGIT_TABLE, c0, 2);
                memcpy(result.Slice(index + olength - i - 3), DIGIT_TABLE, c1, 2);
                i += 4;
            }
            if (output >= 100)
            {
                uint c = (output % 100) << 1;
                output /= 100;
                memcpy(result.Slice(index + olength - i - 1), DIGIT_TABLE, c, 2);
                i += 2;
            }
            if (output >= 10)
            {
                uint c = output << 1;
                // We can't use memcpy here: the decimal dot goes between these two digits.
                result[index + olength - i] = (sbyte)DIGIT_TABLE[c + 1];
                result[index] = (sbyte)DIGIT_TABLE[c];
            }
            else
            {
                result[index] = (sbyte)('0' + output);
            }

            // Print decimal point if needed.
            if (olength > 1)
            {
                result[index + 1] = (sbyte)'.';
                index += (int)olength + 1;
            }
            else
            {
                ++index;
            }

            // Print the exponent.
            int exp = v.exponent + (int)olength - 1;
            if (exp >= -3 && exp < 7)
            {
                return index;
            }

            result[index++] = (sbyte)'E';
            if (exp < 0)
            {
                result[index++] = (sbyte)'-';
                exp = -exp;
            }

            if (exp >= 10)
            {
                memcpy(result.Slice(index), DIGIT_TABLE, 2 * exp, 2);
                index += 2;
            }
            else
            {
                result[index++] = (sbyte)('0' + exp);
            }

            return index;
        }

        static int f2s_buffered_n(float f, Span<sbyte> result)
        {
            // Step 1: Decode the floating-point number, and unify normalized and subnormal cases.
            uint bits = (uint)BitConverter.SingleToInt32Bits(f);

#if RYU_DEBUG
        printf("IN=");
        for (int bit = 31; bit >= 0; --bit)
        {
            printf("%u", (bits >> bit) & 1);
        }
        printf("\n");
#endif

            // Decode bits into sign, mantissa, and exponent.
            bool ieeeSign = ((bits >> (FLOAT_MANTISSA_BITS + FLOAT_EXPONENT_BITS)) & 1) != 0;
            uint ieeeMantissa = bits & ((1u << FLOAT_MANTISSA_BITS) - 1);
            uint ieeeExponent = (bits >> FLOAT_MANTISSA_BITS) & ((1u << FLOAT_EXPONENT_BITS) - 1);

            // Case distinction; exit early for the easy cases.
            if (ieeeExponent == ((1u << FLOAT_EXPONENT_BITS) - 1u) || (ieeeExponent == 0 && ieeeMantissa == 0))
            {
                return copy_special_str(result, ieeeSign, ieeeExponent != 0, ieeeMantissa != 0);
            }

            floating_decimal_32 v = f2d(ieeeMantissa, ieeeExponent);
            return to_sbytes(v, ieeeSign, result);
        }

        public static string SingleToString(float f)
        {
            Span<sbyte> result = stackalloc sbyte[26];
            int index = f2s_buffered_n(f, result);

            // Terminate the string.
            result[index] = default;

            return CopyAsciiSpanToNewString(result, index);
        }
    }
}
