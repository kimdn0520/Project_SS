using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSS.Expedition.Editor
{
    public static class VerticalPlayLayout
    {
        static Color Panel => new Color(.11f,.18f,.21f);
        static PlayPage page;
        static Vector3 Point(float x,float y)=>new Vector3((x-360)/100,(640-y)/100,0);
        static void Ref(UnityEngine.Object target,string name,UnityEngine.Object value){var so=new SerializedObject(target);so.FindProperty(name).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        static void Vector(UnityEngine.Object target,string name,Vector3 value){var so=new SerializedObject(target);so.FindProperty(name).vector3Value=value;so.ApplyModifiedPropertiesWithoutUndo();}
        static void Rect(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static GameObject New(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return go;}
        static Image Box(string name,Transform parent,float x,float y,float w,float h,Color color)
        {var go=New(name,parent);var im=go.AddComponent<Image>();im.sprite=ExpeditionUIArt.Panel();im.type=Image.Type.Sliced;im.color=color;im.raycastTarget=false;Rect(im.rectTransform,x,y,w,h);return im;}
        static TMP_Text Text(string name,Transform parent,string value,float x,float y,float w,float h,float size,Color color)
        {var go=New(name,parent);var t=go.AddComponent<TextMeshProUGUI>();t.font=page.depthLabel.font;t.text=value;t.fontSize=size;t.color=color;t.raycastTarget=false;t.enableAutoSizing=false;Rect(t.rectTransform,x,y,w,h);return t;}
        static Button Button(string name,Transform parent,string value,float x,float y,float w,float h)
        {var im=Box(name,parent,x,y,w,h,Panel);im.raycastTarget=true;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;var t=Text("Label",im.transform,value,6,4,w-12,h-8,21,Color.white);t.alignment=TextAlignmentOptions.Center;return b;}
        static ScrollRect List(Transform parent,string name,float x,float y,float w,float h,out RectTransform content)
        {
            var im=Box(name,parent,x,y,w,h,new Color(.06f,.11f,.14f));im.raycastTarget=true;im.gameObject.AddComponent<RectMask2D>();
            content=(RectTransform)New("Content",im.transform).transform;Rect(content,0,0,w,0);
            var sc=im.gameObject.AddComponent<ScrollRect>();sc.viewport=im.rectTransform;sc.content=content;sc.horizontal=false;sc.movementType=ScrollRect.MovementType.Clamped;return sc;
        }
        static ExpeditionInventory.Row Row(Transform parent,string name,string title,string detail,Sprite icon,float width,bool selectable)
        {
            var panel=Box(name,parent,0,0,width,118,Panel);
            var row=new ExpeditionInventory.Row{root=panel.rectTransform};
            row.icon=Box("Icon",panel.transform,12,15,80,80,Color.white);row.icon.sprite=icon;row.icon.type=Image.Type.Simple;row.icon.preserveAspect=true;
            row.title=Text("Title",panel.transform,title,108,10,width-120,30,23,Color.white);
            row.detail=Text("Stats",panel.transform,detail,108,43,width-120,32,17,new Color(.48f,.84f,.77f));
            row.count=Text("Count",panel.transform,"",108,84,width-120,27,17,new Color(.96f,.76f,.42f));
            if(selectable){panel.raycastTarget=true;row.button=panel.gameObject.AddComponent<Button>();row.button.targetGraphic=panel;}
            return row;
        }
        static string Detail(GearDefinition gear)=>gear.equipSlot==0?$"공격 {gear.damage:0} · 공격 간격 {gear.interval:0.00}초":gear.description;
        public static void Configure(PlayPage target)
        {
            page=target;ConfigureWorld();ConfigureInventory();DigButtonArt.Configure(page);PolishedMiningArt.Configure(page);
        }
        static void ConfigureInventory()
        {
            var bag=page.menuPanels[1].transform;
            foreach(var b in page.routeButtons){b.transform.SetParent(page.menuPanels[3].transform,false);}
            for(int i=0;i<page.routeButtons.Length;i++)Rect((RectTransform)page.routeButtons[i].transform,80+i*190,730,180,44);
            foreach(var child in bag.Cast<Transform>().ToArray())
                if(child.name!="Title" && child.name!="MenuTitle")UnityEngine.Object.DestroyImmediate(child.gameObject);
            // Existing menu headings are part of the shared page chrome; replace bag content only.
            var background=Box("BagBackground",bag,0,100,720,1180,new Color(.06f,.11f,.14f));background.transform.SetAsFirstSibling();background.raycastTarget=true;
            page.bagTitle=Text("BagTitle",bag,"보유 아이템",26,180,660,32,24,Color.white);
            var back=Button("BackToMine1",bag,"채굴로",566,118,128,48);UnityEventTools.AddIntPersistentListener(back.onClick,page.OpenMenu,0);
            var inventory=bag.GetComponent<ExpeditionInventory>();if(inventory==null)inventory=bag.gameObject.AddComponent<ExpeditionInventory>();
            page.inventoryView=inventory;inventory.page=page;inventory.tabs=new Image[3];
            for(int i=0;i<3;i++){var b=Button("BagFilter"+i,bag,new[]{"전체","무기","재료"}[i],26+i*226,228,216,48);UnityEventTools.AddIntPersistentListener(b.onClick,inventory.SelectFilter,i);inventory.tabs[i]=(Image)b.targetGraphic;}
            inventory.scroll=List(bag,"InventoryViewport",20,294,680,840,out inventory.content);
            inventory.gearRows=new ExpeditionInventory.Row[page.catalog.gear.Length];
            for(int i=0;i<inventory.gearRows.Length;i++){var g=page.catalog.gear[i];inventory.gearRows[i]=Row(inventory.content,"BagItem"+i,g.title,Detail(g),g.icon,680,false);}
            inventory.materialRows=new ExpeditionInventory.Row[3];
            for(int i=0;i<3;i++)inventory.materialRows[i]=Row(inventory.content,"Material"+i,new[]{"철광석","서리 결정","유적 파편"}[i],"채광으로 획득하는 재료",page.depositSprites[i],680,false);
            inventory.empty=Text("Empty",bag,"보유한 아이템이 없습니다",80,530,560,70,24,Color.gray);inventory.empty.alignment=TextAlignmentOptions.Center;
            page.gearCards=Array.Empty<PlayPage.GearCard>();
            if(page.equipmentPopup!=null&&!EditorUtility.IsPersistent(page.equipmentPopup))UnityEngine.Object.DestroyImmediate(page.equipmentPopup.gameObject);
            var go=New("EquipmentSelectionPopup",page.transform);var canvas=go.AddComponent<Canvas>();EditorUtility.CopySerialized(page.notice.Canvas,canvas);
            var scale=go.AddComponent<CanvasScaler>();EditorUtility.CopySerialized(page.notice.Canvas.GetComponent<CanvasScaler>(),scale);
            go.AddComponent<GraphicRaycaster>();var group=go.AddComponent<CanvasGroup>();var popup=go.AddComponent<EquipmentSelectionPopup>();page.equipmentPopup=popup;
            Ref(popup,"canvas",canvas);Ref(popup,"canvasGroup",group);popup.page=page;popup.pausePolicy=page.pausePolicy;
            var dim=Button("Curtain",go.transform,"",0,0,720,1280);((Image)dim.targetGraphic).color=new Color(.02f,.04f,.06f,.9f);Ref(popup,"curtainButton",dim);
            Box("Panel",go.transform,30,230,660,830,Panel).raycastTarget=true;
            popup.title=Text("Title",go.transform,"장비 선택",60,268,495,52,28,new Color(.97f,.78f,.4f));
            var close=Button("CloseEquipment",go.transform,"닫기",568,260,94,48);Ref(popup,"closeButton",close);
            popup.scroll=List(go.transform,"EquipmentList",50,338,620,594,out popup.content);
            popup.rows=new ExpeditionInventory.Row[page.catalog.gear.Length];
            for(int i=0;i<popup.rows.Length;i++){var g=page.catalog.gear[i];var row=Row(popup.content,"SelectEquipment"+i,g.title,Detail(g),g.icon,620,true);popup.rows[i]=row;UnityEventTools.AddIntPersistentListener(row.button.onClick,popup.Choose,i);}
            popup.empty=Text("Empty",go.transform,"장착할 수 있는 보유 장비가 없습니다",90,540,540,100,23,Color.gray);popup.empty.alignment=TextAlignmentOptions.Center;
            popup.unequip=Button("RemoveEquipment",go.transform,"장비 해제",70,960,580,58);UnityEventTools.AddPersistentListener(popup.unequip.onClick,popup.RemoveEquipment);
            go.SetActive(false);
        }
        static SpriteRenderer World(string name,Transform parent,Sprite sprite,Vector3 position,float w,float h,Material material,Color color,int order)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=material;sr.color=color;sr.sortingOrder=order;
            go.transform.localScale=new Vector3(w/sprite.bounds.size.x,h/sprite.bounds.size.y,1);return sr;
        }
        static void ConfigureWorld()
        {
            var world=page.miningWorld;var view=page.miningView;
            var mat=page.blocks[0].sharedMaterial;mat.SetVector("_WorldClipRect",new Vector4(-2.05f,-6.4f,2.05f,1.6f));EditorUtility.SetDirty(mat);
            var old=world.Find("CaveBackdrop");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            World("CaveBackdrop",world,ExpeditionUIArt.Panel(),Point(360,890),3.8f,8.2f,mat,new Color(.08f,.13f,.16f),-20);
            var so=new SerializedObject(view);
            // Detach the reusable fracture and chest overlays from the old horizontal block.
            var fracture=world.Find("VerticalFractures");if(fracture==null){fracture=new GameObject("VerticalFractures").transform;fracture.SetParent(world,false);}
            fracture.position=Point(360,900);
            foreach(string array in new[]{"cracks","crackHighlights"})
            {
                var a=so.FindProperty(array);
                for(int i=0;i<a.arraySize;i++){var line=(LineRenderer)a.GetArrayElementAtIndex(i).objectReferenceValue;line.transform.SetParent(fracture,false);line.transform.localPosition=Vector3.zero;line.transform.localRotation=Quaternion.identity;line.transform.localScale=new Vector3(1.5f,.8f,1);}
            }
            var chest=(SpriteRenderer)so.FindProperty("chestVisual").objectReferenceValue;
            var opening=(GameObject)so.FindProperty("chestOpenRoot").objectReferenceValue;
            chest.transform.SetParent(world,true);chest.transform.position=Point(410,778);
            opening.transform.SetParent(world,true);opening.transform.position=chest.transform.position;
            var positions=so.FindProperty("rockPositions");var scales=so.FindProperty("rockScales");
            for(int i=0;i<3;i++)
            {
                var rock=page.blocks[i];rock.transform.position=Point(360,900+i*155);rock.transform.localScale=new Vector3(3.3f/rock.sprite.bounds.size.x,1.5f/rock.sprite.bounds.size.y,1);rock.color=Color.white;
                positions.GetArrayElementAtIndex(i).vector3Value=rock.transform.localPosition;scales.GetArrayElementAtIndex(i).vector3Value=rock.transform.localScale;
            }
            var actorSo=new SerializedObject(page.miner);var hand=(Transform)actorSo.FindProperty("miningHand").objectReferenceValue;
            // Keep feet planted on the top surface; retain the authored body proportions.
            var body=page.miner.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.sprite!=null&&!r.transform.IsChildOf(hand)&&r.name!="HpAnchor").ToArray();
            float foot=body.Min(r=>r.bounds.min.y);
            var p=page.miner.transform.position;p.x=Point(300,0).x;p.y+=Point(0,843).y-foot;page.miner.transform.position=p;
            hand.position=Point(318,795);hand.rotation=Quaternion.Euler(0,0,-60);
            actorSo.FindProperty("miningHandRest").quaternionValue=hand.localRotation;actorSo.ApplyModifiedPropertiesWithoutUndo();
            var flash=(SpriteRenderer)so.FindProperty("impactFlash").objectReferenceValue;flash.transform.position=Point(415,835);
            so.FindProperty("hitOffset").vector3Value=new Vector3(.55f,.65f,0);
            so.FindProperty("descendingMiner").objectReferenceValue=page.miner.transform;so.FindProperty("minerRest").vector3Value=page.miner.transform.localPosition;so.FindProperty("fractureRoot").objectReferenceValue=fracture;
            var bands=so.FindProperty("shaftBands");var rests=so.FindProperty("bandRest");bands.arraySize=rests.arraySize=4;
            var existing=world.Find("ShaftBands");if(existing!=null)UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var bandRoot=new GameObject("ShaftBands").transform;bandRoot.SetParent(world,false);
            for(int i=0;i<4;i++)
            {
                var band=new GameObject("Strata"+i).transform;band.SetParent(bandRoot,false);band.position=Point(360,485+i*310);
                for(int side=0;side<2;side++)for(int row=0;row<2;row++)
                    World("Wall",band,page.blocks[0].sprite,band.position+new Vector3(side==0?-1.84f:1.84f,-.775f-row*1.55f,0),.36f,1.55f,mat,new Color(.30f,.37f,.37f),-10);
                bands.GetArrayElementAtIndex(i).objectReferenceValue=band;rests.GetArrayElementAtIndex(i).vector3Value=band.localPosition;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
