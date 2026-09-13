from pathlib import Path
p=Path('Assets/Prototype/Editor/ExpeditionBuilder.cs');s=p.read_text(encoding='utf-8-sig')
a=s.index('            string[] menus=');b=s.index('            page.menuPanels=',a)
s=s[:a]+'''            var navigation=New("BottomNavigation",ui,true);Stretch((RectTransform)navigation.transform);
            Box(navigation.transform,"NavigationDeck",0,1210,720,70,Hex("202B3A"));
            string[] menus={"용사 관리","가방","도전","설정"};float[] menuX={16,119,506,609};
            string[] menuAssets={"Gear_Helmet_01.png","UI_Common_Bag_01_Brown.png","UI_Rewards_Trophy_01_Gold.png","UI_System_Setting_01.png"};
            page.menuButtons=new Image[4];page.menuIcons=new Transform[4];page.menuIconRest=new Vector3[4];
            for(int i=0;i<4;i++)
            {
                var b=Button(navigation.transform,"Menu"+(i+1),menus[i],menuX[i],1194,95,68,Hex("2B354C"),null);
                var label=b.GetComponentInChildren<TMP_Text>();label.fontSize=18;Rect((RectTransform)label.transform,0,28,95,36);
                var icon=Icon(b.transform,"MenuIcon",SpriteAt(Icons+menuAssets[i]),5,-47,85,85);
                page.menuButtons[i]=(Image)b.targetGraphic;page.menuIcons[i]=icon.transform;page.menuIconRest[i]=icon.transform.localPosition;
                UnityEventTools.AddIntPersistentListener(b.onClick,page.OpenMenu,i+1);
            }
'''+s[b:]
s=s.replace('            foreach(var panel in page.menuPanels)panel.SetActive(false);','            foreach(var panel in page.menuPanels)panel.SetActive(false);\n            navigation.transform.SetAsLastSibling();')
s=s.replace('            Set(page.miningView,"chestVisual",chest);chest.gameObject.SetActive(false);','''            Set(page.miningView,"chestVisual",chest);chest.gameObject.SetActive(false);
            var opening=New("ChestOpening",page.miningWorld);opening.transform.position=chest.transform.position;
            var parts=ChestParts();float scale=1.42f/5.12f;
            var body=New("ChestBody",opening.transform).AddComponent<SpriteRenderer>();body.sprite=parts[0];body.sharedMaterial=miningMaterial;body.sortingOrder=29;body.transform.localScale=Vector3.one*scale;body.transform.localPosition=new Vector3(0,-.385f);
            var lid=New("ChestLid",opening.transform).AddComponent<SpriteRenderer>();lid.sprite=parts[1];lid.sharedMaterial=miningMaterial;lid.sortingOrder=30;lid.transform.localScale=Vector3.one*scale;lid.transform.localPosition=new Vector3(0,-.058f);
            var glow=New("ChestGlow",opening.transform).AddComponent<SpriteRenderer>();glow.sprite=ExpeditionUIArt.Circle();glow.sharedMaterial=miningMaterial;glow.sortingOrder=28;glow.transform.localScale=new Vector3(1.2f/glow.sprite.bounds.size.x,.6f/glow.sprite.bounds.size.y,1);
            opening.transform.SetParent(page.blocks[0].transform,true);opening.SetActive(false);
            Set(page.miningView,"chestOpenRoot",opening);Set(page.miningView,"chestLid",lid.transform);Set(page.miningView,"chestGlow",glow);
            var chestSo=new SerializedObject(page.miningView);chestSo.FindProperty("lidRest").vector3Value=lid.transform.localPosition;chestSo.ApplyModifiedPropertiesWithoutUndo();''')
a=s.index('        private static Sprite BakePortrait')
s=s[:a]+'''        private static Sprite[] ChestParts()
        {
            const string path="Assets/Prototype/Art/ChestParts.asset";
            var parts=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(p=>p.name).ToArray();if(parts.Length==2)return parts;
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Layer Lab/2D Minimal-IconPack/Icons/512/Item_Chest_01_Wood.png");
            var body=Sprite.Create(texture,new Rect(0,0,512,235),new Vector2(.5f,.5f),100);body.name="0_Body";
            var lid=Sprite.Create(texture,new Rect(0,235,512,277),new Vector2(.5f,0),100);lid.name="1_Lid";
            AssetDatabase.CreateAsset(body,path);AssetDatabase.AddObjectToAsset(lid,path);return new[]{body,lid};
        }
'''+s[a:]
p.write_text(s,encoding='utf-8-sig')
