using System.Linq;using UnityEditor;using UnityEngine;using UnityEngine.UI;using TMPro;
namespace ProjectSS.Expedition.Editor
{
 public static class HeroCollectionArt
 {
  public const string CardPath="Assets/Resources/Prefabs/UI/HeroCard.prefab";
  static void Rect(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
  static T Element<T>(Transform parent,string name) where T:Component {var t=parent.Find(name);if(t==null){t=new GameObject(name,typeof(RectTransform),typeof(T)).transform;t.SetParent(parent,false);}return t.GetComponent<T>();}
  static TMP_Text Label(Transform parent,string name,string text,TMP_FontAsset font,float x,float y,float w,float h,float size){var t=Element<TextMeshProUGUI>(parent,name);t.font=font;t.text=text;t.fontSize=size;t.enableAutoSizing=false;t.raycastTarget=false;t.color=new Color(.14f,.22f,.27f);t.alignment=TextAlignmentOptions.Center;Rect(t.rectTransform,x,y,w,h);return t;}
  static void CenterPortrait(Image portrait,Vector2 offset=default)
  {
   var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
   try{
    texture.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(portrait.sprite)));
    var pixels=texture.GetPixels32();int left=texture.width,right=-1,bottom=texture.height,top=-1;
    for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>20){left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
    if(right<left)return;
    float scale=Mathf.Min(110f/(right-left+1),112f/(top-bottom+1));
    float centerX=(left+right+1)*.5f,centerY=texture.height-(bottom+top+1)*.5f;
    Rect(portrait.rectTransform,72-centerX*scale,73-centerY*scale,texture.width*scale,texture.height*scale);
    portrait.rectTransform.anchoredPosition+=offset;
    portrait.preserveAspect=false;
   }finally{Object.DestroyImmediate(texture);}
  }
  public static void Configure(PlayPage page)
  {
   var shared=AssetDatabase.LoadAssetAtPath<GameObject>(CardPath);
   if(shared!=null){ConfigureInstances(page,shared);return;}
   var font=page.depthLabel.font;var gradeFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Arial Rounded Bold SDF.asset");var gradeMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/Arial Rounded Bold SDF Outline.mat");
   var progressSprite=AssetDatabase.LoadAllAssetsAtPath("Assets/Textures/UI/Common/Bars/bg_progressbar_navy.png").OfType<Sprite>().First();
   var fillSprite=AssetDatabase.LoadAllAssetsAtPath("Assets/Textures/UI/Common/Bars/progressbar_green.png").OfType<Sprite>().First();
   page.formationPanel.collectionCells=new HeroCollectionCell[3];
   for(int i=0;i<8;i++)
   {
    var card=page.heroGrid.transform.Find("HeroCard"+i);Rect((RectTransform)card,54+(i%4)*156,382+(i/4)*206,144,162);
    var surface=card.Find("Surface") as RectTransform;if(surface!=null)Rect(surface,4,4,136,154);
    foreach(var name in new[]{"PortraitWell","Role","ManageGear","ActiveStrip"+i}){var t=card.Find(name);if(t!=null)t.gameObject.SetActive(false);}
    var grade=Label(card,"Grade",i<3?"E":"?",gradeFont,3,-5,45,56,46);grade.fontSharedMaterial=gradeMat;grade.color=Color.white;grade.alignment=TextAlignmentOptions.TopLeft;
    Image portrait=null;
    if(i<3){portrait=card.Find("Portrait").GetComponent<Image>();CenterPortrait(portrait);}
    else{Rect((RectTransform)card.Find("Unknown"),20,38,104,84);card.Find("Locked").gameObject.SetActive(false);}
    grade.transform.SetAsLastSibling();
    var stars=new HeroStarGraphic[5];
    for(int s=0;s<5;s++){stars[s]=Element<HeroStarGraphic>(card,"Star"+s);Rect(stars[s].rectTransform,59,128,26,30);if(stars[s].GetComponent<CanvasRenderer>()==null)stars[s].gameObject.AddComponent<CanvasRenderer>();stars[s].raycastTarget=false;stars[s].color=Color.white;stars[s].gameObject.SetActive(i<3&&s==0);}
    var bg=Element<Image>(card,"DuplicateBar");Rect(bg.rectTransform,1,166,142,18);bg.sprite=progressSprite;bg.type=Image.Type.Sliced;bg.pixelsPerUnitMultiplier=3;bg.color=Color.white;bg.raycastTarget=false;
    var clip=Element<RectMask2D>(bg.transform,"FillViewport");Rect((RectTransform)clip.transform,2,2,138,14);
    var oldFill=bg.transform.Find("Fill");if(oldFill!=null)oldFill.SetParent(clip.transform,false);
    var fill=Element<Image>(clip.transform,"Fill");Rect(fill.rectTransform,0,0,138,14);fill.sprite=fillSprite;fill.type=Image.Type.Sliced;fill.pixelsPerUnitMultiplier=3;fill.color=Color.white;fill.raycastTarget=false;clip.gameObject.SetActive(false);
    var progress=Label(bg.transform,"Progress",i<3?"0/5":"미발견",font,0,-1,142,20,14);progress.color=Color.white;progress.fontStyle=FontStyles.Bold;progress.transform.SetAsLastSibling();
    var nameLabel=Label(card,"HeroName",i<3?HeroRoster.Names[i]:"미발견",font,0,186,144,28,19);
    nameLabel.gameObject.SetActive(false);
    if(i<3){var cell=card.GetComponent<HeroCollectionCell>()??card.gameObject.AddComponent<HeroCollectionCell>();cell.page=page;cell.hero=i;cell.grade=grade;cell.progress=progress;cell.nameLabel=nameLabel;cell.stars=stars;cell.progressFill=fill;cell.progressClip=(RectTransform)clip.transform;cell.portrait=portrait;page.formationPanel.collectionCells[i]=cell;
     var state=page.formationPanel.cardStates[i];Rect(state.rectTransform,0,215,144,20);state.fontSize=12;state.color=new Color(.15f,.4f,.37f);state.gameObject.SetActive(false);
    }
   }
   page.formationPanel.hint.gameObject.SetActive(false);
   EditorUtility.SetDirty(page);
   var source=Object.Instantiate(page.formationPanel.collectionCells[0].gameObject);
   source.name="HeroCard";
   var template=source.GetComponent<HeroCollectionCell>();template.page=null;template.hero=-1;
   Rect((RectTransform)source.transform,0,0,144,162);
   foreach(var button in source.GetComponentsInChildren<Button>(true))button.onClick=new Button.ButtonClickedEvent();
   UnityEditor.Events.UnityEventTools.AddPersistentListener(source.GetComponent<Button>().onClick,template.Select);
   var unknown=Label(source.transform,"Unknown","?",font,20,38,104,84,64);unknown.gameObject.SetActive(false);
   PrefabUtility.SaveAsPrefabAsset(source,CardPath);Object.DestroyImmediate(source);
   ConfigureInstances(page,AssetDatabase.LoadAssetAtPath<GameObject>(CardPath));
  }
  static void ConfigureInstances(PlayPage page,GameObject prefab)
  {
   var panel=page.formationPanel;panel.collectionCells=new HeroCollectionCell[3];panel.cardStates=new TMP_Text[3];
   for(int i=0;i<8;i++)
   {
    var old=page.heroGrid.transform.Find("HeroCard"+i);
    var card=old!=null&&PrefabUtility.GetCorrespondingObjectFromSource(old.gameObject)==prefab?old.gameObject:null;
    if(card==null){if(old!=null)Object.DestroyImmediate(old.gameObject);card=(GameObject)PrefabUtility.InstantiatePrefab(prefab,page.heroGrid.transform);}
    card.name="HeroCard"+i;Rect((RectTransform)card.transform,54+(i%4)*156,382+(i/4)*206,144,162);
    var cell=card.GetComponent<HeroCollectionCell>();cell.page=page;cell.hero=i<3?i:-1;
    cell.portrait.gameObject.SetActive(i<3);card.transform.Find("Unknown").gameObject.SetActive(i>=3);
    card.GetComponent<Button>().interactable=i<3;cell.grade.text=i<3?"E":"?";
    cell.progress.text=i<3?"0/5":"미발견";
    for(int s=0;s<cell.stars.Length;s++)cell.stars[s].gameObject.SetActive(i<3&&s==0);
    if(i<3){cell.portrait.sprite=AssetDatabase.LoadAllAssetsAtPath("Assets/Textures/UI/Icons/Portraits/Hero_"+i+".png").OfType<Sprite>().First();CenterPortrait(cell.portrait,cell.portraitOffset);panel.collectionCells[i]=cell;panel.cardStates[i]=card.transform.Find("ActiveLabel0").GetComponent<TMP_Text>();}
    PrefabUtility.RecordPrefabInstancePropertyModifications(cell);
   }
   panel.hint.gameObject.SetActive(false);EditorUtility.SetDirty(page);
  }
 }
}
