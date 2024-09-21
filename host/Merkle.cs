using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Reflection;
using static System.Net.WebRequestMethods;

namespace host
{

    public abstract class Bytes32_Json<T> where T : Bytes32_Json<T>, new()
    {
        public Bytes32_Json(byte[] _data)
        {
            data = new byte[32];
            for (var i = 0; i < 32; i++)
            {
                this.data[i] = _data[i];
            }
        }
        protected Bytes32_Json()
        {
            data = new byte[32];
        }
        public byte[] data
        {
            get;
            private set;
        }
        public JArray ToJson()
        {
            var json = new JArray();
            for (var i = 0; i < 32; i++)
            {
                json.Add(this.data[i]);
            }
            return json;
        }
        public static T FromJson(JArray json)
        {
            T root = new T();
            for (var i = 0; i < 32; i++)
            {
                root.data[i] = (byte)json[i];
            }
            return root;
        }
        public override string ToString()
        {
            return HashTool.Hex2Str(data);
        }
    }
    public class MerkleRoot : Bytes32_Json<MerkleRoot>
    {
        public MerkleRoot()
        {

        }
        public MerkleRoot(byte[] _data) : base(_data)
        {

        }
    }
    public class Hash : Bytes32_Json<Hash>
    {
        public Hash()
        {

        }
        public Hash(byte[] _data) : base(_data)
        {

        }
    }
    public class Data
    {
        public Data(byte[] _data)
        {
            var len = _data.Length / 8;
            if (_data.Length % 8 > 0)
                len++;
            data = new byte[len * 8];
            for(var i=0;i<_data.Length;i++)
            {
                data[i] = _data[i];
            }
        }
        public Data(int len)
        {
            if (len % 8 > 0)
                throw new Exception("must be 8's 倍数");
            data = new byte[len];
        }
        public byte[] data
        {
            get;
            private set;
        }
        public Hash CalcHash()
        {
            return new Hash(HashTool.CalcHash(data));
        }
        public JArray ToJson()
        {
            var json = new JArray();

            for (var i = 0; i < data.Length / 8; i++)
            {
                var l = BitConverter.ToUInt64(data, i * 8);
                json.Add(l.ToString());
            }
            return json;
        }
        public static Data FromJson(JArray json)
        {
            Data root = new Data(json.Count * 8);
            for (var i = 0; i < json.Count; i++)
            {
                var l = ulong.Parse(json[i].ToString());
                var data = BitConverter.GetBytes(l);
                for (var j = 0; j < 8; j++)
                {
                    root.data[i * 8 + j] = data[j];
                }

            }
            return root;
        }
        public override string ToString()
        {
            return HashTool.Hex2Str(data);
        }
    }

    public class MerkleDBHelper
    {
        public static MerkleRoot GetEmptyRoot()
        {
            //这是zkwasm 用来初始化默克尔树的根数据
            //来自https://github.com/DelphinusLab/zkWasm-rust/blob/main/src/merkle.rs
            UInt64 v1 = 14789582351289948625;
            UInt64 v2 = 10919489180071018470;
            UInt64 v3 = 10309858136294505219;
            UInt64 v4 = 2839580074036780766;

            var data1 = BitConverter.GetBytes(v1);
            var data2 = BitConverter.GetBytes(v2);
            var data3 = BitConverter.GetBytes(v3);
            var data4 = BitConverter.GetBytes(v4);
            var root = new byte[32];
            for (var i = 0; i < 8; i++)
            {
                root[0 + i] = data1[i];
                root[8 + i] = data2[i];
                root[16 + i] = data3[i];
                root[24 + i] = data4[i];
            }
            return new MerkleRoot(root);
        }
        public static ulong GetFirstIndex()
        {
            const int depth = 32;
            return (ulong)(((long)2 << (depth - 1)) - 1);
        }
        public static ulong GetLastIndex()
        {
            const int depth = 32;
            return (ulong)(((long)2 << (depth)) - 2);
        }
        public static Uri uri = new Uri("http://18.162.245.133:999/");
        const int MERKLE_TREE_HEIGHT = 32;
        public static async Task<MerkleRoot> update_leaf(MerkleRoot root, ulong address, Hash hash)
        {
            HttpClient http = new HttpClient();
            var req = new JObject();
            req["root"] = root.ToJson();
            req["data"] = hash.ToJson();

            var index = (address ) + (1ul << MERKLE_TREE_HEIGHT) -1;

            req["index"] = index.ToString();

            var jsonrpc = new JObject();
            jsonrpc["jsonrpc"] = "2.0";
            jsonrpc["method"] = "update_leaf";
            jsonrpc["params"] = req;
            jsonrpc["id"] = 1;
            var upstr = jsonrpc.ToString();

            HttpContent content = new StringContent(upstr);
            var res = await http.PostAsync(uri, content);
            var txt = await res.Content.ReadAsStringAsync();
            JArray result = JObject.Parse(txt)["result"] as JArray;
            if (result == null)
                return null;
            var rootResult = MerkleRoot.FromJson(result);
            return rootResult;
        }
        public static async Task<Hash> get_leaf(MerkleRoot root, ulong address)
        {
            HttpClient http = new HttpClient();
            var req = new JObject();
            req["root"] = root.ToJson();

            var index = (address) + (1ul << MERKLE_TREE_HEIGHT) - 1;

            req["index"] = index.ToString();

            var jsonrpc = new JObject();
            jsonrpc["jsonrpc"] = "2.0";
            jsonrpc["method"] = "get_leaf";
            jsonrpc["params"] = req;
            jsonrpc["id"] = 1;
            var upstr = jsonrpc.ToString();

            HttpContent content = new StringContent(upstr);
            var res = await http.PostAsync(uri, content);
            var txt = await res.Content.ReadAsStringAsync();
            JArray result = JObject.Parse(txt)["result"] as JArray;
            if (result == null)
                return null;
            var rootResult = Hash.FromJson(result);
            return rootResult;
        }

        public static async Task<bool> update_record(Hash hash, Data data)
        {
            HttpClient http = new HttpClient();
            var req = new JObject();
            req["hash"] = hash.ToJson();
            req["data"] = data.ToJson();

            var jsonrpc = new JObject();
            jsonrpc["jsonrpc"] = "2.0";
            jsonrpc["method"] = "update_record";
            jsonrpc["params"] = req;
            jsonrpc["id"] = 1;
            var upstr = jsonrpc.ToString();

            HttpContent content = new StringContent(upstr);
            
            var res = await http.PostAsync(uri, content);
            var txt = await res.Content.ReadAsStringAsync();
            JObject result = JObject.Parse(txt);
            if (result.ContainsKey("error"))
                return false;
            return true;
        }
        public static async Task<Data> get_record(Hash hash)
        {
            HttpClient http = new HttpClient();
            var req = new JObject();
            req["hash"] = hash.ToJson();

            var jsonrpc = new JObject();
            jsonrpc["jsonrpc"] = "2.0";
            jsonrpc["method"] = "get_record";
            jsonrpc["params"] = req;
            jsonrpc["id"] = 1;
            var upstr = jsonrpc.ToString();

            HttpContent content = new StringContent(upstr);
            var res = await http.PostAsync(uri, content);
            var txt = await res.Content.ReadAsStringAsync();
            JArray result = JObject.Parse(txt)["result"] as JArray;
            if (result == null)
                return null;
            
            return Data.FromJson(result);
        }
    }
}