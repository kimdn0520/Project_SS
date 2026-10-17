// Read-only pixel comparison against the HTML drawing with the shipped presentation overrides.
const fs=require('fs');const{loadImage,createCanvas}=require('@napi-rs/canvas');
(async()=>{const read=async p=>{const i=await loadImage(p),c=createCanvas(i.width,i.height),x=c.getContext('2d');x.drawImage(i,0,0);return{width:i.width,height:i.height,data:x.getImageData(0,0,i.width,i.height).data};};
const actual=await read('Tools/RuneBreak/QA/PlayPage-540x960.png'),original=await read('Assets/RuneBreak/Art/reference_5.png');
const boxes={board:[96,84,288,288],north:[90,24,300,52],east:[394,78,60,300],recast:[24,465,432,86]};const report={};
for(const[name,[x,y,w,h]]of Object.entries(boxes)){let mismatch=0,maxChannelDelta=0;for(let j=y;j<y+h;j++)for(let i=x;i<x+w;i++){let a=((j+165)*actual.width+(i+30))*4,b=(j*original.width+i)*4;let different=false;for(let c=0;c<3;c++){const d=Math.abs(actual.data[a+c]-original.data[b+c]);maxChannelDelta=Math.max(maxChannelDelta,d);if(d>0)different=true;}if(different)mismatch++;}report[name]={pixels:w*h,differentPixels:mismatch,maxChannelDelta};}
fs.writeFileSync('Tools/RuneBreak/QA/pixel-comparison.json',JSON.stringify(report,null,2));console.log(report);})();
