using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace host
{
    internal class HashTool
    {
        [ThreadStatic]
        static SHA256 sha256;

        public static byte[] CalcHash(byte[] data)
        {
            if (sha256 == null)
            {
                sha256 = SHA256.Create();
            }
            return sha256.ComputeHash(data);
        }
        public static string CalcHashStr(byte[] data)
        {
            var hash =CalcHash(data);
            return Hex2Str(hash);
        }
        public static string Hex2Str(byte[] data)
        {
            var str = "";
            for(var i=0;i<data.Length; i++) {
                str += data[i].ToString("X02");
            }
            return str;
        }
        public static byte[] Str2Hex(string str)
        {
            var data = new byte[str.Length / 2];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = byte.Parse(str.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return data;
        }
    }
}
