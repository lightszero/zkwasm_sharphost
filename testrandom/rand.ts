const a:i64 = 25214903917;
const c:i64 = 11;
export function NextSeed(seed: i64): i64 {
    //Seed 实现
    var _state = (seed*a+c)&(1<<48-1);

    return _state;
}