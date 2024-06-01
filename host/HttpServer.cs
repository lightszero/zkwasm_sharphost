using light.http.server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace host
{
    internal class HttpServer
    {
        //Setup WASM 协议
        public static async Task onSetup(HttpContext context)
        {
            Console.WriteLine("onSetup");
            var jsonResult = new JObject();
            try
            {
                var len = (int)context.Request.ContentLength;
                byte[] data = new byte[len];
                var seek = 0;
                while (seek < len)
                {
                    var read = await context.Request.Body.ReadAsync(data, seek, len - seek);
                    seek += read;
                }
                var hashstr = HashTool.CalcHashStr(data) + "_" + data.Length;

                jsonResult["hash"] = hashstr;
                var state = await WasmTool.SetupWasm(hashstr, data);
                if (state.state == ProcessState.Doing)
                {
                    jsonResult["code"] = -1;
                    jsonResult["txt"] = "该wasm 正在Setup";

                    //正在Setup，别凑热闹
                }
                if (state.state == ProcessState.Fail)
                {
                    jsonResult["code"] = -2;
                    jsonResult["txt"] = "该wasm Setup失败";
                    //正在Setup，别凑热闹
                }
                else if (state.state == ProcessState.Done)
                {
                    //已经有了，别凑热闹
                    jsonResult["code"] = 1;
                    jsonResult["txt"] = "该wasm 已经Setup";
                }

                jsonResult["logs"] = new JArray(state.logs.ToArray());
            }
            catch (Exception ex)
            {
                jsonResult["code"] = -100;
                jsonResult["txt"] = "未知错误:" + ex.ToString();
            }


            try
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/plain;charset=utf-8";
                Console.WriteLine("Setup 返回:" + jsonResult.ToString());
                await context.Response.WriteAsync(jsonResult.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Setup 返回信息失败");
            }
        }

        //ZKWASM Prove协议
        public static async Task onProve(HttpContext context)
        {
            Console.WriteLine("onProve");
            var jsonResult = new JObject();
            try
            {
                //读取url hash，wasm对应的hash
                var hashWasm = context.Request.Query["hash"].ToString();
                var len = (int)context.Request.ContentLength;
                byte[] data = new byte[len];

                var seek = 0;
                while (seek < len)
                {
                    var read = await context.Request.Body.ReadAsync(data, seek, len - seek);
                    seek += read;
                }

                //拼一个总的hash
                var hashInput =  HashTool.CalcHashStr(data) + "_" + data.Length;
                jsonResult["hashWasm"] = hashWasm;
                jsonResult["hashInput"] = hashInput;
                var b = WasmTool.GetSetupState(hashWasm, out var wasm);
                if (b && wasm.state == ProcessState.Done)
                {
                    var state = await WasmTool.ProveWasm(hashWasm, hashInput, data);
                    if (state.state == ProcessState.Doing)
                    {
                        jsonResult["code"] = -1;
                        jsonResult["txt"] = "该wasm 正在Prove";

                        //正在Setup，别凑热闹
                    }
                    if (state.state == ProcessState.Fail)
                    {
                        jsonResult["code"] = -2;
                        jsonResult["txt"] = "该wasm Prove失败";
                        //正在Setup，别凑热闹
                    }
                    else if (state.state == ProcessState.Done)
                    {
                        //已经有了，别凑热闹
                        jsonResult["code"] = 1;
                        jsonResult["txt"] = "该wasm 已经Prove";
                    }

                    jsonResult["logs"] = new JArray(state.logs.ToArray());
                }
                else
                {
                    jsonResult["code"] = -1;
                    jsonResult["txt"] = "wasm 未Setup:" + hashWasm;
                }
            }
            catch (Exception ex)
            {
                jsonResult["code"] = -100;
                jsonResult["txt"] = "未知错误:" + ex.ToString();
            }

            try
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/plain;charset=utf-8";
                Console.WriteLine("Setup 返回:" + jsonResult.ToString());
                await context.Response.WriteAsync(jsonResult.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Setup 返回信息失败");
            }
        }
    }
}
