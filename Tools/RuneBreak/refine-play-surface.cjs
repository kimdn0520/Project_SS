// Native material edit: reuse the original molded block pixels and preserve all original masters.
const fs=require('fs'),path=require('path'),{createCanvas,loadImage}=require('@napi-rs/canvas');
const source=path.join(__dirname,'Source/BlockPunch/Surface'),output=path.resolve(__dirname,'../../Assets/RuneBreak/Art/BlockPunch/Surface');
const manifest={};
function layer(w,h,draw){const c=createCanvas(w*3,h*3);const q=c.getContext('2d');q.scale(3,3);draw(q);return c;}
function save(name,w,h,layers){manifest[name]={w,h,layers:[]};const c=createCanvas(w*3,h*3),q=c.getContext('2d');for(let i=0;i<layers.length;i++){const [label,img]=layers[i],file=name+'-layer-'+i+'.png';fs.writeFileSync(path.join(source,file),img.toBuffer('image/png'));q.drawImage(img,0,0);manifest[name].layers.push({label,file});}fs.writeFileSync(path.join(output,name+'.png'),c.toBuffer('image/png'));}
function box(q,x,y,w,h,r,paint){q.beginPath();q.roundRect(x,y,w,h,r);q.fillStyle=paint;q.fill();}
function gradient(q,x,y,w,h,stops){const g=q.createLinearGradient(x,y,x+w,y+h);for(const [t,c]of stops)g.addColorStop(t,c);return g;}
(async()=>{
 for(const color of ['red','blue','gold','green']){
  const block=await loadImage(path.join(source,'stored_'+color+'-composite.png'));
  const sample=createCanvas(168,168),sq=sample.getContext('2d');sq.drawImage(block,0,0,168,168);
  const tint=y=>{const p=sq.getImageData(84,y,1,1).data;return '#'+[...p].slice(0,3).map(v=>v.toString(16).padStart(2,'0')).join('');};
  const paint={light:tint(19),mid:tint(72),low:tint(119),dark:tint(141),shadow:tint(151)};
  for(const side of ['N','E']){
   const w=side==='N'?352:44,h=side==='N'?44:352;
   const body=layer(w,h,q=>{
    // Bake a gently curved perimeter at the final dimensions; no stretched end caps.
    // Every color comes from the original block's material, and studs keep its actual pixels.
    box(q,7,12,w-14,h-14,5,'#13171555');
    box(q,8,10,w-16,h-14,4.5,paint.shadow);
    box(q,8,8,w-16,h-16,4.5,gradient(q,8,8,side==='E'?28:0,side==='N'?28:0,[[0,paint.light],[.18,paint.mid],[.74,paint.low],[1,paint.dark]]));
    box(q,10,10,w-20,h-20,3,gradient(q,10,10,side==='E'?24:0,side==='N'?24:0,[[0,paint.mid],[1,paint.low]]));
    q.beginPath();q.roundRect(8.7,8.7,w-17.4,h-17.4,4);q.strokeStyle=paint.light+'66';q.lineWidth=.7;q.stroke();
   });
   // Two separate circular studs, inset along the length, with the numeral between them.
   const studs=[.30,.70].map((at,i)=>['Round stud '+(i+1),layer(w,h,q=>{
    const cx=side==='N'?8+336*at:22,cy=side==='N'?22:8+336*at;
    q.save();q.beginPath();q.ellipse(cx,cy+1,11.2,12.2,0,0,Math.PI*2);q.clip();q.drawImage(block,24,18,48,56,cx-12,cy-12.5,24,28);q.restore();
   })]);
   save('gate_studs_'+color+'_'+side,w,h,[['Continuous original molded face',body],...studs]);
  }
 }
 const chassis=await loadImage(path.join(source,'depth-chassis.png')),storage=await loadImage(path.join(source,'storage-alignment-overlay.png'));
 const channels=layer(480,652,q=>{
  for(const [x,y,w,h] of [[72,60,336,28],[420,100,28,336],[32,100,28,336],[72,448,336,28]]){
   const vertical=h>w;
   box(q,x-1,y-1,w+2,h+2,7,'#d8d0bd55');
   box(q,x,y,w,h,6,gradient(q,x,y,vertical?w:0,vertical?0:h,[[0,'#55564f'],[.13,'#696b61'],[.82,'#898b7f'],[1,'#b8b6a6']]));
   box(q,x+2,y+2,w-4,h-4,4,gradient(q,x,y,vertical?w:0,vertical?0:h,[[0,'#686a61'],[.25,'#797b70'],[1,'#8c8e80']]));
  }
 });
 save('cabinet_refined',480,652,[['Original unmarked ivory chassis',layer(480,652,q=>q.drawImage(chassis,0,0,480,652))],['Five recessed storage pockets',layer(480,652,q=>q.drawImage(storage,0,0,480,652))],['Matching empty gate channels on all four sides',channels]]);
 const button=await loadImage(path.join(output,'button_plum.png')),gear=await loadImage(path.join(output,'gear.png'));
 save('settings_button',62,62,[['Original plum button',layer(62,62,q=>q.drawImage(button,0,0,62,62))],['Centered settings symbol',layer(62,62,q=>q.drawImage(gear,16,16.5,28,28))]]);
 let lua="local root=app.params['root']\nlocal base=root..'/Tools/RuneBreak/Source/BlockPunch/Surface/'\n";
 for(const [name,item]of Object.entries(manifest)){
  lua+=`do\n local sprite=Sprite(${item.w*3},${item.h*3},ColorMode.RGB)\n`;
  item.layers.forEach((l,i)=>lua+=` local layer=${i===0?'sprite.layers[1]':'sprite:newLayer()'}\n layer.name=${JSON.stringify(l.label)}\n sprite:newCel(layer,1,Image{fromFile=base..'${l.file}'},Point(0,0))\n`);
  lua+=` sprite:saveAs(base..'${name}.aseprite')\n sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/${name}.png')\n sprite:close()\nend\n`;
 }
 fs.writeFileSync(path.join(__dirname,'refine-play-surface.lua'),lua);fs.writeFileSync(path.join(source,'refined-manifest.json'),JSON.stringify(manifest,null,2));console.log('Prepared '+Object.keys(manifest).length+' layered Aseprite assets.');
})().catch(e=>{console.error(e);process.exitCode=1});
