using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition.Editor
{
    public static class BattleHeaderArt
    {
        public static void Configure(PlayPage page)
        {
            page.Canvas.transform.Find("TopHUD").GetComponent<Image>().enabled=false;
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/Art/UnlitSprite.mat");
            var clip=material.GetVector("_WorldClipRect");clip.w=6.4f;material.SetVector("_WorldClipRect",clip);EditorUtility.SetDirty(material);
            var skies=page.heroes[0].transform.parent.GetComponentsInChildren<SpriteRenderer>(true).Where(s=>s.name=="Sky").ToArray();
            foreach(var sky in skies)
            {
                // Keep its lower edge in place and extend only the sky upward.
                float bottom=sky.bounds.min.y;float top=6.6f;
                sky.transform.localScale=new Vector3(sky.transform.localScale.x,(top-bottom)/sky.sprite.bounds.size.y,1);
                var p=sky.transform.position;p.y=(top+bottom)*.5f;sky.transform.position=p;
            }
            foreach(var text in new[]{page.stageLabel,page.resources})
            {
                string path="Assets/Prototype/Art/HeaderOutline-"+text.name+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat==null){mat=new Material(text.fontSharedMaterial);AssetDatabase.CreateAsset(mat,path);}
                mat.EnableKeyword("OUTLINE_ON");mat.SetColor(ShaderUtilities.ID_OutlineColor,new Color(.025f,.055f,.075f,1));mat.SetFloat(ShaderUtilities.ID_OutlineWidth,.18f);mat.SetFloat(ShaderUtilities.ID_FaceDilate,.25f);
                mat.EnableKeyword("UNDERLAY_ON");mat.SetColor(ShaderUtilities.ID_UnderlayColor,new Color(.025f,.055f,.075f,1));mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX,.5f);mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY,-.5f);mat.SetFloat(ShaderUtilities.ID_UnderlayDilate,.25f);
                text.fontSharedMaterial=mat;text.fontStyle=FontStyles.Bold;text.extraPadding=true;text.UpdateMeshPadding();EditorUtility.SetDirty(mat);
            }
            var parent=page.transform.Find("SidebarCanvas");var old=parent.Find("SkyExtension");
            if(old==null){old=new GameObject("SkyExtension",typeof(RectTransform),typeof(RawImage),typeof(BattleSkyExtension)).transform;old.SetParent(parent,false);}
            old.SetAsFirstSibling();var image=old.GetComponent<RawImage>();var source=skies[0];
            image.texture=source.sprite.texture;image.color=source.color;image.raycastTarget=false;
            // Use the clear sky at the viewport edge for the additional top band. Sampling
            // a whole cloud strip here would stretch clouds vertically on tall phones.
            var rect=source.sprite.rect;
            float skyY=Mathf.Clamp01((6.4f-source.bounds.min.y)/source.bounds.size.y);
            image.uvRect=new Rect((rect.center.x-.5f)/image.texture.width,(rect.y+rect.height*skyY-.5f)/image.texture.height,1f/image.texture.width,1f/image.texture.height);
            var extension=old.GetComponent<BattleSkyExtension>();extension.page=page;extension.sky=image;
        }
    }
}
