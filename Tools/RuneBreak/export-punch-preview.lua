local root=app.params['root']
local folder=app.params['frames'] or 'Temp/BlockPunchFilm'
local output=app.params['output'] or 'Tools/RuneBreak/QA/BlockPunch-Rebound.gif'
local last=tonumber(app.params['last']) or 78
local step=tonumber(app.params['step']) or 2
local fps=tonumber(app.params['fps']) or 30
local sprite=Sprite(540,960,ColorMode.RGB)
sprite.layers[1].name='Unity page capture'
for i=0,last,step do
  local frame=i==0 and sprite.frames[1] or sprite:newEmptyFrame()
  frame.duration=1/fps
  sprite:newCel(sprite.layers[1],frame,Image{fromFile=root..'/'..folder..'/'..string.format('%03d',i)..'.png'},Point(0,0))
end
sprite:saveCopyAs(root..'/'..output)
sprite:close()
