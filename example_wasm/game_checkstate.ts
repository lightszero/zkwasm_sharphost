import { KeyValueMap_SMT, KeyValueMap_SMTU64 } from "../zkWasm-AssemblyScript/kvpair";
import { Merkle } from "../zkWasm-AssemblyScript/merkle";
import { Color, InputData, InputMessage, InputMessage_Put, InputMessageType, OutputData } from "./io";

//调试功能，在wasmlogic 服务器控制台中显示
@external("env", "print_char")
declare function print_char(charCode: i32): void
function print(message: string): void {
    for (var i = 0; i < message.length; i++) {
        print_char(message.charCodeAt(i));
    }
    print_char("\n".charCodeAt(0));
}


export enum CmdType {
    GetGameState = 1,
    GetMapState = 2,
}
const indexMap: u64 = 789;//选一个固定key，用来存map的
const indexState: u64 = 790;//选一个固定key，用来存state的
export function logic(input: u64[]): u64[] {

    //反序列化输入
    let cmd = input[0] as CmdType;
    print("cmd=" + cmd.toString());
    if (cmd == CmdType.GetGameState) {
        //检查游戏状态
        let merkleRoot = [input[1], input[2], input[3], input[4]];

        let MerkleTree: Merkle = Merkle.load(merkleRoot);
        let status = MerkleTree.smt_get_local_u64(indexState, 0) as Color;
        print("status:" + status.toString());
        return [cmd, status];
    }
    if (cmd == CmdType.GetMapState) {
        let merkleRoot = [input[1], input[2], input[3], input[4]];
        let MerkleTree: Merkle = Merkle.load(merkleRoot);
        let status = MerkleTree.smt_get_local([indexState, 0, 0, 0], 0);
        print("status len:" + status.length.toString());
        if (status.length == 0)
            return [cmd, 0];
        else
        {
            let result:u64[] =[cmd,status.length];
            return result.concat(status);
        }
           
    }
    return [0];
}


//logicmain 固定输出
@external("env", "wasm_input")
declare function wasm_input(x: i32): u64


@external("env", "wasm_output")
declare function wasm_output(v: u64): void

export function logicmain(): void {
    //read input
    var count = wasm_input(0);
    var arr: u64[] = [];
    for (var i = 0; i < i32(count); i++) {
        var v = wasm_input(0);
        arr.push(v);
    }

    //do game
    var arroutput = logic(arr);


    //set output
    wasm_output(arroutput.length);
    for (var j = 0; j < arroutput.length; j++) {
        wasm_output(arroutput[j]);
    }
}