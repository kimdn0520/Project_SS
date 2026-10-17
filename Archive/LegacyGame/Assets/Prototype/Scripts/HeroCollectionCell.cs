using TMPro;using UnityEngine;using UnityEngine.UI;
namespace ProjectSS.Expedition
{
 public sealed class HeroCollectionCell:MonoBehaviour
 {
  public PlayPage page;public int hero;
  [Tooltip("Visual adjustment after centering the visible portrait pixels. Positive Y moves upward.")]
  public Vector2 portraitOffset;
  public TMP_Text grade,progress,nameLabel;
  public HeroStarGraphic[] stars;
  public Image progressFill,portrait;
  public RectTransform progressClip;
  public void Select(){if(page!=null&&hero>=0)page.formationPanel.SelectHero(hero);}
  public void Refresh()
  {
   if(page==null||page.Model==null)return;var model=page.Model;bool owned=model.IsOwned(hero);var state=model.HeroProgression(hero);
   grade.text=model.Grade(hero).ToString();nameLabel.gameObject.SetActive(false);portrait.color=owned?Color.white:new Color(.35f,.35f,.35f,.55f);
   int count=owned&&state!=null?state.stars:0;
   for(int s=0;s<stars.Length;s++){
    stars[s].gameObject.SetActive(s<count);
    if(s<count){var r=stars[s].rectTransform;r.anchoredPosition=new Vector2(72-count*13+s*26,-128);r.sizeDelta=new Vector2(26,30);}
   }
   int required=model.RequiredHeroCopies(count);
   progress.text=!owned?"미보유":count>=ExpeditionModel.MaxHeroStars?"MAX":$"{state.duplicates}/{required}";
   float fraction=!owned?0:count>=ExpeditionModel.MaxHeroStars?1:Mathf.Clamp01((float)state.duplicates/required);
   if(progressClip!=null){progressClip.gameObject.SetActive(fraction>0);progressClip.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Mathf.Max(0,((RectTransform)progressClip.parent).rect.width-4)*fraction);}
  }
 }
}
