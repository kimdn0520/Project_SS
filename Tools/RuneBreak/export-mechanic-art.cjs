const fs=require('fs'),path=require('path');const{loadImage,createCanvas}=require('@napi-rs/canvas');
(async()=>{
 const root=path.resolve(__dirname,'../..'),source=path.join(__dirname,'Source'),meta=JSON.parse(fs.readFileSync(path.join(source,'mechanic-art.json')));
 const sheet=await loadImage(path.join(source,'mechanics.png'));
 const canvas=createCanvas(meta.width,meta.height),ctx=canvas.getContext('2d');ctx.drawImage(sheet,0,0);
 const data=ctx.getImageData(0,0,meta.width,meta.height).data;let differences=0;
 const expected=new Map(JSON.parse(fs.readFileSync(path.join(source,'mechanic-pixels.json'))).map(p=>[p.y*meta.width+p.x,p]));
 for(let i=0;i<meta.width*meta.height;i++){const p=expected.get(i),rgba=p?[p.r,p.g,p.b,p.a]:[0,0,0,0];for(let c=0;c<4;c++)if(data[i*4+c]!==rgba[c])differences++;}
 if(differences)throw Error('Aseprite round trip differs: '+differences);
 meta.tiles.forEach((name,i)=>{const tile=createCanvas(48,48),c=tile.getContext('2d');c.imageSmoothingEnabled=false;c.drawImage(sheet,i*48,0,48,48,0,0,48,48);fs.writeFileSync(path.join(root,'Assets/RuneBreak/Art',name+'.png'),tile.toBuffer('image/png'));});
 fs.writeFileSync(path.join(__dirname,'QA/mechanic-art-roundtrip.json'),JSON.stringify({source:'Source/Mechanics.aseprite',tiles:meta.tiles,differentChannels:differences},null,2));
 console.log('4 Aseprite-authored tiles exported; round-trip different channels: '+differences);
})();
