// Edit the tray as a separate native layer; the original HTML composite remains untouched.
const fs=require('fs'),path=require('path'),{createCanvas}=require('@napi-rs/canvas');
const root=path.resolve(__dirname,'../..'),base=path.join(__dirname,'Source/BlockPunch/Surface');
const canvas=createCanvas(1440,1956),q=canvas.getContext('2d');q.scale(3,3);
function shape(x,y,w,h,r,paint){q.beginPath();q.roundRect(x,y,w,h,r);q.fillStyle=paint;q.fill();}
function gradient(y,h,stops){const g=q.createLinearGradient(0,y,0,y+h);for(const [p,c] of stops)g.addColorStop(p,c);return g;}
q.save();q.beginPath();q.roundRect(73,548,334,63,13);q.clip();
shape(73,548,334,63,13,gradient(548,63,[[0,'#69685f'],[.12,'#a29d90'],[.85,'#b9b2a3'],[1,'#d4cebc']]));
for(let i=0;i<5;i++){
 const x=79+66*i,y=551;
 // The light lower rim and dark upper rim describe a recess instead of a raised button.
 shape(x,y,58,58,10,gradient(y,58,[[0,'#55564f'],[.28,'#77776d'],[.72,'#99968a'],[1,'#ddd7c4']]));
 shape(x+1.5,y+1.5,55,55,8.5,gradient(y,58,[[0,'#67685f'],[.13,'#7f8074'],[.6,'#929184'],[1,'#a5a293']]));
 q.save();q.beginPath();q.roundRect(x+2,y+2,54,54,8);q.clip();
 q.fillStyle=gradient(y+2,8,[[0,'#373b3038'],[1,'#373b3000']]);q.fillRect(x+2,y+2,54,8);q.restore();
}
q.restore();fs.writeFileSync(path.join(base,'storage-alignment-overlay.png'),canvas.toBuffer('image/png'));
const lua=`local root=app.params['root']
local base=root..'/Tools/RuneBreak/Source/BlockPunch/Surface/'
local sprite=Sprite(1440,1956,ColorMode.RGB)
sprite.layers[1].name='Original HTML cabinet - preserved'
sprite:newCel(sprite.layers[1],1,Image{fromFile=base..'cabinet-composite.png'},Point(0,0))
local layer=sprite:newLayer();layer.name='Five aligned recessed storage pockets'
sprite:newCel(layer,1,Image{fromFile=base..'storage-alignment-overlay.png'},Point(0,0))
sprite:saveAs(base..'cabinet-aligned.aseprite')
sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/cabinet_aligned.png')
sprite:close()
`;
fs.writeFileSync(path.join(__dirname,'align-storage.lua'),lua);console.log('Prepared the storage correction layer and Aseprite recipe.');
