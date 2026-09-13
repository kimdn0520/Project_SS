from pathlib import Path
p=Path('Assets/Prototype/Editor/ExpeditionValidation.cs');s=p.read_text(encoding='utf-8-sig');a=s.index('        [MenuItem');b=s.index('        private static void Check',a)
s=s[:a]+'''        [MenuItem("ProjectSS/Prototype/Validate Rules")]
        public static void Rules()
        {
            var c=AssetDatabase.LoadAssetAtPath<ExpeditionCatalog>("Assets/Prototype/ExpeditionCatalog.asset");
            var results=new List<string>();var d=ExpeditionSave.Fresh(c.gear.Length);var m=new ExpeditionModel(c,d,12);
            Check(!m.Craft(4)&&d.iron==0,"Insufficient materials never spend inventory",results);
            Check(!m.SelectRoute(2),"Relic route locked until first regional boss",results);
            for(int i=0;i<3;i++)while(d.excavations<=i)m.Dig();
            Check(d.chest,"Early chest guaranteed after three excavations",results);
            int count=d.excavations;while(d.excavations==count)m.Dig();
            Check(m.LastGear>=3&&d.inventory[m.LastGear]>0,"Chest awards a persistent equipment copy",results);
            d.iron=d.crystal=d.relic=500;
            Check(m.Craft(8)&&m.Equip(8,0),"Armor can be crafted and equipped on warrior",results);
            Check(!m.Equip(8,1),"One armor copy cannot be worn by two heroes",results);
            Check(m.Craft(8)&&m.Equip(8,1)&&m.HeroMaxHp(1)==190,"Second armor copy independently equips rogue",results);
            Check(!m.Equip(0,2),"Class-incompatible weapon rejected",results);
            Check(m.Craft(9)&&m.Equip(9,2)&&m.HeroMaxHp(2)==110,"Mage has independent helmet slot",results);
            Check(m.Craft(10)&&m.Equip(10,2)&&m.HeroDamage(2)==15,"Accessory changes actual attack damage",results);
            m.Craft(4);m.Equip(4,0);m.Craft(6);m.Equip(6,1);m.Craft(7);m.Equip(7,2);
            for(int wave=1;wave<=10;wave++)
            {
                Check(m.Region==1&&m.Wave==wave&&m.IsBoss==(wave==10),"Stage 1-"+wave+" has correct boss flag",results);
                m.StartBattle();Simulate(m);Check(m.LastVictory,"Prepared party clears 1-"+wave,results);
            }
            Check(m.Region==2&&m.Wave==1&&m.SelectRoute(2)&&m.MiningPower==2,"Boss unlocks 2-1, relic route and stronger mining",results);
            var weak=ExpeditionSave.Fresh(c.gear.Length);weak.cleared=99;var loser=new ExpeditionModel(c,weak,4);loser.StartBattle();Simulate(loser);
            Check(!loser.LastVictory&&weak.cleared==99,"Defeat stays on same stage",results);
            loser.StartBattle();Check(loser.TeamHp>0,"Retry restores the party",results);
            var restored=JsonUtility.FromJson<ExpeditionSave>(JsonUtility.ToJson(d));
            Check(restored.IsValid(c.gear.Length)&&restored.equipment[6]==8&&restored.cleared==10,"Save preserves progression and per-hero equipment",results);
            var rhythm=new MiningRhythm();rhythm.SetHeld(true);rhythm.Tick(2.5f);for(int i=0;i<6;i++)rhythm.BreakRock();
            Check(rhythm.BurstRemaining>0,"Six breaks trigger burst",results);rhythm.SetHeld(false);Check(rhythm.BurstRemaining==0,"Release immediately cancels burst",results);
            Directory.CreateDirectory("PrototypeQA");File.WriteAllLines("PrototypeQA/rules.txt",results);Debug.Log(string.Join("\\n",results));
        }
        static void Simulate(ExpeditionModel m){for(int i=0;i<4000&&m.Fighting;i++)m.Tick(.1f);if(m.Fighting)throw new Exception("Battle did not terminate");}

'''+s[b:];p.write_text(s,encoding='utf-8-sig')
p=Path('Assets/Scripts/Core/AppManager.cs');s=p.read_text(encoding='utf-8-sig');a=s.index('        if (SpriteManager.Instance != null)');b=s.index('        await UniTask.Delay',a);s=s[:a]+s[b:];a=s.index('        GameObject fadeObj = new GameObject');b=s.index('\n    }',a);s=s[:a]+'        Debug.LogError("AppManager requires the inspector-assigned fade canvas in Splash.");'+s[b:];p.write_text(s,encoding='utf-8-sig')
