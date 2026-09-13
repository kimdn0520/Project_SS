from pathlib import Path
p=Path('Assets/Prototype/Editor/ExpeditionBuilder.cs');s=p.read_text(encoding='utf-8-sig')
a=s.index('            page.routeButtons=new Button[3]');b=s.index('            var auto=Button',a)
s=s[:a]+'''            page.routeButtons=Array.Empty<Button>();page.routeLabels=Array.Empty<TMP_Text>();page.routePanels=Array.Empty<Image>();
            for(int side=0;side<2;side++)for(int row=0;row<3;row++)
                Box(m,"SidebarSlot_"+side+"_"+row,side==0?18:626,421+row*99,76,76,Panel);
'''+s[b:]
a=s.index('            var h=page.menuPanels[0].transform;');b=s.index('            var bag=page.menuPanels[1].transform;',a);s=s[:a]+Path('Tools/HeroCardsUI.txt').read_text(encoding='utf-8-sig')+s[b:]
# Keep vein selection usable in the inventory resource header, not in the sidebar.
a=s.index('            Button(bag,"Unequip"');b=s.index('            var viewport=',a)
s=s[:a]+'''            Button(bag,"Unequip","해제",26,272,100,36,Panel,page.ClearSlot).GetComponentInChildren<TMP_Text>().fontSize=16;
            page.routeButtons=new Button[3];page.routeLabels=new TMP_Text[3];page.routePanels=new Image[3];
            for(int i=0;i<3;i++)
            {
                var b=Button(bag,"Route"+i,new[]{"철 광맥","서리 광맥","유적 광맥"}[i],140+i*188,272,178,36,Panel,null);
                b.GetComponentInChildren<TMP_Text>().fontSize=16;UnityEventTools.AddIntPersistentListener(b.onClick,page.SelectRoute,i);
                page.routeButtons[i]=b;page.routeLabels[i]=b.GetComponentInChildren<TMP_Text>();page.routePanels[i]=(Image)b.targetGraphic;
            }
'''+s[b:]
s=s.replace('        private static ExpeditionNotice MakeNotice',Path('Tools/PortraitBake.txt').read_text(encoding='utf-8-sig')+'\n        private static ExpeditionNotice MakeNotice')
p.write_text(s,encoding='utf-8-sig')
p=Path('Assets/Prototype/Editor/ExpeditionValidation.cs');s=p.read_text(encoding='utf-8-sig').replace('Click("Menu1");Click("Slot2")','Click("Menu1");Click("HeroCard0");Click("Slot2")').replace('Click("Menu1");Click("Slot6")','Click("Menu1");Click("HeroCard1");Click("Slot6")');p.write_text(s,encoding='utf-8-sig')
