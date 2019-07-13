using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Ryu
{
    partial class Global
    {
        const int POW10_ADDITIONAL_BITS = 120;

        static uint mulShift_mod1e9(ulong m, Span<ulong> mul, int j)
        {
            ulong high0;                                   // 64
            ulong low0 = umul128(m, mul[0], out high0); // 0
            ulong high1;                                   // 128
            ulong low1 = umul128(m, mul[1], out high1); // 64
            ulong high2;                                   // 192
            ulong low2 = umul128(m, mul[2], out high2); // 128
            ulong s0low = low0;              // 0
            ulong s0high = low1 + high0;     // 64
            uint c1 = Convert.ToUInt32(s0high < low1);
            ulong s1low = low2 + high1 + c1; // 128
            uint c2 = Convert.ToUInt32(s1low < low2); // high1 + c1 can't overflow, so compare against low2
            ulong s1high = high2 + c2;       // 192

            Debug.Assert(j >= 128);
            Debug.Assert(j <= 180);

            if (j < 160)
            { // j: [128, 160)
                ulong r0 = mod1e9(s1high);
                ulong r1 = mod1e9((r0 << 32) | (s1low >> 32));
                ulong r2 = ((r1 << 32) | (s1low & 0xffffffff));
                return mod1e9(r2 >> (j - 128));
            }
            else
            { // j: [160, 192)
                ulong r0 = mod1e9(s1high);
                ulong r1 = ((r0 << 32) | (s1low >> 32));
                return mod1e9(r1 >> (j - 160));
            }
        }

        static void append_n_digits(int olength, uint digits, Span<char> result)
        {
            int i = 0;
            while (digits >= 10000)
            {
                uint c = digits % 10000;
                digits /= 10000;
                uint c0 = (c % 100) << 1;
                uint c1 = (c / 100) << 1;
                memcpy(result.Slice(olength - i - 2), DIGIT_TABLE, c0, 2);
                memcpy(result.Slice(olength - i - 4), DIGIT_TABLE, c1, 2);
                i += 4;
            }
            if (digits >= 100)
            {
                uint c = (digits % 100) << 1;
                digits /= 100;
                memcpy(result.Slice(olength - i - 2), DIGIT_TABLE, c, 2);
                i += 2;
            }
            if (digits >= 10)
            {
                uint c = digits << 1;
                memcpy(result.Slice(olength - i - 2), DIGIT_TABLE, c, 2);
            }
            else
            {
                result[0] = (char)('0' + digits);
            }
        }

        static void append_d_digits(int olength, uint digits, Span<char> result)
        {
            int i = 0;
            while (digits >= 10000)
            {
                uint c = digits % 10000;
                digits /= 10000;
                uint c0 = (c % 100) << 1;
                uint c1 = (c / 100) << 1;
                memcpy(result.Slice(olength + 1 - i - 2), DIGIT_TABLE, c0, 2);
                memcpy(result.Slice(olength + 1 - i - 4), DIGIT_TABLE, c1, 2);
                i += 4;
            }
            if (digits >= 100)
            {
                uint c = (digits % 100) << 1;
                digits /= 100;
                memcpy(result.Slice(olength + 1 - i - 2), DIGIT_TABLE, c, 2);
                i += 2;
            }
            if (digits >= 10)
            {
                uint c = digits << 1;
                result[2] = DIGIT_TABLE[c + 1];
                result[1] = '.';
                result[0] = DIGIT_TABLE[c];
            }
            else
            {
                result[1] = '.';
                result[0] = (char)('0' + digits);
            }
        }

        static void append_c_digits(int count, uint digits, Span<char> result)
        {
            int i = 0;
            for (; i < count - 1; i += 2)
            {
                uint c = (digits % 100) << 1;
                digits /= 100;
                memcpy(result.Slice(count - i - 2), DIGIT_TABLE, c, 2);
            }
            if (i < count)
            {
                char c = (char)('0' + (digits % 10));
                result[count - i - 1] = c;
            }
        }

        static void append_nine_digits(uint digits, Span<char> result)
        {
            if (digits == 0)
            {
                memset(result, '0', 9);
                return;
            }

            for (int i = 0; i < 5; i += 4)
            {
                uint c = digits % 10000;
                digits /= 10000;
                uint c0 = (c % 100) << 1;
                uint c1 = (c / 100) << 1;
                memcpy(result.Slice(7 - i), DIGIT_TABLE, c0, 2);
                memcpy(result.Slice(5 - i), DIGIT_TABLE, c1, 2);
            }
            result[0] = (char)('0' + digits);
        }

        static uint indexForExponent(uint e)
        {
            return (e + 15) / 16;
        }

        static uint pow10BitsForIndex(uint idx)
        {
            return 16 * idx + POW10_ADDITIONAL_BITS;
        }

        static uint lengthForIndex(uint idx)
        {
            // +1 for ceil, +16 for mantissa, +8 to round up when dividing by 9
            return (log10Pow2(16 * (int)idx) + 1 + 16 + 8) / 9;
        }

        static int copy_special_str_printf(Span<char> result, bool sign, ulong mantissa)
        {
            if (sign)
            {
                result[0] = '-';
            }
            int signI = Convert.ToInt32(sign);
            if (mantissa != 0)
            {
                memcpy(result.Slice(signI), "nan", 3);
                return signI + 3;
            }
            memcpy(result.Slice(signI), "Infinity", 8);
            return signI + 8;
        }

        int d2fixed_buffered_n(double d, int precision, Span<char> result)
        {
            ulong bits = double_to_bits(d);

            // Decode bits into sign, mantissa, and exponent.
            bool ieeeSign = ((bits >> (DOUBLE_MANTISSA_BITS + DOUBLE_EXPONENT_BITS)) & 1) != 0;
            ulong ieeeMantissa = bits & ((1ul << DOUBLE_MANTISSA_BITS) - 1);
            uint ieeeExponent = (uint)((bits >> DOUBLE_MANTISSA_BITS) & ((1u << DOUBLE_EXPONENT_BITS) - 1));

            // Case distinction; exit early for the easy cases.
            if (ieeeExponent == ((1u << DOUBLE_EXPONENT_BITS) - 1u))
            {
                return copy_special_str_printf(result, ieeeSign, ieeeMantissa);
            }

            int index = 0;
            if (ieeeExponent == 0 && ieeeMantissa == 0)
            {
                if (ieeeSign)
                {
                    result[index++] = '-';
                }
                result[index++] = '0';
                if (precision > 0)
                {
                    result[index++] = '.';
                    memset(result.Slice(index), '0', precision);
                    index += precision;
                }
                return index;
            }

            int e2;
            ulong m2;
            if (ieeeExponent == 0)
            {
                e2 = 1 - DOUBLE_BIAS - DOUBLE_MANTISSA_BITS;
                m2 = ieeeMantissa;
            }
            else
            {
                e2 = (int)ieeeExponent - DOUBLE_BIAS - DOUBLE_MANTISSA_BITS;
                m2 = (1ul << DOUBLE_MANTISSA_BITS) | ieeeMantissa;
            }

            bool nonzero = false;
            if (ieeeSign)
            {
                result[index++] = '-';
            }
            if (e2 >= -52)
            {
                uint idx = e2 < 0 ? 0 : indexForExponent((uint)e2);
                uint p10bits = pow10BitsForIndex(idx);
                int len = (int)lengthForIndex(idx);

                for (int i = len - 1; i >= 0; --i)
                {
                    uint j = p10bits - (uint)e2;
                    // Temporary: j is usually around 128, and by shifting a bit, we push it to 128 or above, which is
                    // a slightly faster code path in mulShift_mod1e9. Instead, we can just increase the multipliers.
                    uint digits = mulShift_mod1e9(m2 << 8, POW10_SPLIT[POW10_OFFSET[idx] + i], (int)(j + 8));
                    if (nonzero)
                    {
                        append_nine_digits(digits, result.Slice(index));
                        index += 9;
                    }
                    else if (digits != 0)
                    {
                        int olength = decimalLength9(digits);
                        append_n_digits(olength, digits, result.Slice(index));
                        index += olength;
                        nonzero = true;
                    }
                }
            }
            if (!nonzero)
            {
                result[index++] = '0';
            }
            if (precision > 0)
            {
                result[index++] = '.';
            }

            if (e2 < 0)
            {
                int idx = -e2 / 16;

                int blocks = (precision / 9 + 1);
                // 0 = don't round up; 1 = round up unconditionally; 2 = round up if odd.
                int roundUp = 0;
                int i = 0;
                if (blocks <= MIN_BLOCK_2[idx])
                {
                    i = blocks;
                    memset(result.Slice(index), '0', precision);
                    index += precision;
                }
                else if (i < MIN_BLOCK_2[idx])
                {
                    i = MIN_BLOCK_2[idx];
                    memset(result.Slice(index), '0', 9 * i);
                    index += 9 * i;
                }
                for (; i < blocks; ++i)
                {
                    int j = ADDITIONAL_BITS_2 + (-e2 - 16 * idx);
                    int p = POW10_OFFSET_2[idx] + i - MIN_BLOCK_2[idx];
                    if (p >= POW10_OFFSET_2[idx + 1])
                    {
                        // If the remaining digits are all 0, then we might as well use memset.
                        // No rounding required in this case.
                        int fill = precision - 9 * i;
                        memset(result.Slice(index), '0', fill);
                        index += fill;
                        break;
                    }
                    // Temporary: j is usually around 128, and by shifting a bit, we push it to 128 or above, which is
                    // a slightly faster code path in mulShift_mod1e9. Instead, we can just increase the multipliers.
                    uint digits = mulShift_mod1e9(m2 << 8, POW10_SPLIT_2[p], j + 8);

                    if (i < blocks - 1)
                    {
                        append_nine_digits(digits, result.Slice(index));
                        index += 9;
                    }
                    else
                    {
                        int maximum = precision - 9 * i;
                        uint lastDigit = 0;
                        for (uint k = 0; k < 9 - maximum; ++k)
                        {
                            lastDigit = digits % 10;
                            digits /= 10;
                        }

                        if (lastDigit != 5)
                        {
                            roundUp = Convert.ToInt32(lastDigit > 5);
                        }
                        else
                        {
                            // Is m * 10^(additionalDigits + 1) / 2^(-e2) integer?
                            int requiredTwos = -e2 - (int)precision - 1;
                            bool trailingZeros = requiredTwos <= 0
                             || (requiredTwos < 60 && multipleOfPowerOf2(m2, requiredTwos));
                            roundUp = trailingZeros ? 2 : 1;

                        }
                        if (maximum > 0)
                        {
                            append_c_digits(maximum, digits, result.Slice(index));
                            index += maximum;
                        }
                        break;
                    }
                }

                if (roundUp != 0)
                {
                    int roundIndex = index;
                    int dotIndex = 0; // '.' can't be located at index 0
                    while (true)
                    {
                        --roundIndex;
                        if (roundIndex == -1)
                        {
                            result[roundIndex + 1] = '1';
                            if (dotIndex > 0)
                            {
                                result[dotIndex] = '0';
                                result[dotIndex + 1] = '.';
                            }
                            result[index++] = '0';
                            break;
                        }
                        char c = result[roundIndex];
                        if (c == '-')
                        {
                            result[roundIndex + 1] = '1';
                            if (dotIndex > 0)
                            {
                                result[dotIndex] = '0';
                                result[dotIndex + 1] = '.';
                            }
                            result[index++] = '0';
                            break;
                        }
                        if (c == '.')
                        {
                            dotIndex = roundIndex;
                            continue;
                        }
                        else if (c == '9')
                        {
                            result[roundIndex] = '0';
                            roundUp = 1;
                            continue;
                        }
                        else
                        {
                            if (roundUp == 2 && c % 2 == 0)
                            {
                                break;
                            }
                            result[roundIndex] = Convert.ToChar(c + 1);
                            break;
                        }
                    }
                }
            }
            else
            {
                memset(result.Slice(index), '0', precision);
                index += (int)precision;
            }
            return index;
        }

        void d2fixed_buffered(double d, int precision, Span<char> result)
        {
            int len = d2fixed_buffered_n(d, precision, result);
            result[len] = '\0';
        }

        static int d2exp_buffered_n(double d, int precision, Span<char> result)
        {
            ulong bits = double_to_bits(d);

            // Decode bits into sign, mantissa, and exponent.
            bool ieeeSign = ((bits >> (DOUBLE_MANTISSA_BITS + DOUBLE_EXPONENT_BITS)) & 1) != 0;
            ulong ieeeMantissa = bits & ((1ul << DOUBLE_MANTISSA_BITS) - 1);
            uint ieeeExponent = (uint)((bits >> DOUBLE_MANTISSA_BITS) & ((1u << DOUBLE_EXPONENT_BITS) - 1));

            // Case distinction; exit early for the easy cases.
            if (ieeeExponent == ((1u << DOUBLE_EXPONENT_BITS) - 1u))
            {
                return copy_special_str_printf(result, ieeeSign, ieeeMantissa);
            }
            int index = 0;
            if (ieeeExponent == 0 && ieeeMantissa == 0)
            {
                if (ieeeSign)
                {
                    result[index++] = '-';
                }
                result[index++] = '0';
                if (precision > 0)
                {
                    result[index++] = '.';
                    memset(result.Slice(index), '0', precision);
                    index += precision;
                }
                memcpy(result.Slice(index), "e+00", 4);
                index += 4;
                return index;
            }

            int e2;
            ulong m2;
            if (ieeeExponent == 0)
            {
                e2 = 1 - DOUBLE_BIAS - DOUBLE_MANTISSA_BITS;
                m2 = ieeeMantissa;
            }
            else
            {
                e2 = (int)ieeeExponent - DOUBLE_BIAS - DOUBLE_MANTISSA_BITS;
                m2 = (1ul << DOUBLE_MANTISSA_BITS) | ieeeMantissa;
            }

            bool printDecimalPoint = precision > 0;
            ++precision;
            if (ieeeSign)
            {
                result[index++] = '-';
            }
            uint digits = 0;
            int printedDigits = 0;
            int availableDigits = 0;
            int exp = 0;
            if (e2 >= -52)
            {
                uint idx = e2 < 0 ? 0 : indexForExponent((uint)e2);
                uint p10bits = pow10BitsForIndex(idx);
                int len = (int)lengthForIndex(idx);

                for (int i = len - 1; i >= 0; --i)
                {
                    int j = (int)p10bits - e2;
                    // Temporary: j is usually around 128, and by shifting a bit, we push it to 128 or above, which is
                    // a slightly faster code path in mulShift_mod1e9. Instead, we can just increase the multipliers.
                    digits = mulShift_mod1e9(m2 << 8, POW10_SPLIT[POW10_OFFSET[idx] + i], (int)(j + 8));
                    if (printedDigits != 0)
                    {
                        if (printedDigits + 9 > precision)
                        {
                            availableDigits = 9;
                            break;
                        }
                        append_nine_digits(digits, result.Slice(index));
                        index += 9;
                        printedDigits += 9;
                    }
                    else if (digits != 0)
                    {
                        availableDigits = decimalLength9(digits);
                        exp = i * 9 + (int)availableDigits - 1;
                        if (availableDigits > precision)
                        {
                            break;
                        }
                        if (printDecimalPoint)
                        {
                            append_d_digits(availableDigits, digits, result.Slice(index));
                            index += availableDigits + 1; // +1 for decimal point
                        }
                        else
                        {
                            result[index++] = (char)('0' + digits);
                        }
                        printedDigits = availableDigits;
                        availableDigits = 0;
                    }
                }
            }

            if (e2 < 0 && availableDigits == 0)
            {
                int idx = -e2 / 16;

                for (int i = MIN_BLOCK_2[idx]; i < 200; ++i)
                {
                    int j = ADDITIONAL_BITS_2 + (-e2 - 16 * idx);
                    uint p = POW10_OFFSET_2[idx] + (uint)i - MIN_BLOCK_2[idx];
                    // Temporary: j is usually around 128, and by shifting a bit, we push it to 128 or above, which is
                    // a slightly faster code path in mulShift_mod1e9. Instead, we can just increase the multipliers.
                    digits = (p >= POW10_OFFSET_2[idx + 1]) ? 0 : mulShift_mod1e9(m2 << 8, POW10_SPLIT_2[p], j + 8);

                    if (printedDigits != 0)
                    {
                        if (printedDigits + 9 > precision)
                        {
                            availableDigits = 9;
                            break;
                        }
                        append_nine_digits(digits, result.Slice(index));
                        index += 9;
                        printedDigits += 9;
                    }
                    else if (digits != 0)
                    {
                        availableDigits = decimalLength9(digits);
                        exp = -(i + 1) * 9 + (int)availableDigits - 1;
                        if (availableDigits > precision)
                        {
                            break;
                        }
                        if (printDecimalPoint)
                        {
                            append_d_digits(availableDigits, digits, result.Slice(index));
                            index += availableDigits + 1; // +1 for decimal point
                        }
                        else
                        {
                            result[index++] = (char)('0' + digits);
                        }
                        printedDigits = availableDigits;
                        availableDigits = 0;
                    }
                }
            }

            int maximum = precision - printedDigits;

            if (availableDigits == 0)
            {
                digits = 0;
            }
            uint lastDigit = 0;
            if (availableDigits > maximum)
            {
                for (uint k = 0; k < availableDigits - maximum; ++k)
                {
                    lastDigit = digits % 10;
                    digits /= 10;
                }
            }

            // 0 = don't round up; 1 = round up unconditionally; 2 = round up if odd.
            int roundUp = 0;
            if (lastDigit != 5)
            {
                roundUp = Convert.ToInt32(lastDigit > 5);
            }
            else
            {
                // Is m * 2^e2 * 10^(precision + 1 - exp) integer?
                // precision was already increased by 1, so we don't need to write + 1 here.
                int rexp = (int)precision - exp;
                int requiredTwos = -e2 - rexp;
                bool trailingZeros = requiredTwos <= 0
                  || (requiredTwos < 60 && multipleOfPowerOf2(m2, requiredTwos));
                if (rexp < 0)
                {
                    int requiredFives = -rexp;
                    trailingZeros = trailingZeros && multipleOfPowerOf5(m2, (uint)requiredFives);
                }
                roundUp = trailingZeros ? 2 : 1;

            }
            if (printedDigits != 0)
            {
                if (digits == 0)
                {
                    memset(result.Slice(index), '0', maximum);
                }
                else
                {
                    append_c_digits(maximum, digits, result.Slice(index));
                }
                index += maximum;
            }
            else
            {
                if (printDecimalPoint)
                {
                    append_d_digits(maximum, digits, result.Slice(index));
                    index += maximum + 1; // +1 for decimal point
                }
                else
                {
                    result[index++] = (char)('0' + digits);
                }
            }

            if (roundUp != 0)
            {
                int roundIndex = index;
                while (true)
                {
                    --roundIndex;
                    if (roundIndex == -1)
                    {
                        result[roundIndex + 1] = '1';
                        ++exp;
                        break;
                    }
                    char c = result[roundIndex];
                    if (c == '-')
                    {
                        result[roundIndex + 1] = '1';
                        ++exp;
                        break;
                    }
                    if (c == '.')
                    {
                        continue;
                    }
                    else if (c == '9')
                    {
                        result[roundIndex] = '0';
                        roundUp = 1;
                        continue;
                    }
                    else
                    {
                        if (roundUp == 2 && c % 2 == 0)
                        {
                            break;
                        }
                        result[roundIndex] = Convert.ToChar(c + 1);
                        break;
                    }
                }
            }
            result[index++] = 'e';
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
            else
            {
                memcpy(result.Slice(index), DIGIT_TABLE, 2 * exp, 2);
                index += 2;
            }

            return index;
        }

        void d2exp_buffered(double d, int precision, Span<char> result)
        {
            int len = d2exp_buffered_n(d, precision, result);
            result[len] = '\0';
        }

        public static string DoubleToString(double f, int precision)
        {
            if (precision < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(precision), "Expected [0, 1000]");
            }
            if (precision > 16)
            {
                return DoubleToStringArrayPool(f, precision);
            }
            return DoubleToStringRefStruct(f, precision);
        }

        private static string DoubleToStringRefStruct(double f, int precision)
        {
            Debug.Assert(precision >= 0);
            Debug.Assert(precision <= 16);
            Span<char> span = stackalloc char[precision + 8];
            int index = d2exp_buffered_n(f, precision, span);
            return new string(span.Slice(0, index));
        }

        private static string DoubleToStringArrayPool(double f, int precision)
        {
            ArrayPool<char> pool = ArrayPool<char>.Shared;
            char[] rented = pool.Rent(precision + 8);
            Span<char> rentedSpan = rented.AsSpan();
            int index = d2exp_buffered_n(f, precision, rented);
            string result = new string(rentedSpan.Slice(0, index));
            pool.Return(rented);
            return result;
        }

    }
}
