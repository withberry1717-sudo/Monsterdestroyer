using UnityEngine;

public class KeepScale : MonoBehaviour
{
    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void LateUpdate()
    {
        transform.localScale = originalScale;
    }
}