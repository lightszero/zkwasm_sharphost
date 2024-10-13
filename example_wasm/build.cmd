
//用 cmd 而不是bat，是因为 powershell 执行call 不会中断，而bat 执行第一个call 就中断了，不能一次多个

@echo [skip]call  asc zk.ts -O --noAssert -o .bin\zk.wasm --disable bulk-memory  --runtime stub --use abort=0 

call asc game_checkstate.ts -O --noAssert -o .bin\game_checkstate.wasm --disable bulk-memory  --runtime stub --use abort=0

echo done game_check.

call asc logic.ts -O --noAssert -o .bin\logic.wasm --disable bulk-memory  --runtime stub --use abort=0 

echo done game.