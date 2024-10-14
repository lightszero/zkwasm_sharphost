import * as readline from "readline-sync";
import * as http from "http";


class MerkleRoot {
    v1: bigint = 14789582351289948625n;
    v2: bigint = 10919489180071018470n;
    v3: bigint = 10309858136294505219n;
    v4: bigint = 2839580074036780766n;
    toString(): string {
        return this.v1.toString() + "," + this.v2.toString() + "," + this.v3.toString() + "," + this.v4.toString();
    }
}
let root = new MerkleRoot();
//const serverurl = "18.162.245.133";
const serverurl = "127.0.0.1";

function str2bytes(data: string): Uint8Array {
    var rdata = new Uint8Array(data.length);
    for (var i = 0; i < data.length; i++) {
        rdata[i] = data.charCodeAt(i);
    }
    return rdata;
}
async function callLogicRemote(wasmhash: string, input: Uint8Array): Promise<Uint8Array> {

    var p = new Promise<Uint8Array>((resolve, reject) => {
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
            //console.log("http info." + res.statusMessage + ":" + res.statusCode);
            let data = '';
            res.setEncoding("binary");
            res.on("data", (chunk) => {
                data += chunk;;
            }
            );
            res.on("end", () => {
                // function OnServerReturn(data: string) {
                let bin = str2bytes(data);
                //console.log("--服务器返回--" + bin);

                resolve(bin);

                //}
                //OnServerReturn(data);

            });

        }
        );
        req.write(input, (res) => {

            //console.log("post info." + res?.message);

        })
        req.end();


    })
    return p;


}

enum GameState {
    Error = 0,
    Black = 1,
    White = 2,
    End = 3,
}
enum Grid {
    Empty,
    Black,
    White,
}
const game_wasm_hash = "19CB5785B2795A4008F71720E76E8040167EE016037A00E332157C286BB88BC4_17382";
const game_checkstate_wasm_hash = "1C9F09482A0BD79DC4F52C3657B09AC78BB5F7581D23AE229D4E1D6C2156EC6D_11425";
let state: GameState = GameState.Error;
let map: Grid[] = [];
async function rpc_getState(): Promise<GameState> {
    let cmd = 1; // get Game State
    let input = new Uint8Array(8 * 6);

    let dv = new DataView(input.buffer);
    dv.setBigUint64(0, BigInt(5), false);//先写的是长度
    dv.setBigUint64(8, BigInt(cmd), false);

    dv.setBigUint64(16, root.v1, false);
    dv.setBigUint64(24, root.v2, false);
    dv.setBigUint64(32, root.v3, false);
    dv.setBigUint64(40, root.v4, false);
    var data = await callLogicRemote(game_checkstate_wasm_hash, input);
    if (data.length == 4) {
        var dvout = new DataView(data.buffer);
        var errcode = dvout.getInt32(0, true);
        console.log("rpc_getState 服务器错误:" + errcode);
        return GameState.Error;
    }
    else {
        var dvout = new DataView(data.buffer);
        var buflen = dvout.getBigUint64(0, false);
        var cmdrtn = dvout.getBigUint64(8, false);
        var state = Number(dvout.getBigUint64(16, false)) as GameState;
        if (state == GameState.Error)
            state = GameState.Black;//默认状态，黑子
        return state;
    }
}
async function rpc_getMap(): Promise<Grid[]> {
    let cmd = 2; // get Map State
    let input = new Uint8Array(8 * 6);

    let dv = new DataView(input.buffer);
    dv.setBigUint64(0, BigInt(5), false);//先写的是长度
    dv.setBigUint64(8, BigInt(cmd), false);

    dv.setBigUint64(16, root.v1, false);
    dv.setBigUint64(24, root.v2, false);
    dv.setBigUint64(32, root.v3, false);
    dv.setBigUint64(40, root.v4, false);
    var data = await callLogicRemote(game_checkstate_wasm_hash, input);
    if (data.length == 4) {
        var dvout = new DataView(data.buffer);
        var errcode = dvout.getInt32(0, true);
        console.log("rpc_getMap 服务器错误:" + errcode);
        return null;
    }
    else {
        var dvout = new DataView(data.buffer);
        var buflen = dvout.getBigUint64(0, false);
        var cmdrtn = dvout.getBigUint64(8, false);
        var maplen = Number(dvout.getBigUint64(16, false));
        if (maplen == 0) {
            return null;
        }
        else {
            var seek = 24;
            var map: Grid[] = [];

            for (var i = 0; i < 9; i++) {
                var g = Number(dvout.getBigUint64(seek, false)) as Grid;
                seek += 8;
                map.push(g);
            }
            return map;
        }
    }
}

//落子
async function rpc_put(x: number, y: number): Promise<MerkleRoot> {
    let input = new Uint8Array(8 * 10);
    let dv = new DataView(input.buffer);
    dv.setBigUint64(0, BigInt(9), false);//先写的是数据包长度
    dv.setBigUint64(8, root.v1, false);//然后写入merkleroot
    dv.setBigUint64(16, root.v2, false);
    dv.setBigUint64(24, root.v3, false);
    dv.setBigUint64(32, root.v4, false);
    dv.setBigUint64(40, BigInt(2), false)//写入messagetype2 =  put ,每条消息有自己的编码规则
    dv.setBigUint64(48, BigInt(state), false)//写入color
    dv.setBigUint64(56, BigInt(x), false)//写入x
    dv.setBigUint64(64, BigInt(y), false)//写入y
    dv.setBigUint64(72, BigInt(0), false)//写入messagetype0 =  end ,表示这一包最后一条消息
    var data = await callLogicRemote(game_wasm_hash, input);
    if (data.length == 4) {
        var dvout = new DataView(data.buffer);
        var errcode = dvout.getInt32(0, true);
        console.log("rpc_put 服务器错误:" + errcode);
        return null;
    }
    else {
        var dvout = new DataView(data.buffer);


        var buflen = dvout.getBigUint64(0, false);
        let newm = new MerkleRoot();
        newm.v1 = dvout.getBigUint64(8, false);
        newm.v2 = dvout.getBigUint64(16, false);
        newm.v3 = dvout.getBigUint64(24, false);
        newm.v4 = dvout.getBigUint64(32, false);
        console.log("rpc_put :" + newm.toString());
        return newm;
    }
}
async function UpdateGame() {

    state = await rpc_getState();
    map = await rpc_getMap();
    if (map == null || map.length == 0) {
        map = [];
        for (var i = 0; i < 9; i++) {
            map.push(Grid.Empty);
        }
    }
    console.log("====================================================");
    console.log("当前Root="+root.toString());

    console.log("棋盘\tX:0\tX:1\tX:2");
    for (var y = 0; y < 3; y++) {
        var txt = "Y:" + y + "\t";
        for (var x = 0; x < 3; x++) {
            if (map[y * 3 + x] == Grid.Empty)
                txt += "-\t";
            else if (map[y * 3 + x] == Grid.Black)
                txt += "B\t";
            else if (map[y * 3 + x] == Grid.White)
                txt += "w\t";
        }
        console.log(txt);
    }

    if (state == GameState.End)
        console.log("游戏状态:游戏结束");
    else if (state == GameState.Black)
        console.log("游戏状态:黑棋落子");
    else if (state == GameState.White)
        console.log("游戏状态:白棋落子");
    else if (state == GameState.Error)
        console.log("游戏状态:错误");
}



//重新开始游戏
function onRestart() {
    root = new MerkleRoot();
}
async function onTest() {
    onRestart();
    state = await rpc_getState();
    console.log("state=" + state);
}
async function main() {


    console.log("三子棋游戏");
    console.log("输入 exit 退出游戏");
    console.log("输入 reset 重开游戏");
    console.log("输入 put x y 落子");



    while (true) {
        await UpdateGame();
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
            var nm = await rpc_put(x, y);
            if (nm != null)
                root = nm;
        }
        if (tags[0] == "test") {
            await onTest();
        }

    }

}
main();