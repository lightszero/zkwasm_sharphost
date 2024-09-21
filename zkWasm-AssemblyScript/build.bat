call asc cache.ts -O --noAssert -o .temp\temp.wasm --disable bulk-memory  --runtime stub --use abort=0 
call asc poseidon.ts -O --noAssert -o .temp\temp.wasm --disable bulk-memory  --runtime stub --use abort=0 
call asc merkle.ts -O --noAssert -o .temp\temp.wasm --disable bulk-memory  --runtime stub --use abort=0 
call asc kvpair.ts -O --noAssert -o .temp\temp.wasm --disable bulk-memory  --runtime stub --use abort=0 
