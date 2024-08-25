# 记录命令行，准备改为小服务器
 cargo run --release -- --params params name  setup --host default --wasm ../bin/zk_testzk1.wasm

# prove


# 输入 编码为private
# 输出 编码为public
# 根据逻辑验证输入是否能得到输出
# config1004=1
# config41005=2
# posx = 3
# posy = 3
# length = 5000
# newtimeStamp =66
# state8005 =700
# statePosX = ?
# statePosY = ?
# stateHeatValue = 10
# statetimeStamp = 11

# shouldoutput
# succ=1
# state8005 = 695
# statePosX = 3
# statePosY = 3
# stateHeatValue = 20
# statetimeStamp = 66


cargo run --release -- --params params name  prove  --wasm ../bin/zk.wasm --output ../p2  --private 11:i64,1:i64,2:i64,3:i64,3:i64,5000:i64,66:i64,700:i64,8:i64,9:i64,10:i64,11:i64 --public 6:i64,1:i64,695:i64,3:i64,3:i64,20:i64,66:i64

# verify
#prove 的数据已经写入在 ../p2

cargo run --release -- --params params name  verify --output ../p2