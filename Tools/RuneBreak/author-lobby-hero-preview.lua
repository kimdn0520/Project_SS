-- Encode actual Unity frames as a preview; no shipped sprites are modified.
local root=app.params['root']
local first=Image{fromFile=root..'/Temp/LobbyHeroFilm/000.png'}
local sprite=Sprite(first.width,first.height,ColorMode.RGB)
sprite.layers[1].name='Unity lobby capture'
for i=0,119 do
  if i>0 then sprite:newEmptyFrame() end
  local image=Image{fromFile=root..'/Temp/LobbyHeroFilm/'..string.format('%03d',i)..'.png'}
  sprite:newCel(sprite.layers[1],i+1,image,Point(0,0))
  sprite.frames[i+1].duration=0.05
end
sprite:saveCopyAs(root..'/Tools/RuneBreak/QA/LobbyHero-Motion.gif')
sprite:close()
