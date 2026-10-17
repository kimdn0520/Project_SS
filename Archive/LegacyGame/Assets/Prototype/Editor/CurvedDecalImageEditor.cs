using UnityEditor;

namespace ProjectSS.Expedition.Editor
{
    [CustomEditor(typeof(CurvedDecalImage)), CanEditMultipleObjects]
    public sealed class CurvedDecalImageEditor : UnityEditor.UI.ImageEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("crownRise"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("farEdgeCompression"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
