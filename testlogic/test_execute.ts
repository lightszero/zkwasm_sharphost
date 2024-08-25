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
    //host: "18.162.245.133",
    //host: "18.167.90.229",
    host: "127.0.0.1",
    //host: "18.167.90.229",
    port: 888,
    path: "/executeLogic?hash=E38C20BF275F452ABCFBE01EC5DFA846846814CCD089F4F5ECD261E05399A37D_3635",
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
    console.log("--服务器done-- = " );
    var rdata =str2bytes(data);
    var dv =new DataView(rdata.buffer);
    
    if (data.length <= 4) {
        var len =dv.getInt8(0);
        console.log("--服务器异常-- = " + len);
        return;
    }
    var resultlen =dv.getBigInt64(0,false);
    console.log("resultlen="+resultlen);
    var seek =0;
    for(var i=0;i<resultlen;i++)
        {
            var v = dv.getBigInt64(seek,false);
            seek+=8;
            console.log("result["+i+"]="+v);
        }
    //console.log("--服务器返回--" + len);

}

