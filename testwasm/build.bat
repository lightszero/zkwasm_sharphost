start /b asc zk.ts -O --noAssert -o .bin\zk.wasm --disable bulk-memory  --runtime stub --use abort=0 
start /b asc logic.ts -O --noAssert -o .bin\logic.wasm --disable bulk-memory  --runtime stub --use abort=0 
