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
            var jsonResult = new JObject();
            try
            {
                var form = await FormData.FromRequest(context.Request);
                var data = form.mapFiles["wasm"];
                var hashstr = HashTool.CalcHashStr(data) + "_" + data.Length;
               
                jsonResult["hash"] = hashstr;
                var state = FileTool.LockState(hashstr);
                if (state == FileTool.FileState.InSetup)
                {
                    jsonResult["code"] = -1;
                    jsonResult["txt"] = "该wasm 正在Setup";
                    //正在Setup，别凑热闹
                }
                else if (state == FileTool.FileState.SetupDone)
                {
                    //已经有了，别凑热闹
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                jsonResult["code"] = -100;
                jsonResult["txt"] = "未知错误:" + ex.ToString();
            }
            finally
            {
                FileTool.UnLockState();
            }

            try
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/plain;charset=utf-8";

                await context.Response.WriteAsync(jsonResult.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Setup 返回信息失败");
            }
        }
    }
}
