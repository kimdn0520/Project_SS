// Extend the existing authored lobby icon system with glossy, layered currency art.
const fs=require('fs'),path=require('path');
const {createCanvas,Path2D}=require('@napi-rs/canvas');
const source=path.join(__dirname,'Source/BlockPunch/Lobby/Icons');
const output=path.resolve(__dirname,'../../Assets/RuneBreak/Art/BlockPunch/Lobby/Icons');
fs.mkdirSync(source,{recursive:true});fs.mkdirSync(output,{recursive:true});
function gradient(c,x,y,x2,y2,stops){const g=c.createLinearGradient(x,y,x2,y2);stops.forEach(([at,color])=>g.addColorStop(at,color));return g;}
function ellipse(c,x,y,rx,ry,color){c.fillStyle=color;c.beginPath();c.ellipse(x,y,rx,ry,0,0,Math.PI*2);c.fill();}
function star(c,x,y,r,fill){c.beginPath();for(let i=0;i<10;i++){const a=-Math.PI/2+i*Math.PI/5,s=i%2?r*.47:r;c.lineTo(x+Math.cos(a)*s,y+Math.sin(a)*s);}c.closePath();c.fillStyle=fill;c.fill();}
const heart=new Path2D('M24 42 C19 38 4 27 4 16 C4 5 16 1 24 11 C32 1 44 5 44 16 C44 27 29 38 24 42 Z');
const recipes={
 coin:[
  ['Shadow',c=>ellipse(c,24,27,21,20,'#03071970')],
  ['Gold edge',c=>{ellipse(c,24,25,20,20,gradient(c,6,5,37,46,[[0,'#f8b72a'],[.45,'#cf7708'],[1,'#653302']]));for(let i=0;i<13;i++){const a=i*Math.PI/12;c.strokeStyle=i%2?'#aa5806':'#ffe173';c.lineWidth=.75;c.beginPath();c.moveTo(24+Math.cos(a)*19,24+Math.sin(a)*19);c.lineTo(24+Math.cos(a)*19,27+Math.sin(a)*17);c.stroke();}}],
  ['Bevel',c=>{ellipse(c,24,22,20,20,gradient(c,9,3,39,41,[[0,'#fff8bc'],[.23,'#ffe265'],[.54,'#e99b08'],[.8,'#b66d02'],[1,'#f7c52b']]));ellipse(c,24,22,18.3,18.3,gradient(c,18,3,30,40,[[0,'#fff8b0'],[.24,'#ffd647'],[.65,'#f8b416'],[1,'#dd8903']]));}],
  ['Inset face',c=>{ellipse(c,24,22,15.8,15.8,'#b77606');ellipse(c,24,22.6,14.8,14.8,gradient(c,18,8,28,38,[[0,'#e7a309'],[.45,'#ffcd26'],[1,'#ffdd57']]));c.strokeStyle='#fff0a0';c.lineWidth=.9;c.beginPath();c.arc(24,22.3,16.2,.15,Math.PI*.93);c.stroke();}],
  ['Embossed star',c=>{star(c,24.4,22.7,10.3,'#a96404');star(c,23.9,21.1,10.1,gradient(c,18,11,28,30,[[0,'#fffac0'],[.4,'#ffe66e'],[1,'#efb529']]));}],
  ['Highlights',c=>{c.strokeStyle='#fffde0';c.lineWidth=1.4;c.lineCap='round';c.beginPath();c.arc(24,22,18.9,3.65,4.9);c.stroke();ellipse(c,10.1,10.6,1.5,1,'#ffffffc0');}]
 ],
 heart:[
  ['Shadow',c=>{c.save();c.translate(0,3);c.fillStyle='#03071970';c.fill(heart);c.restore();}],
  ['Ruby edge',c=>{c.save();c.translate(0,1.8);c.fillStyle=gradient(c,10,7,34,42,[[0,'#d92648'],[.65,'#9d1231'],[1,'#5c112a']]);c.fill(heart);c.restore();}],
  ['Bevel',c=>{c.fillStyle=gradient(c,8,4,34,42,[[0,'#ffbdc7'],[.3,'#ff667c'],[.68,'#d82044'],[1,'#a31035']]);c.fill(heart);}],
  ['Puffy face',c=>{c.save();c.translate(2.1,1.5);c.scale(.91,.92);const g=c.createRadialGradient(15,12,1,28,25,31);g.addColorStop(0,'#ff9aac');g.addColorStop(.29,'#ff4c6b');g.addColorStop(.68,'#ed234c');g.addColorStop(1,'#ae0d36');c.fillStyle=g;c.fill(heart);c.restore();}],
  ['Soft reflection',c=>{c.save();c.clip(heart);const g=c.createRadialGradient(11,10,.5,13,12,12);g.addColorStop(0,'#fff1f1bb');g.addColorStop(.48,'#ffcede45');g.addColorStop(1,'#ffffff00');ellipse(c,13,12,13,11,g);c.restore();}],
  ['Highlights',c=>{c.lineCap='round';c.strokeStyle='#ffe6e4';c.lineWidth=1.7;c.beginPath();c.moveTo(8.1,13);c.bezierCurveTo(9,8,14,7.3,17.5,9);c.stroke();c.strokeStyle='#ffb4c5';c.lineWidth=1.1;c.beginPath();c.moveTo(29,9);c.bezierCurveTo(33,5.6,38.6,8,40,12);c.stroke();c.strokeStyle='#fa7699';c.beginPath();c.moveTo(27,36);c.lineTo(31.2,32);c.stroke();}]
 ]
};
const manifest={};
for(const [name,layers] of Object.entries(recipes)){
 const combined=createCanvas(144,144),ctx=combined.getContext('2d');manifest[name]=[];
 layers.forEach(([label,draw],index)=>{const layer=createCanvas(144,144),c=layer.getContext('2d');c.scale(3,3);draw(c);const file=`${name}-${index}.png`;fs.writeFileSync(path.join(source,file),layer.toBuffer('image/png'));ctx.drawImage(layer,0,0);manifest[name].push({label,file});});
 // Initial preview; author-lobby-icons.lua exports the canonical Aseprite composite.
 fs.writeFileSync(path.join(output,`lobby_${name}.png`),combined.toBuffer('image/png'));
}
fs.writeFileSync(path.join(source,'layers.json'),JSON.stringify(manifest,null,2));
console.log('Exported layered glossy coin and heart icons at 144 × 144.');
