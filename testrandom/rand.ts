export function NextSeed(seed: u32): u32 {

    //Seed 实现
    var _state = seed;

    _state ^= _state << 13;
    _state ^= _state >> 17;
    _state ^= _state << 5;




    return _state;
}