@external("env", "cache_set_mode")
declare function cache_set_mode(x: u64): void

@external("env", "cache_set_hash")
declare function cache_set_hash(x: u64): void

@external("env", "cache_store_data")
declare function cache_store_data(x: u64): void

@external("env", "cache_fetch_data")
declare function cache_fetch_data(): u64

import { print } from "./lib";


export class Cache
{
    static store_data(hash: u64[], data: u64[]):void {
        print("Cache.store_data "+hash[0].toString()+","+hash[1].toString()+","+hash[2].toString()+","+hash[3].toString() +" datalen="+data.length.toString());
        cache_set_mode(1);
        for (var i = 0; i < data.length; i++) {
            cache_store_data(data[i]);
        }
        cache_set_hash(hash[0]);
        cache_set_hash(hash[1]);
        cache_set_hash(hash[2]);
        cache_set_hash(hash[3]);

    }


    static get_data(hash: u64[]): u64[] {
       
        var output: u64[] = [];
        cache_set_mode(0);
        cache_set_hash(hash[0]);
        cache_set_hash(hash[1]);
        cache_set_hash(hash[2]);
        cache_set_hash(hash[3]);
        var len:u64 = cache_fetch_data();
        print("Cache.get_data "+hash[0].toString()+","+hash[1].toString()+","+hash[2].toString()+","+hash[3].toString() +" datalen="+len.toString());
        for (var i = 0; i < i32(len); i++) {
            output.push(cache_fetch_data());
        }
        return output;

    }
}
