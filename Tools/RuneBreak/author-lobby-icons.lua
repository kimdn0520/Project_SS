local root=app.params['root']
local base=root..'/Tools/RuneBreak/Source/BlockPunch/Lobby/Icons/'
local labels={coin={'Shadow','Gold edge','Bevel','Inset face','Embossed star','Highlights'},heart={'Shadow','Ruby edge','Bevel','Puffy face','Soft reflection','Highlights'}}
for _,name in ipairs({'coin','heart'}) do
  local sprite=Sprite(144,144,ColorMode.RGB)
  for i,label in ipairs(labels[name]) do
    local layer=i==1 and sprite.layers[1] or sprite:newLayer()
    layer.name=label
    sprite:newCel(layer,1,Image{fromFile=base..name..'-'..(i-1)..'.png'},Point(0,0))
  end
  sprite:saveAs(base..name..'.aseprite')
  sprite:saveCopyAs(base..name..'-roundtrip.png')
  -- The layered Aseprite master owns the final alpha compositing.
  sprite:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Lobby/Icons/lobby_'..name..'.png')
  sprite:close()
end
