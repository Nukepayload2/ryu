using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Ryu
{
    partial class Global //ConversionHelper
    {
        static unsafe int BooleanToInt32(bool value)
        {
            bool* pBool = &value;
            int* pInt32 = (int*)(void*)pBool;
            return *pInt32;
        }

        static unsafe uint BooleanToUInt32(bool value)
        {
            bool* pBool = &value;
            uint* pInt32 = (uint*)(void*)pBool;
            return *pInt32;
        }

        static unsafe ulong BooleanToUInt64(bool value)
        {
            bool* pBool = &value;
            uint* pInt32 = (uint*)(void*)pBool;
            return *pInt32;
        }

        static unsafe void memcpy(void* dest, void* src, int len)
        {
            Buffer.MemoryCopy(src, dest, len, len);
        }

        static unsafe void memcpy(void* dest, byte[] src, uint srcOffset, int len)
        {
            Marshal.Copy(src, (int)srcOffset, new IntPtr(dest), len);
        }

        static unsafe void memcpy(void* dest, byte[] src, int srcOffset, int len)
        {
            Marshal.Copy(src, srcOffset, new IntPtr(dest), len);
        }
    }
}
