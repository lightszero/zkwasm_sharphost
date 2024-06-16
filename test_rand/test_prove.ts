import * as fs from "fs"
import * as http from "http"

console.log("Prove Wasm 演示");
//此文件演示ZKWASM服务器的 Prove 协议 使用方法

//向 "http://127.0.0.1:888/prove?hash=wasmhash" 发送一个uint8array，内容是 zk.wasm
// # 输入 编码为private
// # 输出 编码为public
//var input = [999222];

// # shouldoutput
// resultlen=3
// result[0]=5328
// result[1]=140737488355328
// result[2]=999999999

var input = [999222];
var output = [5328,"140737488355328",999999999];//数值太大，用字符串，会搞成bigint
var tarr = new Uint8Array((input.length + output.length + 2) * 8);
{//组织数据
    var prilen =input.length;
    var publen =output.length;
    console.log("publen=" + publen + " prilen=" + prilen);
    var view = new DataView(tarr.buffer);
    var seek = 0;
    view.setBigInt64(seek, BigInt(prilen), false);
    seek += 8;
    for (var i = 0; i < prilen; i++) {
        view.setBigInt64(seek, BigInt(input[i]), false);
        seek += 8;
    }
    view.setBigInt64(seek, BigInt(publen), false);
    seek += 8;
    for (var i = 0; i < publen; i++) {
        view.setBigInt64(seek, BigInt(output[i]), false);
        seek += 8;
    }
}
var buf = tarr;//fs.readFileSync(".bin/input.test");

console.log("filelen = " + buf.byteLength);

let op: http.RequestOptions = {
    host: "18.162.245.133",
    //host: "18.167.90.229",
    //host:"127.0.0.1",
    //host: "18.167.90.229",
    port: 888,
    path: "/prove?hash=BBEEC88CD56729627F19331E801C0A31B6BD1A8F771AE01EFFD560B0C7EC9AE3_3206",
    method: "post",
    headers: {
        'Content-Type': 'application/octet-stream',
        'Content-Length': buf.byteLength
    }
}
var req = http.request(op, (res) => {
    console.log("http info." + res.statusMessage + ":" + res.statusCode);
    let data = '';
    res.on("data", (chunk) => {
        data += chunk;;
    }
    );
    res.on("end", () => {
        OnServerReturn(data);

    });
}

);
req.write(buf, (res) => {

    console.log("post info." + res?.message);

})
req.end();

function OnServerReturn(data: string) {
    console.log("--服务器返回--"+data);
    var json = JSON.parse(data);
    var hashWasm = json["hashWasm"];
    var hashInput = json["hashInput"];
    var code = json["code"];
    var txt = json["txt"];
    console.log("code=" + code + " 大于0表示成功");
    console.log("txt=" + txt);
    console.log("hashWasm=" + hashWasm + "  电路Hash");
    console.log("hashInput=" + hashInput + "  输入Hash");
}

