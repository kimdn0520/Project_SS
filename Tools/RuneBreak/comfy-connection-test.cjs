// Connection test against the running Comfy Desktop backend. No model download.
const fs=require('fs'),path=require('path');
const {createCanvas,loadImage}=require('@napi-rs/canvas');
const base=process.env.RUNE_COMFY_URL||'http://127.0.0.1:8188';
const source=path.join(__dirname,'Source/BlockPunch/engraved-red-N.png');
const destination=path.join(__dirname,'QA/ComfyUI-connection-block.png');
async function api(endpoint,options){const response=await fetch(base+endpoint,options);if(!response.ok)throw Error(endpoint+': '+response.status+' '+await response.text());return response.json();}
(async()=>{
 const stats=await api('/system_stats');
 const form=new FormData();form.append('image',new Blob([fs.readFileSync(source)],{type:'image/png'}),'RuneBreak-connection-red-N.png');form.append('subfolder','RuneBreak');form.append('type','input');
 const upload=await api('/upload/image',{method:'POST',body:form});
 const prompt={
  '1':{class_type:'LoadImage',inputs:{image:[upload.subfolder,upload.name].filter(Boolean).join('/')}},
  '2':{class_type:'JoinImageWithAlpha',inputs:{image:['1',0],alpha:['1',1]}},
  '3':{class_type:'SaveImage',inputs:{images:['2',0],filename_prefix:'RuneBreak/connection-check'}}
 };
 fs.writeFileSync(path.join(__dirname,'Source/BlockPunch/comfy-connection-api.json'),JSON.stringify(prompt,null,2));
 const job=await api('/prompt',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({prompt,client_id:'runebreak-connection-check'})});
 let result;const deadline=Date.now()+60000;
 while(Date.now()<deadline){const history=await api('/history/'+job.prompt_id);result=history[job.prompt_id];if(result)break;await new Promise(resolve=>setTimeout(resolve,500));}
 if(!result||result.status?.status_str!=='success')throw Error('Workflow did not complete successfully: '+JSON.stringify(result?.status));
 const output=result.outputs['3'].images[0];const response=await fetch(base+'/view?'+new URLSearchParams(output));if(!response.ok)throw Error('Output download failed');
 fs.writeFileSync(destination,Buffer.from(await response.arrayBuffer()));
 function pixels(image){const c=createCanvas(image.width,image.height);c.getContext('2d').drawImage(image,0,0);return c.getContext('2d').getImageData(0,0,image.width,image.height).data;}
 const a=await loadImage(source),b=await loadImage(destination);if(a.width!==b.width||a.height!==b.height)throw Error('Dimensions changed');
 const aa=pixels(a),bb=pixels(b);let changedChannels=0,maxChannelDifference=0;
 for(let i=0;i<aa.length;i++){const d=Math.abs(aa[i]-bb[i]);if(d)changedChannels++;maxChannelDifference=Math.max(maxChannelDifference,d);}
 const report={endpoint:base,comfyVersion:stats.system.comfyui_version,device:stats.devices[0].name,promptId:job.prompt_id,status:result.status.status_str,width:a.width,height:a.height,changedChannels,maxChannelDifference,output:destination};
 fs.writeFileSync(path.join(__dirname,'QA/comfy-connection.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report,null,2));
})().catch(error=>{console.error(error.message);process.exitCode=1;});
