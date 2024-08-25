using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace host
{
    public class MerkleSync
    {
        public static MerkleRoot Ulongs2Root(IList<ulong> arr)
        {
            var b1 = BitConverter.GetBytes(arr[0]);
            var b2 = BitConverter.GetBytes(arr[1]);
            var b3 = BitConverter.GetBytes(arr[2]);
            var b4 = BitConverter.GetBytes(arr[3]);
            byte[] hash = new byte[32];
            for (var i = 0; i < 32; i++)
            {
                hash[0 + i] = b1[i];
                hash[8 + i] = b2[i];
                hash[16 + i] = b3[i];
                hash[24 + i] = b4[i];
            }
            return new MerkleRoot(hash);
        }
        public static Hash Ulongs2Hash(IList<ulong> arr)
        {
            var b1 = BitConverter.GetBytes(arr[0]);
            var b2 = BitConverter.GetBytes(arr[1]);
            var b3 = BitConverter.GetBytes(arr[2]);
            var b4 = BitConverter.GetBytes(arr[3]);
            byte[] hash = new byte[32];
            for (var i = 0; i < 32; i++)
            {
                hash[0 + i] = b1[i];
                hash[8 + i] = b2[i];
                hash[16 + i] = b3[i];
                hash[24 + i] = b4[i];
            }
            return new Hash(hash);
        }
        public static Data Ulongs2Data(IList<ulong> arr)
        {

            byte[] data = new byte[8 * arr.Count];
            for (var i = 0; i < arr.Count; i++)
            {
                var b = BitConverter.GetBytes(arr[i]);
                for (var j = 0; j < 8; j++)
                {
                    data[i * 8 + j] = b[j];
                }
            }
            return new Data(data);
        }
        public static ulong[] update_leaf(ulong[] root, ulong index, ulong[] hash)
        {
            var task = MerkleDBHelper.update_leaf(Ulongs2Root(root), index, Ulongs2Hash(hash));
            task.Wait();
            var outroot = new ulong[4];
            if (task.Result != null)
            {
                for (var i = 0; i < 4; i++)
                {
                    outroot[i] = BitConverter.ToUInt64(task.Result.data, i * 8);
                }
            }
            return outroot;
        }
        public static ulong[] get_leaf(ulong[] root, ulong index)
        {
            var task = MerkleDBHelper.get_leaf(Ulongs2Root(root), index);
            task.Wait();
            var outhash = new ulong[4];
            if (task.Result != null)
            {
                for (var i = 0; i < 4; i++)
                {
                    outhash[i] = BitConverter.ToUInt64(task.Result.data, i * 8);
                }
            }
            return outhash;
        }
        public static bool update_record(ulong[] _hash, List<ulong> data)
        {
            var task = MerkleDBHelper.update_record(Ulongs2Hash(_hash), Ulongs2Data(data));
            task.Wait();
            return task.Result;
        }
        public static bool get_record(ulong[] _hash, List<ulong> outdata)
        {
            var task = MerkleDBHelper.get_record(Ulongs2Hash(_hash));
            task.Wait();
            if (task.Result == null)
                return false;

            outdata.Clear();
            var intcount = task.Result.data.Length / 8;
            for (var i = 0; i < intcount; i++)
            {
                outdata.Add(BitConverter.ToUInt64(task.Result.data, i * 8));
            }
            return true;
        }
    }
}
