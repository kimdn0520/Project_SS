// Preserve the user's self-contained prototype and its PNG masters, without synthesis.
const fs=require('fs'),path=require('path'),vm=require('vm');
const {createCanvas,loadImage}=require('@napi-rs/canvas');
const dir=path.join(__dirname,'Source/BlockPunch');
const html=fs.readFileSync(path.join(dir,'block-punch.html'),'utf8');
const scripts=[...html.matchAll(/<script[^>]*>([\s\S]*?)<\/script>/g)].map(m=>m[1]);
const assets=JSON.parse(scripts[3].match(/window.MaterialAssetData=(\{.*?\});/s)[1]);
const colors=['red','blue','gold','green'];
(async()=>{
 const sheet=createCanvas(840,672),ctx=sheet.getContext('2d');
 for(let row=0;row<4;row++)for(let col=0;col<5;col++){
  const color=colors[row],key=col<4?`engraved-${color}-${['N','E','S','W'][col]}`:`stored-${color}`;
  const bytes=Buffer.from(assets[key].split(',')[1],'base64');fs.writeFileSync(path.join(dir,key+'.png'),bytes);
  ctx.drawImage(await loadImage(bytes),col*168,row*168);
 }
 fs.writeFileSync(path.join(dir,'blocks-master.png'),sheet.toBuffer('image/png'));
 for(const [key,data]of Object.entries(assets))if(/^(gate-bar-|frame-block|tray-block)/.test(key))fs.writeFileSync(path.join(dir,key+'.png'),Buffer.from(data.split(',')[1],'base64'));
 fs.writeFileSync(path.join(dir,'rebound-oracle.cjs'),scripts[7]);
 console.log('Extracted 20 exact block masters, 8 gates, frame, tray, and pure motion oracle.');
})();
