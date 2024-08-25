using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Wasmtime;

namespace host
{
    internal class WasmLogicTool
    {
        static string g_savepath;
        public static void Init(string savepath)
        {
            g_engine = new Wasmtime.Engine();
            g_savepath = savepath;
            if (System.IO.Directory.Exists(g_savepath) == false)
            {
                Console.WriteLine("try create folder=" + g_savepath);
                System.IO.Directory.CreateDirectory(g_savepath);
                Console.WriteLine("create folder=" + g_savepath);


            }
        }
        public static System.Collections.Concurrent.ConcurrentDictionary<string, byte[]> g_datas
            = new System.Collections.Concurrent.ConcurrentDictionary<string, byte[]>();

        public static void SetupWasm(string hashWasm, byte[] data)
        {
            if (g_datas.TryGetValue(hashWasm, out var _data))
            {
                return;
            }
            g_datas[hashWasm] = data;
            var outwasm = System.IO.Path.Combine(g_savepath, hashWasm + ".wasm");
            System.IO.File.WriteAllBytes(outwasm, data);
        }
        [ThreadStatic]
        static Dictionary<string, Wasmtime.Module> g_modules;
        static Wasmtime.Engine g_engine;
        public static Wasmtime.Module GetWasmModule(string hashWasm)
        {
            if (g_modules == null)
            {
                g_modules = new Dictionary<string, Wasmtime.Module>();
            }
            if (g_modules.TryGetValue(hashWasm, out var v))
            {
                return v;
            }

            if (!g_datas.ContainsKey(hashWasm))
            {
                var outwasm = System.IO.Path.Combine(g_savepath, hashWasm + ".wasm");
                if (System.IO.File.Exists(outwasm))
                {
                    g_datas[hashWasm] = System.IO.File.ReadAllBytes(outwasm);
                }

            }
            if (g_datas.TryGetValue(hashWasm, out var data))
            {
                g_modules[hashWasm] = Wasmtime.Module.FromBytes(g_engine, hashWasm, data);
                return g_modules[hashWasm];
            }
            return null;
        }

        public static Int64[] RunWasm(Wasmtime.Module module, Int64[] input)
        {

            using var linker = new Wasmtime.Linker(g_engine);


            List<long> outputs = new List<long>();

            using var store = new Wasmtime.Store(g_engine);

            int seek = 0;
            Wasmtime.CallerFunc<int, Int64> wasm_input = (caller, i) =>
            {
                var v = input[seek];
                seek++;
                return v;
            };
            Wasmtime.CallerAction<int, int, int, int> abort = (caller, a, b, c, d) =>
            {
                throw new Exception("abort has called:" + a + "," + b + "," + c + "," + d);
            };
            Wasmtime.CallerAction<Int64> wasm_output = (caller, v) =>
            {

                outputs.Add(v);

            };
            linker.Define("env", "wasm_input", Wasmtime.Function.FromCallback(store, wasm_input));
            linker.Define("env", "wasm_output", Wasmtime.Function.FromCallback(store, wasm_output));
            LinkMerkleFunc(linker, store);
            LinkCacheFunc(linker, store);

            var inst = linker.Instantiate(store, module);
            var funcs = inst.GetFunctions();
            foreach (var f in funcs)
            {
                var n = f.Name;
            }
            var run = inst.GetAction("logicmain");
            if (run == null)
            {
                throw new Exception("Need a function void logicmain()");
            }
            run();

            return outputs.ToArray();

        }
        private static void LinkMerkleFunc(Linker linker, Store store)
        {
            //    let root = [
            //    14789582351289948625,
            //    10919489180071018470,
            //    10309858136294505219,
            //    2839580074036780766,
            //];
            //pub fn merkle_setroot(x: u64);
            //pub fn merkle_address(x: u64);
            //pub fn merkle_set(x: u64);
            //pub fn merkle_get()->u64;
            //pub fn merkle_getroot()->u64;
            UInt64 address = 0;
            UInt64[] _root = new UInt64[4];
            UInt64[] _hash = new UInt64[4];
            int _rootseek = 0;
            int _hashseek = 0;
            //update操作 merkle_address=>merkle_setroot*4=>merkle_set*4=>merkle_getroot*4
            //get操作    merkle_address=>merkle_setroot*4=>merkle_get*4=>merkle_getroot*4(虽然没有意义，但还是操作一下)
            Wasmtime.CallerAction<Int64> merkle_address = (caller, i) =>
            {
                address = (ulong)i;
                _rootseek = 0;
                _hashseek = 0;
            };
            Wasmtime.CallerAction<Int64> merkle_setroot = (caller, i) => //pub fn merkle_setroot(x: u64);
            {
                _root[_rootseek] = (ulong)i;
                _rootseek++;
            };
            Wasmtime.CallerFunc<Int64> merkle_getroot = (caller) => //pub fn merkle_getroot()->u64;
            {
                var r = _root[_rootseek];
                _rootseek++;
                return (long)r;
            };
            Wasmtime.CallerAction<Int64> merkle_set = (caller, i) => //pub fn merkle_setroot(x: u64);
            {
                _hash[_hashseek] = (ulong)i;
                _hashseek++;
                if(_hashseek == 4)
                {
                    //保存merkleHash
                    var newroot = MerkleSync.update_leaf(_root, address, _hash);
                    _rootseek = 0;
                    //_root = 新的默克尔根
                    for(var _i=0;_i<4;_i++)
                    {
                        _root[_i] = newroot[_i];
                    }
                }
            };
            Wasmtime.CallerFunc<Int64> merkle_get = (caller) => //pub fn merkle_getroot()->u64;
            {
                if(_hashseek==0)
                {
                    //从数据库读取MerkleHash
                    var hash = MerkleSync.get_leaf(_root, address);
                    _rootseek = 0;
                    for (var _i = 0; _i < 4; _i++)
                    {
                        _hash[_i] = hash[_i];
                    }
                }
                var r = _hash[_hashseek];
                _hashseek++;
                return (long)r;
            };
            linker.Define("env", "merkle_address", Wasmtime.Function.FromCallback(store, merkle_address));
            linker.Define("env", "merkle_setroot", Wasmtime.Function.FromCallback(store, merkle_setroot));
            linker.Define("env", "merkle_getroot", Wasmtime.Function.FromCallback(store, merkle_getroot));
            linker.Define("env", "merkle_set", Wasmtime.Function.FromCallback(store, merkle_set));
            linker.Define("env", "merkle_get", Wasmtime.Function.FromCallback(store, merkle_get));
        }
        private static void LinkCacheFunc(Linker linker, Store store)
        {

            //cache function
            int cache_mode = 0;//1 store, 0 fetch
            int cache_hashseek = 0;
            UInt64[] cache_hash = new UInt64[4];
            List<UInt64> cache_data = new List<ulong>(cache_hash);
            int cache_dataseek = -1;
            //store mode, cache_set_mode(1)=>cache_store_data*N=>cache_set_hash*4
            //fetch mode, cache_set_mode(0)=>cache_set_hash*4=>cache_fetch_data len =>cache_fetch_data*N
            Wasmtime.CallerAction<Int64> cache_set_mode = (caller, i) =>
            {
                cache_mode = (int)i;
                cache_hashseek = 0;
                cache_data.Clear();
                cache_dataseek = -1;
            };
            Wasmtime.CallerAction<Int64> cache_set_hash = (caller, i) =>
            {
                cache_hash[cache_hashseek] = (ulong)i;
                cache_hashseek++;
                if (cache_hashseek == 4)
                {
                    if (cache_mode == 1)
                    {
                        //store
                        //根据hash,把cache_data中的数据,放到数据库中
                        MerkleSync.update_record(cache_hash, cache_data);
                    }
                    else
                    {
                        //fetch
                        //根据hash,把数据找出来，放到cache_data
                        MerkleSync.get_record(cache_hash,cache_data);
                    }
                }
            };
            Wasmtime.CallerAction<Int64> cache_store_data = (caller, i) =>
            {
                cache_data.Add((ulong)i);
            };
            Wasmtime.CallerFunc<Int64> cache_fetch_data = (caller) =>
            {
                if (cache_dataseek == -1)
                {
                    var len = (ulong)cache_data.Count;
                    cache_dataseek++;
                    return (long)len;
                }
                else
                {
                    var data = cache_data[cache_dataseek];
                    cache_dataseek++;
                    return (long)data;
                }
            };
            linker.Define("env", "cache_set_mode", Wasmtime.Function.FromCallback(store, cache_set_mode));
            linker.Define("env", "cache_set_hash", Wasmtime.Function.FromCallback(store, cache_set_hash));
            linker.Define("env", "cache_store_data", Wasmtime.Function.FromCallback(store, cache_store_data));
            linker.Define("env", "cache_fetch_data", Wasmtime.Function.FromCallback(store, cache_fetch_data));
        }
    }
}
