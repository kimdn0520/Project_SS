// Layered additions to the existing Block Punch UI recipe; no source blocks are altered.
const fs=require('fs'),path=require('path');
const {createCanvas,loadImage}=require('@napi-rs/canvas');
const source=path.join(__dirname,'Source/BlockPunch/Lobby/Blocks');
const output=path.resolve(__dirname,'../../Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks');
fs.mkdirSync(source,{recursive:true});fs.mkdirSync(output,{recursive:true});
const palettes={gold:['#ffef93','#ffcf16','#edaa00','#965200','#51300c'],violet:['#b4a5ff','#7b61e8','#5038b1','#2b2163','#171331'],blue:['#9ceaff','#08b9f1','#007bbd','#075177','#072a49'],green:['#cbff8b','#6cda25','#36a514','#246714','#163a18'],slate:['#a0a8d4','#545d87','#333b61','#232944','#15192f']};
function grad(c,x,y,x2,y2,stops){const g=c.createLinearGradient(x,y,x2,y2);stops.forEach(([at,color])=>g.addColorStop(at,color));return g;}
function box(c,x,y,w,h,r,fill){c.fillStyle=fill;c.beginPath();c.roundRect(x,y,w,h,r);c.fill();}
function disc(c,x,y,r,fill){c.fillStyle=fill;c.beginPath();c.arc(x,y,r,0,Math.PI*2);c.fill();}
function stud(c,x,y,r,p){disc(c,x,y+1.8,r+1.2,'#02041480');disc(c,x,y,r,grad(c,x-r,y-r,x+r,y+r,[[0,p[0]],[.4,p[1]],[1,p[4]]]));disc(c,x,y-.4,r-1.5,grad(c,x,y-r,x,y+r,[[0,p[0]],[.2,p[1]],[1,p[2]]]));c.strokeStyle=p[0];c.lineWidth=.8;c.lineCap='round';c.beginPath();c.arc(x,y-.4,r-2.7,3.6,5.2);c.stroke();}
const manifest={};
const singleButtons=process.argv.includes('--single-buttons');
const singleNames=new Set(['molded_gold_340_76','molded_gold_300_82','molded_green_210_78','molded_amber_210_78']);
function sprite(name,w,h,layers,border=0){if(singleButtons&&!singleNames.has(name))return;const combined=createCanvas(w*3,h*3),ctx=combined.getContext('2d');manifest[name]={w,h,border,layers:[]};layers.forEach(([label,draw],i)=>{const canvas=createCanvas(w*3,h*3),c=canvas.getContext('2d');c.scale(3,3);draw(c);const file=name+'-'+i+'.png';fs.writeFileSync(path.join(source,file),canvas.toBuffer('image/png'));ctx.drawImage(canvas,0,0);manifest[name].layers.push({label,file});});fs.writeFileSync(path.join(output,name+'.png'),combined.toBuffer('image/png'));}
(async()=>{
sprite('popup_row',404,76,[
 ['Inset edge',c=>box(c,0,0,404,76,17,grad(c,0,0,0,76,[[0,'#0e1127'],[.8,'#292c4c'],[1,'#5b567e']] ))],
 ['Inset face',c=>{box(c,2,2,400,71,15,grad(c,0,0,0,74,[[0,'#1a1c34'],[1,'#25263f']]));box(c,13,3,378,1,.5,'#080d2355');}]
]);
for(const [name,p] of [['on',palettes.green],['off',palettes.slate]])sprite('toggle_'+name,48,48,[
 ['Thickness',c=>{box(c,2,5,44,42,13,'#080b2190');box(c,3,3,42,42,12,p[4]);}],
 ['Beveled switch',c=>{box(c,3,1,42,40,12,grad(c,0,0,48,43,[[0,p[0]],[.4,p[1]],[1,p[3]]]));box(c,6,4,36,33,9,grad(c,0,4,0,37,[[0,p[1]],[1,p[2]]]));}],
 ['Raised symbol',c=>{c.lineCap=c.lineJoin='round';c.lineWidth=3.5;c.strokeStyle=name==='on'?'#214e14':'#252a48';c.beginPath();if(name==='on'){c.moveTo(14,22);c.lineTo(21,29);c.lineTo(34,15);}else{c.moveTo(16,23);c.lineTo(32,23);}c.stroke();c.translate(0,-1.2);c.strokeStyle=name==='on'?'#f3ffcf':'#bcc4df';c.stroke();}]
]);
// Sample the actual supplied molded blocks, retaining their curved bevels,
// specular highlights and contact shadows. Bake each control's real dimensions
// so the circular studs are never stretched by a runtime nine-slice.
const gold=await loadImage(path.join(__dirname,'Source/BlockPunch/stored-gold.png'));
const green=await loadImage(path.join(__dirname,'Source/BlockPunch/stored-green.png'));
const amber=createCanvas(168,168),ac=amber.getContext('2d');ac.drawImage(gold,0,0);
const amberPixels=ac.getImageData(0,0,168,168);
for(let i=0;i<amberPixels.data.length;i+=4){amberPixels.data[i+1]=Math.round(amberPixels.data[i+1]*.87);amberPixels.data[i+2]=Math.round(amberPixels.data[i+2]*.88);}ac.putImageData(amberPixels,0,0);
const blue=await loadImage(path.join(__dirname,'Source/BlockPunch/stored-blue.png'));
const violet=createCanvas(168,168),vc=violet.getContext('2d');vc.drawImage(blue,0,0);
const pixels=vc.getImageData(0,0,168,168);
for(let i=0;i<pixels.data.length;i+=4){
 const r=pixels.data[i]/255,g=pixels.data[i+1]/255,b=pixels.data[i+2]/255,max=Math.max(r,g,b),min=Math.min(r,g,b),d=max-min,l=(max+min)/2;
 if(d<.001)continue;const s=d/(1-Math.abs(2*l-1))*.58;let h=max===r?((g-b)/d+6)%6:max===g?(b-r)/d+2:(r-g)/d+4;h=(h+.90)%6;
 const chroma=(1-Math.abs(2*l-1))*s,x=chroma*(1-Math.abs(h%2-1)),m=l-chroma/2;
 const rgb=h<1?[chroma,x,0]:h<2?[x,chroma,0]:h<3?[0,chroma,x]:h<4?[0,x,chroma]:h<5?[x,0,chroma]:[chroma,0,x];
 for(let k=0;k<3;k++)pixels.data[i+k]=Math.round((rgb[k]+m)*255);
}vc.putImageData(pixels,0,0);
function molded(c,image,w,h,scale=h/168){
 const snap=value=>Math.round(value*3)/3;
 const src=[0,80,88,168],xx=[0,snap(80*scale),snap(w-80*scale),w],yy=[0,snap(80*scale),snap(h-80*scale),h];
 for(let y=0;y<3;y++)for(let x=0;x<3;x++)c.drawImage(image,src[x],src[y],src[x+1]-src[x],src[y+1]-src[y],xx[x],yy[y],xx[x+1]-xx[x],yy[y+1]-yy[y]);
}
for(const [skin,w,h] of [['gold',372,88],['gold',260,60],['gold',194,60],['gold',400,64],['violet',194,60],['violet',280,54],['violet',252,68]]){
 const image=skin==='gold'?gold:violet;
 sprite(`molded_${skin}_${w}_${h}`,w,h,[['Original molded block surface',c=>molded(c,image,w,h)]]);
 // The base is the same curved outline in a darker material, visible below the face.
 sprite(`foundation_${skin}_${w}_${h}`,w,h+12,[['Cast block thickness',c=>{molded(c,image,w,h+12,h/168);c.globalCompositeOperation='source-atop';c.fillStyle='#10071d80';c.fillRect(0,0,w,h+12);}]]);
}
// Single-piece controls reuse the actual block pixels at each final aspect ratio.
// Their baked bevel supplies all thickness; there is no second foundation image.
for(const [skin,w,h] of [['gold',340,76],['gold',300,82],['green',210,78],['amber',210,78]])
 sprite(`molded_${skin}_${w}_${h}`,w,h,[['Original molded block surface',c=>molded(c,skin==='green'?green:skin==='amber'?amber:gold,w,h)]]);
for(const h of [400,468])sprite('molded_panel_'+h,460,h,[
 ['Original beveled block frame',c=>molded(c,violet,460,h,.65)],
 ['Recessed interior',c=>{c.save();c.shadowColor='#050619';c.shadowBlur=9;c.shadowOffsetY=1;box(c,24,24,412,h-55,15,grad(c,0,24,0,h-31,[[0,'#343159'],[.4,'#282642'],[1,'#1e2037']]));c.restore();}],
 ['Four original round studs',c=>{for(const sx of [48,120])for(const sy of [43,113]){const dx=sx<80?sx*.65:460-(168-sx)*.65,dy=sy<80?sy*.65:h-(168-sy)*.65;c.save();c.beginPath();c.ellipse(dx,dy+1,15,17,0,0,Math.PI*2);c.clip();c.drawImage(violet,sx-24,sy-25,48,52,dx-24*.65,dy-25*.65,48*.65,52*.65);c.restore();}}]
]);
fs.writeFileSync(path.join(source,'layers.json'),JSON.stringify(singleButtons?{...JSON.parse(fs.readFileSync(path.join(source,'layers.json'),'utf8')),...manifest}:manifest,null,2));
// Aseprite creates the editable master and owns the final layered composite.
let lua="local root=app.params['root']\nlocal base=root..'/Tools/RuneBreak/Source/BlockPunch/Lobby/Blocks/'\n";
for(const [name,item] of Object.entries(manifest)){
 lua+=`do\n local sprite=Sprite(${item.w*3},${item.h*3},ColorMode.RGB)\n`;
 item.layers.forEach((layer,i)=>lua+=` local layer=${i===0?'sprite.layers[1]':'sprite:newLayer()'}\n layer.name=${JSON.stringify(layer.label)}\n sprite:newCel(layer,1,Image{fromFile=base..'${layer.file}'},Point(0,0))\n`);
 lua+=` sprite:saveAs(base..'${name}.aseprite')\n sprite:saveCopyAs(base..'${name}-roundtrip.png')\n sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks/${name}.png')\n sprite:close()\nend\n`;
}
fs.writeFileSync(path.join(__dirname,singleButtons?'author-single-buttons.lua':'author-block-ui.lua'),lua);console.log('Authored '+Object.keys(manifest).length+' layered block UI sprites.');
})();
