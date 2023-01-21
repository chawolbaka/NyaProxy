using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.Extension
{
    public static class i18nExtension
    {
        public static string Replace(this string str, string input, object replace) => str.Replace(input, replace.ToString());

        public static string Replace(this string str, params object[] replaces)
        {
            if (replaces.Length < 2 || replaces.Length % 2 > 0)
                throw new ArgumentOutOfRangeException(nameof(replaces));

            for (int i = 0; i < replaces.Length; i++)
            {
                str = str.Replace(replaces[i].ToString(), replaces[++i].ToString());
            }
            return str;
        }
    }
}
