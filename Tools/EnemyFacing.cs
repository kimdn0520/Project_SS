using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectSS.Expedition;
public static class EnemyFacing
{
 public static string Execute()
 {
  var page=Object.FindFirstObjectByType<PlayPage>();var log="";
  foreach(var enemy in page.enemies){var so=new SerializedObject(enemy);var root=(Transform)so.FindProperty("motionRoot").objectReferenceValue;log+=enemy.name+" root="+root.name+" scale="+root.localScale+" parent="+root.parent.localScale+"\n";var anim=(Animator)so.FindProperty("animator").objectReferenceValue;foreach(var clip in anim.runtimeAnimatorController.animationClips.Distinct())foreach(var binding in AnimationUtility.GetCurveBindings(clip))if(binding.propertyName.Contains("Scale")&&binding.path=="")log+=clip.name+" "+binding.propertyName+"\n";}
  return log;
 }
}
