using UnityEngine;
using RuneBreak;
public static class RuneWaitQA
{
    public static void Setup()
    {
        var p=Object.FindFirstObjectByType<RunePlayPage>();p.LoadLevel(0);p.Tap(2,false);p.Tap(1,false);
    }
    public static void Capture()
    {
        var p=Object.FindFirstObjectByType<RunePlayPage>();if(p.ActiveFlights!=0||p.State.Filled!=2)throw new System.Exception("WAIT setup not settled");
        int before=p.State.taps;p.Tap(0,true);if(p.State.taps!=before||p.State.Filled!=2)throw new System.Exception("WAIT consumed a rune");
        ScreenCapture.CaptureScreenshot("Tools/RuneBreak/QA/WaitState.png");Debug.Log("RUNE WAIT PASS: locked taps retain the rune and move count.");
    }
}
