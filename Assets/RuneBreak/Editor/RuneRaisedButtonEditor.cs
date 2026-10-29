using UnityEditor;
using UnityEditor.UI;

namespace RuneBreak.Editor
{
    [CustomEditor(typeof(RuneRaisedButton)),CanEditMultipleObjects]
    public sealed class RuneRaisedButtonEditor : ButtonEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Block interaction",EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("face"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("restingPosition"));
            var scale=serializedObject.FindProperty("scalePress");EditorGUILayout.PropertyField(scale);
            if(scale.boolValue||scale.hasMultipleDifferentValues)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pressedScale"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pressDuration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("releaseDuration"));
            }
            else EditorGUILayout.PropertyField(serializedObject.FindProperty("pressDepth"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
