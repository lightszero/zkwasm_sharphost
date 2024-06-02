import * as fs from "fs"
import * as http from "http"

console.log("setProve Wasm 演示");
//此文件演示ZKWASM服务器的 Setup 协议 使用方法

//向 "http://127.0.0.1:888/setup" 发送一个uint8array，内容是 zk.wasm

var buf = fs.readFileSync("./p1.data");
console.log("filelen = " + buf.byteLength);

let op: http.RequestOptions = {
    host: "127.0.0.1",
    port: 888,
    path: "/setprovedata?hashWasm=08C3DF5E77838F560624A6080A55C4D8EC5CBB18351F40A41B5CC2F963A84CBA_3557&hashInput=5EF9BC28CBA34F7A20FB06AEDEA09CEAC895F47288B43F33BCB2963055E9B8F5_152",
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

function OnServerReturn(data:string)
{ 
    console.log("--服务器返回--");
    var json =JSON.parse(data);
    var hash = json["hash"];
    var code =json["code"];
    var txt =json["txt"];
     console.log("code=" + code +" 大于0表示成功");
     console.log("txt=" + txt );
     console.log("hash=" + hash +"  电路Hash,以后使用此电路Hash做后续操作" );
}
   
