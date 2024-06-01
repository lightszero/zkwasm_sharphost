﻿using light.http.server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json.Linq;
using static light.http.server.JSONRPCController;

namespace host
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var info = JObject.Parse(System.IO.File.ReadAllText("config.json"));
            var port = (int)info["port"];
            Console.WriteLine("ZKWASM simple Server at :" + port);

            WasmTool.Init(
                info["zkwasm"]["zkwasmpath"].ToString(),
                info["zkwasm"]["zkwasmbin"].ToString(),
                info["zkwasm"]["zkwasm_wasmpath"].ToString()
                );

            light.http.server.httpserver serv = new light.http.server.httpserver();
            serv.SetFailAction(on404);
            serv.SetJsonRPCFail("/zkwasm", onRPCFail);
            serv.SetHttpAction("/setup", HttpServer.onSetup);
            serv.Start(port);
            while (true)
            {
                Console.ReadLine();
            }
        }
        static async Task on404(HttpContext context)
        {
            await context.Response.WriteAsync("404:" + context.Request.GetDisplayUrl());
        }
        static async Task<ErrorObject> onRPCFail(JObject request, string errorMessage)
        {
            ErrorObject obj = new ErrorObject();
            obj.code = -1;
            obj.message = errorMessage;
            obj.data = request;
            return obj;
        }

    }
}