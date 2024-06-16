import * as fs from "fs"
import * as http from "http"

console.log("Prove Wasm 演示");
//此文件演示ZKWASM服务器的 Prove 协议 使用方法

//向 "http://127.0.0.1:888/prove?hash=wasmhash" 发送一个uint8array,内容是输入
// # 输入 编码为private
// # 输出 编码为public
// # 根据输入Seed得到一个随机数
// # seed=10086


// # shouldoutput
// # succ=1
// # state8005 = 695
// # statePosX = 3
// # statePosY = 3
// # stateHeatValue = 20
// # statetimeStamp = 66
var input = [999222];

var tarr = new Uint8Array((input.length + 1) * 8);
{//组织input数据
    var prilen = input.length;

    console.log(" prilen=" + prilen);
    var view = new DataView(tarr.buffer);
    var seek = 0;
    view.setBigInt64(seek, BigInt(prilen), false);
    seek += 8;
    for (var i = 0; i < prilen; i++) {
        view.setBigInt64(seek, BigInt(input[i]), false);
        seek += 8;
    }

}
var buf = tarr;//fs.readFileSync(".bin/input.test");

console.log("filelen = " + buf.byteLength);

let op: http.RequestOptions = {
    host: "18.162.245.133",
    //host: "18.167.90.229",
    //host: "127.0.0.1",
    //host: "18.167.90.229",
    port: 888,
    path: "/executeLogic?hash=6CFEA9A5DA8001A4366121E53247FC81DAA5933678C5EF939F56D71A14C055C5_3120",
    method: "post",
    headers: {
        'Content-Type': 'application/octet-stream',
        'Content-Length': buf.byteLength
    }
}
var req = http.request(op, (res) => {
    console.log("http info." + res.statusMessage + ":" + res.statusCode);
    let data = '';
    res.setEncoding("binary");
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
function str2bytes(data: string): Uint8Array {
    var rdata = new Uint8Array(data.length);
    for (var i = 0; i < data.length; i++) {
        rdata[i] = data.charCodeAt(i);
    }
    return rdata;
}
function OnServerReturn(data: string) {
    console.log("--服务器done-- = ");
    var rdata = str2bytes(data);

    var dv = new DataView(rdata.buffer);

    if (data.length <= 4) {
        var len = dv.getInt8(0);
        console.log("--服务器异常-- = " + len);
        return;
    }
    var resultlen = dv.getBigInt64(0, false);
    console.log("resultlen=" + resultlen);
    var seek = 8;
    for (var i = 0; i < resultlen; i++) {
        var v = dv.getBigInt64(seek,false);
       
        seek += 8;
        console.log("result[" + i + "]=" + v.toString());
    }
    //console.log("--服务器返回--" + len);

}

console.log("保留返回的随机种子140737488355328，作为下一次的种子输入，这样就能实现随机的统一");