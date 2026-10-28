// Preserve the molded lighting and alpha; remap only the neutral material palette.
const fs=require('fs'),path=require('path'),{createCanvas,loadImage}=require('@napi-rs/canvas');
const base=path.join(__dirname,'Source/BlockPunch/Surface'),out=path.resolve(__dirname,'../../Assets/RuneBreak/Art/BlockPunch/Surface');
(async()=>{
 const image=await loadImage(path.join(out,'cabinet_clean.png')),c=createCanvas(image.width,image.height),q=c.getContext('2d');q.drawImage(image,0,0);const pixels=q.getImageData(0,0,c.width,c.height);
 const stops=[[0,'171625'],[.25,'393747'],[.52,'777487'],[.65,'9692a3'],[.85,'cecada'],[1,'f1edf9']].map(([v,h])=>[v,[0,2,4].map(i=>parseInt(h.slice(i,i+2),16))]);
 for(let i=0;i<pixels.data.length;i+=4){const p=pixels.data;if(!p[i+3])continue;const l=(p[i]*.2126+p[i+1]*.7152+p[i+2]*.0722)/255;let n=1;while(n<stops.length-1&&l>stops[n][0])n++;const a=stops[n-1],b=stops[n],t=(l-a[0])/(b[0]-a[0]);for(let k=0;k<3;k++)p[i+k]=Math.round(a[1][k]+(b[1][k]-a[1][k])*t);}
 q.putImageData(pixels,0,0);fs.writeFileSync(path.join(base,'cabinet_lavender-source.png'),c.toBuffer('image/png'));
 fs.writeFileSync(path.join(base,'board-palette-manifest.json'),JSON.stringify({cabinet_lavender:{source:'cabinet_clean.png',width:c.width,height:c.height,palette:stops}},null,2));
 fs.writeFileSync(path.join(__dirname,'author-board-palette.lua'),`local root=app.params['root']\nlocal base=root..'/Tools/RuneBreak/Source/BlockPunch/Surface/'\nlocal s=Sprite(${c.width},${c.height},ColorMode.RGB)\ns.layers[1].name='Lavender gray molded material'\ns:newCel(s.layers[1],1,Image{fromFile=base..'cabinet_lavender-source.png'},Point(0,0))\ns:saveAs(base..'cabinet_lavender.aseprite')\ns:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/cabinet_lavender.png')\ns:close()\n`);console.log('Lavender material prepared; original ivory master retained.');
})().catch(e=>{console.error(e);process.exitCode=1});
