use std::sync::Mutex;
use context::{poseidon::PoseidonContext};
pub mod poseidon;
pub mod context;
lazy_static::lazy_static! {
    //pub static ref DATACACHE_CONTEXT: Mutex<CacheContext> = Mutex::new(CacheContext::new());
    //pub static ref MERKLE_CONTEXT: Mutex<MerkleContext> = Mutex::new(MerkleContext::new(0));
    pub static ref POSEIDON_CONTEXT: Mutex<PoseidonContext> = Mutex::new(PoseidonContext::default(0));
    //pub static ref JUBJUB_CONTEXT: Mutex<BabyJubjubSumContext> = Mutex::new(BabyJubjubSumContext::default(0));
}
#[no_mangle]
pub extern "C" fn poseidon_new(arg: u64) {
    POSEIDON_CONTEXT.lock().unwrap().poseidon_new(arg as usize);
}

#[no_mangle]
pub extern "C" fn poseidon_push(arg: u64) {
    POSEIDON_CONTEXT.lock().unwrap().poseidon_push(arg);
}

#[no_mangle]
pub extern "C" fn poseidon_finalize() -> u64 {
    POSEIDON_CONTEXT.lock().unwrap().poseidon_finalize()
}