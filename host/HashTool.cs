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
            return ToHexStr(hash);
        }
        public static string ToHexStr(byte[] data)
        {
            var str = "";
            for(var i=0;i<data.Length; i++) {
                str += data[i].ToString("X02");
            }
            return str;
        }
    }
}
