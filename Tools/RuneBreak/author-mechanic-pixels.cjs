// Pixel coordinates for new Aseprite layers. Uses only colors already present in the reference.
// Existing rune PNGs and RuneBreak.aseprite remain untouched.
const fs=require('fs'),path=require('path');const W=192,H=48,pixels=new Map();
const ink='15232e',shadow='25373b',steel='6c967e',edge='a3afa0',shine='f7ebc1',blue='669fb4',light='c0e4e5',gold='e2b860';
let ox=0;
function pixel(x,y,c){const n=parseInt(c,16);pixels.set((x+ox)+','+y,{x:x+ox,y,r:n>>16,g:(n>>8)&255,b:n&255,a:255});}
function rect(x,y,w,h,c){for(let j=y;j<y+h;j++)for(let i=x;i<x+w;i++)pixel(i,j,c);}
function link(x,y,vertical=false){const w=vertical?5:9,h=vertical?9:5;rect(x,y,w,h,ink);rect(x+1,y+1,w-2,h-2,edge);rect(x+2,y+2,Math.max(1,w-4),Math.max(1,h-4),shadow);rect(x+1,y+1,w-2,1,shine);}
// Chain wrap: the arrow stays readable in the centre; a small lock hangs at the bottom.
for(const y of [4,12,20,28]){link(5,y,true);link(38,y,true);}
for(const x of [7,15,23,31]){link(x,5);link(x,35);}
rect(18,30,13,15,ink);rect(21,28,7,3,edge);rect(19,30,3,6,edge);rect(27,30,3,6,edge);
rect(19,35,11,8,steel);rect(20,35,9,2,shine);rect(24,38,2,4,ink);
// Fixed mirror, slash. Chunky bevels are deliberately drawn on the same 48px grid.
function mirror(backslash){
 rect(5,7,38,35,ink);rect(7,5,34,39,ink);rect(8,8,32,32,shadow);rect(9,8,30,2,steel);rect(8,10,2,28,steel);
 for(const [x,y] of [[11,11],[34,11],[11,34],[34,34]]){rect(x-2,y-2,5,5,ink);rect(x-1,y-1,3,3,gold);pixel(x,y,shine);}
 for(let i=0;i<27;i++){const x=11+i,y=backslash?11+i:37-i;rect(x-2,y-2,5,5,ink);}
 for(let i=0;i<25;i++){const x=12+i,y=backslash?12+i:36-i;rect(x-1,y-1,3,3,blue);pixel(x,y,light);pixel(x,y-1,shine);}
 rect(21,22,7,5,light);rect(23,20,3,9,shine);
}
ox=48;mirror(false);ox=96;mirror(true);
// Stack rim and preview mount. Preview itself reuses the exact original rune sprites.
ox=144;rect(7,10,36,34,ink);rect(5,12,40,29,ink);rect(7,12,36,29,shadow);rect(7,39,36,2,steel);rect(41,14,2,25,steel);
rect(22,23,25,24,ink);rect(24,24,21,21,edge);rect(25,26,19,18,shadow);rect(25,25,19,1,shine);
const out=path.join(__dirname,'Source');fs.writeFileSync(path.join(out,'mechanic-pixels.json'),JSON.stringify([...pixels.values()]));
fs.writeFileSync(path.join(out,'mechanic-art.json'),JSON.stringify({width:W,height:H,tiles:['chain_wrap','mirror_slash','mirror_backslash','stack_mount']},null,2));
console.log('Prepared '+pixels.size+' exact-palette pixels for Aseprite, 4 tiles.');
