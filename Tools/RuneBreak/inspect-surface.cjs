const fs=require('fs'),path=require('path');
const html=fs.readFileSync('C:/Users/kimdn/Desktop/block-play-surface-buttons.html','utf8');
const scripts=[...html.matchAll(/<script[^>]*>([\s\S]*?)<\/script>/g)].map(m=>m[1]);
console.log(scripts.map((s,i)=>[i,s.slice(0,160)]));
const dir=path.join(__dirname,'Source/BlockPunch/Surface');fs.mkdirSync(dir,{recursive:true});
fs.copyFileSync('C:/Users/kimdn/Desktop/block-play-surface-buttons.html',path.join(dir,'block-play-surface-buttons.html'));
const entries=[...html.matchAll(/(--[\w-]+)\s*:\s*url\(["']?data:image\/png;base64,([^"')]+)["']?\)/g)];
for(const m of entries)fs.writeFileSync(path.join(dir,m[1].slice(2)+'.png'),Buffer.from(m[2],'base64'));
console.log(entries.map(m=>[m[1],m[2].length]));
fs.writeFileSync(path.join(dir,'scripts-index.json'),JSON.stringify(scripts.map((s,i)=>({i,start:s.slice(0,160)})),null,2));
const vm=require('vm');const ctx={window:{}};vm.createContext(ctx);
for(const s of scripts.filter(s=>s.includes('window.MaterialAssetData=')||s.startsWith('window.PlayTheme'))){try{vm.runInContext(s,ctx)}catch(e){console.log(e.message)}}
if(ctx.window.MaterialAssetData){for(const [name,data] of Object.entries(ctx.window.MaterialAssetData))fs.writeFileSync(path.join(dir,name+'.png'),Buffer.from(data.split(',')[1],'base64'));console.log('MaterialAssetData',Object.keys(ctx.window.MaterialAssetData));}
