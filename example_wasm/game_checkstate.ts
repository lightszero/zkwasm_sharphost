import { KeyValueMap_SMT, KeyValueMap_SMTU64 } from "../zkWasm-AssemblyScript/kvpair";
import { Merkle } from "../zkWasm-AssemblyScript/merkle";
import { Color, InputData, InputMessage, InputMessage_Put, InputMessageType, OutputData } from "./io";

@external("env", "print")
declare function print(message:string): void

export enum CmdType
{
    GetGameState = 1,

}
const indexMap:u64= 789;//选一个固定key，用来存map的
const indexState:u64= 790;//选一个固定key，用来存state的
export function logic(input: u64[]): u64[] {
    //反序列化输入
    let cmd = input[0] as CmdType;
    if(cmd==  CmdType.GetGameState)
    {
        let merkleRoot = [input[1],input[2],input[3],input[4]];
      
        let MerkleTree:Merkle = Merkle.load(merkleRoot);
        let status = MerkleTree.smt_get_local_u64(indexState,0) as Color;
        print("status:"+status.toString());
        return [cmd,status];
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