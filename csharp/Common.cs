using System;
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
        static int copy_special_str(Span<char> result, bool sign, bool exponent, bool mantissa)
        {
            if (mantissa)
            {
                result[0] = 'N';
                result[1] = 'a';
                result[2] = 'N';
                return 3;
            }
            if (sign)
            {
                result[0] = '-';
            }
            int signI = BooleanToInt32(sign);
            if (exponent)
            {
                result[0] = '∞';
                return signI + 1;
            }
            else
            {
                result[0] = '0';
                result[1] = 'E';
                result[2] = '0';
                return signI + 3;
            }
        }
    }
}
