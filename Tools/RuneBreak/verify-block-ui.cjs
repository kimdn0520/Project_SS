const fs=require('fs'),path=require('path');
const {createCanvas,loadImage}=require('@napi-rs/canvas');
async function pixels(file){const img=await loadImage(file),canvas=createCanvas(img.width,img.height),c=canvas.getContext('2d');c.drawImage(img,0,0);return c.getImageData(0,0,img.width,img.height).data;}
(async()=>{
 const folder=path.join(__dirname,'Source/BlockPunch/Lobby/Blocks'),manifest=JSON.parse(fs.readFileSync(path.join(folder,'layers.json'),'utf8')),report={sprites:0,asepriteChannelDifferences:0};
 for(const name of Object.keys(manifest)){
  const a=await pixels(path.resolve(__dirname,'../../Assets/RuneBreak/Art/BlockPunch/Lobby/Blocks',name+'.png')),b=await pixels(path.join(folder,name+'-roundtrip.png'));
  if(a.length!==b.length)throw Error('Dimension mismatch: '+name);let differences=0;for(let i=0;i<a.length;i++)if(a[i]!==b[i])differences++;
  if(differences)throw Error(name+': '+differences+' changed channels');report.sprites++;
 }
 fs.writeFileSync(path.join(__dirname,'QA/block-ui-art.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
})();
