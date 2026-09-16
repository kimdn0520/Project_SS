using UnityEditor;using UnityEngine;using ProjectSS.Expedition;
namespace ProjectSS.ContentEditor
{
 internal sealed class HeroManagementPanel:IContentManagementPanel
 {
  public string Title=>"캐릭터";
  public void Draw(ContentManagementWindow host)
  {
   if(host.draft==null){EditorGUILayout.HelpBox("카탈로그를 선택하세요.",MessageType.Info);return;}
   EditorGUILayout.LabelField("용사 등급",EditorStyles.boldLabel);
   var catalog=host.draft;
   if(catalog.heroes==null||catalog.heroes.Length==0)catalog.heroes=new[]{new HeroDefinition{id="rowen"},new HeroDefinition{id="rin"},new HeroDefinition{id="mira"}};
   EditorGUI.BeginChangeCheck();
   foreach(var hero in catalog.heroes){int index=HeroRoster.Index(hero.id);hero.grade=(HeroGrade)EditorGUILayout.EnumPopup(index>=0?HeroRoster.Names[index]:hero.id,hero.grade);}
   EditorGUILayout.Space();EditorGUILayout.LabelField("성급 승급에 필요한 중복 획득 수",EditorStyles.boldLabel);
   if(catalog.heroStarCosts==null||catalog.heroStarCosts.Length!=4)catalog.heroStarCosts=new[]{5,10,20,40};
   for(int i=0;i<4;i++)catalog.heroStarCosts[i]=Mathf.Max(1,EditorGUILayout.IntField($"{i+1}성 → {i+2}성",catalog.heroStarCosts[i]));
   EditorGUILayout.HelpBox("등급(E~S)과 성급(1~5성)은 별개입니다. 최초 획득은 1성으로 시작하며 이후 중복 획득으로 승급합니다. 5성 이후 중복 수량은 보존됩니다. 뽑기 확률과 전투 보너스는 별도 설정 대상입니다.",MessageType.Info);
   if(EditorGUI.EndChangeCheck())host.Changed();
  }
 }
}
