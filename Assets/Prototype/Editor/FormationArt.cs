using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition.Editor
{
    public static class FormationArt
    {
        static void Rect(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static TMP_Text Text(Transform parent,string name,string value,TMP_FontAsset font,float x,float y,float w,float h,float size)
        {
            var child=parent.Find(name);if(child==null){child=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)).transform;child.SetParent(parent,false);}
            var t=child.GetComponent<TMP_Text>();t.font=font;t.text=value;t.fontSize=size;t.color=new Color(.20f,.25f,.25f);t.raycastTarget=false;t.alignment=TextAlignmentOptions.Center;
            Rect(t.rectTransform,x,y,w,h);return t;
        }
        static Button Button(Transform parent,string name,string text,TMP_FontAsset font,float x,float y,float w,float h)
        {
            var t=parent.Find(name);
            if(t==null){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/ActionButton.prefab");t=((GameObject)PrefabUtility.InstantiatePrefab(prefab,parent)).transform;t.name=name;}
            Rect((RectTransform)t,x,y,w,h);var b=t.GetComponent<Button>();b.onClick=new Button.ButtonClickedEvent();
            var label=b.GetComponentInChildren<TMP_Text>();label.font=font;label.text=text;label.fontSizeMax=22;label.fontSizeMin=14;return b;
        }
        public static void Configure(PlayPage page)
        {
            var old=page.Canvas.GetComponentsInChildren<Toggle>(true).FirstOrDefault(t=>t.name=="AutoBattle");if(old!=null)Object.DestroyImmediate(old.gameObject);
            if(page.formationPositions==null||page.formationPositions.Length!=3)page.formationPositions=page.heroes.Select(h=>h.transform.localPosition).OrderByDescending(p=>p.x).ToArray();
            var grid=page.heroGrid.transform;
            var control=grid.GetComponent<FormationPanel>()??grid.gameObject.AddComponent<FormationPanel>();page.formationPanel=control;control.page=page;
            control.slotLabels=new TMP_Text[3];control.slotBackgrounds=new Image[3];control.removeButtons=new Button[3];control.cardStates=new TMP_Text[3];
            var font=page.depthLabel.font;
            for(int s=0;s<3;s++)
            {
                var b=Button(grid,"FormationSlot"+s,HeroRoster.Positions[s],font,54+s*210,190,192,76);
                UnityEventTools.AddIntPersistentListener(b.onClick,control.SelectPosition,s);control.slotLabels[s]=b.GetComponentInChildren<TMP_Text>();control.slotBackgrounds[s]=b.GetComponent<Image>();
                var remove=Button(grid,"RemoveSlot"+s,"해제",font,54+s*210,272,192,40);UnityEventTools.AddIntPersistentListener(remove.onClick,control.Remove,s);control.removeButtons[s]=remove;
            }
            control.hint=Text(grid,"CollectionHint","슬롯 선택 → 용사 선택",font,54,324,612,36,18);
            page.pendingBattleLabel=Text(page.menuPanels[0].transform,"PendingBattleChanges","다음 전투부터 적용됩니다",font,54,160,612,26,17);
            page.pendingBattleLabel.color=new Color(.67f,.31f,.12f);
            for(int i=0;i<8;i++)
            {
                var card=grid.Find("HeroCard"+i);Rect((RectTransform)card,54+(i%3)*210,382+(i/3)*232,192,214);
                var surface=card.Find("Surface") as RectTransform;if(surface!=null)Rect(surface,4,4,184,206);
                if(i<3)
                {
                    var b=card.GetComponent<Button>();b.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddIntPersistentListener(b.onClick,control.SelectHero,i);
                    Rect((RectTransform)card.Find("PortraitWell"),10,10,172,100);Rect((RectTransform)card.Find("Portrait"),35,29,122,84);
                    var role=Text(card,"Role",HeroRoster.Roles[i]+" · 추천 "+HeroRoster.Positions[i],font,12,12,168,24,14);
                    Text(card,"HeroName",HeroRoster.Names[i],font,10,112,172,30,21);
                    Rect((RectTransform)card.Find("ActiveStrip"+i),20,146,152,23);
                    control.cardStates[i]=Text(card,"ActiveLabel"+i,"출전 중",font,20,145,152,25,14);control.cardStates[i].color=Color.white;
                    var gear=Button(card,"ManageGear","장비",font,28,174,136,34);UnityEventTools.AddIntPersistentListener(gear.onClick,page.OpenHero,i);
                }
                else
                {
                    Rect((RectTransform)card.Find("Unknown"),16,40,160,90);Rect((RectTransform)card.Find("Locked"),16,157,160,35);
                }
            }
            for(int h=0;h<3;h++){Rect(page.heroDetails[h].rectTransform,54,489,612,90);page.heroDetails[h].fontSize=22;}
            EditorUtility.SetDirty(page);
        }
    }
}
