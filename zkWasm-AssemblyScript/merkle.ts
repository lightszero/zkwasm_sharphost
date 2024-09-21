@external("env", "require")
declare function require(x: i32): void

@external("env", "merkle_setroot")
declare function merkle_setroot(x: u64): void

@external("env", "merkle_getroot")
declare function merkle_getroot(): u64

@external("env", "merkle_address")
declare function merkle_address(x: u64): void

@external("env", "merkle_set")
declare function merkle_set(x: u64): void

@external("env", "merkle_get")
declare function merkle_get(): u64

import { Cache } from "./cache";
import { PoseidonHasher } from "./poseidon";

const LEAF_NODE: u64 = 0;
const TREE_NODE: u64 = 1;

const IS_NODE_BIT: u64 = 0b1000000 << 56;
const IS_EMPTY_BIT: u64 = 0b100000 << 56;
export class CacheData {
    hash: u64[] = [];
    data: u64[] = [];
}
export class Merkle {
    root: u64[] = [0, 0, 0, 0];


    // internal func: key must have length 4
    static data_matches_key(data: u64[], key: u64[]): bool {
        // Recall that data[0] == LEAF_NODE
        return data[1] == key[0] && data[2] == key[1] && data[3] == key[2] && data[4] == key[3]
        /*
        for i in 0..4 {
            if data[i + 1] != key[i] {
                return false;
            };
        }
        return true;
        */
    }

    // using a static buf to avoid memory allocation in smt implementation
    static set_smt_data(t: u64, key: u64[], data: u64[]): u64[] {
        let buf: u64[] = [];
        buf.push(t);
        buf.push(key[0]);
        buf.push(key[1]);
        buf.push(key[2]);
        buf.push(key[3]);
        return buf.concat(data);
    }

    static is_leaf(a: u64): bool {
        return (a & IS_NODE_BIT) == 0
    }

    static is_empty(a: u64): bool {
        return (a & IS_EMPTY_BIT) == 0
    }
    static load(_root: u64[]): Merkle {
        let m = new Merkle();
        m.root[0] = _root[0];
        m.root[1] = _root[1];
        m.root[2] = _root[2];
        m.root[3] = _root[3];
        return m;
    }
    static new(): Merkle {
        //THE following is the depth=31, 32 level merkle root default
        let root: u64[] = [
            14789582351289948625,
            10919489180071018470,
            10309858136294505219,
            2839580074036780766,
        ];
        return Merkle.load(root);
    }
    /// Get the raw leaf data of a merkle subtree
    get_simple(index: u32, data: u64[]): void {
        merkle_address(index as u64);
        merkle_setroot(this.root[0]);
        merkle_setroot(this.root[1]);
        merkle_setroot(this.root[2]);
        merkle_setroot(this.root[3]);
        data[0] = merkle_get();
        data[1] = merkle_get();
        data[2] = merkle_get();
        data[3] = merkle_get();
        //             //enforce root does not change
        merkle_getroot();
        merkle_getroot();
        merkle_getroot();
        merkle_getroot();
    }


    /// Set the raw leaf data of a merkle subtree but does enforced the get/set pair convention
    set_simple_unsafe(index: u32, data: u64[]): void {

        // perform the set
        merkle_address(index as u64);

        merkle_setroot(this.root[0]);
        merkle_setroot(this.root[1]);
        merkle_setroot(this.root[2]);
        merkle_setroot(this.root[3]);

        merkle_set(data[0]);
        merkle_set(data[1]);
        merkle_set(data[2]);
        merkle_set(data[3]);

        this.root[0] = merkle_getroot();
        this.root[1] = merkle_getroot();
        this.root[2] = merkle_getroot();
        this.root[3] = merkle_getroot();

    }

    /// Set the raw leaf data of a merkle subtree
    set_simple(index: u32, data: u64[], hint: u64[] | null): void {
        //         // place a dummy get for merkle proof convension
        //         unsafe {
        merkle_address(index as u64);
        merkle_setroot(this.root[0]);
        merkle_setroot(this.root[1]);
        merkle_setroot(this.root[2]);
        merkle_setroot(this.root[3]);
        //         }
        if (hint != null) {
            require(hint[0] == merkle_get());
            require(hint[1] == merkle_get());
            require(hint[2] == merkle_get());
            require(hint[3] == merkle_get());

        } else {

            merkle_get();
            merkle_get();
            merkle_get();
            merkle_get();
        }


        merkle_getroot();
        merkle_getroot();
        merkle_getroot();
        merkle_getroot();

        this.set_simple_unsafe(index, data);

    }

    get(index: u32, pad: bool): CacheData {
        let hash: u64[] = [];
        hash.length = 4;
        this.get_simple(index, hash);

        let data = Cache.get_data(hash);
        if (data != null && data.length > 0) {
            // //             // FIXME: avoid copy here
            let hash_check = PoseidonHasher.hash(data, pad);
            // //             unsafe {
            require(hash[0] == hash_check[0]);
            require(hash[1] == hash_check[1]);
            require(hash[2] == hash_check[2]);
            require(hash[3] == hash_check[3]);
            // //             }
        } else {

            require(hash[0] == 0);
            require(hash[1] == 0);
            require(hash[2] == 0);
            require(hash[3] == 0);

        }

        let result = new CacheData();
        result.hash = hash;
        result.data = data;
        return result;
    }

    //     /// safe version of set which enforces a get before set
    set(index: u32, data: u64[], pad: bool, hint: u64[] | null): void {
        let hash = PoseidonHasher.hash(data, pad);
        Cache.store_data(hash, data);
        this.set_simple(index, hash, hint);
    }

    //     /// unsafe version of set which does not enforce the get/set pair convention
    set_unsafe(index: u32, data: u64[], pad: bool): void {
        let hash = PoseidonHasher.hash(data, pad);
        Cache.store_data(hash, data);
        this.set_simple_unsafe(index, hash);
    }

    smt_get_local(key: u64[], path_index: u32): u64[] {
        require(path_index < 8);
        let local_index = (key[path_index / 2] >> (32 * (path_index % 2))) as u32;
        // pad is true since the leaf might the root of a sub merkle
        let result = this.get(local_index, true);
        if (result.data == null || result.data.length == 0) {
            // no node was find
            return [];
        } else {
            // crate::dbg!("smt_get_local with data {:?}\n", data);
            if ((result.data[0] & 0x1) == LEAF_NODE) {
                // crate::dbg!("smt_get_local is leaf\n");
                if (Merkle.data_matches_key(result.data, key)) {
                    return result.data.slice(5);
                    //return data[5..data.len()].to_vec();
                } else {
                    // not hit and return len = 0
                    return [];
                }
            } else {
                // crate::dbg!("smt_get_local is node: continue in sub merkle\n");
                require((data[0] & 0x1) == TREE_NODE);
                let sub_merkle = Merkle.load(result.data.slice(1, 5));
                return sub_merkle.smt_get_local(key, path_index + 1)
            }
        }
    }

    smt_set_local(key: u64[], path_index: u32, data: u64[]): void {
        require(path_index < 8);
        let local_index = (key[path_index / 2] >> (32 * (path_index % 2))) as u32;
        let result = this.get(local_index, true);
        if (result.data == null || result.data.length == 0) {
            // let root = self.root;
            // crate::dbg!("smt add new leaf {:?} {:?}\n", root, data);
            let node_buf = Merkle.set_smt_data(LEAF_NODE, key, data);

            this.set_unsafe(local_index, node_buf, true);

        } else {
            //crate::dbg!("smt set local hit:\n");
            if ((result.data[0] & 0x1) == LEAF_NODE) {
                //crate::dbg!("current node for set is leaf:\n");
                if (Merkle.data_matches_key(result.data, key)) {
                    //crate::dbg!("key match update data:\n");
                    // if hit the current node
                    let node_buf = Merkle.set_smt_data(LEAF_NODE, key, data);
                    //                     unsafe {
                    this.set_unsafe(local_index, node_buf, true);
                    //                     }
                } else {
                    //crate::dbg!("key not match, creating sub node:\n");
                    // conflict of key here
                    // 1. start a new merkle sub tree
                    let sub_merkle = Merkle.new();
                    sub_merkle.smt_set_local(
                        result.data.slice(1, 5),
                        path_index + 1,
                        result.data.slice(5)
                    );
                    sub_merkle.smt_set_local(key, path_index + 1, data);
                    let node_buf = Merkle.set_smt_data(TREE_NODE, sub_merkle.root, []);
                    // 2 update the current node with the sub merkle tree
                    // crate::dbg!("created sub node {:?}:\n", node_buf);
                    // OPT: shoulde be able to use the hint_hash in the future
                    this.set(local_index, node_buf.slice(0, 5), true, null);
                }
            } else {
                //crate::dbg!("current node for set is node:\n");
                // the node is already a sub merkle
                require((result.data[0] & 0x1) == TREE_NODE);
                let sub_merkle = Merkle.load(result.data.slice(1, 5));
                sub_merkle.smt_set_local(key, path_index + 1, data);
                let node_buf = Merkle.set_smt_data(TREE_NODE, sub_merkle.root, []);
                this.set(local_index, node_buf.slice(0, 5), true, null);
            }
        }
    }

    //     // optimized version for
    smt_get_local_u64(key: u64, path_index: u32): u64 {
        //crate::dbg!("start smt_get_local {}\n", path_index);
        require(path_index < 2);
        let local_index = (key >> (32 * (path_index % 2))) as u32;
        // pad is true since the leaf might the root of a sub merkle
        let stored_data: u64[] = [0, 0, 0, 0];

        this.get_simple(local_index, stored_data);
        // data is stored in little endian
        let is_leaf = is_leaf(stored_data[3]);
        if (is_leaf) {
            // second highest bit indicates the leaf node is empty or not
            let is_empty = is_empty(stored_data[3]);
            let stored_key = stored_data[0];
            if ((!is_empty) && (key == stored_key)) {
                return stored_data[1];
            } else {
                // is empty or not hit
                return 0;
            }
        } else {
            //crate::dbg!("smt_get_local is node: continue in sub merkle\n");
            // make sure that there are only 2 level

            require(path_index == 0);

            stored_data[3] = stored_data[3] & !IS_NODE_BIT;
            let sub_merkle = Merkle.load(stored_data);
            return sub_merkle.smt_get_local_u64(key, path_index + 1)
        }
    }

    smt_set_local_u64(key: u64, path_index: u32, data: u64): void {
        require(path_index < 2);
        let local_index = (key >> (32 * path_index)) as u32;
        let stored_data: u64[] = [0, 0, 0, 0];
        this.get_simple(local_index, stored_data);
        let is_leaf = is_leaf(stored_data[3]);

        // LEAF_NODE must equal zero
        if (is_leaf) {
            let is_empty = is_empty(stored_data[3]);
            if (is_empty) {
                self.set_simple(local_index, [key, data, 0, IS_EMPTY_BIT], None);
            } else {
                //crate::dbg!("smt set local hit:\n");
                if (key == stored_data[0]) {
                    //crate::dbg!("current node for set is leaf:\n");
                    stored_data[0] = key;
                    stored_data[1] = data;
                    self.set_simple(local_index, stored_data, None);
                } else {
                    //crate::dbg!("key not match, creating sub node:\n");
                    // conflict of key here
                    // 1. start a new merkle sub tree
                    let sub_merkle = Merkle.new();
                    sub_merkle.smt_set_local_u64(stored_data[0], path_index + 1, stored_data[1]);
                    sub_merkle.smt_set_local_u64(key, path_index + 1, data);
                    stored_data = sub_merkle.root;
                    stored_data[3] = stored_data[3] | IS_NODE_BIT;
                    // 2 update the current node with the sub merkle tree
                    self.set_simple(local_index, stored_data, None);
                }
            }
        } else {
            //crate::dbg!("current node for set is node:\n");
            // make sure that there are only 2 level
            require(path_index == 0);
            stored_data[3] = stored_data[3] & !IS_NODE_BIT;
            let sub_merkle = Merkle.load(stored_data);
            sub_merkle.smt_set_local_u64(key, path_index + 1, data);
            sub_merkle.root[3] = sub_merkle.root[3] | IS_NODE_BIT;
            self.set_simple(local_index, sub_merkle.root, None);
        }
    }
}


// impl SMT for Merkle {
//     fn smt_get(&self, key: &[u64; 4]) -> Vec<u64> {
//         self.smt_get_local(key, 0)
//     }

//     fn smt_set(&mut self, key: &[u64; 4], data: &[u64]) {
//         self.smt_set_local(key, 0, data)
//     }
// }


// impl SMTU64 for Merkle {
//     fn smt_get(&self, key: u64) -> u64 {
//         self.smt_get_local_u64(key, 0)
//     }

//     fn smt_set(&mut self, key: u64, data: u64) {
//         self.smt_set_local_u64(key, 0, data)
//     }
// }