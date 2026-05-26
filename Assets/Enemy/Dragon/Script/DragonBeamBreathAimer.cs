using UnityEngine;

public class DragonBeamBreathAimer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("狙うプレイヤー")]
    public Transform player;

    [Header("Prediction")]
    [Tooltip("プレイヤーの何秒先を狙うか")]
    public float predictTime = 0.45f;

    [Tooltip("プレイヤーの進行方向の少し前を狙う距離")]
    public float aimAheadDistance = 1.2f;

    [Tooltip("狙う高さ補正")]
    public float aimHeightOffset = 1.0f;

    [Header("Tracking")]
    [Tooltip("ビームの追尾回転速度")]
    public float trackingTurnSpeed = 5.5f;

    [Tooltip("ビームの正面方向がズレる場合に調整。0, 90, -90, 180を試す")]
    public float beamForwardOffsetY = 0f;

    private Vector3 lastPlayerPosition;
    private bool hasLastPosition = false;

    private void OnEnable()
    {
        if (player != null)
        {
            lastPlayerPosition = player.position;
            hasLastPosition = true;
        }
    }

    public void AimInstant()
    {
        if (player == null) return;

        transform.rotation = GetTargetRotation();
    }

    public void AimSmooth()
    {
        if (player == null) return;

        Quaternion targetRot = GetTargetRotation();
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, trackingTurnSpeed * Time.deltaTime);
    }

    private Quaternion GetTargetRotation()
    {
        Vector3 target = GetPredictedPlayerPosition();
        Vector3 dir = target - transform.position;

        if (dir.sqrMagnitude < 0.001f)
        {
            dir = transform.forward;
        }

        Quaternion lookRot = Quaternion.LookRotation(dir.normalized);
        return lookRot * Quaternion.Euler(0f, beamForwardOffsetY, 0f);
    }

    private Vector3 GetPredictedPlayerPosition()
    {
        Vector3 current = player.position;
        Vector3 velocity = Vector3.zero;

        if (hasLastPosition)
        {
            velocity = (current - lastPlayerPosition) / Mathf.Max(Time.deltaTime, 0.001f);
        }

        lastPlayerPosition = current;
        hasLastPosition = true;

        Vector3 predicted = current + velocity * predictTime;

        Vector3 forward = player.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude > 0.001f)
        {
            predicted += forward.normalized * aimAheadDistance;
        }

        predicted.y += aimHeightOffset;

        return predicted;
    }
}