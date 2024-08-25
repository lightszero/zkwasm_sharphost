//buzz2.ts

import * as cache from "./cache";   

const knum: i64 = 1000;


export function logic(input: u64[]): u64[] {

    
    var hash:u64[]=[1,2,3,4];
    var data:u64[]=[1,2,3,4,5,6]; 
    cache.store_data(hash,data);

    var data2 = cache.get_data(hash);

    var output: u64[] = []
    output.push(100);
    output.push(data2.length);
    for(var i=0;i<data2.length;i++){
        output.push(data2[i]);
    }
    output.push(200);
    return output;
}