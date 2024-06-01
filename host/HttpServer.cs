using light.http.server;
using Microsoft.AspNetCore.Http;
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

        public static async Task onSetup(HttpContext context)
        {
            Console.WriteLine("onSetup");
            var jsonResult = new JObject();
            try
            {
                var form = await FormData.FromRequest(context.Request);
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
                if (state.state == FileState.InSetup)
                {
                    jsonResult["code"] = -1;
                    jsonResult["txt"] = "该wasm 正在Setup";

                    //正在Setup，别凑热闹
                }
                if (state.state == FileState.SetupFail)
                {
                    jsonResult["code"] = -2;
                    jsonResult["txt"] = "该wasm Setup失败";
                    //正在Setup，别凑热闹
                }
                else if (state.state == FileState.SetupDone)
                {
                    //已经有了，别凑热闹
                    jsonResult["code"] = 1;
                    jsonResult["txt"] = "该wasm 已经Setup";
                }
                else
                {

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
    }
}
