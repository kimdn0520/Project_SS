using System;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace RuneBreak.Editor
{
    // Shared SDF preset for the gate's single remaining-hit count, using the downloaded font.
    public static class RuneGateNumberAuthoring
    {
        public const string MaterialPath=RuneEnglishAuthoring.Root+"Fredoka-GateNumbers.mat";
        private static Material Preset()
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if(material!=null)return material;
            material=new Material(RuneEnglishAuthoring.Bold.material){name="Fredoka-GateNumbers"};
            Configure(material);AssetDatabase.CreateAsset(material,MaterialPath);return material;
        }
        private static void Configure(Material material)
        {
            material.SetColor("_FaceColor",Color.white);material.SetFloat("_FaceDilate",.10f);
            material.SetColor("_OutlineColor",RuneArtLibrary.Hex("59382f"));material.SetFloat("_OutlineWidth",.31f);material.SetFloat("_OutlineSoftness",0);
            material.EnableKeyword("OUTLINE_ON");material.EnableKeyword("UNDERLAY_ON");material.DisableKeyword("UNDERLAY_INNER");
            material.SetColor("_UnderlayColor",new Color(.16f,.10f,.12f,.72f));material.SetFloat("_UnderlayOffsetX",0);material.SetFloat("_UnderlayOffsetY",-.35f);material.SetFloat("_UnderlayDilate",.10f);material.SetFloat("_UnderlaySoftness",.04f);
            EditorUtility.SetDirty(material);
        }
        public static void Style(TMP_Text label)
        {
            label.font=RuneEnglishAuthoring.Bold;label.fontSharedMaterial=Preset();label.fontStyle=FontStyles.Normal;label.fontWeight=FontWeight.Regular;
            label.fontSize=30;label.enableAutoSizing=false;
            // Leave room for Fredoka's line metrics while centering the visible numeral on the gate.
            var rect=label.rectTransform;float extra=Mathf.Max(0,48-rect.sizeDelta.y);
            rect.sizeDelta+=new Vector2(0,extra);rect.anchoredPosition+=new Vector2(0,extra*(rect.pivot.y-.5f));
            label.alignment=TextAlignmentOptions.MidlineGeoAligned;
            label.color=Color.white;label.enableVertexGradient=true;label.colorGradientPreset=null;
            var top=Color.white;var bottom=RuneArtLibrary.Hex("fff3d9");label.colorGradient=new VertexGradient(top,top,bottom,bottom);
            label.extraPadding=true;label.raycastTarget=false;label.UpdateMeshPadding();
        }
        [MenuItem("Rune Break/Apply gate number style")]
        public static void Apply()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play mode before authoring.");Configure(Preset());
            foreach(string path in new[]{"Assets/Resources/Prefabs/MainPage.prefab","Assets/Resources/Prefabs/PlayPage.prefab"})
            {
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var play=root.GetComponent<RunePlayPage>();if(play!=null)foreach(var label in play.gateCounts)Style(label);
                    var lobby=root.GetComponent<RuneLobbyPage>();if(lobby!=null&&lobby.showcase!=null)Style(lobby.showcase.gateCount);
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();
        }
    }
}
