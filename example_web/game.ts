
let stateText: HTMLSpanElement;
let map: HTMLDivElement[];
function NewLine() {
    document.body.appendChild(document.createElement("br"));
}

const  MERKLE_TREE_HEIGHT = 32;
async function getDataFromMerkle(root:string,address: number):Promise<bigint[]> {
  
    var index = BigInt(address) + 1n<<BigInt(MERKLE_TREE_HEIGHT) -1n;

    var body ={"jsonrpc":"2.0","id":1,"method":"get_leaf","params":{"root":root,"index":index.toString()}};
  
    var r = await fetch("http://18.162.245.133:999/", {
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        "body": JSON.stringify(body), "method": "POST"
    });
    var result =JSON.parse( await r.text());
    console.log(result);    
    return [BigInt(0)];
}
async function onInit() {
    getDataFromMerkle("0000",789);
}
//点击棋盘
function onClick(mapgrid: number) {
    var grid = map[mapgrid];
    var x = mapgrid % 3;
    var y = Math.floor(mapgrid / 3);
    console.log("click " + x + "," + y);
}
//重新开始游戏
function onRestart() {

}
window.onload = async () => {

    var title = document.createElement("span");
    title.innerHTML = "三子棋游戏";
    title.style.fontSize = "30px";
    document.body.appendChild(title);  //创建标题
    NewLine()



    stateText = document.createElement("span");
    stateText.innerHTML = "==>stateText";
    document.body.appendChild(stateText);
    NewLine()

    //布置棋盘
    map = [];
    for (var y = 0; y < 3; y++) {
        for (var x = 0; x < 3; x++) {
            var divgrid = document.createElement("div");
            divgrid.innerHTML = " ";
            divgrid.style.backgroundColor = "white";
            divgrid.style.width = "64px";
            divgrid.style.height = "64px";
            divgrid.style.border = "1px solid black";
            divgrid.style.display = "inline-block";
            let mapgrid = y * 3 + x;
            divgrid.onclick = () => {
                onClick(mapgrid);

            }
            document.body.appendChild(divgrid);
            map.push(divgrid);

        }
        NewLine()
    }

    //放置重新开始按钮
    NewLine()
    var btn = document.createElement("button");
    btn.innerHTML = "重新开始";
    document.body.appendChild(btn);
    btn.onclick = () => {
        onRestart();
    }


    //初始化
    await onInit();
}