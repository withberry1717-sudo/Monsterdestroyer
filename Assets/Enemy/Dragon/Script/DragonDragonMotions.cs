using UnityEngine;
using System.Collections;

public class DragonDragonMotion : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Dragon_AllObjectsについているAnimator")]
    public Animator animator;

    [Tooltip("プレイヤー")]
    public Transform player;

    [Header("Model Direction Fix")]
    [Tooltip("ドラゴンの見た目の正面がUnityのForwardとズレている時に調整。0、90、-90、180を試す")]
    public float modelForwardOffsetY = 0f;

    [Tooltip("Y座標を固定する。基本オン")]
    public bool lockYPosition = true;

    [Header("Animation")]
    [Tooltip("アニメーション切り替えの滑らかさ")]
    public float crossFadeTime = 0.12f;

    [Tooltip("FBXアニメーションのFPS")]
    public float animationFPS = 30f;

    [Tooltip("移動アニメーション確認間隔")]
    public float moveAnimCheckInterval = 0.25f;

    [Tooltip("非ループ移動アニメが終わりそうな時に再生し直すタイミング")]
    [Range(0.5f, 0.99f)]
    public float moveAnimRestartNormalizedTime = 0.95f;

    [Header("Animation State Names")]
    public string idleAnim = "Idle_Battle";
    public string walkAnim = "walk";
    public string runAnim = "Running";
    public string roarAnim = "Roar";
    public string breathAnim = "Breath";
    public string leftSwipeAnim = "Arm Swipe_Left";
    public string rightSwipeAnim = "Arm Swipe_Right";
    public string bigHitAnim = "Big hit";
    public string deathAnim = "Death";
    public string downAnim = "Down";
    public string stepAnim = "Step_L_R_B";
    public string tailSlamAnim = "Tail Slam";
    public string tailSwipeAnim = "Tail Swipe";

    [Header("Facing")]
    [Tooltip("待機中にプレイヤーを見る速度")]
    public float idleTurnSpeed = 8f;

    [Tooltip("攻撃前にプレイヤーを見る速度")]
    public float actionTurnSpeed = 6f;

    private float startY;
    private string currentAnimName = "";
    private float currentAnimSpeed = 1f;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        startY = transform.position.y;
    }

    public void SetPlayer(Transform target)
    {
        player = target;
    }

    public void PlayAnim(string stateName, bool force = false)
    {
        if (animator == null) return;
        if (string.IsNullOrEmpty(stateName)) return;

        if (!force && currentAnimName == stateName)
        {
            return;
        }

        animator.CrossFade(stateName, crossFadeTime);
        currentAnimName = stateName;
    }

    public void KeepMoveAnim(string stateName, ref float checkTimer)
    {
        if (animator == null) return;
        if (string.IsNullOrEmpty(stateName)) return;

        if (checkTimer < moveAnimCheckInterval)
        {
            return;
        }

        checkTimer = 0f;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);

        if (!info.IsName(stateName))
        {
            PlayAnim(stateName, true);
            return;
        }

        if (!info.loop && info.normalizedTime >= moveAnimRestartNormalizedTime)
        {
            PlayAnim(stateName, true);
        }
    }

    public void SetAnimatorSpeed(float speed)
    {
        if (animator == null) return;

        currentAnimSpeed = speed;
        animator.speed = speed;
    }

    public void ResetAnimatorSpeed()
    {
        SetAnimatorSpeed(1f);
    }

    public void FacePlayerInstant()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion lookRotation = Quaternion.LookRotation(dir);
        transform.rotation = lookRotation * Quaternion.Euler(0f, modelForwardOffsetY, 0f);
    }

    public void FacePlayerSmooth(float speed)
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion lookRotation = Quaternion.LookRotation(dir);
        Quaternion targetRotation = lookRotation * Quaternion.Euler(0f, modelForwardOffsetY, 0f);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, speed * Time.deltaTime);
    }

    public IEnumerator FacePlayerForSeconds(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            FacePlayerSmooth(actionTurnSpeed);
            yield return null;
        }

        FacePlayerInstant();
    }

    public Vector3 GetDirectionToPlayer()
    {
        if (player == null)
        {
            return GetMoveForward();
        }

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return GetMoveForward();
        }

        return direction.normalized;
    }

    public Vector3 GetMoveForward()
    {
        Vector3 dir = transform.rotation * Quaternion.Euler(0f, -modelForwardOffsetY, 0f) * Vector3.forward;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
        {
            dir = transform.forward;
            dir.y = 0f;
        }

        return dir.normalized;
    }

    public Vector3 GetRandomSideStepDirection()
    {
        Vector3 forward = GetMoveForward();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        return Random.value > 0.5f ? right : -right;
    }

    public void MoveDragon(Vector3 delta)
    {
        transform.position += delta;

        if (lockYPosition)
        {
            Vector3 pos = transform.position;
            pos.y = startY;
            transform.position = pos;
        }
    }

    public IEnumerator MoveInDirectionForSeconds(Vector3 direction, float distance, float duration, string moveAnim)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = -GetMoveForward();
        }

        direction.Normalize();

        PlayAnim(moveAnim, true);

        float timer = 0f;
        float checkTimer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            checkTimer += Time.deltaTime;

            KeepMoveAnim(moveAnim, ref checkTimer);

            float t = Mathf.Clamp01(timer / duration);
            float curve = Mathf.Sin(t * Mathf.PI);
            float speed = distance / Mathf.Max(0.01f, duration);

            MoveDragon(direction * speed * curve * Time.deltaTime);

            yield return null;
        }
    }

    public IEnumerator MoveToPositionForSeconds(Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = transform.position;
        float timer = 0f;

        if (duration <= 0f)
        {
            ApplyPosition(targetPosition);
            yield break;
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector3 newPos = Vector3.Lerp(startPosition, targetPosition, t);
            ApplyPosition(newPos);

            yield return null;
        }

        ApplyPosition(targetPosition);
    }

    private void ApplyPosition(Vector3 pos)
    {
        if (lockYPosition)
        {
            pos.y = startY;
        }

        transform.position = pos;
    }

    public IEnumerator RotateTo(Quaternion targetRotation, float duration)
    {
        Quaternion startRotation = transform.rotation;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(timer / duration);

            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        transform.rotation = targetRotation;
    }

    public Quaternion GetRotationToPlayerWithOffset(float angleOffset)
    {
        if (player == null)
        {
            return transform.rotation;
        }

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
        {
            return transform.rotation;
        }

        Quaternion lookRotation = Quaternion.LookRotation(dir);
        return lookRotation * Quaternion.Euler(0f, modelForwardOffsetY + angleOffset, 0f);
    }

    public Quaternion GetTailRotationToPlayer(float tailFacePlayerOffsetY, float extraOffsetY)
    {
        if (player == null)
        {
            return transform.rotation;
        }

        Vector3 dirToPlayer = player.position - transform.position;
        dirToPlayer.y = 0f;

        if (dirToPlayer.sqrMagnitude < 0.001f)
        {
            return transform.rotation;
        }

        Quaternion tailLookRotation = Quaternion.LookRotation(-dirToPlayer.normalized);
        return tailLookRotation * Quaternion.Euler(0f, modelForwardOffsetY + tailFacePlayerOffsetY + extraOffsetY, 0f);
    }

    public float GetDistanceToPlayer()
    {
        if (player == null) return 999f;

        Vector3 a = transform.position;
        Vector3 b = player.position;

        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    public float FrameToSeconds(int frame)
    {
        return frame / Mathf.Max(1f, animationFPS);
    }

    public float FramesToDuration(int startFrame, int endFrame)
    {
        return Mathf.Max(0f, endFrame - startFrame) / Mathf.Max(1f, animationFPS);
    }
}