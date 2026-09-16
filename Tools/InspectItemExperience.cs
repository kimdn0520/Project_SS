using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectSS.Expedition;
public static class InspectItemExperience
{
 public static string Execute()
 {
  var go=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Popups/ExpeditionNoticePopup.prefab");
  var so=new SerializedObject(go.GetComponent<ExpeditionNoticePopup>());
  string result="";
  foreach(var f in new[]{"canvas","canvasGroup","curtainButton","closeButton","titleText","bodyText","actionText"})result+=f+"="+so.FindProperty(f)?.objectReferenceValue+"\n";
  foreach(var b in go.GetComponentsInChildren<Button>(true))result+="Button "+b.name+" parent="+b.transform.parent.name+"\n";
  foreach(var t in go.GetComponentsInChildren<TMP_Text>(true))result+="Text "+t.name+" parent="+t.transform.parent.name+"\n";
  return result;
 }
}
