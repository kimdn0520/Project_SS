// Extract the supplied HTML artwork verbatim; bake UI dimensions before Unity import.
const fs=require('fs'),path=require('path');
const {createCanvas,loadImage}=require('@napi-rs/canvas');
const {chromium}=require('playwright');
const source=path.join(__dirname,'Source/BlockPunch/Surface');
const output=path.resolve(__dirname,'../../Assets/RuneBreak/Art/BlockPunch/Surface');
fs.mkdirSync(output,{recursive:true});
const manifest={};
async function asset(name,origin,w,h,borders){
 const img=await loadImage(path.join(source,origin+'.png'));
 const c=createCanvas(w*3,h*3),q=c.getContext('2d');
 if(borders){const [left,top,right,bottom]=borders;const xx=[0,left,w*3-right,w*3],yy=[0,top,h*3-bottom,h*3],sx=[0,left,img.width-right,img.width],sy=[0,top,img.height-bottom,img.height];for(let y=0;y<3;y++)for(let x=0;x<3;x++)q.drawImage(img,sx[x],sy[y],sx[x+1]-sx[x],sy[y+1]-sy[y],xx[x],yy[y],xx[x+1]-xx[x],yy[y+1]-yy[y]);}
 else q.drawImage(img,0,0,c.width,c.height);
 const file=name+'.png';fs.writeFileSync(path.join(output,file),c.toBuffer('image/png'));fs.writeFileSync(path.join(source,name+'-composite.png'),c.toBuffer('image/png'));
 manifest[name]={w,h,source:origin,layers:[{label:'Original HTML material',file:name+'-composite.png'}]};
}
(async()=>{
 await asset('cabinet','surface-cabinet',480,652);
 for(const color of ['red','blue','gold','green']){
  for(const dir of ['N','E','S','W'])await asset('rune_'+color+'_'+dir,'engraved-'+color+'-'+dir,56,56);
  await asset('stored_'+color,'stored-'+color,56,56);
  for(const side of ['N','E'])await asset('gate_'+color+'_'+side,'gate-bar-'+color+'-'+side,side==='N'?352:44,side==='N'?44:352,side==='N'?[54,0,54,0]:[0,54,0,54]);
 }
 await asset('settings','ivory-settings',440,452,[170,280,170,290]);
 await asset('result','clear-shell',440,350,[150,160,150,290]);
 await asset('close','action-continue',340,76,[60,0,60,0]);
 await asset('next','clear-continue',230,70,[60,0,60,0]);
 await asset('home','clear-retry',140,70,[60,0,60,0]);
 await asset('retry','clear-retry',230,70,[60,0,60,0]);
 for(const color of ['plum','blue','aqua'])await asset('button_'+color,'button-'+color,80,80);
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
 const page=await browser.newPage({viewport:{width:540,height:960},deviceScaleFactor:3});
 await page.goto('file:///'+path.join(source,'block-play-surface-buttons.html').replaceAll('\\','/'));await page.waitForTimeout(500);
 // Capture CSS-only controls from the actual reference. Text remains a native TMP font in Unity.
 const css=await page.evaluate(()=>[...document.querySelectorAll('style')].map(x=>x.textContent).join('\n'));
 async function dom(name,w,h,body,extra=''){
  await page.setContent('<html data-play-theme="ivory"><head><style>'+css+'\nhtml,body{margin:0!important;padding:0!important;background:transparent!important;display:block!important;width:100%;height:100%;}*{animation:none!important;transition:none!important}#asset{position:absolute;left:0;top:0;width:'+w+'px;height:'+h+'px;}'+extra+'</style></head><body><div id="asset">'+body+'</div></body></html>');
  await page.locator('#asset').screenshot({path:path.join(output,name+'.png'),omitBackground:true});
  fs.copyFileSync(path.join(output,name+'.png'),path.join(source,name+'-composite.png'));manifest[name]={w,h,source:'HTML CSS',layers:[{label:'Original CSS surface',file:name+'-composite.png'}]};
 }
 await dom('row',368,86,'<div class="settings"><div class="setting-line"></div></div>','.setting-line{position:absolute;inset:3px 2px 7px;min-height:0!important}');
 for(const on of [false,true])await dom('switch_'+(on?'on':'off'),92,46,'<label class="setting-line" style="all:initial"><input type="checkbox" '+(on?'checked':'')+'><span class="switch"><i class="switch-knob"></i></span></label>','.switch{position:absolute;left:4px;top:3px}input{display:none}');
 await dom('backdrop',540,960,'<div class="scene"></div>','.scene{width:540px!important;height:960px!important;border:0!important;border-radius:0!important;box-shadow:none!important}');
 // Use the original SVG icon, not a synthesized font character.
 await dom('gear',32,32,'<div class="gear-button" style="background:none!important;width:32px;height:32px"><svg viewBox="0 0 24 24"><path d="m9.7 2-.6 2.3-2 .9-2.1-.7-2.2 3.8 1.6 1.7-.2 2.2-1.4 1.7L5 17.7l2.3-.5 1.8 1.2.5 2.6h4.8l.5-2.6 1.8-1.2 2.3.5 2.2-3.8-1.4-1.7-.2-2.2 1.6-1.7L19 4.5l-2.1.7-2-.9L14.3 2Z"/><circle cx="12" cy="11.5" r="3.4"/></svg></div>');
 await browser.close();
 // Empty slot background is already authored into the exact cabinet master.
 const blank=createCanvas(174,174);fs.writeFileSync(path.join(output,'slot.png'),blank.toBuffer('image/png'));fs.writeFileSync(path.join(source,'slot-composite.png'),blank.toBuffer('image/png'));manifest.slot={w:58,h:58,source:'transparent overlay',layers:[{label:'Transparent overlay; well lives in cabinet',file:'slot-composite.png'}]};
 let lua="local root=app.params['root']\nlocal base=root..'/Tools/RuneBreak/Source/BlockPunch/Surface/'\n";
 for(const [name,item] of Object.entries(manifest)){
  lua+=`do\n local sprite=Sprite(${item.w*3},${item.h*3},ColorMode.RGB)\n`;
  item.layers.forEach((layer,i)=>lua+=` local layer=${i===0?'sprite.layers[1]':'sprite:newLayer()'}\n layer.name=${JSON.stringify(layer.label)}\n sprite:newCel(layer,1,Image{fromFile=base..'${layer.file}'},Point(0,0))\n`);
  lua+=` sprite:saveAs(base..'${name}.aseprite')\n sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/${name}.png')\n sprite:close()\nend\n`;
 }
 fs.writeFileSync(path.join(source,'manifest.json'),JSON.stringify(manifest,null,2));fs.writeFileSync(path.join(__dirname,'author-surface.lua'),lua);console.log('Exported '+Object.keys(manifest).length+' reference surfaces.');
})().catch(e=>{console.error(e);process.exitCode=1});
