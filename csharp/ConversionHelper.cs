using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Ryu
{
    partial class Global //ConversionHelper
    {
        static int BooleanToInt32(bool value)
        {
            return value ? 1 : 0;
        }

        static uint BooleanToUInt32(bool value)
        {
            return value ? 1u : 0u;
        }

        static ulong BooleanToUInt64(bool value)
        {
            return value ? 1ul : 0ul;
        }

        static void memcpy(Span<sbyte> dest, sbyte[] src, uint srcOffset, int len)
        {
            src.AsSpan().Slice((int)srcOffset, len).CopyTo(dest);
        }

        static void memcpy(Span<sbyte> dest, sbyte[] src, int srcOffset, int len)
        {
            src.AsSpan().Slice(srcOffset, len).CopyTo(dest);
        }
    }
}
