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
    public enum FileState
    {
        NotExist,
        InSetup,
        SetupDone,
        SetupFail,
    }

    public class WasmState
    {
        public FileState state;
        public List<string> logs;
        public void CheckState()
        {
            foreach(var l in logs)
            {
                if(l.IndexOf("Error:") ==0)
                {
                    state = FileState.SetupFail;
                    break;
                }
                if(l.IndexOf("The configuration is saved at")==0)
                {
                    state = FileState.SetupDone;
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
                outputs.Add(txt);
            };

            var exe = System.IO.Path.Combine(path, execute);
            var basharg = "-c \"" + exe + " " + args+"\"";
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


        static System.Collections.Concurrent.ConcurrentDictionary<string, WasmState> g_wasmstate
            = new System.Collections.Concurrent.ConcurrentDictionary<string, WasmState>();
        public static async Task<WasmState> SetupWasm(string filename, byte[] data)
        {
            if (g_wasmstate.TryGetValue(filename, out var state))
            {
                //如果已经完成，就别Setup了
                if (state.state == FileState.SetupDone)
                    return state;
                if (state.state == FileState.InSetup)
                    return state;

            }

            var finalfilewasm = System.IO.Path.Combine(g_wasmout, filename + ".wasm");
            var finalfilewasmstate = System.IO.Path.Combine(g_wasmout, filename + ".wasm.state");
            var finalparamdir = System.IO.Path.Combine(g_wasmout, filename + "_param");
            if (System.IO.File.Exists(finalfilewasm) && System.IO.File.Exists(finalfilewasmstate))
            {
                //如果文件存在
                //判断一下状态
                var statelines = System.IO.File.ReadAllLines(finalfilewasmstate);
                if (statelines.Length > 0)
                {
                    WasmState wstate = new WasmState();
                    g_wasmstate[filename] = wstate;
                    wstate.logs = new List<string>(statelines);
                    wstate.CheckState();
                    if (wstate.state == FileState.SetupDone)
                        return wstate;
                }
            }
            {
                WasmState wstate = new WasmState();
                g_wasmstate[filename] = wstate;
                wstate.state = FileState.InSetup;
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

    }
}
