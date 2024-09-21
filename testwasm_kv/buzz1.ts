//buzz2.ts
  
import {Cache} from "../zkWasm-AssemblyScript/cache";   
import {PoseidonHasher} from "../zkWasm-AssemblyScript/poseidon";
import {Merkle} from "../zkWasm-AssemblyScript/merkle";
import {KeyValueMap_SMT,KeyValueMap_SMTU64} from "../zkWasm-AssemblyScript/kvpair";

export function logic(input: u64[]): u64[] {

    
    var hash:u64[]=[1,2,3,4];
    var data:u64[]=[1,2,3,4,5,6]; 
    Cache.store_data(hash,data);

    var data2 = Cache.get_data(hash);




    //起始默克尔根
    let root1:u64[] = [
        14789582351289948625,
        10919489180071018470,
        10309858136294505219,
        2839580074036780766,
    ];
    let merkle = Merkle.load(root1);
    merkle.smt_set_local([0x100000002,0,0,0],u32(0),hash);
    let root2 = merkle.root;

    var output: u64[] = []
    output.push(100);
    output.push(data2.length);
    for(var i=0;i<data2.length;i++){
        output.push(data2[i]);
    }
    output.push(200);

    output.push(root1[0]);
    output.push(root1[1]);
    output.push(root1[2]);
    output.push(root1[3]);
    output.push(210);
    output.push(root2[0]);
    output.push(root2[1]);
    output.push(root2[2]);
    output.push(root2[3]);
    output.push(250);
    return output;
}