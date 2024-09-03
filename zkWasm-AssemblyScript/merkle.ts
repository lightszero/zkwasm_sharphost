

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


//root :u64[4]
//path :u64
//hash :u64[4]
//return new root:u64[4]
export function store_hash(root: u64[], path: u64, hash: u64[]): u64[] {
    merkle_address(path);

    merkle_setroot(root[0]);
    merkle_setroot(root[1]);
    merkle_setroot(root[2]);
    merkle_setroot(root[3]);

    merkle_set(hash[0]);
    merkle_set(hash[1]);
    merkle_set(hash[2]);
    merkle_set(hash[3]);
    var newroot: u64[] = [];
    newroot.push(merkle_getroot());
    newroot.push(merkle_getroot());
    newroot.push(merkle_getroot());
    newroot.push(merkle_getroot());
    return newroot;
}
//root :u64[4]
//path :u64
//return hash :u64[4]
export function get_hash(root: u64[], path: u64): u64[] {
    merkle_address(path); // build in merkle address has default depth 32

    merkle_setroot(root[0]);
    merkle_setroot(root[1]);
    merkle_setroot(root[2]);
    merkle_setroot(root[3]);
    var hash: u64[] = [];
    hash.push(merkle_get());
    hash.push(merkle_get());
    hash.push(merkle_get());
    hash.push(merkle_get());

    //enforce root does not change
    merkle_getroot();
    merkle_getroot();
    merkle_getroot();
    merkle_getroot();

    return hash;
}