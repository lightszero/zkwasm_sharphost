//buzz2.ts

const knum: i64 = 1000;


export function logic(input: i64[]): i64[] {
    var succ: boolean = true;
    if (input.length < 9)
        succ = false;//输入长度不够
    //不能用小数，用千分位证书代替
    var config1004 = input[0];//步数消耗配置
    var config41005 = input[1];//add headvalue 配置
    var movePosEndX = input[2];
    var movePosEndY = input[3];//采用千分位整数 ， 即1.11 表示为 1110
    var moveLength = input[4];//采用千分位整数， 即1.11 表示为 1110
    var timeStamp = input[5];//时间戳
    var state8005 = input[6];//8005道具数量
    var statePosX = input[7];
    var statePosY = input[8];
    var stateHeatValue = input[9];//StateValue
    var statetimeStamp = input[10];//StateValue

    var cost: i64 = moveLength * config1004 / knum;
    if (succ) {//检查扣费

        if (cost <= state8005) {
            succ == true;
            state8005 -= cost;//扣钱
        }
        else {
            succ = false;
        }
    }
    if (succ) //改变位置状态
    {
        statePosX = movePosEndX;
        statePosY = movePosEndY;
    }
    if (succ)//增加Heatvalue
    {
        stateHeatValue += cost * config41005;
    }
    if (succ) {
        if (timeStamp > statetimeStamp) {
            statetimeStamp = timeStamp;//更新时间戳
        }
        else {
            succ = false;
        }
    }
    var output: i64[] = []

    if (succ) {
        output.push(1);
        output.push(state8005);//output.push(0);
        output.push(statePosX);
        output.push(statePosY);
        output.push(stateHeatValue);
        output.push(statetimeStamp);
    }
    else {
        output.push(0);
        output.push(0);
        output.push(0);
        output.push(0);
        output.push(0);
        output.push(0);
    }
    return output;
}