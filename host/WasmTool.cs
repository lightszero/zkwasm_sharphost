using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static host.WasmTool;

namespace host
{
    public enum ProcessState
    {
        NotExist,
        Doing,
        Done,
        Fail,
    }

    public class ProcessInfo
    {
        public ProcessState state;
        public List<string> logs;
        public void CheckState()
        {
            foreach (var l in logs)
            {
                //Setup State
                if (l.IndexOf("Error:") == 0)
                {
                    state = ProcessState.Fail;
                    break;
                }
                if (l.IndexOf("The configuration is saved at") == 0)
                {
                    state = ProcessState.Done;
                    break;
                }

                //prove tag
                if (l.IndexOf("thread 'main' panicked at") == 0)
                {
                    state = ProcessState.Fail;
                    break;
                }

                if (l.IndexOf("[8/8] Saving proof load info to") == 0)
                {
                    state = ProcessState.Done;
                }

                if (l.IndexOf("FORCEDONE") == 0)
                {
                    state = ProcessState.Done;
                    break;
                }
            }
        }
    }
    internal static class WasmTool
    {
        //执行命令行
        public static async Task RunCmd(bool combine, string path, string execute, string args, List<string> outputs)
        {
            Func<string, CancellationToken, Task> onOutput = async (txt, c) =>
            {
                Console.WriteLine(txt);
                outputs.Add(txt);
            };

            var exe = System.IO.Path.Combine(path, execute);
            var basharg = "-c \"" + exe + " " + args + (combine ? ">1.txt\"" : "\"");//直接输出会报错

            Console.WriteLine("try execute " + basharg);

            var r = await
            CliWrap.Cli.Wrap("/bin/bash")
                .WithArguments(basharg)
                .WithStandardOutputPipe(CliWrap.PipeTarget.ToDelegate(onOutput))
                .WithStandardErrorPipe(CliWrap.PipeTarget.ToDelegate(onOutput))
                .WithValidation(CliWrap.CommandResultValidation.None)
                .ExecuteAsync();

            if (combine)
            {
                var lines = System.IO.File.ReadAllLines("1.txt");
                if (lines != null && lines.Length > 0)
                    outputs.AddRange(lines);
            }
            Console.WriteLine("outputs=" + outputs.Count);

            return;
        }
        static string g_wasmpath;
        static string g_wasmbin;
        static string g_wasmout;

        public static void Init(string wasmpath, string wasmbin, string wasmout)
        {
            g_wasmpath = wasmpath;
            g_wasmbin = wasmbin;
            g_wasmout = wasmout;
            Console.WriteLine("WasmTool Init g_wasmpath=" + g_wasmpath);
            Console.WriteLine("WasmTool Init g_wasmbin=" + g_wasmbin);
            Console.WriteLine("WasmTool Init g_wasmout=" + g_wasmout);

            if (System.IO.Directory.Exists(g_wasmout) == false)
            {
                Console.WriteLine("try create folder=" + g_wasmout);
                try
                {
                    System.IO.Directory.CreateDirectory(g_wasmout);
                    Console.WriteLine("create folder=" + g_wasmout);
                }
                catch (Exception err)
                {
                    Console.WriteLine("error:" + err.Message);
                }
            }
        }


        static System.Collections.Concurrent.ConcurrentDictionary<string, ProcessInfo> g_wasm_setupstate
            = new System.Collections.Concurrent.ConcurrentDictionary<string, ProcessInfo>();
        static System.Collections.Concurrent.ConcurrentDictionary<string, ProcessInfo> g_wasm_provestate
    = new System.Collections.Concurrent.ConcurrentDictionary<string, ProcessInfo>();
        public static bool GetSetupState(string hash, out ProcessInfo state)
        {
            var b = g_wasm_setupstate.TryGetValue(hash, out state);
            if (b)
                return b;

            var finalfilewasm = System.IO.Path.Combine(g_wasmout, hash + ".wasm");
            var finalfilewasmstate = System.IO.Path.Combine(g_wasmout, hash + ".wasm.state");
            var finalparamdir = System.IO.Path.Combine(g_wasmout, hash + "_param");
            if (System.IO.File.Exists(finalfilewasm) && System.IO.File.Exists(finalfilewasmstate))
            {
                var statelines = System.IO.File.ReadAllLines(finalfilewasmstate);
                if (statelines.Length > 0)
                {
                    ProcessInfo wstate = new ProcessInfo();
                    g_wasm_setupstate[hash] = wstate;
                    wstate.logs = new List<string>(statelines);
                    wstate.CheckState();
                    if (wstate.state == ProcessState.Done)
                        state = wstate;

                    return true;
                }
            }
            return b;
        }
        public static bool GetProveState(string hashWasm, string hashInput, out ProcessInfo state)
        {
            var hash = hashWasm + "_" + hashInput;
            var b = g_wasm_provestate.TryGetValue(hash, out state);
            if (b)
                return b;


            var finalProvedir = System.IO.Path.Combine(g_wasmout, hashWasm + "/" + hashInput);
            if (System.IO.Directory.Exists(finalProvedir) == false)
            {
                System.IO.Directory.CreateDirectory(finalProvedir);
            }
            var finalfilewasmstate = System.IO.Path.Combine(finalProvedir, ".state");
            if (System.IO.File.Exists(finalfilewasmstate))
            {
                var statelines = System.IO.File.ReadAllLines(finalfilewasmstate);
                if (statelines.Length > 0)
                {
                    ProcessInfo wstate = new ProcessInfo();
                    g_wasm_provestate[hash] = wstate;
                    wstate.logs = new List<string>(statelines);
                    wstate.CheckState();
                    if (wstate.state == ProcessState.Done)
                        state = wstate;

                    return true;
                }
            }
            return b;

        }
        public static async Task<ProcessInfo> SetupWasm(string hash, byte[] data)
        {
            if (g_wasm_setupstate.TryGetValue(hash, out var state))
            {
                //如果已经完成，就别Setup了
                if (state.state == ProcessState.Done)
                    return state;
                if (state.state == ProcessState.Doing)
                    return state;

            }

            var finalfilewasm = System.IO.Path.Combine(g_wasmout, hash + ".wasm");
            var finalfilewasmstate = System.IO.Path.Combine(g_wasmout, hash + ".wasm.state");
            var finalparamdir = System.IO.Path.Combine(g_wasmout, hash + "_param");
            if (System.IO.File.Exists(finalfilewasm) && System.IO.File.Exists(finalfilewasmstate))
            {
                //如果文件存在
                //判断一下状态
                var statelines = System.IO.File.ReadAllLines(finalfilewasmstate);
                if (statelines.Length > 0)
                {
                    ProcessInfo wstate = new ProcessInfo();
                    g_wasm_setupstate[hash] = wstate;
                    wstate.logs = new List<string>(statelines);
                    wstate.CheckState();
                    if (wstate.state == ProcessState.Done)
                        return wstate;
                }
            }
            {
                ProcessInfo wstate = new ProcessInfo();
                g_wasm_setupstate[hash] = wstate;
                wstate.state = ProcessState.Doing;
                wstate.logs = new List<string>();
                System.IO.File.WriteAllBytes(finalfilewasm, data);
                Console.WriteLine("save file:" + finalfilewasm);
                string setup = $" --params {finalparamdir} root  setup --host default --wasm {finalfilewasm}";
                await RunCmd(false, g_wasmpath, g_wasmbin, setup, wstate.logs);
                System.IO.File.Delete(finalfilewasmstate);
                //判断编译结果
                System.IO.File.WriteAllLines(finalfilewasmstate, wstate.logs);
                wstate.CheckState();
                return wstate;
            }

        }
        static byte[] buf = new byte[8];
        static byte[] buf2 = new byte[8];
        public static long ReadI64Big(System.IO.Stream stream)
        {
            stream.Read(buf, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                for (var i = 0; i < 8; i++)
                {
                    buf2[7 - i] = buf[i];
                }
                return BitConverter.ToInt64(buf2, 0);
            }
            else
            {
                return BitConverter.ToInt64(buf, 0);
            }
        }
        public static void WriteI64Big(System.IO.Stream stream, long v)
        {
            var data = BitConverter.GetBytes(v);

            if (BitConverter.IsLittleEndian)
            {
                for (var i = 0; i < 8; i++)
                {
                    buf2[7 - i] = data[i];
                }
                stream.Write(buf2, 0, buf2.Length);
            }
            else
            {
                stream.Write(data, 0, buf2.Length);
            }
        }
        public static byte[] I64BigArrayToBytes(long[] output)
        {
            using var ms = new MemoryStream();
            for (var i = 0; i < output.Length; i++)
            {
                WriteI64Big(ms, output[i]);
            }
            return ms.ToArray();
        }
        static ulong ReadU64Big(System.IO.Stream stream)
        {
            stream.Read(buf, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                for (var i = 0; i < 8; i++)
                {
                    buf2[7 - i] = buf[i];
                }
                return BitConverter.ToUInt64(buf2, 0);
            }
            else
            {
                return BitConverter.ToUInt64(buf, 0);
            }
        }
        static string FormatI64(long num)
        {
            if (num < 0)
            {
                var dat = BitConverter.GetBytes(num);
                var outstr = "0x";
                for (var i = 0; i < 8; i++)
                    outstr += dat[i].ToString("x02");
                outstr += ":bytes-packed";
                return outstr;
            }
            else
            {
                return num + ":i64";
            }
        }
        public static async Task<ProcessInfo> ProveWasm(string hashWasm, string hashInput, byte[] data)
        {
            var hash = hashWasm + "_" + hashInput;
            if (g_wasm_provestate.TryGetValue(hash, out var state))
            {
                //如果已经完成，就别Prove了
                if (state.state == ProcessState.Done)
                    return state;
                if (state.state == ProcessState.Doing)
                    return state;
            }

            var finalfilewasm = System.IO.Path.Combine(g_wasmout, hashWasm + ".wasm");
            var finalparamdir = System.IO.Path.Combine(g_wasmout, hashWasm + "_param");
            var finalProvedir = System.IO.Path.Combine(g_wasmout, hashWasm + "/" + hashInput);
            if (System.IO.Directory.Exists(finalProvedir) == false)
            {
                System.IO.Directory.CreateDirectory(finalProvedir);
            }
            var finalfilewasmstate = System.IO.Path.Combine(finalProvedir, ".state");
            if (System.IO.File.Exists(finalfilewasmstate))
            {
                //如果文件存在
                //判断一下状态
                var statelines = System.IO.File.ReadAllLines(finalfilewasmstate);
                if (statelines.Length > 0)
                {
                    ProcessInfo wstate = new ProcessInfo();
                    g_wasm_provestate[hash] = wstate;
                    wstate.logs = new List<string>(statelines);
                    wstate.CheckState();
                    if (wstate.state == ProcessState.Done)
                        return wstate;
                }
            }
            {
                ProcessInfo wstate = new ProcessInfo();
                g_wasm_provestate[hash] = wstate;
                wstate.state = ProcessState.Doing;
                wstate.logs = new List<string>();
                using var ms = new System.IO.MemoryStream(data);



                //全部大头u64，第一个是长度，分别是private 和 public



                string privalues = "";
                string pubvalues = "";
                {//读取Input并拆开
                    long vpri = ReadI64Big(ms);
                    privalues += FormatI64(vpri);
                    for (var i = 0; i < vpri; i++)
                    {
                        privalues += "," + FormatI64(ReadI64Big(ms));
                    }
                    long vpub = ReadI64Big(ms);
                    pubvalues += FormatI64(vpub);
                    for (var i = 0; i < vpub; i++)
                    {
                        pubvalues += "," + FormatI64(ReadI64Big(ms));
                    }
                }

                string prove = $" --params {finalparamdir} root  prove --wasm {finalfilewasm} --output {finalProvedir} --private {privalues} --public {pubvalues}";

                await RunCmd(false, g_wasmpath, g_wasmbin, prove, wstate.logs);
                System.IO.File.Delete(finalfilewasmstate);
                //判断编译结果
                System.IO.File.WriteAllLines(finalfilewasmstate, wstate.logs);
                wstate.CheckState();
                return wstate;
            }
        }
        public static byte[] GetProveData(string hashWasm, string hashInput)
        {
            var finalProvedir = System.IO.Path.Combine(g_wasmout, hashWasm + "/" + hashInput);
            var d1 = System.IO.File.ReadAllBytes(System.IO.Path.Combine(finalProvedir, "root.loadinfo.json"));//配置
            var d2 = System.IO.File.ReadAllBytes(System.IO.Path.Combine(finalProvedir, "root.0.transcript.data"));//证明数据
            var d3 = System.IO.File.ReadAllBytes(System.IO.Path.Combine(finalProvedir, "root.0.instance.data"));//pubdata
            using var ms = new MemoryStream();
            ms.Write(BitConverter.GetBytes(d1.Length));
            ms.Write(BitConverter.GetBytes(d2.Length));
            ms.Write(BitConverter.GetBytes(d3.Length));
            ms.Write(d1);
            ms.Write(d2);
            ms.Write(d3);
            return ms.ToArray();
        }
        public static void SetProveData(string hashWasm, string hashInput, byte[] data)
        {
            var finalProvedir = System.IO.Path.Combine(g_wasmout, hashWasm + "/" + hashInput);
            if (System.IO.Directory.Exists(finalProvedir) == false)
            {
                System.IO.Directory.CreateDirectory(finalProvedir);
            }
            using var ms = new MemoryStream(data);
            var buf = new byte[4];
            ms.Read(buf, 0, 4);
            int jsonlen = BitConverter.ToInt32(buf, 0);
            ms.Read(buf, 0, 4);
            int provelen = BitConverter.ToInt32(buf, 0);
            ms.Read(buf, 0, 4);
            int publen = BitConverter.ToInt32(buf, 0);
            {
                var datajson = new byte[jsonlen];
                ms.Read(datajson, 0, jsonlen);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(finalProvedir, "root.loadinfo.json"), datajson);
            }
            {
                var datajson = new byte[provelen];
                ms.Read(datajson, 0, provelen);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(finalProvedir, "root.0.transcript.data"), datajson);
            }
            {
                var datajson = new byte[publen];
                ms.Read(datajson, 0, publen);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(finalProvedir, "root.0.instance.data"), datajson);
            }
            System.IO.File.WriteAllText(System.IO.Path.Combine(finalProvedir, ".state"), "FORCEDONE.\n");

        }
        public static async Task<ProcessInfo> VerifyWasm(string hashWasm, string hashInput)
        {
            ProcessInfo wstate = new ProcessInfo();
            wstate.state = ProcessState.Fail;
            wstate.logs = new List<string>();
            var finalparamdir = System.IO.Path.Combine(g_wasmout, hashWasm + "_param");
            var finalProvedir = System.IO.Path.Combine(g_wasmout, hashWasm + "/" + hashInput);
            //cargo run --release----params params name verify --output.. / p2
            string verify = $" --params {finalparamdir} root  verify --output {finalProvedir}";

            await RunCmd(true, g_wasmpath, g_wasmbin, verify, wstate.logs);
            foreach (var l in wstate.logs)
            {
                if (l.IndexOf("Verification succeeded!") == 0)
                {
                    wstate.state = ProcessState.Done;
                    break;
                }
            }
            return wstate;
        }
    }
}
