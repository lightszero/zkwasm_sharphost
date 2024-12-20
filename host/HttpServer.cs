﻿using light.http.server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
                context.Response.Headers["Access-Control-Allow-Origin"] = "*";
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
                var hashInput = HashTool.CalcHashStr(data) + "_" + data.Length;
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
                context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                Console.WriteLine("Prove 返回:" + jsonResult.ToString());
                await context.Response.WriteAsync(jsonResult.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Prove 返回信息失败");
            }
        }


        //下载证明结果
        public static async Task onGetProve(HttpContext context)
        {
            Console.WriteLine("onGetProve");
            byte[] outData;
            try
            {
                //读取url hash，wasm对应的hash
                var hashWasm = context.Request.Query["hashWasm"].ToString();
                var hashInput = context.Request.Query["hashInput"].ToString();

                Console.WriteLine("hashWasm:" + hashWasm);
                Console.WriteLine("hashInput:" + hashInput);
                var b = WasmTool.GetSetupState(hashWasm, out var wasm);
                if (b && wasm.state == ProcessState.Done)
                {
                    var b2 = WasmTool.GetProveState(hashWasm, hashInput, out var provestate);
                    if (b2 && provestate.state == ProcessState.Done)
                    {
                        outData = WasmTool.GetProveData(hashWasm, hashInput);
                        if (outData == null)
                            outData = BitConverter.GetBytes(-3);
                    }
                    else
                    {
                        outData = BitConverter.GetBytes(-2);
                    }


                }
                else
                {
                    outData = BitConverter.GetBytes(-1);
                }
            }
            catch (Exception ex)
            {
                outData = BitConverter.GetBytes(-100);
            }

            try
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/octet-stream";
                context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                Console.WriteLine("Prove 返回:" + outData.Length);
                await context.Response.Body.WriteAsync(outData, 0, outData.Length);
                context.Response.Body.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Prove 返回信息失败");
            }
        }
        //设置证明结果
        public static async Task onSetProve(HttpContext context)
        {
            Console.WriteLine("onProve");
            var jsonResult = new JObject();
            try
            {
                //读取url hash，wasm对应的hash
                var hashWasm = context.Request.Query["hashWasm"].ToString();
                var hashInput = context.Request.Query["hashInput"].ToString();
                var len = (int)context.Request.ContentLength;
                byte[] data = new byte[len];

                var seek = 0;
                while (seek < len)
                {
                    var read = await context.Request.Body.ReadAsync(data, seek, len - seek);
                    seek += read;
                }

                //拼一个总的hash
                var b = WasmTool.GetSetupState(hashWasm, out var wasm);
                bool skip = false;
                if (b && wasm.state == ProcessState.Done)
                {
                    var b2 = WasmTool.GetProveState(hashWasm, hashInput, out var prove);
                    {
                        if (b2 && prove.state == ProcessState.Done)
                        {
                            jsonResult["code"] = 1;
                            jsonResult["txt"] = "证明存在，不用上传";
                            skip = true;
                        }
                        if (b2 && prove.state == ProcessState.Doing)
                        {
                            jsonResult["code"] = -1;
                            jsonResult["txt"] = "证明中，不能上传";
                            skip = true;
                        }
                    }

                    if (!skip)
                    {
                        WasmTool.SetProveData(hashWasm, hashInput, data);
                        jsonResult["code"] = 1;
                        jsonResult["txt"] = "证明上传";
                    }
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
                context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                Console.WriteLine("Prove 返回:" + jsonResult.ToString());
                await context.Response.WriteAsync(jsonResult.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Prove 返回信息失败");
            }
        }

        //ZKWASM 验证协议，需要 WASM被Setup，证明被Set 或者 被Prove
        public static async Task onVerify(HttpContext context)
        {
            Console.WriteLine("onProve");

            var jsonResult = new JObject();
            try
            {
                //读取url hash，wasm对应的hash
                var hashWasm = context.Request.Query["hashWasm"].ToString();
                var hashInput = context.Request.Query["hashInput"].ToString();

                Console.WriteLine("hashWasm:" + hashWasm);
                Console.WriteLine("hashInput:" + hashInput);
                var b = WasmTool.GetSetupState(hashWasm, out var wasm);
                if (b && wasm.state == ProcessState.Done)
                {
                    var b2 = WasmTool.GetProveState(hashWasm, hashInput, out var provestate);
                    if (b2 && provestate.state == ProcessState.Done)
                    {
                        var state = await WasmTool.VerifyWasm(hashWasm, hashInput);
                        jsonResult["logs"] = new JArray(state.logs.ToArray());
                        if (state.state == ProcessState.Done)
                        {
                            jsonResult["code"] = 1;
                            jsonResult["txt"] = "verify 成功";
                        }
                        else
                        {
                            jsonResult["code"] = -3;
                            jsonResult["txt"] = "verify 失败";
                        }
                    }
                    else
                    {
                        //已经有了，别凑热闹
                        jsonResult["code"] = -2;
                        jsonResult["txt"] = "ProveData 未准备好";
                    }


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
                context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                Console.WriteLine("Prove 返回:" + jsonResult.ToString());
                await context.Response.WriteAsync(jsonResult.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Prove 返回信息失败");
            }
        }

        public static async Task onSetupLogic(HttpContext context)
        {
            Console.WriteLine("onSetupLogic");
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
                WasmLogicTool.SetupWasm(hashstr, data);
                jsonResult["code"] = 1;
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
                context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                Console.WriteLine("Setup 返回:" + jsonResult.ToString());
                await context.Response.WriteAsync(jsonResult.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Setup 返回信息失败");
            }
        }
        public static async Task onExecuteLogic(HttpContext context)
        {
            Console.WriteLine("onExecuteLogic");
            var jsonResult = new JObject();
            byte[] result = null;
            try
            {
                var len = (int)context.Request.ContentLength;
                byte[] input = new byte[len];
                var seek = 0;
                while (seek < len)
                {
                    var read = await context.Request.Body.ReadAsync(input, seek, len - seek);
                    seek += read;
                }
                var hashstr = context.Request.Query["hash"];
                long[] inputvalues;

                {//读取Input并拆开
                    using var ms = new MemoryStream(input);
                    long vpri = WasmTool.ReadI64Big(ms);
                    inputvalues = new long[vpri + 1];
                    inputvalues[0] = vpri;
                    for (var i = 0; i < vpri; i++)
                    {
                        inputvalues[i + 1] = WasmTool.ReadI64Big(ms);
                    }
                }

                var module = WasmLogicTool.GetWasmModule(hashstr);
                var output = WasmLogicTool.RunWasm(module, inputvalues);
                result = WasmTool.I64BigArrayToBytes(output);
                if (result == null)
                    result = BitConverter.GetBytes(-3);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                result = BitConverter.GetBytes(-100);
            }


            try
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/octet-stream";
                context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                Console.WriteLine("onExecuteLogic 返回:" + result.Length);
                await context.Response.Body.WriteAsync(result, 0, result.Length);
                context.Response.Body.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("onExecuteLogic 返回信息失败");
            }
        }

        public static async Task onUpdateRecord(HttpContext context)
        {
            Console.WriteLine("onUpdateRecord");
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
                var hashstr = context.Request.Query["hash"].ToString();


                Hash hash = new Hash();
                for (var i = 0; i < 32; i++)
                {
                    hash.data[i] = byte.Parse(hashstr.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
                }


                jsonResult["hash"] = hashstr;
                var b = await MerkleDBHelper.update_record(hash, new Data(data));
                jsonResult["succ"] = b;
                jsonResult["code"] = 1;
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
                context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                Console.WriteLine("onUpdateRecord 返回:" + jsonResult.ToString());
                await context.Response.WriteAsync(jsonResult.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("onUpdateRecord 返回信息失败");
            }
        }
        public static async Task onGetRecord(HttpContext context)
        {
            Console.WriteLine("onGetRecord");
            var jsonResult = new JObject();
            byte[] result = null;
            try
            {
                var len = (int)context.Request.ContentLength;
                byte[] input = new byte[len];
                var seek = 0;
                while (seek < len)
                {
                    var read = await context.Request.Body.ReadAsync(input, seek, len - seek);
                    seek += read;
                }
                var hashstr = context.Request.Query["hash"].ToString();
                Hash hash = new Hash();
                for (var i = 0; i < 32; i++)
                {
                    hash.data[i] = byte.Parse(hashstr.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
                }

                var data = await MerkleDBHelper.get_record(hash);
                result = data.data;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                result = BitConverter.GetBytes(-100);
            }


            try
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/octet-stream";
                context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                Console.WriteLine("onGetRecord 返回:" + result.Length);
                await context.Response.Body.WriteAsync(result, 0, result.Length);
                context.Response.Body.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("onGetRecord 返回信息失败");
            }
        }
    }



}
