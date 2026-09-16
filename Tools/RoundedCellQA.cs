using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ProjectSS.Expedition;

public static class RoundedCellQA
{
    public static void Execute() { Run().Forget(); }
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static async UniTask Shot(PlayPage page, string name)
    {
        await UniTask.Delay(350, ignoreTimeScale:true);
        await UniTask.WaitForEndOfFrame(page);
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes("PrototypeQA/rounded-" + name + ".png", texture.EncodeToPNG());
        UnityEngine.Object.Destroy(texture);
    }
    static async UniTaskVoid Run()
    {
        var page = UnityEngine.Object.FindFirstObjectByType<PlayPage>();
        try
        {
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();
            page.pausePolicy.ReleasePause("AppFocusLoss");
            var font = page.depthLabel.font;
            Check(font.name == "Arial Rounded Bold SDF", "Rounded font not bound");
            Check(page.depthLabel.fontSharedMaterial.name.Contains("Outline"), "Outline preset not bound");
            Check(page.depthLabel.fontSharedMaterial.GetFloat(ShaderUtilities.ID_OutlineWidth) > 0, "Outline disabled");
            Check(font.HasCharacters("ABC 0123456789 용사 장비 광맥", out uint[] missing, true, true), "Missing font glyphs");
            page.OpenMenu(1); await Shot(page, "heroes");
            foreach (var icon in page.slotIcons)
                Check(icon.transform.parent.GetComponent<Image>().sprite.name == "bg_cell", "Slot skin missing");
            page.OpenHero(0); await Shot(page, "slots");
            page.SelectSlot(0); await UniTask.Delay(350, ignoreTimeScale:true);
            var popup = (EquipmentSelectionPopup)PopupManager.CurrentPopup;
            foreach (var row in popup.rows)
                Check(row.root.GetComponent<Image>().sprite.name == "bg_cell", "Equipment skin missing");
            await Shot(page, "equipment");
            popup.OnClickClose(); await UniTask.Delay(350, ignoreTimeScale:true);
            Check(!PopupManager.IsOpenAny && page.ActiveTab == 1, "Equipment close failed");
            page.OpenMenu(2); await Shot(page, "bag");
            page.OpenMenu(0); page.OpenVeins(); await Shot(page, "veins");
            PopupManager.CurrentPopup.OnEscape(); await UniTask.Delay(350, ignoreTimeScale:true);
            Check(!PopupManager.IsOpenAny, "Vein close failed");
            File.WriteAllText("PrototypeQA/rounded-cell.txt", "PASS: Arial Rounded Bold Outline preset, Korean fallback glyphs, equipment slots/rows, popup close/navigation. Screenshots captured for heroes, slots, equipment, bag and veins.");
        }
        catch (Exception e) { File.WriteAllText("PrototypeQA/rounded-cell.txt", "FAIL: " + e); Debug.LogException(e); }
        finally { PopupManager.Clear(); page.OpenMenu(0); }
    }
}
