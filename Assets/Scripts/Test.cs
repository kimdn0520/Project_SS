using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private async UniTaskVoid WaitMove()
    {
        await transform.DOMove(new Vector3(0, 3, 0), 3.0f).AsyncWaitForCompletion();
        Debug.Log("XYZ(0, 3, 0) ��ġ�� �̵� �Ϸ�");
    }
}
