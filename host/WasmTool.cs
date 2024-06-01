using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    }
    internal static class WasmTool
    {
        //执行命令行
        public static async Task RunCmd(string path, string execute, string args, List<string> outputs)
        {

            Func<string, CancellationToken, Task> onOutput = async (txt, c) =>
            {
                outputs.Add(txt);
            };

            var r = await
            CliWrap.Cli.Wrap(execute)
            .WithWorkingDirectory(path)
            .WithArguments(args)
            .WithStandardOutputPipe(CliWrap.PipeTarget.ToDelegate(onOutput))
            .WithStandardErrorPipe(CliWrap.PipeTarget.ToDelegate(onOutput))
            .ExecuteAsync();

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
            if (System.IO.File.Exists(finalfilewasm) && System.IO.File.Exists(finalfilewasmstate))
            {
                //如果文件存在
                //判断一下状态
                var statelines = System.IO.File.ReadAllLines(finalfilewasmstate);
                if (statelines.Length > 0)
                {
                    if (statelines[0] == "done")
                    {
                        WasmState wstate = new WasmState();
                        g_wasmstate[filename] = wstate;
                        wstate.state = FileState.SetupDone;
                        return wstate;
                    }
                }
            }
            {
                WasmState wstate = new WasmState();
                g_wasmstate[filename] = wstate;
                wstate.state = FileState.InSetup;
                wstate.logs = new List<string>();
                await RunCmd(g_wasmpath, g_wasmbin, "setup", wstate.logs);
                return wstate;
            }

        }

    }
}
