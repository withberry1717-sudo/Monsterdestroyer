using UnityEngine;

public class TitleCameraSway : MonoBehaviour
{
    [Header("見回す揺れ (Rotation)")]
    [Tooltip("左右に首を振る角度")]
    [SerializeField] private float swayAmountX = 3.0f;

    [Tooltip("上下に首を振る角度")]
    [SerializeField] private float swayAmountY = 1.5f;

    [Tooltip("揺れるスピード")]
    [SerializeField] private float swaySpeed = 0.4f;

    private Quaternion startRot;

    void Start()
    {
        // 最初の角度を記憶（インスペクターの X:-8.848 などを基準にする）
        startRot = transform.rotation;
    }

    void Update()
    {
        // Time.unscaledTime を使って、ゲーム停止時でも確実に動かす
        float offsetX = Mathf.Sin(Time.unscaledTime * swaySpeed) * swayAmountX;
        float offsetY = Mathf.Cos(Time.unscaledTime * swaySpeed * 0.8f) * swayAmountY; // 0.8を掛けて上下左右の周期をズラす（自然な揺れになる）

        // X軸回転(上下)、Y軸回転(左右)を初期角度に足し合わせる
        transform.rotation = startRot * Quaternion.Euler(offsetY, offsetX, 0);
    }
}