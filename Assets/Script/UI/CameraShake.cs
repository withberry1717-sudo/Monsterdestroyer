using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private float shakeTimeRemaining;
    private float shakePower;

    private void Awake()
    {
        Instance = this;
    }

    public void Shake(float duration, float magnitude)
    {
        shakeTimeRemaining = duration;
        shakePower = magnitude;
    }

    
    private void LateUpdate()
    {
        if (shakeTimeRemaining > 0)
        {
            float x = Random.Range(-1f, 1f) * shakePower;
            float y = Random.Range(-1f, 1f) * shakePower;

            // システムが固定した位置から、無理やり少しだけズラす
            transform.localPosition += new Vector3(x, y, 0);

            shakeTimeRemaining -= Time.deltaTime;
        }
    }
}