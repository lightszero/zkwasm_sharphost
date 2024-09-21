@external("env", "poseidon_new")
declare function poseidon_new(x: u64): void

@external("env", "poseidon_push")
declare function poseidon_push(x: u64): void

@external("env", "poseidon_finalize")
declare function poseidon_finalize(): u64

export class PoseidonHasher {
    value0: i32 = 0;
    constructor() {
        poseidon_new(1);
        this.value0 = 0;
    }
    static hash(data: u64[], padding: bool): u64[] {
        let hasher = new PoseidonHasher();
        if (padding) {
            let group = data.length / 3;
            let j = 0;
            for (let i = 0; i < group; i++) {
                j = i * 3;
                hasher.update(data[j]);
                hasher.update(data[j + 1]);
                hasher.update(data[j + 2]);
                hasher.update(0);
            }
            j += 3;
            for (var i = j; i < data.length; i++) {
                hasher.update(data[i]);
            }
        } else {
            for (let i = 0; i < data.length; i++) {
                hasher.update(data[i]);
            }
        }
        return hasher.finalize()
    }
    update(v: u64):void {

        poseidon_push(v);

        this.value0 += 1;
        if (this.value0 == 32) {

            poseidon_finalize();
            poseidon_finalize();
            poseidon_finalize();
            poseidon_finalize();
            poseidon_new(0);

            this.value0 = 0;
        }
    }
    finalize(): u64[] {
        if ((this.value0 & 0x3) != 0) {
            for (var i = (this.value0 & 0x3); i < 4; i++) {
                poseidon_push(0);
                this.value0 += 1;
            }
        }
        if (this.value0 == 32) {

            poseidon_finalize();
            poseidon_finalize();
            poseidon_finalize();
            poseidon_finalize();
            poseidon_new(0);

            this.value0 = 0;
        }

        poseidon_push(1);

        this.value0 += 1;
        for (var i2 = this.value0; i2 < 32; i2++) {
            poseidon_push(0);
            this.value0 += 1;
            poseidon_push(0);
        }

        let outdata:u64[]=[];
        outdata.push(poseidon_finalize());
        outdata.push(poseidon_finalize());
        outdata.push(poseidon_finalize());
        outdata.push(poseidon_finalize());
        return outdata;
    }
}