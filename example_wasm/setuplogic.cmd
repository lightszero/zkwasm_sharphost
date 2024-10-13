//用 cmd 而不是bat，是因为 powershell 执行call 不会中断，而bat 执行第一个call 就中断了，不能一次多个

call npx ts-node node_setuplogic.ts .bin/logic.wasm
call npx ts-node node_setuplogic.ts .bin/game_checkstate.wasm
