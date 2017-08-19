using System;
using System.Collections.Generic;
using System.Text;

namespace Pipenet
{
    internal class Util
    {
        public static bool ByteArrayEquals(byte[] a,byte[] b)
        {
            int a_len = a.Length;
            int b_len = b.Length;
            if (a_len != b_len)
                return false;
            for(int i = 0; i < a_len; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;

        }
        public static byte[] JoinByteArray(byte[] a,byte[] b)
        {
            List<byte> temp = new List<byte>();
            temp.AddRange(a);
            temp.AddRange(b);
            return temp.ToArray();
        }
    }
}
