using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace ProjectSS.Expedition.Editor
{
    public static class ExpeditionValidation
    {
        [MenuItem("ProjectSS/Prototype/Validate Rules")]
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
            Directory.CreateDirectory("PrototypeQA");File.WriteAllLines("PrototypeQA/rules.txt",results);Debug.Log(string.Join("\n",results));
        }
        static void Simulate(ExpeditionModel m){for(int i=0;i<4000&&m.Fighting;i++)m.Tick(.1f);if(m.Fighting)throw new Exception("Battle did not terminate");}

        private static void Check(bool condition, string label, List<string> results)
        {
            if (!condition) throw new Exception("FAIL: " + label);
            results.Add("PASS: " + label);
        }

        public static void InspectLive()
        {
            var page = UnityEngine.Object.FindFirstObjectByType<PlayPage>();
            if (page == null || page.Model == null) throw new Exception("Live page is not initialized");
            Debug.Log($"LIVE: tab={page.ActiveTab}; depth={page.Model.Data.depth}; cleared={page.Model.Data.cleared}; fighting={page.Model.Fighting}; popup={PopupManager.IsOpenAny}; pause={page.pausePolicy.IsPaused}");
            foreach (var text in page.GetComponentsInChildren<TMP_Text>())
                if (text.isTextTruncated) Debug.LogWarning("TRUNCATED: " + text.name + ": " + text.text);
        }

        public static void Hold() { HoldAsync(8).Forget(); }
        public static void CloseMenu(){UnityEngine.Object.FindFirstObjectByType<PlayPage>().OpenMenu(0);}
        public static void JourneyCheck()
        {
            var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Play"||PageManager.Instance.CurrentPageType!=UIPageType.PlayPage)throw new Exception("Splash did not enter PlayPage");
            if(PageManager.Instance.PageCount!=1)throw new Exception("Unexpected lobby registration");
            page.Model.Data.autoBattle=true;
            int before=page.Model.Data.cleared;
            for(int i=0;i<1800 && page.Model.Data.cleared==before;i++)page.TickJourney(.1f);
            if(page.Model.Data.cleared!=before+1)throw new Exception("Journey failed to advance");
            page.Model.Data.autoBattle=false;
            for(int i=0;i<60;i++)page.TickJourney(.1f);
            if(page.State==PlayPage.Journey.Waiting)throw new Exception("Legacy auto OFF incorrectly stopped automatic progression");
            page.Model.Data.autoBattle=true;
            Debug.Log("JOURNEY PASS: Splash -> PlayPage, one page, walk -> battle -> next wave, legacy auto OFF ignored");
        }
        public static void UiCheck()
        {
            var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();var data=page.Model.Data;
            var inventory=(int[])data.inventory.Clone();var gear=(int[])data.equipment.Clone();int iron=data.iron,crystal=data.crystal,relic=data.relic;
            try
            {
                data.iron=data.crystal=data.relic=500;
                Click("Menu1");Click("HeroCard0");Click("Slot2");
                if(page.ActiveTab!=2)throw new Exception("Slot did not open inventory");
                page.Refresh();Click("Craft8");page.Refresh();Click("Equip8");
                if(page.Model.Equipped(0,2)!=8)throw new Exception("Warrior armor UI equip failed");
                Click("Menu1");Click("HeroCard1");Click("Slot6");page.Refresh();
                if(page.gearCards[8].button.IsInteractable())throw new Exception("Same armor copy available to second hero");
                Click("Craft8");page.Refresh();Click("Equip8");
                if(page.Model.Equipped(1,2)!=8)throw new Exception("Rogue separate armor equip failed");
                Click("Menu3");if(page.ActiveTab!=3)throw new Exception("Challenge menu failed");
                Click("Menu4");Click("Sound");Click("Sound");
                page.OpenMenu(0);
                var rect=(RectTransform)page.digButton.transform;var camera=page.Canvas.worldCamera;
                var center=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center));
                var corner=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.max));
                if(!page.holdDig.IsRaycastLocationValid(center,camera)||page.holdDig.IsRaycastLocationValid(corner,camera))throw new Exception("Circular DIG hit region failed");
                if(page.deposits.Any(d=>d.enabled))throw new Exception("Ore icons remain embedded");
                var roots=page.miningWorld.GetComponentsInChildren<Transform>(true).Length;
                foreach(var item in page.itemEffects)
                {
                    var rented=page.pool.RentCached(item.poolName) as WeaponItem;
                    if(rented==null)throw new Exception("Weapon prefab pool missing");rented.Return();
                }
                if(roots!=page.miningWorld.GetComponentsInChildren<Transform>(true).Length)throw new Exception("Pool created mining objects");
                Debug.Log("UI PASS: per-hero slots, crafting and separate copies, four menus, circular DIG, hidden ore icons, prewarmed weapon prefab pools");
            }
            finally {data.inventory=inventory;data.equipment=gear;data.iron=iron;data.crystal=crystal;data.relic=relic;page.SendMessage("ApplyEquipment");page.OpenMenu(0);}
        }
        public static void Fractures()
        {
            var page = UnityEngine.Object.FindFirstObjectByType<PlayPage>();
            page.SelectTab(0);
            var lines = page.miningView.GetComponentsInChildren<LineRenderer>(true);
            for (int stage = 1; stage <= 4; stage++)
            {
                page.miningView.Strike(stage == 4, false, 0, stage * 0.25f);
                int visible = lines.Count(line => line.enabled);
                if (visible != stage * 2) throw new Exception($"Fracture stage {stage}: expected {stage * 2} strokes, got {visible}");
                Capture(); File.Copy("PrototypeQA/latest.png", $"PrototypeQA/fracture-{stage}.png", true);
            }
            if (page.GetComponentsInChildren<Transform>(true).Any(t => t.name == "RockHP")) throw new Exception("Ore HP bar remains");
            Debug.Log("FRACTURES PASS: four progressively larger dark cracks with bright edges; no ore HP bar");
        }
        private static async UniTaskVoid HoldAsync(float seconds)
        {
            var page = UnityEngine.Object.FindFirstObjectByType<PlayPage>();
            try
            {
                page.SelectTab(0); page.Model.Data.autoMine = false;
                int before = page.Model.Data.excavations;
                int objects = page.miningWorld.GetComponentsInChildren<Transform>(true).Length;
                var pointer = new PointerEventData(EventSystem.current) { pointerId = 77, button = PointerEventData.InputButton.Left };
                ExecuteEvents.Execute(page.digButton.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: page.destroyCancellationToken);
                int after = page.Model.Data.excavations;
                Capture();
                ExecuteEvents.Execute(page.digButton.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                bool chestPending=page.ChestOpening;
                await UniTask.Delay(1300, cancellationToken: page.destroyCancellationToken);
                int released=page.Model.Data.excavations;
                if (after <= before + 3 || page.holdDig.IsPressed || released > after+(chestPending?1:0)) throw new Exception("Hold/release mining check failed");
                await UniTask.Delay(350,cancellationToken:page.destroyCancellationToken);
                if(page.Model.Data.excavations!=released)throw new Exception("Released DIG continued mining");
                if (page.miningWorld.GetComponentsInChildren<Transform>(true).Length != objects) throw new Exception("Mining created or destroyed map objects");
                Directory.CreateDirectory("PrototypeQA");
                File.WriteAllText("PrototypeQA/hold.txt", $"PASS: actual pointer hold mined {after - before} rocks in {seconds}s\nPASS: pointer up stops immediately\nPASS: map object count unchanged ({objects})\n");
                Debug.Log("HOLD PASS: " + (after - before) + " rocks; immediate release; zero map creation");
            }
            catch (Exception e) { Debug.LogException(e); }
            finally { if (page != null) page.holdDig.HardCancel(); }
        }

        public static void Capture()
        {
            var camera = Camera.main;
            Directory.CreateDirectory("PrototypeQA");
            var rt = new RenderTexture(720, 1280, 24);
            var priorTarget = camera.targetTexture; var priorRect = camera.rect;
            camera.targetTexture = rt; camera.rect = new Rect(0, 0, 1, 1);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(720, 1280, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 720, 1280), 0, 0); tex.Apply();
            File.WriteAllBytes("PrototypeQA/latest.png", tex.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = priorTarget; camera.rect = priorRect;
            UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(rt);
            Debug.Log("Captured PrototypeQA/latest.png");
        }

        // Editor-only UI driver: dispatches through the actual persistent Unity UI button listeners.
        public static void Click(string name)
        {
            var button = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == name);
            if (button == null || !button.IsInteractable()) throw new Exception("Button unavailable: " + name);
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
        }
    }
}
