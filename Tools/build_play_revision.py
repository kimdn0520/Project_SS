from pathlib import Path
p=Path('Assets/Prototype/Editor/ExpeditionBuilder.cs');s=p.read_text(encoding='utf-8-sig').replace('ExpeditionPage','PlayPage').replace('Assets/Prototype/ExpeditionPrototype.unity','Assets/Scenes/Play.unity').replace('UIPageType.ExpeditionPrototype','UIPageType.PlayPage')
a=s.index('        private static void BuildUI(');b=s.index('        private static ExpeditionNotice MakeNotice',a);s=s[:a]+Path('Tools/PlayUI.txt').read_text(encoding='utf-8-sig')+'\n\n'+s[b:]
s=s.replace('new Vector4(-3.6f, 1.36f, 3.6f, 4.58f)','new Vector4(-3.6f, 2.66f, 3.6f, 5.4f)').replace('new Vector4(-3.6f, -3.88f, 3.6f, -1.34f)','new Vector4(-3.6f, -2.38f, 3.6f, 0.8f)')
s=s.replace('TextOverflowModes.Ellipsis','TextOverflowModes.Truncate')
s=s.replace('            EditorSceneManager.SaveScene(scene, ScenePath);','''            // All services and popup references are part of the page prefab; the camera is injected.
            foreach (var service in new MonoBehaviour[] { sprites, pool, pause, popupManager })
            {
                service.transform.SetParent(root.transform, true);
                var config = new SerializedObject(service);
                var persist = config.FindProperty("persistAcrossScenes"); if (persist != null) persist.boolValue = false;
                config.ApplyModifiedPropertiesWithoutUndo();
            }
            var pmConfig = new SerializedObject(pageManager); pmConfig.FindProperty("persistAcrossScenes").boolValue=false;pmConfig.ApplyModifiedPropertiesWithoutUndo();
            page.notice.transform.SetParent(root.transform, true);
            const string prefabPath="Assets/Resources/Prefabs/PlayPage.prefab";
            AssetDatabase.DeleteAsset(prefabPath);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root,prefabPath,InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Splash.unity",true),new EditorBuildSettingsScene(ScenePath,true)};''')
s=s.replace('Set(popupManager, "popupRoot", New("PopupRoot").transform);','Set(popupManager, "popupRoot", New("PopupRoot", root.transform).transform);')
s=s.replace('Gear("수호 갑옷", "전사 · 원정대 최대 체력 +100", "Chest", "FA_Chest_001_Silver", 0, 0, 0, GearEffect.Armor, 18, 6),','''Gear("수호 갑옷", "공용 · 최대 체력 +100", "Chest", "FA_Chest_001_Silver", -1, 0, 0, GearEffect.Armor, 18, 6),
                Gear("탐험 투구", "공용 · 최대 체력 +35", "Helmet", "FA_Helmet_001_Ivory", -1, 0, 0, GearEffect.Normal, 9, 3),
                new GearDefinition {title="서리 부적",description="공용 · 공격 +5 / 체력 +15",hero=-1,equipSlot=3,slot="Accessory",spriteKey="Economy_Gem_03_Blue",icon=SpriteAt(Icons+"Economy_Gem_03_Blue.png"),damage=5,health=15,iron=8,crystal=8},''')
s=s.replace('            registered = catalog.gear.Select(g => SpriteAt(PartPath(g.slot, g.spriteKey))).Distinct().ToArray();','''            catalog.gear[8].equipSlot=2;catalog.gear[8].health=100;
            catalog.gear[9].equipSlot=1;catalog.gear[9].health=35;
            for(int i=3;i<8;i++){catalog.gear[i].effect=GearEffect.Normal;catalog.gear[i].description="기본 공격 "+catalog.gear[i].damage+" · "+catalog.gear[i].interval+"초 간격";catalog.gear[i].unlock=0;}
            registered = catalog.gear.Select(g => g.slot=="Accessory"?g.icon:SpriteAt(PartPath(g.slot,g.spriteKey))).Distinct().ToArray();
            Directory.CreateDirectory("Assets/Resources/Weapon/Prefabs");
            for(int i=0;i<catalog.gear.Length;i++)
            {
                var g=catalog.gear[i];var obj=New(g.spriteKey);var item=obj.AddComponent<WeaponItem>();item.catalogId=i;
                item.visual=New("Visual",obj.transform).AddComponent<SpriteRenderer>();item.visual.sprite=g.icon;
                var prefab=PrefabUtility.SaveAsPrefabAsset(obj,"Assets/Resources/Weapon/Prefabs/"+g.spriteKey+".prefab");
                g.prefab=prefab.GetComponent<WeaponItem>();UnityEngine.Object.DestroyImmediate(obj);
            }''')
# Battle is built in its own root so its composition moves without affecting mining.
s=s.replace('        private static void BuildWorld(PlayPage page, Transform parent)\n        {','''        private static void BuildWorld(PlayPage page, Transform parent)
        {
            var gameRoot=parent;
            parent=New("BattleWorld",gameRoot).transform;''')
s=s.replace('            page.miningWorld = New("MiningWorld", parent).transform;','''            var scrollParts=new System.Collections.Generic.List<Transform>();
            foreach(var sr in parent.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.sortingOrder<0).ToArray())
            {
                var tile=New(sr.name+"_Scroll",parent).transform;tile.position=Vector3.zero;sr.transform.SetParent(tile,true);
                var copy=UnityEngine.Object.Instantiate(tile.gameObject,parent).transform;copy.localPosition=Vector3.right*7.6f;
                scrollParts.Add(tile);scrollParts.Add(copy);
            }
            page.scrolling=scrollParts.ToArray();parent.position=Vector3.up*1.3f;
            page.enemyRest=page.enemies.Select(e=>e.transform.position).ToArray();
            page.miningWorld = New("MiningWorld", gameRoot).transform;''')
s=s.replace('                page.deposits[i] = WorldSprite','                page.deposits[i] = WorldSprite')
s=s.replace('            SetArray(page.miningView, "cracks", crackLines);','''            var chest=WorldSprite("TreasureChest",page.blocks[0].transform,SpriteAt("Assets/Textures/Game/Item_Chest_01_Wood.png"),372,895,142,142,25,Color.white);
            Set(page.miningView,"chestVisual",chest);chest.gameObject.SetActive(false);
            foreach(var deposit in page.deposits)deposit.enabled=false;
            page.miningWorld.position=Vector3.up*1.5f;
            SetArray(page.miningView, "cracks", crackLines);''')
s=s.replace('Text(canvas.transform, "NoticeKicker", "MINERS & HEROES"','Text(canvas.transform, "NoticeKicker", "안내"')
s=s.replace('            Set(actor, "animator", visual.GetComponentInChildren<Animator>(true));','''            var animator=visual.GetComponentInChildren<Animator>(true);
            Set(actor, "animator", animator);
            if(animator!=null && animator.runtimeAnimatorController!=null)
            {
                var walk=animator.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name.IndexOf("Walk",StringComparison.OrdinalIgnoreCase)>=0);
                var config=new SerializedObject(actor);config.FindProperty("walkState").stringValue=walk!=null?walk.name:"Run";config.ApplyModifiedPropertiesWithoutUndo();
            }''')
p.write_text(s,encoding='utf-8-sig')
# API bridge and editor-only reference names
for p in [Path('Assets/Prototype/Editor/ExpeditionValidation.cs'),Path('Tools/PrototypeBridge.cs')]:
 s=p.read_text(encoding='utf-8-sig').replace('ExpeditionPage','PlayPage');p.write_text(s,encoding='utf-8-sig')
