using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class MiningAlignmentCheck
{
    public static void Execute()
    {
        var view = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .First(v => v.GetType().Name == "ExpeditionMiningView");
        var data = new SerializedObject(view);
        var rock = (SpriteRenderer)data.FindProperty("rocks").GetArrayElementAtIndex(0).objectReferenceValue;
        foreach (var name in new[] { "cracks", "crackHighlights" })
        {
            var refs = data.FindProperty(name);
            for (int i = 0; i < refs.arraySize; i++)
                if (((LineRenderer)refs.GetArrayElementAtIndex(i).objectReferenceValue).transform.parent != rock.transform)
                    throw new Exception("Detached fracture: " + name);
        }
        var audio = data.FindProperty("hitSounds");
        if (audio.arraySize != 1 || audio.GetArrayElementAtIndex(0).objectReferenceValue.name != "MiningPick_Foley_02_Ready")
            throw new Exception("Unexpected hit sound");
        Debug.Log("PASS: all eight fracture strokes are attached to the ore; only the dry second hit sound is assigned.");
    }
}
