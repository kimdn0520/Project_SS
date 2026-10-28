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
 local s=Sprite(209,267,ColorMode.RGB)
 s.layers[1].name='Original coin material'
 s:newCel(s.layers[1],1,Image{fromFile=base..'reward_piece_0-source.png'},Point(0,0))
 s:saveAs(base..'reward_piece_0.aseprite')
 s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_piece_0.png')
 s:close()
end
do
 local s=Sprite(222,307,ColorMode.RGB)
 s.layers[1].name='Original coin material'
 s:newCel(s.layers[1],1,Image{fromFile=base..'reward_piece_1-source.png'},Point(0,0))
 s:saveAs(base..'reward_piece_1.aseprite')
 s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_piece_1.png')
 s:close()
end
do
 local s=Sprite(209,237,ColorMode.RGB)
 s.layers[1].name='Original coin material'
 s:newCel(s.layers[1],1,Image{fromFile=base..'reward_piece_2-source.png'},Point(0,0))
 s:saveAs(base..'reward_piece_2.aseprite')
 s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_piece_2.png')
 s:close()
end
do
 local s=Sprite(201,202,ColorMode.RGB)
 s.layers[1].name='Original coin material'
 s:newCel(s.layers[1],1,Image{fromFile=base..'reward_piece_3-source.png'},Point(0,0))
 s:saveAs(base..'reward_piece_3.aseprite')
 s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_piece_3.png')
 s:close()
end
do
 local s=Sprite(232,284,ColorMode.RGB)
 s.layers[1].name='Original coin material'
 s:newCel(s.layers[1],1,Image{fromFile=base..'reward_piece_4-source.png'},Point(0,0))
 s:saveAs(base..'reward_piece_4.aseprite')
 s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_piece_4.png')
 s:close()
end
do
 local s=Sprite(222,247,ColorMode.RGB)
 s.layers[1].name='Original coin material'
 s:newCel(s.layers[1],1,Image{fromFile=base..'reward_piece_5-source.png'},Point(0,0))
 s:saveAs(base..'reward_piece_5.aseprite')
 s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_piece_5.png')
 s:close()
end
do
 local s=Sprite(193,197,ColorMode.RGB)
 s.layers[1].name='Original coin material'
 s:newCel(s.layers[1],1,Image{fromFile=base..'reward_piece_6-source.png'},Point(0,0))
 s:saveAs(base..'reward_piece_6.aseprite')
 s:saveCopyAs(root..'/Assets/RuneBreak/Art/BlockPunch/Surface/reward_piece_6.png')
 s:close()
end
do
 local s=Sprite(589,225,ColorMode.RGB)
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
