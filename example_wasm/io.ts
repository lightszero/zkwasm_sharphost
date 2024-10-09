

export enum InputMessageType
{
    EndMessage=0,
    SayHello,
    Put,//落子
}
export enum Color
{
    Error=0,
    Black = 1,
    White = 2,
    End=3,
}
export class InputMessage
{
    private type:InputMessageType;
    get Type():InputMessageType
    {
        return this.type;
    }
    constructor(_type:InputMessageType)
    {
        this.type=_type; 
    }
}
export class InputMessage_SayHello extends InputMessage
{
    constructor()
    {
        super(InputMessageType.SayHello);
    }
}

export class InputMessage_Put extends InputMessage
{
    constructor()
    {
        super(InputMessageType.Put);
    }
    color:Color;
    x:i32;
    y:i32;
    LoadFrom(data:u64[],_seek:i32):i32
    {
        let seek=_seek;
        this.type=InputMessageType.Put;
        this.color=data[seek] as Color;
        this.x=data[seek+1] as i32;
        this.y=data[seek+2] as i32;
        return seek+3;
    }
}
export class InputData
{
    MerkleRoot: u64[]=[];
    Messages:InputMessage[]=[];
    LoadFrom(data:u64[],_seek:i32):i32
    {
        let seek=_seek;
        this.MerkleRoot=[data[0],data[1],data[2],data[3]];
        this.Messages=[];
        seek+=4;
        while(true)
        {
            let messagetype = data[seek] as InputMessageType;
            seek+=1;
            if(messagetype==InputMessageType.EndMessage)
            {
                break;
            }
            else if(messagetype==InputMessageType.SayHello)
            {
                let message = new InputMessage_SayHello();
                this.Messages.push(message);
            }
            else if(messagetype==InputMessageType.Put)
            { 
                let message = new InputMessage_Put();
                seek+=message.LoadFrom(data,seek);
                this.Messages.push(message);
            }
        }
       return seek;
    }
}

export  class  OutputData
{
    MerkleRoot: u64[]=[];
    Save():u64[]
    {
        let data:u64[]=[this.MerkleRoot[0],this.MerkleRoot[1],this.MerkleRoot[2],this.MerkleRoot[3]];
      
        return data;
    }
}

