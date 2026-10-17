const fs=require('fs'),vm=require('vm'),{createCanvas,loadImage}=require('@napi-rs/canvas');
const html=fs.readFileSync('Tools/RuneBreak/Source/rune-break-layers.html','utf8');const scripts=[...html.matchAll(/<script[^>]*>([\s\S]*?)<\/script>/g)].map(x=>x[1]);for(const i of[0,1,2])vm.runInThisContext(scripts[i]);
(async()=>{const l=RuneLevels[0];let s=RuneEngine.initial(l);s=RuneEngine.step(l,s,{type:'board',id:'b2'}).state;s=RuneEngine.step(l,s,{type:'board',id:'b1'}).state;
const ref=createCanvas(480,584),c=ref.getContext('2d');RuneArt.frame(c,0,true);RuneArt.drawSlots(c,l,s,0,new Set(),null,0);const r=c.getImageData(0,0,480,584).data;
const img=await loadImage('Tools/RuneBreak/QA/WaitState.png'),a=createCanvas(img.width,img.height),ctx=a.getContext('2d');ctx.drawImage(img,0,0);const data=ctx.getImageData(0,0,img.width,img.height).data;
let n=0;for(let y=477;y<537;y++)for(let x=66;x<198;x++){let q=((y+165)*img.width+x+30)*4,p=(y*480+x)*4;if([0,1,2].some(i=>r[p+i]!==data[q+i]))n++;}
const report={pixels:132*60,differentPixels:n};fs.writeFileSync('Tools/RuneBreak/QA/wait-pixel-comparison.json',JSON.stringify(report,null,2));console.log(report);})();
