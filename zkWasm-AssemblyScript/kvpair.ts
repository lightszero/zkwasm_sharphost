import {Merkle} from "./merkle";

//约等于接口
// pub trait SMT {
//     fn smt_get(&self, key: &[u64; 4]) -> Vec<u64>;
//     fn smt_set(&mut self, key: &[u64; 4], data: &[u64]);
// }

// pub trait SMTU64 {
//     fn smt_get(&self, key: u64) -> u64;
//     fn smt_set(&mut self, key: u64, data: u64);
// }

// /// sparse merkle tree implemented by adding indicators at leafs of each group (32 depth)
// /// to indicate whether the leaf is a data leaf or a root of a deeper merkle tree
// pub struct KeyValueMap<S: SMT> {
//     pub merkle: S,
// }

// impl<S: SMT> KeyValueMap<S> {
//     pub fn new(root_merkle: S) -> Self {
//         KeyValueMap {
//             merkle: root_merkle,
//         }
//     }
//     pub fn set(&mut self, key: &[u64; 4], data_buf: &[u64]) {
//         self.merkle.smt_set(key, data_buf);
//     }
//     pub fn get(&self, key: &[u64; 4]) -> Vec<u64> {
//         self.merkle.smt_get(key)
//     }
// }

// pub struct KeyValueMapU64<S: SMTU64> {
//     pub merkle: S,
// }

// impl<S: SMTU64> KeyValueMapU64<S> {
//     pub fn new(root_merkle: S) -> Self {
//         KeyValueMapU64 {
//             merkle: root_merkle,
//         }
//     }
//     pub fn set(&mut self, key: u64, data: u64) {
//         self.merkle.smt_set(key, data);
//     }
//     pub fn get(&self, key: u64) -> u64 {
//         self.merkle.smt_get(key)
//     }
// }


export class KeyValueMap_SMT
{
    merkle:Merkle;
    constructor(root_merkle: Merkle){
        this.merkle =root_merkle;
    }
    set(key:u64[],data_buf:u64[]):void
    {
        this.merkle.smt_set_local(key,0,data_buf);
    }
    get(key:u64[]):u64[]
    {
        return this.merkle.smt_get_local(key,0);
    }
}
export class KeyValueMap_SMTU64
{
    merkle:Merkle;
    constructor(root_merkle: Merkle){
        this.merkle =root_merkle;
    }
    set(key:u64,data_buf:u64):void
    {
        this.merkle.smt_set_local_u64(key,0,data_buf);
    }
    get(key:u64):u64
    {
        return this.merkle.smt_get_local_u64(key,0);
    }
}
