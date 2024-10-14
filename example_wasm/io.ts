@external("env", "print_char")
declare function print_char(charCode: i32): void
function print(message: string): void {
    for (var i = 0; i < message.length; i++) {
        print_char(message.charCodeAt(i));
    }
    print_char("\n".charCodeAt(0));
}

export enum InputMessageType {
    EndMessage = 0,
    SayHello = 1,
    Put,//落子
}
export enum Color {
    Error = 0,
    Black = 1,
    White = 2,
    End = 3,
}
export class InputMessage {
    private type: InputMessageType;
    get Type(): InputMessageType {
        return this.type;
    }
    constructor(_type: InputMessageType) {
        this.type = _type;
    }
}
export class InputMessage_SayHello extends InputMessage {
    constructor() {
        super(InputMessageType.SayHello);
    }
}

export class InputMessage_Put extends InputMessage {
    constructor() {
        super(InputMessageType.Put);
    }
    color: Color;
    x: i32;
    y: i32;
    LoadFrom(data: u64[], _seek: i32): i32 {
        print("InputMessage_Put.LoadFrom")
        let seek = _seek;
        this.type = InputMessageType.Put;
        this.color = data[seek] as Color;
        this.x = data[seek + 1] as i32;
        this.y = data[seek + 2] as i32;
        print("InputMessage_Put.LoadFrom done.")
        return seek + 3;
    }
}
export class InputData {
    MerkleRoot: u64[] = [];
    Messages: InputMessage[] = [];
    LoadFrom(data: u64[], _seek: i32): i32 {
        let seek = _seek;
        this.MerkleRoot = [data[0], data[1], data[2], data[3]];
        this.Messages = [];
        seek += 4;
        while (true) {
            let messagetype = data[seek] as InputMessageType;
            seek += 1;
            if (messagetype == InputMessageType.EndMessage) {
                print("InputData.LoadFrom end")
                break;
            }
            else if (messagetype == InputMessageType.SayHello) {
                print("InputData.LoadFrom hello")
                let message = new InputMessage_SayHello();
                this.Messages.push(message);
            }
            else if (messagetype == InputMessageType.Put) {
                print("InputData.LoadFrom put")
                let message = new InputMessage_Put();
                seek = message.LoadFrom(data, seek);
                this.Messages.push(message);
            }
            else
            {
                print("InputData.LoadFrom error:"+messagetype.toString());
            }
        }
        return seek;
    }
}

export class OutputData {
    MerkleRoot: u64[] = [];
    Save(): u64[] {
        let data: u64[] = [this.MerkleRoot[0], this.MerkleRoot[1], this.MerkleRoot[2], this.MerkleRoot[3]];

        return data;
    }
}

