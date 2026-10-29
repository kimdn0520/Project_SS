// Extend the existing native block/coin artwork. Aseprite owns the layered masters.
const fs=require('fs'),path=require('path'),{createCanvas,loadImage}=require('@napi-rs/canvas');
const base=path.join(__dirname,'Source/BlockPunch/Surface'),out=path.resolve(__dirname,'../../Assets/RuneBreak/Art/BlockPunch/Surface');
const manifest={};
const {drawCoin}=require('./coin-material.cjs');
function coinAt(q,x,y,w,h,angle=0){q.save();q.translate(x,y);q.rotate(angle);q.scale(w/48,h/48);drawCoin(q,false);q.restore();}
function layer(w,h,draw){const c=createCanvas(w*3,h*3),q=c.getContext('2d');q.scale(3,3);draw(q);return c;}
function save(name,w,h,layers){const c=createCanvas(w*3,h*3),q=c.getContext('2d');manifest[name]={w,h,layers:[]};layers.forEach(([label,img],i)=>{const file=name+'-layer-'+i+'.png';fs.writeFileSync(path.join(base,file),img.toBuffer('image/png'));q.drawImage(img,0,0);manifest[name].layers.push({label,file});});fs.writeFileSync(path.join(out,name+'.png'),c.toBuffer('image/png'));}
(async()=>{
 const chassis=await loadImage(path.join(base,'depth-chassis.png')),storage=await loadImage(path.join(base,'storage-alignment-overlay.png'));
 save('cabinet_clean',480,652,[['Original chassis without gate grooves',layer(480,652,q=>q.drawImage(chassis,0,0,480,652))],['Existing recessed storage',layer(480,652,q=>q.drawImage(storage,0,0,480,652))]]);
 const piles=[];
 piles.push(['Soft contact shadow',layer(320,250,q=>{const g=q.createRadialGradient(160,203,5,160,203,125);g.addColorStop(0,'#1b112d55');g.addColorStop(1,'#1b112d00');q.save();q.translate(0,144);q.scale(1,.3);q.fillStyle=g;q.fillRect(0,0,320,300);q.restore();})]);
 for(const [i,s]of [[90,141,5,79],[162,116,6,84],[232,143,4,79],[57,197,3,76],[130,197,5,88],[207,195,4,84],[267,203,3,73]].entries()){
  piles.push(['Milled coin stack '+(i+1),layer(320,250,q=>{for(let n=0;n<s[2];n++)coinAt(q,s[0]-s[3]/2+(n%2)*1.2,s[1]-n*9-s[3]*.33,s[3],s[3]*.66);})]);
 }
 piles.push(['Leaning foreground coins',layer(320,250,q=>{for(const [x,y,a]of [[93,205,-.32],[234,214,.32]]){q.save();q.translate(x,y);q.rotate(a);coinAt(q,-31,-35,62,70);q.restore();}})]);
 save('reward_coins',320,250,piles);
 save('reward_rays',440,440,[['Soft golden rays',layer(440,440,q=>{q.translate(220,220);const g=q.createRadialGradient(0,0,15,0,0,220);g.addColorStop(0,'#ffe58f66');g.addColorStop(.55,'#ffe58f26');g.addColorStop(1,'#ffe58f00');q.fillStyle=g;for(let i=0;i<16;i++){let a=i*Math.PI/8;q.beginPath();q.moveTo(0,0);q.arc(0,0,220,a,a+.10);q.closePath();q.fill();}})]]);
 save('reward_wallet',160,56,[['Glossy navy currency bar',layer(160,56,q=>{q.fillStyle='#080e2599';q.beginPath();q.roundRect(0,3,160,52,17);q.fill();const g=q.createLinearGradient(0,0,0,52);g.addColorStop(0,'#646286');g.addColorStop(.1,'#38384e');g.addColorStop(1,'#252638');q.fillStyle=g;q.beginPath();q.roundRect(1,1,158,49,16);q.fill();q.strokeStyle='#a8a1ca77';q.lineWidth=1;q.stroke();})]]);
 let lua="local root=app.params['root']\nlocal base=root..'/Tools/RuneBreak/Source/BlockPunch/Surface/'\n";
 for(const [name,m]of Object.entries(manifest)){lua+=`do\n local sprite=Sprite(${m.w*3},${m.h*3},ColorMode.RGB)\n`;m.layers.forEach((l,i)=>lua+=` local layer=${i===0?'sprite.layers[1]':'sprite:newLayer()'}\n layer.name=${JSON.stringify(l.label)}\n sprite:newCel(layer,1,Image{fromFile=base..'${l.file}'},Point(0,0))\n`);lua+=` sprite:saveAs(base..'${name}.aseprite')\n sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/${name}.png')\n sprite:close()\nend\n`;}
 fs.writeFileSync(path.join(base,'reward-manifest.json'),JSON.stringify(manifest,null,2));fs.writeFileSync(path.join(__dirname,'author-reward.lua'),lua);console.log('Prepared four layered reward/board assets.');
})().catch(e=>{console.error(e);process.exitCode=1});
