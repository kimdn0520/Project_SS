using UnityEngine;
using System.Collections;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    private bool isWaiting = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Stop(float duration)
    {
        if (isWaiting) return;
        Time.timeScale = 0.05f;
        StartCoroutine(Wait(duration));
    }

    private IEnumerator Wait(float duration)
    {
        isWaiting = true;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1.0f;
        isWaiting = false;
    }
}
