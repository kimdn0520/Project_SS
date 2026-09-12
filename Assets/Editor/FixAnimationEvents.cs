using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class FixAnimationEvents
{
    [MenuItem("ProjectSS/Fix Animation Events")]
    public static void Execute()
    {
        string searchDir = "Assets/Layer Lab";
        string[] animGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { searchDir });
        int fixedClips = 0;
        int removedEvents = 0;

        foreach (string guid in animGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) continue;

            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            if (events == null || events.Length == 0) continue;

            List<AnimationEvent> validEvents = new List<AnimationEvent>();
            bool hasEmpty = false;

            foreach (var ev in events)
            {
                if (string.IsNullOrWhiteSpace(ev.functionName))
                {
                    hasEmpty = true;
                    removedEvents++;
                }
                else
                {
                    validEvents.Add(ev);
                }
            }

            if (hasEmpty)
            {
                AnimationUtility.SetAnimationEvents(clip, validEvents.ToArray());
                EditorUtility.SetDirty(clip);
                fixedClips++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=green>[FixAnimationEvents] 완료! 수정된 클립: {fixedClips}개, 제거된 빈 이벤트: {removedEvents}개</color>");
    }
}
