import * as fs from "fs"
import * as http from "http"

console.log("Prove Wasm 演示");
//此文件演示ZKWASM服务器的 Prove 协议 使用方法

//向 "http://127.0.0.1:888/prove?hash=wasmhash" 发送一个uint8array，内容是 zk.wasm
// # 输入 编码为private
// # 输出 编码为public
// # 根据逻辑验证输入是否能得到输出
// # config1004=1
// # config41005=2
// # posx = 3
// # posy = 3
// # length = 5000
// # newtimeStamp =66
// # state8005 =700
// # statePosX = ?
// # statePosY = ?
// # stateHeatValue = 10
// # statetimeStamp = 11

// # shouldoutput
// # succ=1
// # state8005 = 695
// # statePosX = 3
// # statePosY = 3
// # stateHeatValue = 20
// # statetimeStamp = 66
var input = [1, 2, 3, 3, 5000, 66, 700, 8, 9, 10, 11];
var output = [1, 695, 3, 3, 20, 66];
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
    host:"127.0.0.1",
    //host: "18.167.90.229",
    port: 888,
    path: "/prove?hash=08C3DF5E77838F560624A6080A55C4D8EC5CBB18351F40A41B5CC2F963A84CBA_3557",
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

