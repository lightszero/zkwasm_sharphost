//buzz2.ts
import * as rand from "./rand"

//input 0 //随机种子，存下来

//output 0 ,得到的随机数
//output 1 ,新的随机种子
export function logic(input: i64[]): i64[] {
    var output: i64[] = [];

    var seed = input[0];//拿到输入的随机种子
    seed = rand.NextSeed(seed);//调用rand.NextSeed函数，得到新的随机种子
    var rv = seed % 10000;//产生一个0~9999的随机数\
   
    output.push(rv);//返回随机数
    output.push(seed);//返回新的随机种子
    output.push(999999999);//随便返回点啥
    return output;
}