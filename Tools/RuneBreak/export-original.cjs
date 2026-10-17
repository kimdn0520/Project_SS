// Runs the supplied prototype's drawing functions with explicit product UI overrides.
const fs=require('fs'), path=require('path'), vm=require('vm');
const {createCanvas}=require('@napi-rs/canvas');
const root=path.resolve(__dirname,'../..');
const source=process.argv[2]||path.join(__dirname,'Source/rune-break-layers.html');
const html=fs.readFileSync(source,'utf8');
const scripts=[...html.matchAll(/<script[^>]*>([\s\S]*?)<\/script>/g)].map(x=>x[1]);
scripts[2]=require('./presentation.cjs')(scripts[2]);
for(const i of [0,1,2])vm.runInThisContext(scripts[i]);
const A=RuneArt,E=RuneEngine,levels=RuneLevels;
// Optional external catalog supports adding levels without changing rule/view code.
if(process.argv[3])levels.splice(0,levels.length,...JSON.parse(fs.readFileSync(process.argv[3],'utf8')).levels);
const art=path.join(root,'Assets/RuneBreak/Art'),data=path.join(root,'Assets/RuneBreak/Data');
fs.mkdirSync(art,{recursive:true});fs.mkdirSync(data,{recursive:true});
fs.mkdirSync(path.join(__dirname,'Source'),{recursive:true});
if(path.resolve(source)!==path.join(__dirname,'Source/rune-break-layers.html'))fs.copyFileSync(source,path.join(__dirname,'Source/rune-break-layers.html'));
fs.writeFileSync(path.join(data,'levels.json'),JSON.stringify({levels},null,2));
const manifest=[];
function png(name,w,h,draw){const c=createCanvas(w,h),ctx=c.getContext('2d');ctx.imageSmoothingEnabled=false;draw(ctx);const file=path.join(art,name+'.png'),bytes=c.toBuffer('image/png');if(!fs.existsSync(file)||!fs.readFileSync(file).equals(bytes))fs.writeFileSync(file,bytes);manifest.push(name);return c;}
for(let f=0;f<3;f++)png('cabinet_'+f,480,584,c=>{A.frame(c,f/9,false);A.drawGate(c,'S',levels[0],E.initial(levels[0]),[],0,null);A.drawGate(c,'W',levels[0],E.initial(levels[0]),[],0,null);A.drawSlots(c,levels[0],E.initial(levels[0]),0,new Set(),null,0);});
for(const col of E.COLORS)for(const d of ['N','E','S','W','gem'])png('rune_'+col+'_'+d,48,48,c=>A.crystal(c,col,24,22,40,d==='gem'?{}:{dir:d}));
for(const col of E.COLORS)png('stored_'+col,48,48,c=>A.crystal(c,col,24,22,38));
png('slot_full',60,60,c=>{A.box(c,0,0,60,60,'#243c41','#b4745e',4);A.rect(c,4,4,52,2,'#7d9586');A.rect(c,4,5,2,50,'#455758');});
png('sparkle',5,5,c=>A.diamond(c,2,2,2,'#ffffff'));
for(const col of E.COLORS)for(const full of [false,true])png('wait_'+col+(full?'_full':''),60,60,c=>{const l={capacity:5,blocks:[{id:'test',color:col}],seals:{N:[],E:[]}},s=E.initial(l);s.slots=full?Array(5).fill('test'):['test',null,null,null,null];c.translate(-66,-477);A.drawSlots(c,l,s,full?.2:0,new Set(),null,0);});
png('white',1,1,c=>A.rect(c,0,0,1,1,'white'));
png('lock',14,18,c=>{A.box(c,0,0,14,18,'#25373b','#15272e',2);A.rect(c,5,2,5,2,'#a3afa0');A.rect(c,3,4,2,4,'#a3afa0');A.rect(c,10,4,2,4,'#a3afa0');A.rect(c,2,8,11,7,'#a3afa0');A.rect(c,6,10,2,3,'#25373b');});
png('slot',60,60,c=>{A.box(c,0,0,60,60,'#243c41','#121f28',4);A.rect(c,4,4,52,2,'#7d9586');A.rect(c,4,5,2,50,'#455758');});
// Gate variants preserve every possible impact hole, including non-sequential direct hits.
const gates=[];
levels.forEach((l,li)=>{for(const side of E.SIDES){let spent=0;for(let layer=0;layer<l.seals[side].length;layer++){const max=l.seals[side][layer].hp;for(let mask=0;mask<(1<<max)-1;mask++){const holes=Array.from({length:max},(_,i)=>i).filter(i=>mask&(1<<i));const s=E.initial(l);s.spent[side]=spent+holes.length;const name=`gate_${li}_${side}_${layer}_${mask}`;const r=side==='N'?{x:90,y:24,w:300,h:52}:{x:394,y:78,w:60,h:300};png(name,r.w,r.h,c=>{c.translate(-r.x,-r.y);A.drawGate(c,side,l,s,holes,0,null,s);});gates.push({name,level:li,side,layer,mask});}spent+=max;}const s=E.initial(l);s.spent[side]=E.total(l,side);const r=side==='N'?{x:90,y:24,w:300,h:52}:{x:394,y:78,w:60,h:300};const name=`gate_${li}_${side}_done`;png(name,r.w,r.h,c=>{c.translate(-r.x,-r.y);A.drawGate(c,side,l,s,[],0,null,s);});gates.push({name,level:li,side,layer:l.seals[side].length,mask:0});}});
fs.writeFileSync(path.join(data,'gates.json'),JSON.stringify({gates}));
// Pixel glyph definitions drive the Unity mesh text; no font approximation in the cabinet.
const glyphMatch=scripts[2].match(/const glyphs=(\{[\s\S]*?\n\});/);
const glyphs=vm.runInNewContext('('+glyphMatch[1]+')');
fs.writeFileSync(path.join(data,'glyphs.json'),JSON.stringify({glyphs:Object.entries(glyphs).map(([key,rows])=>({key,rows}))}));
const refs=[];levels.forEach((l,i)=>{let s=E.initial(l);const c=png('reference_'+i,480,584,c=>{A.frame(c,0,false);for(const side of ['N','E','S','W'])A.drawGate(c,side,l,s,[],0,null,s);for(const b of l.blocks){const p=A.center(b);A.crystal(c,b.color,p.x,p.y,40,{dir:b.dir});}A.drawSlots(c,l,s,0,new Set(),null,0);});refs.push(c);});
const report=levels.map(l=>{E.validate(l);const route=E.solve(l);let state=E.initial(l);const traces=[];for(const a of route){const out=E.step(l,state,a);state=out.state;traces.push({action:a,state});}return {id:l.id,route,traces,capacity4:E.analyze(l,E.initial(l),{capacity:4}).status};});
fs.writeFileSync(path.join(data,'oracle.json'),JSON.stringify({reports:report}));
const sheet=createCanvas(240,192),ctx=sheet.getContext('2d');let k=0;for(const col of E.COLORS)for(const d of ['N','E','S','W','gem']){A.crystal(ctx,col,(k%5)*48+24,Math.floor(k/5)*48+22,40,d==='gem'?{}:{dir:d});k++;}
fs.writeFileSync(path.join(__dirname,'Source/runes.png'),sheet.toBuffer('image/png'));
fs.writeFileSync(path.join(__dirname,'Source/manifest.json'),JSON.stringify({source,manifest,gates:gates.length,levels:report.map(x=>({id:x.id,moves:x.route.length,capacity4:x.capacity4}))},null,2));
console.log(JSON.stringify({sprites:manifest.length,gates:gates.length,levels:report.map(x=>({id:x.id,moves:x.route.length,capacity4:x.capacity4}))}));
