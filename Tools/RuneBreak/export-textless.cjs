// Derive text-free UI art from the original drawing functions. Galmuri TMP labels
// replace every inscription; the original HTML, PNGs and Aseprite files stay intact.
const fs=require('fs'),path=require('path'),vm=require('vm');
const {createCanvas}=require('@napi-rs/canvas');
const root=path.resolve(__dirname,'../..'),art=path.join(root,'Assets/RuneBreak/Art');
const html=fs.readFileSync(path.join(__dirname,'Source/rune-break-layers.html'),'utf8');
const scripts=[...html.matchAll(/<script[^>]*>([\s\S]*?)<\/script>/g)].map(m=>m[1]);
scripts[2]=require('./presentation.cjs')(scripts[2]);
const signature="function pixText(c,text,x,y,size=1,color='#f4ebce',align='left'){";
if(scripts[2].split(signature).length!==2)throw new Error('Original text function changed');
scripts[2]=scripts[2].replace(signature,signature+'if(globalThis.RUNE_TEXTLESS)return;');
for(const script of scripts.slice(0,3))vm.runInThisContext(script);
const A=RuneArt,E=RuneEngine,level=RuneLevels[0],reports=[];
function write(name,w,h,draw,allowed){
 const render=textless=>{globalThis.RUNE_TEXTLESS=textless;const c=createCanvas(w,h);draw(c.getContext('2d'));return c;};
 const baseline=render(false),output=render(true),a=baseline.getContext('2d').getImageData(0,0,w,h).data,b=output.getContext('2d').getImageData(0,0,w,h).data;
 let changed=0;
 for(let y=0;y<h;y++)for(let x=0;x<w;x++){const i=(y*w+x)*4;if(a[i]===b[i]&&a[i+1]===b[i+1]&&a[i+2]===b[i+2]&&a[i+3]===b[i+3])continue;changed++;if(!allowed.some(r=>x>=r[0]&&x<r[0]+r[2]&&y>=r[1]&&y<r[1]+r[3]))throw new Error(name+' changed non-text pixel '+x+','+y);}
 fs.writeFileSync(path.join(art,name+'_textless.png'),output.toBuffer('image/png'));reports.push({name,removedTextPixels:changed,nonTextDifferences:0});
}
for(let f=0;f<3;f++)write('cabinet_'+f,480,584,c=>{A.frame(c,f/9,false);A.drawGate(c,'S',level,E.initial(level),[],0,null);A.drawGate(c,'W',level,E.initial(level),[],0,null);A.drawSlots(c,level,E.initial(level),0,new Set(),null,0);},[[235,31,10,7],[235,419,10,7],[47,449,60,7],[400,449,40,7],[100,568,280,7],...Array.from({length:5},(_,i)=>[73+i*72,524,8,7])]);
for(const color of E.COLORS)for(const full of [false,true])write('wait_'+color+(full?'_full':''),60,60,c=>{const l={capacity:5,blocks:[{id:'test',color}],seals:{N:[],E:[]}},s=E.initial(l);s.slots=full?Array(5).fill('test'):['test',null,null,null,null];c.translate(-66,-477);A.drawSlots(c,l,s,full?.2:0,new Set(),null,0);},[[15,49,30,7]]);
fs.writeFileSync(path.join(__dirname,'QA/textless-art-report.json'),JSON.stringify({sprites:reports},null,2));
console.log(JSON.stringify({sprites:reports.length,nonTextDifferences:0}));
