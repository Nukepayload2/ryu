﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Ryu
{
    partial class Global // Common
    {
        // Returns e == 0 ? 1 : ceil(log_2(5^e)).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int pow5bits(int e)
        {
            // This approximation works up to the point that the multiplication overflows at e = 3529.
            // If the multiplication were done in 64 bits, it would fail at 5^4004 which is just greater
            // than 2^9297.
            return (int)(((((uint)e) * 1217359) >> 19) + 1);
        }

        // Returns floor(log_10(2^e)).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint log10Pow2(int e)
        {
            // The first value this approximation fails for is 2^1651 which is just greater than 10^297.
            return (((uint)e) * 78913) >> 18;
        }

        // Returns floor(log_10(5^e)).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint log10Pow5(int e)
        {
            // The first value this approximation fails for is 5^2621 which is just greater than 10^1832.
            return (((uint)e) * 732923) >> 20;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int copy_special_str(Span<sbyte> result, bool sign, bool exponent, bool mantissa)
        {
            if (mantissa)
            {
                result[0] = (sbyte)'N';
                result[1] = (sbyte)'a';
                result[2] = (sbyte)'N';
                return 3;
            }
            if (sign)
            {
                result[0] = (sbyte)'-';
            }
            int signI = BooleanToInt32(sign);
            if (exponent)
            {
                result[0] = (sbyte)'I';
                result[1] = (sbyte)'n';
                result[2] = (sbyte)'f';
                result[3] = (sbyte)'i';
                result[4] = (sbyte)'n';
                result[5] = (sbyte)'i';
                result[6] = (sbyte)'t';
                result[7] = (sbyte)'y';
                return signI + 8;
            }
            else
            {
                result[0] = (sbyte)'0';
                result[1] = (sbyte)'E';
                result[2] = (sbyte)'0';
                return signI + 3;
            }
        }

        [ThreadStatic]
        private static StringBuilder t_NumberFormatterSharedStringBuilder;

        private static string CopyAsciiSpanToNewString(Span<sbyte> result, int strLen)
        {
            if (t_NumberFormatterSharedStringBuilder == null)
            {
                t_NumberFormatterSharedStringBuilder = new StringBuilder();
            }
            else
            {
                t_NumberFormatterSharedStringBuilder.Clear();
            }
            var sb = t_NumberFormatterSharedStringBuilder;
            for (int i = 0; i < strLen; i++)
            {
                var ch = result[i];
                sb.Append((char)ch);
            }
            return sb.ToString();
        }
    }
}
