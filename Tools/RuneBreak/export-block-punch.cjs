const fs=require('fs'),path=require('path'),vm=require('vm');
const {createCanvas,loadImage}=require('@napi-rs/canvas');
const source=path.join(__dirname,'Source/BlockPunch'),out=path.resolve(__dirname,'../../Assets/RuneBreak/Art/BlockPunch');
const colors=['red','blue','gold','green'],dirs=['N','E','S','W','gem'];
const html=fs.readFileSync(path.join(source,'block-punch.html'),'utf8');
const scripts=[...html.matchAll(/<script[^>]*>([\s\S]*?)<\/script>/g)].map(m=>m[1]);
fs.mkdirSync(out,{recursive:true});
global.window=global;global.MaterialImages={};
function save(name,w,h,draw){const c=createCanvas(w*3,h*3),ctx=c.getContext('2d');ctx.scale(3,3);draw(ctx);fs.writeFileSync(path.join(out,'punch_'+name+'.png'),c.toBuffer('image/png'));return c;}
(async()=>{
 for(const file of fs.readdirSync(source).filter(f=>f.endsWith('.png')))MaterialImages[file.slice(0,-4)]=await loadImage(path.join(source,file));
 for(const i of [0,1,2,4,5])vm.runInThisContext(scripts[i]);
 const A=RuneArt,E=RuneEngine,l=RuneLevels[0],s=E.initial(l);
 for(const color of colors){
  for(const d of dirs)save('rune_'+color+'_'+d,48,48,c=>A.crystal(c,color,24,22,40,d==='gem'?{}:{dir:d}));
  save('stored_'+color,48,48,c=>A.crystal(c,color,24,22,38));
  for(const side of ['N','E'])fs.copyFileSync(path.join(source,`long-gate-${color}-${side}.png`),path.join(out,`punch_gate_${color}_${side}.png`));
 }
 // The frame and storage tray are the prototype's original high-resolution masters.
 save('cabinet',480,584,c=>{
  c.drawImage(MaterialImages['frame-block'],0,0,480,584);
  // Draw neutral continuous rails; omit the prototype's storage plaque and labels.
  const fill=(x,y,w,h)=>{const g=c.createLinearGradient(x,y,x+w,y+h);g.addColorStop(0,'#06091e');g.addColorStop(.55,'#191c40');g.addColorStop(1,'#343b78');c.fillStyle=g;c.beginPath();c.roundRect(x,y,w,h,7);c.fill();};
  for(const side of ['N','E','S','W']){const r=A.gateRect(side);fill(r.x,r.y,r.w,r.h);}
  c.drawImage(MaterialImages['tray-block'],0,438,480,140);
 });
 function slot(c,full){c.fillStyle=full?'#b67a97':'#5260a0';c.beginPath();c.roundRect(0,0,60,60,8);c.fill();c.fillStyle='#1b1c34';c.beginPath();c.roundRect(1,1,58,58,7);c.fill();}
 save('slot',60,60,c=>slot(c,false));save('slot_full',60,60,c=>slot(c,true));
 for(const color of colors)for(const full of [false,true])save('wait_'+color+(full?'_full':''),60,60,c=>{slot(c,full);c.globalAlpha=.56;A.crystal(c,color,30,30,34);});
 save('shadow',48,16,c=>{const g=c.createRadialGradient(24,8,0,24,8,24);g.addColorStop(0,'#03081870');g.addColorStop(1,'#03081800');c.fillStyle=g;c.beginPath();c.ellipse(24,8,24,8,0,0,Math.PI*2);c.fill();});
 // Pixel comparison proves the Aseprite master round trip did not alter the source.
 const a=MaterialImages['blocks-master'],b=MaterialImages['blocks-roundtrip'];
 function bytes(img){const c=createCanvas(img.width,img.height);c.getContext('2d').drawImage(img,0,0);return c.getContext('2d').getImageData(0,0,img.width,img.height).data;}
 const aa=bytes(a),bb=bytes(b);let differences=0;for(let i=0;i<aa.length;i++)if(aa[i]!==bb[i])differences++;
 if(differences)throw Error('Aseprite master pixels changed: '+differences);
 const cases=[];const M=require('./Source/BlockPunch/rebound-oracle.cjs');
 for(const dir of ['N','E','S','W'])for(const reduced of [false,true])for(let slot=0;slot<5;slot++){
  const from={x:216,y:204},edge=dir==='N'?{x:216,y:59}:dir==='S'?{x:216,y:401}:dir==='W'?{x:69,y:204}:{x:411,y:204};
  const to={x:96+slot*72,y:502},m=M.create(from,edge,to,dir,reduced);
  for(let n=0;n<=80;n++){const time=m.duration*n/80;cases.push({dir,reduced,from,edge,to,time,duration:m.duration,position:M.sample(m,time),pose:M.pose(m,time)});}
 }
 fs.writeFileSync(path.join(source,'motion-samples.json'),JSON.stringify({cases}));
 fs.writeFileSync(path.join(__dirname,'QA/block-punch-art.json'),JSON.stringify({asepriteChannelDifferences:differences,exportedSprites:fs.readdirSync(out).filter(f=>f.endsWith('.png')).length,motionSamples:cases.length},null,2));
 console.log(JSON.stringify({pixelDifferences:differences,motionSamples:cases.length}));
})();
