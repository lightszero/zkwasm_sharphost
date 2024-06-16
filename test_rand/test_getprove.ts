import * as fs from "fs"
import * as http from "http"

console.log("GetProve Wasm 演示");
//此文件演示ZKWASM服务器的 Verify 协议 使用方法


let op: http.RequestOptions = {
    //host: "18.167.90.229",
    host: "18.162.245.133",
    //host:"127.0.0.1",
    //host: "18.167.90.229",
    port: 888,
    path: "/getprovedata?hashWasm=08C3DF5E77838F560624A6080A55C4D8EC5CBB18351F40A41B5CC2F963A84CBA_3557&hashInput=5EF9BC28CBA34F7A20FB06AEDEA09CEAC895F47288B43F33BCB2963055E9B8F5_152",
    method: "get",
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


req.end();

function OnServerReturn(data: string) {
    console.log("--服务器返回--"+data.length);
    fs.writeFileSync("./p1.data",data,"binary");
}

