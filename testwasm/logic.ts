import * as buzz1 from "./buzz1"

@external("env", "wasm_input")
declare function wasm_input(x: i32): i64


@external("env", "wasm_output")
declare function wasm_output(v: i64): void

export function gamemain(): void {
    //read input
    var count = wasm_input(0);
    var arr: i64[] = [];
    for (var i = 0; i < count; i++) {
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