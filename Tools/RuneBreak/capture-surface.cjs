const fs=require('fs'),path=require('path');
const {chromium}=require('playwright');
const root=path.join(__dirname,'Source/BlockPunch/Surface');
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
 const page=await browser.newPage({viewport:{width:540,height:960},deviceScaleFactor:1});
 await page.goto('file:///'+path.join(root,'block-play-surface-buttons.html').replaceAll('\\','/'));
 await page.waitForTimeout(1200);await page.screenshot({path:path.join(__dirname,'QA/Surface-Reference-Play.png')});
 await page.evaluate(()=>{document.querySelector('#settingsModal').hidden=false});await page.waitForTimeout(400);
 await page.screenshot({path:path.join(__dirname,'QA/Surface-Reference-Settings.png')});
 await page.evaluate(()=>{document.querySelector('#settingsModal').hidden=true;document.querySelector('#result').hidden=false});await page.waitForTimeout(400);
 await page.screenshot({path:path.join(__dirname,'QA/Surface-Reference-Result.png')});
 const exported=await page.evaluate(()=>{
   const out={};const save=(name,w,h,draw)=>{const c=document.createElement('canvas');c.width=w*3;c.height=h*3;const q=c.getContext('2d');q.scale(3,3);draw(q);out[name]=c.toDataURL()};
   save('surface-cabinet',480,652,q=>{RuneArt.frame(q,0,true);for(const side of ['W','S'])RuneArt.drawGate(q,side,RuneDebug.level,RuneDebug.state,[],0,null,RuneDebug.state);});
   save('surface-slot',58,58,q=>{});
   return {images:out,materials:window.MaterialAssetData,layout:window.StorageLayout,globals:Object.keys(window).filter(k=>/Art|Material|Surface|Depth/.test(k))};
 });
 for(const [name,data]of Object.entries({...exported.materials,...exported.images}))fs.writeFileSync(path.join(root,name+'.png'),Buffer.from(data.split(',')[1],'base64'));
 console.log({layout:exported.layout,globals:exported.globals,materials:Object.keys(exported.materials)});
 await browser.close();
})().catch(e=>{console.error(e);process.exitCode=1});
