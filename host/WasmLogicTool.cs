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
    }
}
