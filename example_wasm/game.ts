import { Cache } from "../zkWasm-AssemblyScript/cache";
import { PoseidonHasher } from "../zkWasm-AssemblyScript/poseidon";
import { Merkle } from "../zkWasm-AssemblyScript/merkle";
import { KeyValueMap_SMT, KeyValueMap_SMTU64 } from "../zkWasm-AssemblyScript/kvpair";
import { Color, InputData, InputMessage, InputMessage_Put, InputMessageType, OutputData } from "./io";

@external("env", "print")
declare function print(message:string): void


export function logic(input: u64[]): u64[] {
    //反序列化输入
    let inputdata = new InputData();
    inputdata.LoadFrom(input, 0);

    let game = new Game();
    game.Init(inputdata.MerkleRoot);

    //从输入消息中得到步骤并执行
    for (let i = 0; i < inputdata.Messages.length; i++) {
        game.DoStep(inputdata.Messages[i]);
    }

    //输出结果
    let outputdata = new OutputData();
    outputdata.MerkleRoot = game.MerkleTree.root;
    return outputdata.Save();
}
const indexMap:u64= 789;//选一个固定key，用来存map的
const indexState:u64= 790;//选一个固定key，用来存state的
class Game {
    MerkleTree:Merkle = Merkle.new();
    map:u64[]=[];
    state:Color=Color.Black;
   
    //初始化游戏
    Init(MerkleRoot: u64[]):void {
        this.MerkleTree=Merkle.load([MerkleRoot[0],MerkleRoot[1],MerkleRoot[2],MerkleRoot[3]]);
      
 
        this.map = this.MerkleTree.smt_get_local([indexMap,0,0,0],0);
        if(this.map.length!=9)
            this.map=[0,0,0,0,0,0,0,0,0];

       
        let state:u64[] = this.MerkleTree.smt_get_local([indexState,0,0,0],0);
        if(state.length==0)
            state=[Color.Black];

        this.state=state[0] as Color;   
    }
    DoStep(message:InputMessage): void {
        if(message.Type==InputMessageType.SayHello){
            this.DoStep_SayHello(message);
        }
        else if(message.Type==InputMessageType.Put){
            let put = message as InputMessage_Put;
            this.DoStep_Put(put);
           

        }
    }       
    DoStep_SayHello(message:InputMessage): void {
        print("hello");
    }
    DoStep_Put(message:InputMessage_Put): void {
       
        var v = message.color;
        if(this.map[message.y*3+message.x]!=0){
            throw new Error("已经放置过了");
        }
        if(this.IsEnd())
        {
            throw new Error("游戏已结束");
        }
        if(v!=this.state)
        {
            throw new Error("不是你的回合");
        }
        //落子
        this.map[message.y*3+message.x]=message.color;

        //反转颜色
        if(this.state==  Color.Black)
        {
            this.state=Color.White;
        }
        else
        {
            this.state=Color.Black;
        }
        if(this.IsEnd())
        {
            print("游戏已结束");
            this.state= Color.End;
        }

        //更新MerkleTree 数据
        this.MerkleTree.smt_set_local([indexMap,0,0,0],0,this.map);
            
        let statedata:u64[] =[this.state];
        this.MerkleTree.smt_set_local([indexState,0,0,0],0,statedata);
        //这个实现还有点问题
        //this.MerkleTree.smt_set_local_u64(indexState,0,this.state as u64);
    }
    IsEnd():boolean{
        
        for(var y=0;y<3;y++)
        {
            //横向连
            if(this.map[y*3+0]!=0&&this.map[y*3+0]==this.map[y*3+1]&&this.map[y*3+0]==this.map[y*3+2])
                return true;
        }
        for(var x=0;x<3;x++)
        {
            //纵向连
            if(this.map[0*3+x]!=0&&this.map[0*3+x]==this.map[1*3+x]&&this.map[0*3+x]==this.map[2*3+x])
                return true;
        }
        //斜向判断
        if(this.map[0*3+0]!=0&&this.map[0*3+0]==this.map[1*3+1]&&this.map[0*3+0]==this.map[2*3+2])
            return true;
        if(this.map[0*3+2]!=0&&this.map[0*3+2]==this.map[1*3+1]&&this.map[0*3+2]==this.map[2*3+0])
            return true;
        return false;
    }
}