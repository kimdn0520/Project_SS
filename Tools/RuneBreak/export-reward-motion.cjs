// Separate the existing Aseprite pile layers without repainting their coin pixels.
const fs=require('fs'),path=require('path'),{createCanvas,loadImage}=require('@napi-rs/canvas');
const base=path.join(__dirname,'Source/BlockPunch/Surface'),out=path.resolve(__dirname,'../../Assets/RuneBreak/Art/BlockPunch/Surface');
(async()=>{
 const manifest={};let lua="local root=app.params['root']\nlocal base=root..'/Tools/RuneBreak/Source/BlockPunch/Surface/'\n";
 function save(name,c,meta){const file=name+'-source.png';fs.writeFileSync(path.join(base,file),c.toBuffer('image/png'));manifest[name]={...meta,width:c.width,height:c.height};lua+=`do\n local s=Sprite(${c.width},${c.height},ColorMode.RGB)\n s.layers[1].name='Original coin material'\n s:newCel(s.layers[1],1,Image{fromFile=base..'${file}'},Point(0,0))\n s:saveAs(base..'${name}.aseprite')\n s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/${name}.png')\n s:close()\nend\n`;}
 for(let i=0;i<9;i++){
  const img=await loadImage(path.join(base,`reward_coins-layer-${i}.png`)),c=createCanvas(img.width,img.height),q=c.getContext('2d');q.drawImage(img,0,0);const pixels=q.getImageData(0,0,c.width,c.height).data;let x0=c.width,y0=c.height,x1=0,y1=0;
  for(let y=0;y<c.height;y++)for(let x=0;x<c.width;x++)if(pixels[(y*c.width+x)*4+3]){x0=Math.min(x0,x);x1=Math.max(x1,x);y0=Math.min(y0,y);y1=Math.max(y1,y);}
  const crop=createCanvas(x1-x0+1,y1-y0+1);crop.getContext('2d').drawImage(c,x0,y0,crop.width,crop.height,0,0,crop.width,crop.height);
  save(i===0?'reward_pile_shadow':`reward_piece_${i-1}`,crop,{x:x0/3,y:y0/3,w:crop.width/3,h:crop.height/3});
 }
 const c=createCanvas(72,72),q=c.getContext('2d');q.translate(36,36);const g=q.createRadialGradient(0,0,0,0,0,33);g.addColorStop(0,'#fffce9ff');g.addColorStop(.4,'#fff1b5aa');g.addColorStop(1,'#ffe58f00');q.fillStyle=g;q.beginPath();q.arc(0,0,33,0,Math.PI*2);q.fill();q.fillStyle='#fffce9';q.beginPath();q.moveTo(0,-28);q.quadraticCurveTo(3,-3,24,0);q.quadraticCurveTo(3,3,0,28);q.quadraticCurveTo(-3,3,-24,0);q.quadraticCurveTo(-3,-3,0,-28);q.fill();save('reward_glint',c,{});
 fs.writeFileSync(path.join(base,'reward-motion-manifest.json'),JSON.stringify(manifest,null,2));fs.writeFileSync(path.join(__dirname,'author-reward-motion.lua'),lua);console.log('Prepared 8 cropped pile pieces, shadow and glint for Aseprite.');
})().catch(e=>{console.error(e);process.exitCode=1});
