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

                //prove error tag
                if(l.IndexOf("thread 'main' panicked at")==0)
                {
                    state = ProcessState.Fail;
                    break;
                }
            }
        }
    }
    internal static class WasmTool
    {
        //执行命令行
        public static async Task RunCmd(string path, string execute, string args, List<string> outputs)
        {
            //var pro = new ProcessStartInfo();
            //pro.FileName = "/bin/bash";
            //pro.UseShellExecute = false;
            //pro.CreateNoWindow = true;
            //pro.Arguments = "";
            //pro.RedirectStandardOutput = true;
            //pro.RedirectStandardError = true;
            //pro.RedirectStandardInput = true;
            //var p = Process.Start(pro);
            //p.OutputDataReceived += (s, e) =>
            //{
            //    outputs.Add(e.Data);

            //};
            //p.ErrorDataReceived += (s, e) =>
            //{
            //    outputs.Add(e.Data);
            //};

            //p.StandardInput.WriteLine("cd " + path);
            //p.StandardInput.WriteLine(execute + " " + args);
            //p.StandardInput.WriteLine("exit");
            //await p.WaitForExitAsync();


            Func<string, CancellationToken, Task> onOutput = async (txt, c) =>
            {
                Console.WriteLine(txt);
                outputs.Add(txt);
            };

            var exe = System.IO.Path.Combine(path, execute);
            var basharg = "-c \"" + exe + " " + args + "\"";
            var r = await
            CliWrap.Cli.Wrap("/bin/bash")

                .WithArguments(basharg)
                .WithStandardOutputPipe(CliWrap.PipeTarget.ToDelegate(onOutput))
                .WithStandardErrorPipe(CliWrap.PipeTarget.ToDelegate(onOutput))
                .WithValidation(CliWrap.CommandResultValidation.None)
                .ExecuteAsync();

            Console.WriteLine("try execute " + basharg);
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
        public static bool GetProveSate(string hash, out ProcessInfo state)
        {
            return g_wasm_provestate.TryGetValue(hash, out state);

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
                await RunCmd(g_wasmpath, g_wasmbin, setup, wstate.logs);
                System.IO.File.Delete(finalfilewasmstate);
                //判断编译结果
                System.IO.File.WriteAllLines(finalfilewasmstate, wstate.logs);
                wstate.CheckState();
                return wstate;
            }

        }
        static byte[] buf = new byte[8];
        static byte[] buf2 = new byte[8];
        static long ReadI64Big(System.IO.Stream stream)
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
                    privalues += vpri + ":i64";
                    for (var i = 0; i < vpri; i++)
                    {
                        privalues += "," + ReadI64Big(ms) + ":i64";
                    }
                    long vpub = ReadI64Big(ms);
                    pubvalues += vpub + ":i64";
                    for (var i = 0; i < vpub; i++)
                    {
                        pubvalues += "," + ReadI64Big(ms) + ":i64";
                    }
                }
                string prove = $" --params {finalparamdir} root  prove --wasm {finalfilewasm} --output {finalProvedir} --private {privalues} --public {pubvalues}";

                await RunCmd(g_wasmpath, g_wasmbin, prove, wstate.logs);
                System.IO.File.Delete(finalfilewasmstate);
                //判断编译结果
                System.IO.File.WriteAllLines(finalfilewasmstate, wstate.logs);
                wstate.CheckState();
                return wstate;
            }
        }
    }
}
