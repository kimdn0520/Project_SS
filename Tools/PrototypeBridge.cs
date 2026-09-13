using UnityEditor;
using UnityEngine;
public static class PrototypeBridge
{
    public static void Execute() { AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate); Debug.Log("Prototype sources refreshed"); }
    public static void Build() { try { System.Type.GetType("ProjectSS.Expedition.Editor.ExpeditionBuilder, Assembly-CSharp-Editor").GetMethod("Build").Invoke(null, null); } catch (System.Reflection.TargetInvocationException e) { throw e.InnerException; } }
    public static void Rules() { Invoke("Rules"); }
    public static void Capture() { Invoke("Capture"); }
    public static void Inspect() { Invoke("InspectLive"); }
    public static void Forge() { Click("Menu2"); }
    public static void Heroes() { Click("Menu1"); }
    public static void Settings() { Click("Menu4"); }
    public static void Mine() { FocusGame(); Invoke("CloseMenu"); }
    public static void Journey() { FocusGame(); Invoke("JourneyCheck"); }
    public static void UI() { FocusGame(); Invoke("UiCheck"); }
    public static void FullJourney() { FocusGame(); System.Type.GetType("ProjectSS.Expedition.Editor.PlayJourneyQA, Assembly-CSharp-Editor").GetMethod("Start").Invoke(null,null); }
    public static void Battle() { Click("StartBattle"); }
    public static void Accept() { Click("Accept"); }
    public static void Help() { Click("Help"); }
    public static void Sword() { Click("Craft3"); }
    public static void Hammer() { Click("Craft4"); }
    public static void Frost() { Click("Craft5"); }
    public static void Leech() { Click("Craft6"); }
    public static void Flame() { Click("Craft7"); }
    public static void Armor() { Click("Craft8"); }
    public static void Iron() { Click("Route0"); }
    public static void Crystal() { Click("Route1"); }
    public static void Relic() { Click("Route2"); }
    public static void Shield() { Click("Shield"); }
    public static void Hold() { Invoke("Hold"); }
    public static void Fractures() { FocusGame(); Invoke("Fractures"); }
    public static void Reset() { Click("Reset"); }
    private static void Click(string name) { FocusGame(); Invoke("Click", new object[] { name }); }
    private static void FocusGame()
    {
        var gameType = System.Type.GetType("UnityEditor.GameView, UnityEditor");
        if (gameType != null) EditorWindow.GetWindow(gameType).Focus();
        // Editor-only automation provides the focus signal; popup/lifecycle pauses remain intact.
        if (EditorApplication.isPlaying) SessionPausePolicy.Instance.ReleasePause("AppFocusLoss");
    }
    private static void Invoke(string method, object[] args = null)
    {
        if (method == "Hold") FocusGame();
        try { System.Type.GetType("ProjectSS.Expedition.Editor.ExpeditionValidation, Assembly-CSharp-Editor").GetMethod(method).Invoke(null, args); }
        catch (System.Reflection.TargetInvocationException e) { throw e.InnerException; }
    }
}
