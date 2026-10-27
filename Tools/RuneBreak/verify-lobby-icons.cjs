const fs=require('fs'),path=require('path');
const {createCanvas,loadImage}=require('@napi-rs/canvas');
async function pixels(file){const image=await loadImage(file),canvas=createCanvas(image.width,image.height);const c=canvas.getContext('2d');c.drawImage(image,0,0);return c.getImageData(0,0,image.width,image.height).data;}
(async()=>{
 const report={};
 for(const name of ['coin','heart']){
  const original=await pixels(path.resolve(__dirname,`../../Assets/RuneBreak/Art/BlockPunch/Lobby/Icons/lobby_${name}.png`));
  const roundtrip=await pixels(path.join(__dirname,`Source/BlockPunch/Lobby/Icons/${name}-roundtrip.png`));
  if(original.length!==roundtrip.length)throw Error(name+': size mismatch');
  let differences=0;for(let i=0;i<original.length;i++)if(original[i]!==roundtrip[i])differences++;
  report[name]={width:144,height:144,layers:6,channelDifferences:differences};
  if(differences)throw Error(name+': Aseprite pixels changed ('+differences+')');
 }
 fs.writeFileSync(path.join(__dirname,'QA/lobby-currency-art.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
})();
