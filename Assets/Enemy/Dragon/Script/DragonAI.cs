using UnityEngine;
using System.Collections;

public class DragonAI : MonoBehaviour
{
    private enum DragonState
    {
        Intro,
        Idle,
        Acting,
        Down,
        Dead
    }

    [Header("参照")]
    public Animator animator;
    public DragonHP dragonHP;
    public Transform player;

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

    [Header("アニメーション基本")]
    public float crossFadeTime = 0.12f;
    public float animationFPS = 30f;

    [Header("距離判定")]
    public float closeRange = 5f;
    public float middleRange = 12f;
    public float farRange = 20f;

    [Header("行動間隔")]
    public float minActionInterval = 0.4f;
    public float maxActionInterval = 1.0f;

    [Header("向き調整")]
    public float turnSpeed = 8f;
    public bool alwaysFacePlayerWhileIdle = true;

    [Header("歩き・走り")]
    public float walkSpeed = 2f;
    public float runSpeed = 7f;
    public float chargeSpeed = 12f;
    public float chargeDuration = 1.2f;
    public float chargeStopDistance = 2.5f;

    [Header("ステップ")]
    public float stepDistance = 4f;
    public float stepDuration = 0.45f;

    [Header("ひっかき前進")]
    public float swipeForwardDistance = 1.2f;
    public float swipeForwardTime = 0.18f;
    public float swipeReturnTime = 0.25f;

    [Header("ブレス")]
    public DragonAttackHitbox breathHitbox;
    public int breathStartFrame = 30;
    public int breathEndFrame = 120;
    public float breathDuration = 4.2f;

    [Header("腕攻撃Hitbox")]
    public DragonAttackHitbox leftArmHitbox;
    public DragonAttackHitbox rightArmHitbox;
    public int swipeHitStartFrame = 35;
    public int swipeHitEndFrame = 55;
    public float swipeAnimDuration = 2.0f;

    [Header("尻尾攻撃Hitbox")]
    public DragonAttackHitbox tailHitbox;

    [Header("Tail Slam設定")]
    public float tailSlamDuration = 4.8f;
    public int tailSlamAimStartFrame = 5;
    public int tailSlamAimEndFrame = 31;
    public int tailSlamHitStartFrame = 73;
    public int tailSlamHitEndFrame = 82;
    public int tailSlamReturnStartFrame = 105;
    public int tailSlamReturnEndFrame = 138;
    public float tailSlamAngleOffset = -15f;

    [Header("Tail Swipe設定")]
    public float tailSwipeDuration = 4.6f;
    public int tailSwipeAimStartFrame = 8;
    public int tailSwipeAimEndFrame = 31;
    public int tailSwipeSlamHitStartFrame = 77;
    public int tailSwipeSlamHitEndFrame = 87;
    public int tailSwipeSecondHitStartFrame = 104;
    public int tailSwipeSecondHitEndFrame = 133;
    public float tailSwipeLeftOffset = -35f;
    public float tailSwipeRightOffset = 35f;
    public bool chooseTailSwipeDirectionByPlayerPosition = true;

    [Header("咆哮")]
    public float roarDuration = 2.8f;
    public float roarStaggerRadius = 15f;
    public float roarStaggerTime = 1.0f;
    public LayerMask playerLayer;
    public float roarCameraShakeDuration = 0.5f;
    public float roarCameraShakeStrength = 0.15f;

    [Header("ダウン")]
    public float downDuration = 9.11f;

    [Header("強化フェーズ")]
    public bool isPhase2 = false;
    public float phase2SpeedMultiplier = 1.2f;
    public float phase2ActionIntervalMultiplier = 0.7f;

    private DragonState state = DragonState.Intro;
    private bool isBusy = false;
    private Vector3 spawnForward;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (dragonHP == null) dragonHP = GetComponent<DragonHP>();

        spawnForward = transform.forward;
    }

    void OnEnable()
    {
        if (dragonHP != null)
        {
            dragonHP.OnHalfHP += HandleHalfHP;
            dragonHP.OnDeath += HandleDeath;
            dragonHP.OnCrystalBroken += HandleCrystalBroken;
        }
    }

    void OnDisable()
    {
        if (dragonHP != null)
        {
            dragonHP.OnHalfHP -= HandleHalfHP;
            dragonHP.OnDeath -= HandleDeath;
            dragonHP.OnCrystalBroken -= HandleCrystalBroken;
        }
    }

    void Start()
    {
        StartCoroutine(IntroRoutine());
    }

    void Update()
    {
        if (state == DragonState.Dead || state == DragonState.Down || isBusy) return;

        if (alwaysFacePlayerWhileIdle && player != null)
        {
            FacePlayerSmooth();
        }
    }

    private IEnumerator IntroRoutine()
    {
        state = DragonState.Intro;
        isBusy = true;

        Play(roarAnim);
        DoRoarEffect();

        yield return new WaitForSeconds(roarDuration);

        isBusy = false;
        state = DragonState.Idle;

        StartCoroutine(AILoop());
    }

    private IEnumerator AILoop()
    {
        while (state != DragonState.Dead)
        {
            if (state == DragonState.Down || isBusy || player == null)
            {
                yield return null;
                continue;
            }

            float interval = Random.Range(minActionInterval, maxActionInterval);

            if (isPhase2)
                interval *= phase2ActionIntervalMultiplier;

            yield return new WaitForSeconds(interval);

            if (state == DragonState.Dead || state == DragonState.Down || isBusy)
                continue;

            float distance = Vector3.Distance(transform.position, player.position);

            if (isPhase2)
                yield return ChoosePhase2Action(distance);
            else
                yield return ChoosePhase1Action(distance);
        }
    }

    private IEnumerator ChoosePhase1Action(float distance)
    {
        if (distance >= middleRange)
        {
            int r = Random.Range(0, 2);

            if (r == 0)
                yield return BreathThenCharge();
            else
                yield return BreathAttack();
        }
        else if (distance >= closeRange)
        {
            int r = Random.Range(0, 3);

            if (r == 0)
                yield return StepThenSwipe();
            else if (r == 1)
                yield return StepThenTailSlam();
            else
                yield return TailSlam();
        }
        else
        {
            int r = Random.Range(0, 3);

            if (r == 0)
                yield return SwipeAttack(Random.value > 0.5f);
            else if (r == 1)
                yield return TailSwipe();
            else
                yield return TailSlam();
        }
    }

    private IEnumerator ChoosePhase2Action(float distance)
    {
        if (distance >= middleRange)
        {
            int r = Random.Range(0, 3);

            if (r == 0)
                yield return BreathThenCharge();
            else if (r == 1)
                yield return BreathStepTailSwipeCombo();
            else
                yield return ChargeAttack();
        }
        else if (distance >= closeRange)
        {
            int r = Random.Range(0, 3);

            if (r == 0)
                yield return StepThenSwipeTailCombo();
            else if (r == 1)
                yield return BreathStepTailSwipeCombo();
            else
                yield return TailSwipe();
        }
        else
        {
            int r = Random.Range(0, 3);

            if (r == 0)
                yield return SwipeTailCombo();
            else if (r == 1)
                yield return TailSwipe();
            else
                yield return SwipeAttack(Random.value > 0.5f);
        }
    }

    private IEnumerator BreathThenCharge()
    {
        yield return BreathAttack();
        yield return new WaitForSeconds(0.2f);
        yield return ChargeAttack();
    }

    private IEnumerator StepThenSwipe()
    {
        yield return StepRandom();
        yield return new WaitForSeconds(0.1f);
        yield return SwipeAttack(Random.value > 0.5f);
    }

    private IEnumerator StepThenTailSlam()
    {
        yield return StepRandom();
        yield return new WaitForSeconds(0.1f);
        yield return TailSlam();
    }

    private IEnumerator SwipeTailCombo()
    {
        yield return SwipeAttack(Random.value > 0.5f);
        yield return new WaitForSeconds(0.15f);
        yield return TailSlam();
    }

    private IEnumerator StepThenSwipeTailCombo()
    {
        yield return StepRandom();
        yield return new WaitForSeconds(0.1f);
        yield return SwipeAttack(Random.value > 0.5f);
        yield return new WaitForSeconds(0.15f);
        yield return TailSwipe();
    }

    private IEnumerator BreathStepTailSwipeCombo()
    {
        yield return BreathAttack();
        yield return new WaitForSeconds(0.1f);
        yield return StepRandom();
        yield return new WaitForSeconds(0.1f);
        yield return TailSwipe();
    }

    private IEnumerator BreathAttack()
    {
        isBusy = true;
        state = DragonState.Acting;

        FacePlayerInstant();
        Play(breathAnim);

        float start = FrameToSeconds(breathStartFrame);
        float end = FrameToSeconds(breathEndFrame);

        yield return new WaitForSeconds(start);

        if (breathHitbox != null)
            breathHitbox.EnableHitbox();

        yield return new WaitForSeconds(Mathf.Max(0f, end - start));

        if (breathHitbox != null)
            breathHitbox.DisableHitbox();

        yield return new WaitForSeconds(Mathf.Max(0f, breathDuration - end));

        ReturnToIdle();
    }

    private IEnumerator SwipeAttack(bool left)
    {
        isBusy = true;
        state = DragonState.Acting;

        FacePlayerInstant();

        Vector3 startPos = transform.position;
        Vector3 forwardTarget = startPos + transform.forward * swipeForwardDistance;

        string animName = left ? leftSwipeAnim : rightSwipeAnim;
        DragonAttackHitbox hitbox = left ? leftArmHitbox : rightArmHitbox;

        Play(animName);

        yield return MoveToPosition(forwardTarget, swipeForwardTime);

        float hitStart = FrameToSeconds(swipeHitStartFrame);
        float hitEnd = FrameToSeconds(swipeHitEndFrame);

        yield return new WaitForSeconds(Mathf.Max(0f, hitStart - swipeForwardTime));

        if (hitbox != null)
            hitbox.EnableHitbox();

        yield return new WaitForSeconds(Mathf.Max(0f, hitEnd - hitStart));

        if (hitbox != null)
            hitbox.DisableHitbox();

        yield return MoveToPosition(startPos, swipeReturnTime);

        yield return new WaitForSeconds(Mathf.Max(0f, swipeAnimDuration - hitEnd - swipeReturnTime));

        ReturnToIdle();
    }

    private IEnumerator TailSlam()
    {
        isBusy = true;
        state = DragonState.Acting;

        Quaternion originalRot = transform.rotation;
        Quaternion targetRot = GetRotationToPlayerWithOffset(tailSlamAngleOffset);

        Play(tailSlamAnim);

        StartCoroutine(RotateBetweenFrames(originalRot, targetRot, tailSlamAimStartFrame, tailSlamAimEndFrame));

        float hitStart = FrameToSeconds(tailSlamHitStartFrame);
        float hitEnd = FrameToSeconds(tailSlamHitEndFrame);

        yield return new WaitForSeconds(hitStart);

        if (tailHitbox != null)
            tailHitbox.EnableHitbox();

        yield return new WaitForSeconds(Mathf.Max(0f, hitEnd - hitStart));

        if (tailHitbox != null)
            tailHitbox.DisableHitbox();

        float returnStart = FrameToSeconds(tailSlamReturnStartFrame);
        float waitToReturn = Mathf.Max(0f, returnStart - hitEnd);

        yield return new WaitForSeconds(waitToReturn);

        yield return RotateOverTime(transform.rotation, originalRot, FrameToSeconds(tailSlamReturnEndFrame - tailSlamReturnStartFrame));

        float remaining = Mathf.Max(0f, tailSlamDuration - FrameToSeconds(tailSlamReturnEndFrame));
        yield return new WaitForSeconds(remaining);

        ReturnToIdle();
    }

    private IEnumerator TailSwipe()
    {
        isBusy = true;
        state = DragonState.Acting;

        Quaternion originalRot = transform.rotation;

        float offset = GetTailSwipeOffset();
        Quaternion targetRot = GetRotationToPlayerWithOffset(offset);

        Play(tailSwipeAnim);

        StartCoroutine(RotateBetweenFrames(originalRot, targetRot, tailSwipeAimStartFrame, tailSwipeAimEndFrame));

        float firstHitStart = FrameToSeconds(tailSwipeSlamHitStartFrame);
        float firstHitEnd = FrameToSeconds(tailSwipeSlamHitEndFrame);

        yield return new WaitForSeconds(firstHitStart);

        if (tailHitbox != null)
            tailHitbox.EnableHitbox();

        yield return new WaitForSeconds(Mathf.Max(0f, firstHitEnd - firstHitStart));

        if (tailHitbox != null)
            tailHitbox.DisableHitbox();

        float secondHitStart = FrameToSeconds(tailSwipeSecondHitStartFrame);
        float waitSecond = Mathf.Max(0f, secondHitStart - firstHitEnd);

        yield return new WaitForSeconds(waitSecond);

        if (tailHitbox != null)
            tailHitbox.EnableHitbox();

        Quaternion secondRot = originalRot;
        StartCoroutine(RotateOverTime(transform.rotation, secondRot, FrameToSeconds(tailSwipeSecondHitEndFrame - tailSwipeSecondHitStartFrame)));

        float secondHitEnd = FrameToSeconds(tailSwipeSecondHitEndFrame);
        yield return new WaitForSeconds(Mathf.Max(0f, secondHitEnd - secondHitStart));

        if (tailHitbox != null)
            tailHitbox.DisableHitbox();

        float remaining = Mathf.Max(0f, tailSwipeDuration - secondHitEnd);
        yield return new WaitForSeconds(remaining);

        transform.rotation = originalRot;
        ReturnToIdle();
    }

    private IEnumerator ChargeAttack()
    {
        isBusy = true;
        state = DragonState.Acting;

        FacePlayerInstant();
        Play(runAnim);

        float timer = 0f;
        float speed = isPhase2 ? chargeSpeed * phase2SpeedMultiplier : chargeSpeed;

        while (timer < chargeDuration)
        {
            timer += Time.deltaTime;

            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.position);

                if (distance <= chargeStopDistance)
                    break;

                FacePlayerSmooth();
            }

            transform.position += transform.forward * speed * Time.deltaTime;

            yield return null;
        }

        ReturnToIdle();
    }

    private IEnumerator StepRandom()
    {
        isBusy = true;
        state = DragonState.Acting;

        Play(stepAnim);

        int dir = Random.Range(0, 3);

        Vector3 moveDir;

        if (dir == 0)
            moveDir = -transform.right;
        else if (dir == 1)
            moveDir = transform.right;
        else
            moveDir = -transform.forward;

        Vector3 start = transform.position;
        Vector3 end = start + moveDir.normalized * stepDistance;

        yield return MoveToPosition(end, stepDuration);

        ReturnToIdle();
    }

    private void HandleHalfHP()
    {
        if (state == DragonState.Dead) return;

        isPhase2 = true;
        StopAllCoroutines();
        StartCoroutine(HalfHPRoarRoutine());
    }

    private IEnumerator HalfHPRoarRoutine()
    {
        isBusy = true;
        state = DragonState.Acting;

        Play(roarAnim);
        DoRoarEffect();

        yield return new WaitForSeconds(roarDuration);

        isBusy = false;
        state = DragonState.Idle;

        StartCoroutine(AILoop());
    }

    private void HandleCrystalBroken()
    {
        if (state == DragonState.Dead) return;

        StopAllCoroutines();
        StartCoroutine(DownRoutine());
    }

    private IEnumerator DownRoutine()
    {
        isBusy = true;
        state = DragonState.Down;

        if (breathHitbox != null) breathHitbox.DisableHitbox();
        if (leftArmHitbox != null) leftArmHitbox.DisableHitbox();
        if (rightArmHitbox != null) rightArmHitbox.DisableHitbox();
        if (tailHitbox != null) tailHitbox.DisableHitbox();

        Play(downAnim);

        yield return new WaitForSeconds(downDuration);

        isBusy = false;
        state = DragonState.Idle;

        StartCoroutine(AILoop());
    }

    private void HandleDeath()
    {
        StopAllCoroutines();

        state = DragonState.Dead;
        isBusy = true;

        if (breathHitbox != null) breathHitbox.DisableHitbox();
        if (leftArmHitbox != null) leftArmHitbox.DisableHitbox();
        if (rightArmHitbox != null) rightArmHitbox.DisableHitbox();
        if (tailHitbox != null) tailHitbox.DisableHitbox();

        Play(deathAnim);
    }

    private void DoRoarEffect()
    {
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(roarCameraShakeDuration, roarCameraShakeStrength);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, roarStaggerRadius, playerLayer);

        foreach (Collider hit in hits)
        {
            hit.SendMessage("DragonStagger", roarStaggerTime, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void ReturnToIdle()
    {
        isBusy = false;
        state = DragonState.Idle;
        Play(idleAnim);
    }

    private void Play(string stateName)
    {
        if (animator == null) return;
        animator.CrossFade(stateName, crossFadeTime);
    }

    private float FrameToSeconds(int frame)
    {
        return frame / animationFPS;
    }

    private IEnumerator MoveToPosition(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = duration <= 0f ? 1f : timer / duration;
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        transform.position = target;
    }

    private IEnumerator RotateBetweenFrames(Quaternion from, Quaternion to, int startFrame, int endFrame)
    {
        float start = FrameToSeconds(startFrame);
        float duration = FrameToSeconds(endFrame - startFrame);

        yield return new WaitForSeconds(start);
        yield return RotateOverTime(from, to, duration);
    }

    private IEnumerator RotateOverTime(Quaternion from, Quaternion to, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = duration <= 0f ? 1f : timer / duration;
            transform.rotation = Quaternion.Slerp(from, to, t);
            yield return null;
        }

        transform.rotation = to;
    }

    private void FacePlayerInstant()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.LookRotation(dir);
    }

    private void FacePlayerSmooth()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }

    private Quaternion GetRotationToPlayerWithOffset(float angleOffset)
    {
        if (player == null)
            return transform.rotation;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
            return transform.rotation;

        Quaternion lookRot = Quaternion.LookRotation(dir);
        return lookRot * Quaternion.Euler(0f, angleOffset, 0f);
    }

    private float GetTailSwipeOffset()
    {
        if (!chooseTailSwipeDirectionByPlayerPosition || player == null)
        {
            return Random.value > 0.5f ? tailSwipeLeftOffset : tailSwipeRightOffset;
        }

        Vector3 localPlayerPos = transform.InverseTransformPoint(player.position);

        if (localPlayerPos.x < 0f)
            return tailSwipeLeftOffset;
        else
            return tailSwipeRightOffset;
    }
}