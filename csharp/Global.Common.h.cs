using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Ryu
{
    partial class Global
    {
        // Copyright 2018 Ulf Adams
        //
        // The contents of this file may be used under the terms of the Apache License,
        // Version 2.0.
        //
        //    (See accompanying file LICENSE-Apache or copy at
        //     http://www.apache.org/licenses/LICENSE-2.0)
        //
        // Alternatively, the contents of this file may be used under the terms of
        // the Boost Software License, Version 1.0.
        //    (See accompanying file LICENSE-Boost or copy at
        //     https://www.boost.org/LICENSE_1_0.txt)
        //
        // Unless required by applicable law or agreed to in writing, this software
        // is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
        // KIND, either express or implied.

        static int decimalLength9(uint v)
        {
            // Function precondition: v is not a 10-digit number.
            // (f2s: 9 digits are sufficient for round-tripping.)
            // (d2fixed: We print 9-digit blocks.)
            Debug.Assert(v < 1000000000);
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

        // Returns e == 0 ? 1 : ceil(log_2(5^e)).
        static int pow5bits(int e)
        {
            // This approximation works up to the point that the multiplication overflows at e = 3529.
            // If the multiplication were done in 64 bits, it would fail at 5^4004 which is just greater
            // than 2^9297.
            Debug.Assert(e >= 0);
            Debug.Assert(e <= 3528);
            return (int)(((((uint)e) * 1217359) >> 19) + 1);
        }

        // Returns floor(log_10(2^e)).
        static uint log10Pow2(int e)
        {
            // The first value this approximation fails for is 2^1651 which is just greater than 10^297.
            Debug.Assert(e >= 0);
            Debug.Assert(e <= 1650);
            return (((uint)e) * 78913) >> 18;
        }

        // Returns floor(log_10(5^e)).
        static uint log10Pow5(int e)
        {
            // The first value this approximation fails for is 5^2621 which is just greater than 10^1832.
            Debug.Assert(e >= 0);
            Debug.Assert(e <= 2620);
            return (((uint)e) * 732923) >> 20;
        }

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
            if (exponent)
            {
                result[0] = '∞';
                return Convert.ToInt32(sign) + 1;
            }
            else
            {
                result[0] = '0';
                result[1] = 'E';
                result[2] = '0';
                return Convert.ToInt32(sign) + 3;
            }
        }

        static uint float_to_bits(float f)
        {
            return (uint)BitConverter.SingleToInt32Bits(f);
        }

        static ulong double_to_bits(double d)
        {
            return (ulong)BitConverter.DoubleToInt64Bits(d);
        }
    }
}
