using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Ryu
{
    partial class Global
    {
        static ulong umul128(ulong a, ulong b, out ulong productHi)
        {
            // The casts here help MSVC to avoid calls to the __allmul library function.
            uint aLo = (uint)a;
            uint aHi = (uint)(a >> 32);
            uint bLo = (uint)b;
            uint bHi = (uint)(b >> 32);

            ulong b00 = (ulong)aLo * bLo;
            ulong b01 = (ulong)aLo * bHi;
            ulong b10 = (ulong)aHi * bLo;
            ulong b11 = (ulong)aHi * bHi;

            uint b00Lo = (uint)b00;
            uint b00Hi = (uint)(b00 >> 32);

            ulong mid1 = b10 + b00Hi;
            uint mid1Lo = (uint)(mid1);
            uint mid1Hi = (uint)(mid1 >> 32);

            ulong mid2 = b01 + mid1Lo;
            uint mid2Lo = (uint)(mid2);
            uint mid2Hi = (uint)(mid2 >> 32);

            ulong pHi = b11 + mid1Hi + mid2Hi;
            ulong pLo = ((ulong)mid2Lo << 32) | b00Lo;

            productHi = pHi;
            return pLo;
        }

        static ulong shiftright128(ulong lo, ulong hi, int dist)
        {
            // We don't need to handle the case dist >= 64 here (see above).
            Debug.Assert(dist < 64);

            // Avoid a 64-bit shift by taking advantage of the range of shift values.
            Debug.Assert(dist >= 32);
            return (hi << (64 - dist)) | ((uint)(lo >> 32) >> (dist - 32));
        }

        static ulong div5(ulong x)
        {
            return x / 5;
        }

        static ulong div10(ulong x)
        {
            return x / 10;
        }

        static ulong div100(ulong x)
        {
            return x / 100;
        }

        static ulong div1e8(ulong x)
        {
            return x / 100000000;
        }

        static ulong div1e9(ulong x)
        {
            return x / 1000000000;
        }

        static uint mod1e9(ulong x)
        {
            return (uint)(x - 1000000000 * div1e9(x));
        }

        static uint pow5Factor(ulong value)
        {
            uint count = 0;
            for (; ; )
            {
                Debug.Assert(value != 0);
                ulong q = div5(value);
                uint r = ((uint)value) - 5 * ((uint)q);
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
        static bool multipleOfPowerOf5(ulong value, uint p)
        {
            // I tried a case distinction on p, but there was no performance difference.
            return pow5Factor(value) >= p;
        }

        // Returns true if value is divisible by 2^p.
        static bool multipleOfPowerOf2(ulong value, int p)
        {
            Debug.Assert(value != 0);
            // return __builtin_ctzll(value) >= p;
            return (value & ((1ul << p) -1)) == 0;
        }

    }
}
