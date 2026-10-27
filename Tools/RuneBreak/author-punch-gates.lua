-- Run in Aseprite batch mode. Keep source body and round studs on separate layers.
local root=app.params['root']
for _,color in ipairs({'red','blue','gold','green'}) do
  local block=app.open(root..'/engraved-'..color..'-N.png')
  local pixels=block.cels[1].image
  for _,side in ipairs({'N','E'}) do
    local sprite=app.open(root..'/gate-bar-'..color..'-'..side..'.png')
    sprite.layers[1].name='Original HTML long gate'
    local layer=sprite:newLayer();layer.name='Two matching round studs'
    local studs=Image(sprite.spec)
    for _,along in ipairs({51,861}) do
      local cx=side=='N' and along or 63
      local cy=side=='N' and 63 or along
      for y=-21,23 do for x=-21,21 do
        -- The circular highlight, bevel and contact shadow come from the block.
        if (x/20.5)^2+((y-1)/22)^2<=1 then
          studs:drawPixel(cx+x,cy+y,pixels:getPixel(48+x,43+y))
        end
      end end
    end
    sprite:newCel(layer,1,studs,Point(0,0))
    sprite:saveAs(root..'/LongGate-'..color..'-'..side..'.aseprite')
    sprite:saveCopyAs(root..'/long-gate-'..color..'-'..side..'.png')
    sprite:close()
  end
  block:close()
end
