-- Encode actual Unity reward frames without modifying shipped art.
local root=app.params['root']
local first=Image{fromFile=root..'/Temp/RewardArrivalFilm/000.png'}
local sprite=Sprite(first.width,first.height,ColorMode.RGB)
sprite.layers[1].name='Unity reward capture'
for i=0,95 do
  if i>0 then sprite:newEmptyFrame() end
  local image=Image{fromFile=root..'/Temp/RewardArrivalFilm/'..string.format('%03d',i)..'.png'}
  sprite:newCel(sprite.layers[1],i+1,image,Point(0,0))
  sprite.frames[i+1].duration=1/30
end
sprite:saveCopyAs(root..'/Tools/RuneBreak/QA/RewardArrival-Motion.gif')
sprite:close()
