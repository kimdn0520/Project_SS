local root=app.params['root']
local base=root..'/Tools/RuneBreak/Source/BlockPunch/Surface/'
do
 local s=Sprite(746,199,ColorMode.RGB)
 s.layers[1].name='Original coin material'
 s:newCel(s.layers[1],1,Image{fromFile=base..'reward_pile_shadow-source.png'},Point(0,0))
 s:saveAs(base..'reward_pile_shadow.aseprite')
 s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_pile_shadow.png')
 s:close()
end
do
 local s=Sprite(209,249,ColorMode.RGB)
 s.layers[1].name='Original coin material'
 s:newCel(s.layers[1],1,Image{fromFile=base..'reward_piece_0-source.png'},Point(0,0))
 s:saveAs(base..'reward_piece_0.aseprite')
 s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_piece_0.png')
 s:close()
end
do
 local s=Sprite(221,286,ColorMode.RGB)
 s.layers[1].name='Original coin material'
 s:newCel(s.layers[1],1,Image{fromFile=base..'reward_piece_1-source.png'},Point(0,0))
 s:saveAs(base..'reward_piece_1.aseprite')
 s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_piece_1.png')
 s:close()
end
do
 local s=Sprite(209,222,ColorMode.RGB)
 s.layers[1].name='Original coin material'
 s:newCel(s.layers[1],1,Image{fromFile=base..'reward_piece_2-source.png'},Point(0,0))
 s:saveAs(base..'reward_piece_2.aseprite')
 s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_piece_2.png')
 s:close()
end
do
 local s=Sprite(201,190,ColorMode.RGB)
 s.layers[1].name='Original coin material'
 s:newCel(s.layers[1],1,Image{fromFile=base..'reward_piece_3-source.png'},Point(0,0))
 s:saveAs(base..'reward_piece_3.aseprite')
 s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_piece_3.png')
 s:close()
end
do
 local s=Sprite(231,265,ColorMode.RGB)
 s.layers[1].name='Original coin material'
 s:newCel(s.layers[1],1,Image{fromFile=base..'reward_piece_4-source.png'},Point(0,0))
 s:saveAs(base..'reward_piece_4.aseprite')
 s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_piece_4.png')
 s:close()
end
do
 local s=Sprite(221,232,ColorMode.RGB)
 s.layers[1].name='Original coin material'
 s:newCel(s.layers[1],1,Image{fromFile=base..'reward_piece_5-source.png'},Point(0,0))
 s:saveAs(base..'reward_piece_5.aseprite')
 s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_piece_5.png')
 s:close()
end
do
 local s=Sprite(193,185,ColorMode.RGB)
 s.layers[1].name='Original coin material'
 s:newCel(s.layers[1],1,Image{fromFile=base..'reward_piece_6-source.png'},Point(0,0))
 s:saveAs(base..'reward_piece_6.aseprite')
 s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_piece_6.png')
 s:close()
end
do
 local s=Sprite(587,215,ColorMode.RGB)
 s.layers[1].name='Original coin material'
 s:newCel(s.layers[1],1,Image{fromFile=base..'reward_piece_7-source.png'},Point(0,0))
 s:saveAs(base..'reward_piece_7.aseprite')
 s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_piece_7.png')
 s:close()
end
do
 local s=Sprite(72,72,ColorMode.RGB)
 s.layers[1].name='Original coin material'
 s:newCel(s.layers[1],1,Image{fromFile=base..'reward_glint-source.png'},Point(0,0))
 s:saveAs(base..'reward_glint.aseprite')
 s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_glint.png')
 s:close()
end
