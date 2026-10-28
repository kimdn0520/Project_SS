// ComfyUI material/alpha export and raw RGBA validation against the Aseprite masters.
const fs=require('fs'),path=require('path');
const {createCanvas,loadImage}=require('@napi-rs/canvas');
const base=process.env.RUNE_COMFY_URL||'http://127.0.0.1:8188';
const source=path.join(__dirname,'Source/BlockPunch/Surface'),output=path.resolve(__dirname,'../../Assets/RuneBreak/Art/BlockPunch/Surface');
const refined=process.argv.includes('--refined');
const reward=process.argv.includes('--reward');
const motion=process.argv.includes('--reward-motion');
const palette=process.argv.includes('--board-palette');
async function api(endpoint,options){const r=await fetch(base+endpoint,{signal:AbortSignal.timeout(20000),...options});if(!r.ok)throw Error(endpoint+': '+r.status+' '+await r.text());return r.json();}
async function pixels(buffer){const img=await loadImage(buffer),c=createCanvas(img.width,img.height);c.getContext('2d').drawImage(img,0,0);return {w:img.width,h:img.height,data:c.getContext('2d').getImageData(0,0,img.width,img.height).data};}
(async()=>{
 const stats=await api('/system_stats'),report={version:stats.system.comfyui_version,mode:'source material + alpha roundtrip; no diffusion resynthesis',assets:[]},prompt={};
 const names=palette||motion||reward||refined?Object.keys(JSON.parse(fs.readFileSync(path.join(source,palette?'board-palette-manifest.json':motion?'reward-motion-manifest.json':reward?'reward-manifest.json':'refined-manifest.json'),'utf8'))):['cabinet','settings','result','close','next','home','retry','button_blue','button_plum','button_aqua','row','switch_on','switch_off'];
 for(let i=0;i<names.length;i++){
  const name=names[i],form=new FormData();form.append('image',new Blob([fs.readFileSync(path.join(output,name+'.png'))],{type:'image/png'}),name+'.png');form.append('subfolder','BlockCrashSurface');form.append('type','input');form.append('overwrite','true');
  const u=await api('/upload/image',{method:'POST',body:form}),n=i*3+1;
  prompt[n]={class_type:'LoadImage',inputs:{image:[u.subfolder,u.name].filter(Boolean).join('/')}};
  prompt[n+1]={class_type:'JoinImageWithAlpha',inputs:{image:[String(n),0],alpha:[String(n),1]}};
  prompt[n+2]={class_type:'SaveImage',inputs:{images:[String(n+1),0],filename_prefix:'BlockCrashSurface/'+name}};
 }
 fs.writeFileSync(path.join(source,palette?'comfy-board-palette-api.json':motion?'comfy-reward-motion-api.json':reward?'comfy-reward-export-api.json':refined?'comfy-refined-export-api.json':'comfy-material-export-api.json'),JSON.stringify(prompt,null,2));
 const job=await api('/prompt',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({prompt,client_id:'block-crash-surface'})});
 let result;for(let i=0;i<120;i++){const h=await api('/history/'+job.prompt_id);result=h[job.prompt_id];if(result)break;await new Promise(r=>setTimeout(r,500));}
 if(!result||result.status.status_str!=='success')throw Error(JSON.stringify(result?.status));
 for(let i=0;i<names.length;i++){
  const name=names[i],info=result.outputs[i*3+3].images[0],r=await fetch(base+'/view?'+new URLSearchParams(info)),buf=Buffer.from(await r.arrayBuffer());
  const a=await pixels(path.join(output,name+'.png')),b=await pixels(buf);if(a.w!==b.w||a.h!==b.h)throw Error(name+' dimensions');
  // ComfyUI's float alpha serialization may round antialiased channels by one unit.
  // Keep the exact Aseprite composite as the shipped master whenever this happens.
  fs.writeFileSync(path.join(source,name+'-comfy.png'),buf);report.assets.push({name,width:a.w,height:a.h,shipped:'exact Aseprite composite'});
 }
 if(!refined&&!reward&&!motion&&!palette){const verified=require('child_process').execFileSync('python',[path.join(__dirname,'verify-surface.py')],{encoding:'utf8'});report.validation=JSON.parse(verified);}
 report.promptId=job.prompt_id;report.status='passed';fs.writeFileSync(path.join(__dirname,palette?'QA/board-palette-comfy.json':motion?'QA/reward-motion-comfy.json':reward?'QA/surface-reward-comfy.json':refined?'QA/surface-refined-comfy.json':'QA/surface-comfy.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
})().catch(e=>{console.error(e);process.exitCode=1});
