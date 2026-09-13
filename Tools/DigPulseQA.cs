using System;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEditor;
using Cysharp.Threading.Tasks;
using ProjectSS.Expedition;
public static class DigPulseQA
{
    public static void Execute() { Run().Forget(); }
    static async UniTaskVoid Run()
    {
        var page = UnityEngine.Object.FindFirstObjectByType<PlayPage>();
        try
        {
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();
            SessionPausePolicy.Instance.ReleasePause("AppFocusLoss");
            page.SelectTab(0);
            var input = page.digButton.transform;
            var face = input.Find("PressableFace");
            var position = input.localPosition;
            var faceRest = face.localPosition;
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 81, button = PointerEventData.InputButton.Left };
            int before = page.Model.Data.excavations;
            ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            float min = float.MaxValue, max = float.MinValue;
            int reversals = 0; float previous = face.localPosition.y, direction = 0;
            for (int i = 0; i < 200; i++)
            {
                await UniTask.Delay(20);
                if (input.localPosition != position || input.localScale != Vector3.one) throw new Exception("Input surface moved");
                float y = face.localPosition.y;
                min = Mathf.Min(min, y); max = Mathf.Max(max, y);
                float next = Mathf.Sign(y - previous);
                if (Mathf.Abs(y - previous) > .1f) { if (direction != 0 && next != direction) reversals++; direction = next; }
                previous = y;
            }
            ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            await UniTask.Delay(250);
            if (page.holdDig.IsPressed || Vector3.Distance(face.localPosition, faceRest) > .01f || face.localScale != Vector3.one) throw new Exception("Release did not restore cap");
            if (reversals < 8 || max-min < 6 || page.Model.Data.excavations <= before) throw new Exception("Missing repeated mining pulse");
            ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.pointerExitHandler);
            await UniTask.Delay(250);
            if (page.holdDig.IsPressed || Vector3.Distance(face.localPosition, faceRest) > .01f) throw new Exception("Exit did not reset");
            ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            page.holdDig.HardCancel();
            if (page.holdDig.IsPressed || face.localPosition != faceRest) throw new Exception("Cancel did not reset");
            string result = $"PASS: real hold mined, cap reversed direction {reversals} times; travel {max-min:F1}px; input root fixed; release/exit/hard cancel reset cap.";
            File.WriteAllText("PrototypeQA/dig-pulse.txt", result); Debug.Log(result);
        }
        catch(Exception e) { Debug.LogException(e); }
        finally { if(page != null) page.holdDig.HardCancel(); }
    }
}
