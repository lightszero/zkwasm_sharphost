import * as readline from "readline-sync";
import * as http from "http";


class MerkleRoot {
    v1: bigint = 14789582351289948625n;
    v2: bigint = 10919489180071018470n;
    v3: bigint = 10309858136294505219n;
    v4: bigint = 2839580074036780766n;

}
let root = new MerkleRoot();
//const serverurl = "18.162.245.133";
const serverurl = "127.0.0.1";
const MERKLE_TREE_HEIGHT = 32;
async function getDataFromMerkle(root: string, address: number): Promise<bigint[]> {

    var index = BigInt(address) + 1n << BigInt(MERKLE_TREE_HEIGHT) - 1n;

    var body = { "jsonrpc": "2.0", "id": 1, "method": "get_leaf", "params": { "root": root, "index": index.toString() } };

    var r = await fetch("http://18.162.245.133:999/", {
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        "body": JSON.stringify(body), "method": "POST"
    });
    var result = JSON.parse(await r.text());
    console.log(result);
    return [BigInt(0)];
}
function str2bytes(data: string): Uint8Array {
    var rdata = new Uint8Array(data.length);
    for (var i = 0; i < data.length; i++) {
        rdata[i] = data.charCodeAt(i);
    }
    return rdata;
}
async function callLogicRemote(wasmhash: string, input: Uint8Array): Promise<Uint8Array> {

    var p =new Promise<Uint8Array>((resolve, reject) => {
        let op: http.RequestOptions = {
            //host: "18.162.245.133",
            //host: "18.167.90.229",
            host: serverurl,
            port: 888,
            path: "/executeLogic?hash=" + wasmhash,
            method: "post",
            headers: {
                'Content-Type': 'application/octet-stream',
                'Content-Length': input.byteLength
            }
        }
        var req = http.request(op, (res) => {
            console.log("http info." + res.statusMessage + ":" + res.statusCode);
            let data = '';
            res.setEncoding("binary");
            res.on("data", (chunk) => {
                data += chunk;;
                console.log("receive." + chunk);
            }
            );
            res.on("end", () => {
                // function OnServerReturn(data: string) {
                    let bin = str2bytes(data);
                    console.log("--服务器返回--"+bin);

                    resolve(bin);

                //}
                //OnServerReturn(data);
    
            });
    
        }
        );
        req.write(input, (res) => {
    
            console.log("post info." + res?.message);
    
        })
        req.end();
    
    
    })
    return p;


}
async function onInit() {
    getDataFromMerkle("0000", 789);
}

//重新开始游戏
function onRestart() {
    root = new MerkleRoot();
}
async function onTest() {
    let cmd = 1;
    let input = new Uint8Array(8 * 100);
    let dv = new DataView(input.buffer);
    dv.setBigUint64(0, BigInt(cmd), false);
    let _root = new MerkleRoot();
    dv.setBigUint64(8, _root.v1, false);
    dv.setBigUint64(16, _root.v2, false);
    dv.setBigUint64(24, _root.v3, false);
    dv.setBigUint64(32, _root.v4, false);
    //call game_checkstate.wasm
    var r = await callLogicRemote("32078AD3998533F52BB1FF592A0129C0FDAE8B830A70B082C3E2478EFDC777F7_6055", input);
    console.log(r);
}
async function main() {
    await onTest();

    console.log("三子棋游戏");
    console.log("输入 exit 退出游戏");
    console.log("输入 reset 重开游戏");
    console.log("输入 put x y 落子");
    while (true) {
        var input = readline.question("-->");
        var tags = input.split(" ");
        if (tags[0] == "exit") {
            console.log("游戏结束");
            break;
        }
        if (tags[0] == "reset") {
            onRestart();
        }
        if (tags[0] == "put") {
            let x = parseInt(tags[1]);
            let y = parseInt(tags[2]);
        }
        if (tags[0] == "test") {
            await onTest();
        }
    }

    //初始化
    await onInit();
}
main();