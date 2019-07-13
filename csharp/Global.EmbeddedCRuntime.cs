using System;
using System.Collections.Generic;
using System.Text;

namespace Ryu
{
    partial class Global
    {
        static void memcpy(Span<char> dest, char[] src, uint srcOffset, int len)
        {
            src.AsSpan().Slice((int)srcOffset, len).CopyTo(dest);
        }

        static void memcpy(Span<char> dest, string src, int len)
        {
            src.AsSpan().Slice(0, len).CopyTo(dest);
        }

        static void memcpy(Span<char> dest, char[] src, int srcOffset, int len)
        {
            src.AsSpan().Slice(srcOffset, len).CopyTo(dest);
        }

        static void memset(Span<char> str, char value, int count)
        {
            for (int i = 0; i < count; i++)
            {
                str[i] = value;
            }
        }
    }
}
