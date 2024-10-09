import * as buzz1 from "./game"


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
    var arroutput = buzz1.logic(arr);


    //set output
    wasm_output(arroutput.length);
    for (var j = 0; j < arroutput.length; j++) {
        wasm_output(arroutput[j]);
    }
}