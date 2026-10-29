using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
namespace RuneBreak.Editor
{
    public static class RuneRewardAuthoring
    {
        private static Sprite Art(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(RuneSurfaceAuthoring.Folder+name+".png");
        public static Sprite Coin=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RuneBreak/Art/BlockPunch/Lobby/Icons/lobby_coin.png");
        public static void StyleHeadline(TMP_Text t)
        {
            const string path=RuneEnglishAuthoring.Root+"Fredoka-RewardTitle.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(RuneEnglishAuthoring.Bold.material){name="Fredoka-RewardTitle"};material.SetFloat("_FaceDilate",.08f);material.SetColor("_OutlineColor",RuneArtLibrary.Hex("39262f"));material.SetFloat("_OutlineWidth",.27f);material.EnableKeyword("OUTLINE_ON");material.EnableKeyword("UNDERLAY_ON");material.SetColor("_UnderlayColor",RuneArtLibrary.Hex("271d2d"));material.SetFloat("_UnderlayOffsetY",-.45f);material.SetFloat("_UnderlayDilate",.12f);AssetDatabase.CreateAsset(material,path);}
            t.font=RuneEnglishAuthoring.Bold;t.fontSharedMaterial=material;t.extraPadding=true;t.UpdateMeshPadding();
        }
        public static RectTransform Wallet(Transform parent,string name,float y,out TMP_Text balance,out RectTransform target)
        {
            var body=Image(name,parent,Art("reward_wallet"),22,y,160,56);var coin=Image("Coin",body.transform,Coin,9,6,40,40);coin.rectTransform.pivot=new Vector2(.5f,.5f);coin.rectTransform.anchoredPosition=new Vector2(29,-26);target=coin.rectTransform;
            balance=Text("Balance",body.transform,"0",52,5,98,42,24);balance.font=RuneEnglishAuthoring.Bold;balance.color=RuneArtLibrary.Hex("fff6dc");balance.enableAutoSizing=true;balance.fontSizeMin=15;balance.fontSizeMax=24;return body.rectTransform;
        }
        public static void Configure(RuneResultPanel p)
        {
            var curtain=p.transform.Find("Curtain").GetComponent<Image>();curtain.color=new Color(.075f,.085f,.14f,.65f);
            var root=p.panel;Rect(root,0,0,540,960);root.GetComponent<Image>().color=Color.clear;root.GetComponent<Image>().raycastTarget=false;
            foreach(string name in new[]{"SurfaceTitle","TopEdge","BottomEdge","TitlePlate"}){var t=root.Find(name);if(t!=null)Object.DestroyImmediate(t.gameObject);}
            p.clearTitle="Clear!";p.nextLabel=p.nextDemoLabel="Next";
            Rect(p.title.rectTransform,25,145,490,160);p.title.text="Clear!";p.title.fontSize=94;p.title.alignment=TextAlignmentOptions.MidlineGeoAligned;p.title.color=RuneArtLibrary.Hex("fff5d9");StyleHeadline(p.title);
            Rect(p.description.rectTransform,40,548,460,90);p.description.name="RewardAmount";p.description.text="+40";p.description.fontSize=54;p.description.alignment=TextAlignmentOptions.Midline;p.description.color=RuneArtLibrary.Hex("fff5dc");StyleHeadline(p.description);
            p.rays=Image("RewardRays",root,Art("reward_rays"),50,245,440,440);p.raysRect=p.rays.rectTransform;p.raysRect.pivot=new Vector2(.5f,.5f);p.raysRect.anchoredPosition=new Vector2(270,-465);p.rays.transform.SetAsFirstSibling();
            p.hero=Image("RewardCoins",root,Art("reward_coins"),110,302,320,250);p.heroRect=p.hero.rectTransform;p.heroRect.pivot=new Vector2(.5f,.5f);p.heroRect.anchoredPosition=new Vector2(270,-427);
            ConfigurePile(p);
            ConfigureActions(p);
            p.walletRect=Wallet(root.parent,"RewardWallet",34,out p.balanceLabel,out p.coinTarget);
            var layout=p.GetComponent<RuneSafeLayout>();layout.pins=new[]{new RuneSafeLayout.Pin{target=p.walletRect,position=p.walletRect.anchoredPosition,top=true}};
            var old=root.parent.Find("RewardCoinPool");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var poolRoot=new GameObject("RewardCoinPool",typeof(RectTransform),typeof(RuneLobbyFlightPool));poolRoot.transform.SetParent(root.parent,false);p.coinRoot=(RectTransform)poolRoot.transform;Rect(p.coinRoot,0,0,540,960);p.coinPool=poolRoot.GetComponent<RuneLobbyFlightPool>();
            var so=new SerializedObject(p.coinPool);so.FindProperty("persistAcrossScenes").boolValue=false;so.FindProperty("useBakedPools").boolValue=true;var pools=so.FindProperty("bakedPools");pools.arraySize=1;var entry=pools.GetArrayElementAtIndex(0);entry.FindPropertyRelative("name").stringValue="RewardCoins";var refs=entry.FindPropertyRelative("instances");refs.arraySize=12;p.coinInstances=new RuneCoinFlight[12];
            for(int i=0;i<12;i++){var img=Image("CoinFlight_"+i,p.coinRoot,Coin,0,0,44,44);img.rectTransform.pivot=new Vector2(.5f,.5f);var f=img.gameObject.AddComponent<RuneCoinFlight>();f.rect=img.rectTransform;f.image=img;f.poolName="RewardCoins";p.coinInstances[i]=f;refs.GetArrayElementAtIndex(i).objectReferenceValue=f;img.gameObject.SetActive(false);}so.ApplyModifiedPropertiesWithoutUndo();
            p.rewardAudio=p.GetComponent<AudioSource>();if(p.rewardAudio==null)p.rewardAudio=p.gameObject.AddComponent<AudioSource>();p.rewardAudio.playOnAwake=false;p.rewardAudio.spatialBlend=0;
            var play=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/PlayPage.prefab").GetComponent<RunePlayPage>();p.coinSound=play.sounds.Length>9?play.sounds[9]:null;
            ConfigurePresentation(p);
        }
        public static void StyleClearTitle(TMP_Text text)
        {
            const string path=RuneEnglishAuthoring.Root+"Fredoka-ClearTitle.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)
            {
                material=new Material(RuneEnglishAuthoring.Bold.material){name="Fredoka-ClearTitle"};material.SetFloat("_FaceDilate",.035f);material.SetColor("_OutlineColor",RuneArtLibrary.Hex("703b15"));material.SetFloat("_OutlineWidth",.19f);material.EnableKeyword("OUTLINE_ON");material.EnableKeyword("UNDERLAY_ON");material.SetColor("_UnderlayColor",RuneArtLibrary.Hex("171326"));material.SetFloat("_UnderlayOffsetY",-.45f);material.SetFloat("_UnderlayDilate",.08f);AssetDatabase.CreateAsset(material,path);
            }
            text.font=RuneEnglishAuthoring.Bold;text.fontSharedMaterial=material;text.fontStyle=FontStyles.Normal;text.fontWeight=FontWeight.Regular;text.extraPadding=true;text.enableAutoSizing=false;text.UpdateMeshPadding();
        }
        public static void ConfigurePresentation(RuneResultPanel p)
        {
            p.transform.Find("Curtain").GetComponent<Image>().color=new Color32(12,13,25,230);
            var old=p.panel.Find("ClearWordmark");var go=old!=null?old.gameObject:new GameObject("ClearWordmark",typeof(RectTransform));go.transform.SetParent(p.panel,false);p.titleGroup=(RectTransform)go.transform;
            Rect(p.titleGroup,270,225,490,174);p.titleGroup.pivot=new Vector2(.5f,.5f);p.titleGroup.localRotation=Quaternion.Euler(0,0,-4);p.titleGroup.localScale=Vector3.one;
            p.title.transform.SetParent(p.titleGroup,false);Rect(p.title.rectTransform,0,0,490,174);p.title.text="Clear!";p.title.fontSize=102;p.title.characterSpacing=.5f;p.title.alignment=TextAlignmentOptions.MidlineGeoAligned;StyleClearTitle(p.title);
            p.title.enableVertexGradient=true;p.title.color=Color.white;var top=RuneArtLibrary.Hex("fffef1");var bottom=RuneArtLibrary.Hex("ffe071");p.title.colorGradientPreset=null;p.title.colorGradient=new VertexGradient(top,top,bottom,bottom);
            p.titleDepth=Text("ResultTitleDepth",p.titleGroup,"Clear!",0,6,490,174,102);p.titleDepth.characterSpacing=.5f;p.titleDepth.color=RuneArtLibrary.Hex("bc7613");StyleClearTitle(p.titleDepth);p.titleDepth.transform.SetAsFirstSibling();
            var fxOld=p.coinRoot.parent.Find("RewardArrivalFX");var fxGo=fxOld!=null?fxOld.gameObject:new GameObject("RewardArrivalFX",typeof(RectTransform),typeof(CanvasRenderer),typeof(RuneFxGraphic));fxGo.transform.SetParent(p.coinRoot.parent,false);
            p.rewardEffects=fxGo.GetComponent<RuneFxGraphic>();p.rewardEffects.raycastTarget=false;p.rewardEffects.rewardFlights=p.coinInstances;Rect(p.rewardEffects.rectTransform,0,0,540,960);
            // All three share one popup Canvas, with no masks or sorting overrides.
            // The moving coin stays in front of the wallet until its centered absorb.
            p.walletRect.SetAsLastSibling();p.rewardEffects.transform.SetAsLastSibling();p.coinRoot.SetAsLastSibling();
            p.swooshSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/RuneBreak/Audio/swoosh.wav");p.coinSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/RuneBreak/Audio/coin_pop.wav");
        }
        public static void ConfigurePlay(RunePlayPage p)
        {
            var wallet=Wallet(p.cabinet.parent,"CoinWallet",34,out p.coinBalance,out _);
            var layout=p.GetComponent<RuneSafeLayout>();layout.pins=layout.pins.Where(pin=>pin.target!=wallet).Concat(new[]{new RuneSafeLayout.Pin{target=wallet,position=wallet.anchoredPosition,top=true}}).ToArray();
            p.gateCracks=new RuneGateDamageGraphic[2];int count=p.gatePreviews.Length/2;
            for(int side=0;side<2;side++)
            {
                Rect(p.gates[side].rectTransform,side==0?64:RuneFlight.SurfaceEastGateLeft-8,side==0?52:92,side==0?352:44,side==0?44:352);
                Rect(p.gateFlashes[side].rectTransform,side==0?72:RuneFlight.SurfaceEastGateLeft,side==0?60:100,side==0?336:28,side==0?28:336);
                Rect(p.gateCounts[side].rectTransform,side==0?218:RuneFlight.SurfaceEastGateCenter-17,side==0?59:251,side==0?44:34,30);
                // Move out first: inserting an earlier sibling at the gate's old index would cover the gate.
                for(int j=0;j<count;j++){var image=p.gatePreviews[side*count+j];Rect(image.rectTransform,side==0?64:RuneFlight.SurfaceEastGateLeft-1,side==0?44:92,side==0?352:44,side==0?44:352);image.color=Color.white;image.transform.SetAsLastSibling();image.transform.SetSiblingIndex(p.gates[side].transform.GetSiblingIndex());}
                var old=p.cabinet.Find("GateCracks_"+side);var go=old!=null?old.gameObject:new GameObject("GateCracks_"+side,typeof(RectTransform),typeof(CanvasRenderer),typeof(RuneGateDamageGraphic));go.transform.SetParent(p.cabinet,false);var cracks=go.GetComponent<RuneGateDamageGraphic>();cracks.vertical=side==1;cracks.raycastTarget=false;Rect((RectTransform)go.transform,side==0?72:RuneFlight.SurfaceEastGateLeft,side==0?60:100,side==0?336:28,side==0?28:336);go.transform.SetAsLastSibling();go.transform.SetSiblingIndex(p.gates[side].transform.GetSiblingIndex()+1);p.gateCracks[side]=cracks;
            }
            for(int i=0;i<p.effects.captionLabels.Length;i++){var r=p.effects.captionRects[i];r.sizeDelta=new Vector2(330,80);Rect(p.effects.captionLabels[i].rectTransform,0,0,330,80);Rect(p.effects.captionShadows[i].rectTransform,2,3,330,80);StyleHeadline(p.effects.captionLabels[i]);p.effects.captionLabels[i].alignment=p.effects.captionShadows[i].alignment=TextAlignmentOptions.MidlineGeoAligned;}
        }
        public static void ConfigureActions(RuneResultPanel p)
        {
            var root=p.panel;
            var claim=root.Find("Claim");if(claim==null){var go=new GameObject("Claim",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(RuneRaisedButton));go.transform.SetParent(root,false);p.claimButton=go.GetComponent<RuneRaisedButton>();Text("Label",go.transform,"Claim",0,0,300,76,30);}else p.claimButton=claim.GetComponent<Button>();
            Rect((RectTransform)p.claimButton.transform,120,660,300,82);RuneSurfaceAuthoring.Button(p.claimButton,"close",300,82,"Claim",30);
            for(int i=p.claimButton.onClick.GetPersistentEventCount()-1;i>=0;i--)UnityEventTools.RemovePersistentListener(p.claimButton.onClick,i);UnityEventTools.AddPersistentListener(p.claimButton.onClick,p.ClaimReward);
            Rect((RectTransform)p.primaryButton.transform,282,690,210,78);RuneSurfaceAuthoring.Button(p.primaryButton,"next",210,78,"Next",27);p.primaryRect=(RectTransform)p.primaryButton.transform;p.primaryFace=(Image)p.primaryButton.targetGraphic;p.primaryLabel=p.primaryButton.GetComponentInChildren<TMP_Text>(true);p.nextSprite=RuneBlockUiAuthoring.LongButtonSprite("green",210,78);p.homeSprite=RuneBlockUiAuthoring.LongButtonSprite("amber",210,78);
            Rect((RectTransform)p.homeButton.transform,48,690,210,78);RuneSurfaceAuthoring.Button(p.homeButton,"home",210,78,"Home",27);
        }
        public static void ConfigurePile(RuneResultPanel p)
        {
            var data=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Tools/RuneBreak/Source/BlockPunch/Surface/reward-motion-manifest.json"));
            p.coinPile=p.hero.GetComponent<RuneRewardPile>();if(p.coinPile==null)p.coinPile=p.hero.gameObject.AddComponent<RuneRewardPile>();var pile=p.coinPile;
            p.hero.enabled=false;pile.pieces=new RuneRewardPile.Piece[8];pile.launchPoints=new RectTransform[7];
            for(int i=-1;i<8;i++)
            {
                string name=i<0?"reward_pile_shadow":"reward_piece_"+i;var d=data[name];var img=Image(i<0?"ContactShadow":"PilePiece_"+i,p.heroRect,Art(name),(float)d["x"],(float)d["y"],(float)d["w"],(float)d["h"]);
                if(i<0){pile.shadow=img;continue;}var r=img.rectTransform;r.pivot=new Vector2(.5f,0);r.anchoredPosition=new Vector2((float)d["x"]+(float)d["w"]*.5f,-(float)d["y"]-(float)d["h"]);
                pile.pieces[i]=new RuneRewardPile.Piece{rect=r,image=img,position=r.anchoredPosition,delay=.045f*i};
                if(i<7){var old=r.Find("CoinOrigin");var go=old!=null?old.gameObject:new GameObject("CoinOrigin",typeof(RectTransform));go.transform.SetParent(r,false);var anchor=(RectTransform)go.transform;anchor.anchorMin=anchor.anchorMax=anchor.pivot=new Vector2(.5f,1);anchor.anchoredPosition=new Vector2(0,-(float)d["h"]*.15f);anchor.sizeDelta=Vector2.zero;pile.launchPoints[i]=anchor;}
            }
            pile.glints=new Image[3];pile.glintRects=new RectTransform[3];var points=new[]{new Vector2(162,48),new Vector2(81,86),new Vector2(266,167)};
            for(int i=0;i<3;i++){var g=Image("CoinGlint_"+i,p.heroRect,Art("reward_glint"),points[i].x-9,points[i].y-9,18,18);g.rectTransform.pivot=new Vector2(.5f,.5f);g.rectTransform.anchoredPosition=new Vector2(points[i].x,-points[i].y);pile.glints[i]=g;pile.glintRects[i]=g.rectTransform;g.color=Color.clear;}
        }
        private static Image Image(string name,Transform parent,Sprite sprite,float x,float y,float w,float h){var old=parent.Find(name);var go=old!=null?old.gameObject:new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);var img=go.GetComponent<Image>();img.sprite=sprite;img.color=Color.white;img.raycastTarget=false;Rect(img.rectTransform,x,y,w,h);return img;}
        private static TMP_Text Text(string name,Transform parent,string value,float x,float y,float w,float h,float size){var old=parent.Find(name);var go=old!=null?old.gameObject:new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var text=go.GetComponent<TMP_Text>();RuneEnglishAuthoring.Style(text);text.text=value;text.fontSize=size;text.alignment=TextAlignmentOptions.MidlineGeoAligned;text.textWrappingMode=TextWrappingModes.NoWrap;Rect(text.rectTransform,x,y,w,h);return text;}
        private static void Rect(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    }
}
