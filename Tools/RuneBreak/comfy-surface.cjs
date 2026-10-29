// ComfyUI material/alpha export and raw RGBA validation against the Aseprite masters.
const fs=require('fs'),path=require('path');
const {createCanvas,loadImage}=require('@napi-rs/canvas');
const base=process.env.RUNE_COMFY_URL||'http://127.0.0.1:8188';
const source=path.join(__dirname,'Source/BlockPunch/Surface'),output=path.resolve(__dirname,'../../Assets/RuneBreak/Art/BlockPunch/Surface');
const refined=process.argv.includes('--refined');
const reward=process.argv.includes('--reward');
const motion=process.argv.includes('--reward-motion');
const palette=process.argv.includes('--board-palette');
const controls=process.argv.includes('--button-coins');
async function api(endpoint,options){const r=await fetch(base+endpoint,{signal:AbortSignal.timeout(20000),...options});if(!r.ok)throw Error(endpoint+': '+r.status+' '+await r.text());return r.json();}
async function pixels(buffer){const img=await loadImage(buffer),c=createCanvas(img.width,img.height);c.getContext('2d').drawImage(img,0,0);return {w:img.width,h:img.height,data:c.getContext('2d').getImageData(0,0,img.width,img.height).data};}
(async()=>{
 const stats=await api('/system_stats'),report={version:stats.system.comfyui_version,mode:'source material + alpha roundtrip; no diffusion resynthesis',assets:[]},prompt={};
 const names=controls?['molded_gold_372_88','molded_gold_340_76','molded_gold_300_82','molded_green_210_78','molded_amber_210_78','lobby_coin','reward_coins',...Array.from({length:8},(_,i)=>'reward_piece_'+i)]:palette||motion||reward||refined?Object.keys(JSON.parse(fs.readFileSync(path.join(source,palette?'board-palette-manifest.json':motion?'reward-motion-manifest.json':reward?'reward-manifest.json':'refined-manifest.json'),'utf8'))):['cabinet','settings','result','close','next','home','retry','button_blue','button_plum','button_aqua','row','switch_on','switch_off'];
 const assetPath=name=>path.join(name.startsWith('molded_')?path.resolve(output,'../Lobby/Blocks'):name==='lobby_coin'?path.resolve(output,'../Lobby/Icons'):output,name+'.png');
 for(let i=0;i<names.length;i++){
  const name=names[i],form=new FormData();form.append('image',new Blob([fs.readFileSync(assetPath(name))],{type:'image/png'}),name+'.png');form.append('subfolder','BlockCrashSurface');form.append('type','input');form.append('overwrite','true');
  const u=await api('/upload/image',{method:'POST',body:form}),n=i*3+1;
  prompt[n]={class_type:'LoadImage',inputs:{image:[u.subfolder,u.name].filter(Boolean).join('/')}};
  prompt[n+1]={class_type:'JoinImageWithAlpha',inputs:{image:[String(n),0],alpha:[String(n),1]}};
  prompt[n+2]={class_type:'SaveImage',inputs:{images:[String(n+1),0],filename_prefix:'BlockCrashSurface/'+name}};
 }
 fs.writeFileSync(path.join(source,controls?'comfy-button-coins-api.json':palette?'comfy-board-palette-api.json':motion?'comfy-reward-motion-api.json':reward?'comfy-reward-export-api.json':refined?'comfy-refined-export-api.json':'comfy-material-export-api.json'),JSON.stringify(prompt,null,2));
 const job=await api('/prompt',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({prompt,client_id:'block-crash-surface'})});
 let result;for(let i=0;i<120;i++){const h=await api('/history/'+job.prompt_id);result=h[job.prompt_id];if(result)break;await new Promise(r=>setTimeout(r,500));}
 if(!result||result.status.status_str!=='success')throw Error(JSON.stringify(result?.status));
 for(let i=0;i<names.length;i++){
  const name=names[i],info=result.outputs[i*3+3].images[0],r=await fetch(base+'/view?'+new URLSearchParams(info)),buf=Buffer.from(await r.arrayBuffer());
  const a=await pixels(assetPath(name)),b=await pixels(buf);if(a.w!==b.w||a.h!==b.h)throw Error(name+' dimensions');
  let changed=0,maxDelta=0,alphaDelta=0,maxCompositeDelta=0;
  for(let p=0;p<a.data.length;p+=4){alphaDelta=Math.max(alphaDelta,Math.abs(a.data[p+3]-b.data[p+3]));if(a.data[p+3]===0&&b.data[p+3]===0)continue;for(let k=0;k<4;k++){const delta=Math.abs(a.data[p+k]-b.data[p+k]);if(delta)changed++;maxDelta=Math.max(maxDelta,delta);if(k<3)for(const bg of [0,255])maxCompositeDelta=Math.max(maxCompositeDelta,Math.abs((a.data[p+k]*a.data[p+3]+bg*(255-a.data[p+3]))/255-(b.data[p+k]*b.data[p+3]+bg*(255-b.data[p+3]))/255));}}
  // Straight RGB is unstable on almost-transparent edge pixels. Compare both
  // black and white alpha composites as well as alpha itself, never ignore edges.
  if(alphaDelta>1||maxCompositeDelta>2)throw Error(name+' material roundtrip differs: composite '+maxCompositeDelta+' alpha '+alphaDelta);
  // ComfyUI's float alpha serialization may round antialiased channels by one unit.
  // Keep the exact Aseprite composite as the shipped master whenever this happens.
  fs.writeFileSync(path.join(source,name+'-comfy.png'),buf);report.assets.push({name,width:a.w,height:a.h,changedChannels:changed,maxDelta,alphaDelta,maxCompositeDelta,shipped:'exact Aseprite composite'});
 }
 if(!refined&&!reward&&!motion&&!palette&&!controls){const verified=require('child_process').execFileSync('python',[path.join(__dirname,'verify-surface.py')],{encoding:'utf8'});report.validation=JSON.parse(verified);}
 report.promptId=job.prompt_id;report.status='passed';fs.writeFileSync(path.join(__dirname,controls?'QA/button-coins-comfy.json':palette?'QA/board-palette-comfy.json':motion?'QA/reward-motion-comfy.json':reward?'QA/surface-reward-comfy.json':refined?'QA/surface-refined-comfy.json':'QA/surface-comfy.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
})().catch(e=>{console.error(e);process.exitCode=1});
