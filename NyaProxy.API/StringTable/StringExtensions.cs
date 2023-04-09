using System;
using System.Collections.Generic;
using System.Text;

namespace StringTable
{
    public static class StringExtensions
    {
        public static int RealLength(this string value, bool withUtf8Characters)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            if (!withUtf8Characters)
                return value.Length;

            int count = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '§')
                    i += 1;
                else if (c >= 0x4e00 && c <= 0x9fbb)
                    count += 2; //utf8
                else
                    count++;
            }
            return count;
        }
    }
}
