using UnityEngine;
using System.Collections;
using NaughtyCharacter;

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

    private enum DragonAction
    {
        None,
        Breath,
        Charge,
        Swipe,
        TailSlam,
        TailSwipe,
        BackStepBreath,
        BackStepCharge,
        SideStepSwipe,
        SideStepTailSlam,
        Combo,
        Approach
    }

    [Header("References")]
    [Tooltip("Dragon_AllObjectsについているAnimatorを入れる")]
    public Animator animator;

    [Tooltip("DragonCoreについているDragonHPを入れる")]
    public DragonHP dragonHP;

    [Tooltip("プレイヤー本体のTransformを入れる")]
    public Transform player;

    [Header("Model Direction Fix")]
    [Tooltip("ドラゴンの見た目の正面がUnityのForwardとズレている時に調整する。0、90、-90、180を試す")]
    [SerializeField] private float modelForwardOffsetY = 0f;

    [Tooltip("Y座標のズレを防ぐ。基本はオン")]
    [SerializeField] private bool lockYPosition = true;

    private float startY;

    [Header("Intro")]
    [Tooltip("開始後、咆哮する前に待機する秒数")]
    public float introIdleBeforeRoarTime = 3f;

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

    [Header("Animation")]
    public float crossFadeTime = 0.12f;
    public float animationFPS = 30f;

    [Tooltip("歩き中、アニメが止まって見える時は小さくする")]
    public float walkAnimationRefreshTime = 0.35f;

    [Tooltip("走り中、アニメが止まって見える時は小さくする")]
    public float runAnimationRefreshTime = 0.25f;

    [Header("Range")]
    public float closeRange = 7f;
    public float middleRange = 15f;
    public float farRange = 25f;
    public float approachStopDistance = 8f;

    [Header("Approach")]
    public float walkSpeed = 3.2f;
    public float runChaseSpeed = 6.2f;
    public float switchToRunDistance = 14f;
    public float runChaseStopDistance = 8f;

    [Header("Melee Attack Position")]
    public bool approachBeforeNonBreathAttack = true;
    public float meleeAttackStartDistance = 5.5f;
    public float meleeApproachSpeed = 4.0f;
    public float meleeApproachTurnSpeed = 7f;
    public float meleeApproachTimeout = 4f;

    [Header("Escape Tackle")]
    public bool tackleWhenTargetEscapes = true;
    public float tackleEscapeDistance = 11f;
    public bool useTackleDuringMeleeAttacks = true;

    [Header("Action Interval")]
    public float minActionInterval = 0.35f;
    public float maxActionInterval = 0.9f;

    [Header("Repeat Prevention")]
    public bool preventSameActionRepeat = true;
    public bool avoidLastTwoActions = true;
    public bool allowRepeatIfNoOtherChoice = true;

    private DragonAction lastAction = DragonAction.None;
    private DragonAction secondLastAction = DragonAction.None;

    [Header("Facing")]
    public float idleTurnSpeed = 8f;
    public float actionTurnSpeed = 6f;
    public float facePlayerBeforeActionTime = 0.25f;

    [Header("Charge Attack")]
    public DragonAttackHitbox chargeHitbox;

    [Tooltip("突進の基準速度。距離から突進時間を自動計算する")]
    public float chargeSpeed = 24f;

    [Tooltip("プレイヤーをどれくらい通り過ぎるか")]
    public float chargeOvershootDistance = 4f;

    [Tooltip("突進時間の最小値")]
    public float chargeMinDuration = 0.75f;

    [Tooltip("突進時間の最大値")]
    public float chargeMaxDuration = 1.8f;

    [Tooltip("この距離未満から突進する場合だけ2回バックステップする")]
    public float chargeMinStartDistance = 8f;

    [Tooltip("突進前の溜め時間。長いほど分かりやすい")]
    public float chargeTellTime = 1.0f;

    [Tooltip("溜め中のRunningアニメーション速度。小さいほどスロー")]
    public float chargeTellAnimationSpeed = 0.18f;

    [Tooltip("溜め中にRunningを再生し直す間隔")]
    public float chargeTellAnimationRefreshTime = 0.25f;

    [Tooltip("溜め完了後、突進前に一瞬止める時間")]
    public float chargeReadyPauseTime = 0.15f;

    [Tooltip("突進後の硬直時間")]
    public float chargeRecoveryTime = 0.25f;

    [Tooltip("近距離突進前のバックステップ距離")]
    public float closeChargeBackStepDistance = 5f;

    [Tooltip("近距離突進前のバックステップ1回分の時間")]
    public float closeChargeBackStepDuration = 0.38f;

    [Tooltip("近距離突進前のバックステップ回数")]
    public int closeChargeBackStepCount = 2;

    [Tooltip("突進の加速割合")]
    [Range(0.01f, 0.5f)] public float chargeAccelerationRatio = 0.18f;

    [Tooltip("突進の減速割合")]
    [Range(0.01f, 0.5f)] public float chargeDecelerationRatio = 0.22f;

    [Tooltip("突進攻撃を使う")]
    public bool useChargeAsAttack = true;

    [Header("Charge Particles")]
    public ParticleSystem chargeHoldParticle;
    public ParticleSystem chargeReadyParticle;
    public ParticleSystem chargeRunParticle;

    [Header("Step")]
    public float sideStepDistance = 2f;
    public float backStepDistance = 3f;
    public float stepDuration = 0.45f;

    [Tooltip("バックステップ後にブレスを使う確率")]
    [Range(0f, 1f)] public float afterBackStepBreathChance = 0.75f;

    [Tooltip("バックステップ後に突進を使う確率。低め推奨")]
    [Range(0f, 1f)] public float afterBackStepChargeChance = 0.12f;

    [Header("Swipe Forward And Return")]
    public float swipeForwardDistance = 2.0f;
    public float swipeForwardTime = 0.22f;
    public float swipeReturnTime = 0.25f;
    public bool swipeReturnToStartPosition = true;
    public bool swipeLungeTowardPlayer = true;

    [Header("Breath")]
    public DragonAttackHitbox breathHitbox;
    public int breathStartFrame = 30;
    public int breathEndFrame = 120;
    public float breathDuration = 4.2f;
    public float breathTurnTime = 0.35f;

    [Header("Arm Hitboxes")]
    public DragonAttackHitbox leftArmHitbox;
    public DragonAttackHitbox rightArmHitbox;
    public int swipeHitStartFrame = 35;
    public int swipeHitEndFrame = 55;
    public float swipeAnimDuration = 2.0f;

    [Header("Tail Hitbox")]
    public DragonAttackHitbox tailHitbox;

    [Header("Tail Rotation Control")]
    public bool tailAttacksTurnTailToPlayer = true;
    public float tailFacePlayerOffsetY = 0f;
    public float tailSlamAttackOffsetY = -35f;
    public float tailSwipeLeftAttackOffsetY = -55f;
    public float tailSwipeRightAttackOffsetY = 15f;
    public float tailTrackingTurnSpeed = 12f;

    [Header("Tail Slam")]
    public float tailSlamDuration = 4.8f;
    public int tailSlamAimStartFrame = 5;
    public int tailSlamTrackUntilFrame = 73;
    public int tailSlamHitStartFrame = 73;
    public int tailSlamHitEndFrame = 82;
    public int tailSlamReturnStartFrame = 105;
    public int tailSlamReturnEndFrame = 138;
    public float tailSlamAngleOffset = -15f;

    [Header("Tail Swipe")]
    public float tailSwipeDuration = 4.6f;
    public int tailSwipeAimStartFrame = 8;
    public int tailSwipeTrackUntilFrame = 77;
    public int tailSwipeSlamHitStartFrame = 77;
    public int tailSwipeSlamHitEndFrame = 87;
    public int tailSwipeSecondHitStartFrame = 104;
    public int tailSwipeSecondHitEndFrame = 133;
    public bool chooseTailSwipeDirectionByPlayerPosition = true;

    [Header("Roar")]
    public float roarDuration = 2.8f;
    public float roarStaggerRadius = 15f;
    public float roarStaggerTime = 1.0f;
    public LayerMask playerLayer;
    public float roarCameraShakeDuration = 0.5f;
    public float roarCameraShakeStrength = 0.15f;

    [Header("Down")]
    public float downDuration = 9.11f;

    [Header("Phase 2")]
    public bool isPhase2 = false;
    public float phase2SpeedMultiplier = 1.15f;
    public float phase2ActionIntervalMultiplier = 0.7f;
    [Range(0f, 1f)] public float phase2ComboChance = 0.6f;

    private DragonState state = DragonState.Intro;
    private bool isBusy = false;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (dragonHP == null)
        {
            dragonHP = GetComponent<DragonHP>();
        }
    }

    private void OnEnable()
    {
        if (dragonHP != null)
        {
            dragonHP.OnHalfHP += HandleHalfHP;
            dragonHP.OnDeath += HandleDeath;
            dragonHP.OnCrystalBroken += HandleCrystalBroken;
        }
    }

    private void OnDisable()
    {
        if (dragonHP != null)
        {
            dragonHP.OnHalfHP -= HandleHalfHP;
            dragonHP.OnDeath -= HandleDeath;
            dragonHP.OnCrystalBroken -= HandleCrystalBroken;
        }
    }

    private void Start()
    {
        startY = transform.position.y;
        DisableAllHitboxes();
        StopAllChargeParticles();
        SetAnimatorSpeed(1f);
        StartCoroutine(IntroRoutine());
    }

    private void Update()
    {
        if (state == DragonState.Dead) return;
        if (state == DragonState.Down) return;
        if (isBusy) return;

        FacePlayerSmooth(idleTurnSpeed);
    }

    private IEnumerator IntroRoutine()
    {
        state = DragonState.Intro;
        isBusy = true;

        Play(idleAnim);

        float idleTimer = 0f;

        while (idleTimer < introIdleBeforeRoarTime)
        {
            idleTimer += Time.deltaTime;
            FacePlayerSmooth(idleTurnSpeed);
            yield return null;
        }

        yield return FacePlayerForSeconds(0.25f);

        Play(roarAnim);
        DoRoarEffect();

        yield return new WaitForSeconds(roarDuration);

        ReturnToIdle();
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
            {
                interval *= phase2ActionIntervalMultiplier;
            }

            yield return new WaitForSeconds(interval);

            if (state == DragonState.Down || state == DragonState.Dead || isBusy)
            {
                continue;
            }

            float distance = GetDistanceToPlayer();

            yield return DecideAction(distance);
        }
    }

    private IEnumerator DecideAction(float distance)
    {
        if (player == null) yield break;

        if (distance > farRange)
        {
            RegisterAction(DragonAction.Approach);
            yield return ApproachPlayer();
            yield break;
        }

        if (distance >= middleRange)
        {
            yield return ExecuteAction(PickFarAction());
            yield break;
        }

        if (distance >= closeRange)
        {
            yield return ExecuteAction(PickMiddleAction());
            yield break;
        }

        yield return ExecuteAction(PickCloseAction());
    }

    private DragonAction PickFarAction()
    {
        DragonAction[] actions = useChargeAsAttack
            ? new DragonAction[]
            {
                DragonAction.Breath,
                DragonAction.Breath,
                DragonAction.Breath,
                DragonAction.Approach,
                DragonAction.Charge
            }
            : new DragonAction[]
            {
                DragonAction.Breath,
                DragonAction.Breath,
                DragonAction.Breath,
                DragonAction.Approach
            };

        return PickActionWithoutRepeat(actions);
    }

    private DragonAction PickMiddleAction()
    {
        DragonAction[] actions = useChargeAsAttack
            ? new DragonAction[]
            {
                DragonAction.Swipe,
                DragonAction.Swipe,
                DragonAction.TailSlam,
                DragonAction.TailSwipe,
                DragonAction.SideStepSwipe,
                DragonAction.SideStepTailSlam,
                DragonAction.BackStepBreath,
                DragonAction.Breath,
                DragonAction.Charge
            }
            : new DragonAction[]
            {
                DragonAction.Swipe,
                DragonAction.Swipe,
                DragonAction.TailSlam,
                DragonAction.TailSwipe,
                DragonAction.SideStepSwipe,
                DragonAction.SideStepTailSlam,
                DragonAction.BackStepBreath,
                DragonAction.Breath
            };

        return PickActionWithoutRepeat(actions);
    }

    private DragonAction PickCloseAction()
    {
        DragonAction[] actions;

        if (isPhase2 && Random.value < phase2ComboChance)
        {
            actions = useChargeAsAttack
                ? new DragonAction[]
                {
                    DragonAction.Combo,
                    DragonAction.Combo,
                    DragonAction.Swipe,
                    DragonAction.TailSwipe,
                    DragonAction.TailSlam,
                    DragonAction.BackStepBreath,
                    DragonAction.BackStepCharge
                }
                : new DragonAction[]
                {
                    DragonAction.Combo,
                    DragonAction.Combo,
                    DragonAction.Swipe,
                    DragonAction.TailSwipe,
                    DragonAction.TailSlam,
                    DragonAction.BackStepBreath
                };
        }
        else if (useChargeAsAttack)
        {
            actions = new DragonAction[]
            {
                DragonAction.Swipe,
                DragonAction.Swipe,
                DragonAction.TailSlam,
                DragonAction.TailSwipe,
                DragonAction.SideStepSwipe,
                DragonAction.SideStepTailSlam,
                DragonAction.BackStepBreath,
                DragonAction.Breath,
                DragonAction.BackStepCharge
            };
        }
        else
        {
            actions = new DragonAction[]
            {
                DragonAction.Swipe,
                DragonAction.Swipe,
                DragonAction.TailSlam,
                DragonAction.TailSwipe,
                DragonAction.SideStepSwipe,
                DragonAction.SideStepTailSlam,
                DragonAction.BackStepBreath,
                DragonAction.Breath
            };
        }

        return PickActionWithoutRepeat(actions);
    }

    private DragonAction PickActionWithoutRepeat(DragonAction[] candidates)
    {
        if (candidates == null || candidates.Length == 0)
        {
            return DragonAction.Swipe;
        }

        for (int i = 0; i < 25; i++)
        {
            DragonAction picked = candidates[Random.Range(0, candidates.Length)];

            if (!preventSameActionRepeat)
            {
                return picked;
            }

            if (picked == lastAction)
            {
                continue;
            }

            if (avoidLastTwoActions && picked == secondLastAction)
            {
                continue;
            }

            return picked;
        }

        if (allowRepeatIfNoOtherChoice)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != lastAction)
                {
                    return candidates[i];
                }
            }
        }

        return candidates[Random.Range(0, candidates.Length)];
    }

    private IEnumerator ExecuteAction(DragonAction action)
    {
        RegisterAction(action);

        switch (action)
        {
            case DragonAction.Breath:
                yield return BreathAttack();
                break;
            case DragonAction.Charge:
                yield return ChargeAttack(false);
                break;
            case DragonAction.BackStepCharge:
                yield return ChargeAttack(true);
                break;
            case DragonAction.Swipe:
                yield return SwipeAttack(Random.value > 0.5f);
                break;
            case DragonAction.TailSlam:
                yield return TailSlam();
                break;
            case DragonAction.TailSwipe:
                yield return TailSwipe();
                break;
            case DragonAction.BackStepBreath:
                yield return BackStepThenMaybeBreath();
                break;
            case DragonAction.SideStepSwipe:
                yield return SideStepThenSwipe();
                break;
            case DragonAction.SideStepTailSlam:
                yield return SideStepThenTailSlam();
                break;
            case DragonAction.Combo:
                yield return RandomCombo();
                break;
            case DragonAction.Approach:
                yield return ApproachPlayer();
                break;
            default:
                yield return SwipeAttack(Random.value > 0.5f);
                break;
        }
    }

    private void RegisterAction(DragonAction action)
    {
        secondLastAction = lastAction;
        lastAction = action;
    }

    private IEnumerator RandomCombo()
    {
        int combo = useChargeAsAttack ? Random.Range(0, 5) : Random.Range(0, 4);

        if (combo == 0)
            yield return SwipeTailCombo();
        else if (combo == 1)
            yield return SwipeThenTailSwipeCombo();
        else if (combo == 2)
            yield return TailSlamThenSwipeCombo();
        else if (combo == 3)
            yield return BackStepThenMaybeBreath();
        else
            yield return ChargeAttack(true);
    }

    private IEnumerator ApproachPlayer()
    {
        isBusy = true;
        state = DragonState.Acting;

        DisableAllHitboxes();

        bool running = GetDistanceToPlayer() >= switchToRunDistance;
        Play(running ? runAnim : walkAnim);

        float refreshTimer = 0f;

        while (player != null && GetDistanceToPlayer() > approachStopDistance)
        {
            float distance = GetDistanceToPlayer();

            if (distance >= switchToRunDistance && !running)
            {
                running = true;
                Play(runAnim);
                refreshTimer = 0f;
            }
            else if (running && distance <= runChaseStopDistance)
            {
                running = false;
                Play(walkAnim);
                refreshTimer = 0f;
            }

            refreshTimer += Time.deltaTime;

            float refreshTime = running ? runAnimationRefreshTime : walkAnimationRefreshTime;

            if (refreshTimer >= refreshTime)
            {
                Play(running ? runAnim : walkAnim);
                refreshTimer = 0f;
            }

            FacePlayerSmooth(actionTurnSpeed);

            Vector3 forward = GetMoveForward();
            float speed = running ? runChaseSpeed : walkSpeed;

            MoveDragon(forward * speed * Time.deltaTime);

            yield return null;
        }

        ReturnToIdle();
    }

    private IEnumerator ApproachMeleeRange()
    {
        if (!approachBeforeNonBreathAttack) yield break;
        if (player == null) yield break;

        float timer = 0f;
        float refreshTimer = 0f;

        bool running = GetDistanceToPlayer() >= switchToRunDistance;
        Play(running ? runAnim : walkAnim);

        while (player != null && GetDistanceToPlayer() > meleeAttackStartDistance)
        {
            timer += Time.deltaTime;
            refreshTimer += Time.deltaTime;

            if (timer > meleeApproachTimeout)
            {
                break;
            }

            float distance = GetDistanceToPlayer();

            if (distance >= switchToRunDistance && !running)
            {
                running = true;
                Play(runAnim);
                refreshTimer = 0f;
            }
            else if (running && distance <= runChaseStopDistance)
            {
                running = false;
                Play(walkAnim);
                refreshTimer = 0f;
            }

            float refreshTime = running ? runAnimationRefreshTime : walkAnimationRefreshTime;

            if (refreshTimer >= refreshTime)
            {
                Play(running ? runAnim : walkAnim);
                refreshTimer = 0f;
            }

            FacePlayerSmooth(meleeApproachTurnSpeed);

            Vector3 forward = GetMoveForward();
            float speed = running ? runChaseSpeed : meleeApproachSpeed;

            MoveDragon(forward * speed * Time.deltaTime);

            yield return null;
        }

        yield return FacePlayerForSeconds(0.15f);
    }

    private bool ShouldTackleBecauseTargetEscaped()
    {
        if (!useChargeAsAttack) return false;
        if (!tackleWhenTargetEscapes) return false;
        if (!useTackleDuringMeleeAttacks) return false;
        if (player == null) return false;

        return GetDistanceToPlayer() >= tackleEscapeDistance;
    }

    private IEnumerator BackStepThenMaybeBreath()
    {
        float r = Random.value;

        if (useChargeAsAttack && r < afterBackStepChargeChance)
        {
            yield return ChargeAttack(true);
            yield break;
        }

        yield return StepAction(-GetMoveForward());

        if (r < afterBackStepChargeChance + afterBackStepBreathChance)
        {
            yield return new WaitForSeconds(0.12f);
            yield return BreathAttack();
        }
    }

    private IEnumerator DoubleBackStepForCloseCharge()
    {
        int count = Mathf.Max(1, closeChargeBackStepCount);

        for (int i = 0; i < count; i++)
        {
            yield return FacePlayerForSeconds(0.08f);

            Play(stepAnim);

            Vector3 backDirection = -GetMoveForward();

            yield return MoveInDirectionForSeconds(
                backDirection,
                closeChargeBackStepDistance,
                closeChargeBackStepDuration
            );

            yield return new WaitForSeconds(0.08f);
        }
    }

    private IEnumerator SideStepThenSwipe()
    {
        yield return StepAction(GetRandomSideStepDirection());
        yield return new WaitForSeconds(Random.Range(0.05f, 0.18f));
        yield return SwipeAttack(Random.value > 0.5f);
    }

    private IEnumerator SideStepThenTailSlam()
    {
        yield return StepAction(GetRandomSideStepDirection());
        yield return new WaitForSeconds(Random.Range(0.05f, 0.18f));
        yield return TailSlam();
    }

    private IEnumerator SwipeTailCombo()
    {
        yield return SwipeAttack(Random.value > 0.5f);
        yield return new WaitForSeconds(Random.Range(0.08f, 0.18f));
        yield return TailSlam();
    }

    private IEnumerator SwipeThenTailSwipeCombo()
    {
        yield return SwipeAttack(Random.value > 0.5f);
        yield return new WaitForSeconds(Random.Range(0.08f, 0.18f));
        yield return TailSwipe();
    }

    private IEnumerator TailSlamThenSwipeCombo()
    {
        yield return TailSlam();
        yield return new WaitForSeconds(Random.Range(0.08f, 0.18f));
        yield return SwipeAttack(Random.value > 0.5f);
    }

    private IEnumerator BreathAttack()
    {
        isBusy = true;
        state = DragonState.Acting;

        DisableAllHitboxes();

        yield return FacePlayerForSeconds(breathTurnTime);

        Play(breathAnim);

        float start = FrameToSeconds(breathStartFrame);
        float end = FrameToSeconds(breathEndFrame);

        yield return new WaitForSeconds(start);

        if (breathHitbox != null)
        {
            breathHitbox.EnableHitbox();
        }

        yield return new WaitForSeconds(Mathf.Max(0f, end - start));

        if (breathHitbox != null)
        {
            breathHitbox.DisableHitbox();
        }

        yield return new WaitForSeconds(Mathf.Max(0f, breathDuration - end));

        ReturnToIdle();
    }

    private IEnumerator ChargeAttack(bool forceCloseBackStep)
    {
        isBusy = true;
        state = DragonState.Acting;

        DisableAllHitboxes();
        StopAllChargeParticles();
        SetAnimatorSpeed(1f);

        float distance = GetDistanceToPlayer();

        if (forceCloseBackStep || distance < chargeMinStartDistance)
        {
            yield return DoubleBackStepForCloseCharge();
        }

        yield return FacePlayerForSeconds(0.15f);

        Play(runAnim);

        if (chargeHoldParticle != null)
        {
            chargeHoldParticle.Play();
        }

        float tellTimer = 0f;
        float tellRefreshTimer = 0f;

        while (tellTimer < chargeTellTime)
        {
            tellTimer += Time.deltaTime;
            tellRefreshTimer += Time.deltaTime;

            SetAnimatorSpeed(chargeTellAnimationSpeed);

            if (tellRefreshTimer >= chargeTellAnimationRefreshTime)
            {
                Play(runAnim);
                tellRefreshTimer = 0f;
            }

            FacePlayerSmooth(actionTurnSpeed);
            yield return null;
        }

        if (chargeHoldParticle != null)
        {
            chargeHoldParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (chargeReadyParticle != null)
        {
            chargeReadyParticle.Play();
        }

        SetAnimatorSpeed(1f);
        Play(runAnim);

        yield return new WaitForSeconds(chargeReadyPauseTime);

        Vector3 chargeDirection = GetDirectionToPlayer();
        float targetDistance = GetChargeTargetDistance(chargeDirection);
        float baseSpeed = isPhase2 ? chargeSpeed * phase2SpeedMultiplier : chargeSpeed;
        float chargeTime = Mathf.Clamp(targetDistance / Mathf.Max(0.01f, baseSpeed), chargeMinDuration, chargeMaxDuration);

        float timer = 0f;
        float runRefreshTimer = 0f;
        float previousDistance = 0f;

        if (chargeRunParticle != null)
        {
            chargeRunParticle.Play();
        }

        if (chargeHitbox != null)
        {
            chargeHitbox.EnableHitbox();
        }

        Play(runAnim);
        SetAnimatorSpeed(1f);

        while (timer < chargeTime)
        {
            timer += Time.deltaTime;
            runRefreshTimer += Time.deltaTime;

            if (runRefreshTimer >= runAnimationRefreshTime)
            {
                Play(runAnim);
                runRefreshTimer = 0f;
            }

            SetAnimatorSpeed(1f);

            float t = Mathf.Clamp01(timer / chargeTime);
            float eased = EvaluateChargeMoveCurve(t);
            float currentDistance = targetDistance * eased;
            float deltaDistance = currentDistance - previousDistance;
            previousDistance = currentDistance;

            MoveDragon(chargeDirection * deltaDistance);

            yield return null;
        }

        if (chargeHitbox != null)
        {
            chargeHitbox.DisableHitbox();
        }

        StopAllChargeParticles();
        SetAnimatorSpeed(1f);

        yield return new WaitForSeconds(chargeRecoveryTime);

        ReturnToIdle();
    }

    private float GetChargeTargetDistance(Vector3 chargeDirection)
    {
        if (player == null)
        {
            return chargeSpeed;
        }

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        float distanceToPlayer = Mathf.Max(0f, Vector3.Dot(toPlayer, chargeDirection.normalized));
        return distanceToPlayer + chargeOvershootDistance;
    }

    private float EvaluateChargeMoveCurve(float t)
    {
        t = Mathf.Clamp01(t);

        float accelEnd = Mathf.Clamp01(chargeAccelerationRatio);
        float decelStart = Mathf.Clamp01(1f - chargeDecelerationRatio);

        if (t < accelEnd)
        {
            float local = t / Mathf.Max(0.001f, accelEnd);
            float eased = Mathf.SmoothStep(0f, 1f, local);
            return t * Mathf.Lerp(0.35f, 1f, eased);
        }

        if (t > decelStart)
        {
            float local = (t - decelStart) / Mathf.Max(0.001f, 1f - decelStart);
            float eased = Mathf.SmoothStep(0f, 1f, local);
            float linear = t;
            float slowed = Mathf.Lerp(linear, 1f, eased);
            return slowed;
        }

        return t;
    }

    private IEnumerator SwipeAttack(bool left)
    {
        isBusy = true;
        state = DragonState.Acting;

        DisableAllHitboxes();

        yield return ApproachMeleeRange();

        if (ShouldTackleBecauseTargetEscaped())
        {
            yield return ChargeAttack(false);
            yield break;
        }

        yield return FacePlayerForSeconds(facePlayerBeforeActionTime);

        if (ShouldTackleBecauseTargetEscaped())
        {
            yield return ChargeAttack(false);
            yield break;
        }

        Vector3 startPosition = transform.position;

        string animName = left ? leftSwipeAnim : rightSwipeAnim;
        DragonAttackHitbox hitbox = left ? leftArmHitbox : rightArmHitbox;

        Play(animName);

        Vector3 lungeDirection = swipeLungeTowardPlayer ? GetDirectionToPlayer() : GetMoveForward();

        yield return MoveInDirectionLinearForSeconds(lungeDirection, swipeForwardDistance, swipeForwardTime);

        float hitStart = FrameToSeconds(swipeHitStartFrame);
        float hitEnd = FrameToSeconds(swipeHitEndFrame);

        float waitAfterForward = Mathf.Max(0f, hitStart - swipeForwardTime);
        yield return new WaitForSeconds(waitAfterForward);

        if (hitbox != null)
        {
            hitbox.EnableHitbox();
        }

        yield return new WaitForSeconds(Mathf.Max(0f, hitEnd - hitStart));

        if (hitbox != null)
        {
            hitbox.DisableHitbox();
        }

        if (swipeReturnToStartPosition)
        {
            yield return MoveToPositionForSeconds(startPosition, swipeReturnTime);
        }

        float remaining = Mathf.Max(0f, swipeAnimDuration - hitEnd - swipeReturnTime);
        yield return new WaitForSeconds(remaining);

        ReturnToIdle();
    }

    private IEnumerator TailSlam()
    {
        isBusy = true;
        state = DragonState.Acting;

        DisableAllHitboxes();

        yield return ApproachMeleeRange();

        if (ShouldTackleBecauseTargetEscaped())
        {
            yield return ChargeAttack(false);
            yield break;
        }

        yield return FacePlayerForSeconds(facePlayerBeforeActionTime);

        if (ShouldTackleBecauseTargetEscaped())
        {
            yield return ChargeAttack(false);
            yield break;
        }

        Quaternion originalRotation = transform.rotation;

        Play(tailSlamAnim);

        yield return WaitUntilFrame(tailSlamAimStartFrame);

        if (tailAttacksTurnTailToPlayer)
        {
            yield return TrackTailToPlayerForDuration(FramesToDuration(tailSlamAimStartFrame, tailSlamTrackUntilFrame), tailSlamAttackOffsetY);
        }
        else
        {
            Quaternion attackRotation = GetRotationToPlayerWithOffset(tailSlamAngleOffset);
            yield return RotateTo(attackRotation, FramesToDuration(tailSlamAimStartFrame, tailSlamTrackUntilFrame));
        }

        if (tailHitbox != null)
        {
            tailHitbox.EnableHitbox();
        }

        yield return new WaitForSeconds(FramesToDuration(tailSlamHitStartFrame, tailSlamHitEndFrame));

        if (tailHitbox != null)
        {
            tailHitbox.DisableHitbox();
        }

        yield return WaitUntilFrameFromCurrent(tailSlamHitEndFrame, tailSlamReturnStartFrame);
        yield return RotateTo(originalRotation, FramesToDuration(tailSlamReturnStartFrame, tailSlamReturnEndFrame));

        float remaining = Mathf.Max(0f, tailSlamDuration - FrameToSeconds(tailSlamReturnEndFrame));
        yield return new WaitForSeconds(remaining);

        transform.rotation = originalRotation;

        ReturnToIdle();
    }

    private IEnumerator TailSwipe()
    {
        isBusy = true;
        state = DragonState.Acting;

        DisableAllHitboxes();

        yield return ApproachMeleeRange();

        if (ShouldTackleBecauseTargetEscaped())
        {
            yield return ChargeAttack(false);
            yield break;
        }

        yield return FacePlayerForSeconds(facePlayerBeforeActionTime);

        if (ShouldTackleBecauseTargetEscaped())
        {
            yield return ChargeAttack(false);
            yield break;
        }

        Quaternion originalRotation = transform.rotation;
        float swipeOffset = GetTailSwipeAttackOffset();

        Play(tailSwipeAnim);

        yield return WaitUntilFrame(tailSwipeAimStartFrame);

        if (tailAttacksTurnTailToPlayer)
        {
            yield return TrackTailToPlayerForDuration(FramesToDuration(tailSwipeAimStartFrame, tailSwipeTrackUntilFrame), swipeOffset);
        }
        else
        {
            Quaternion attackRotation = GetRotationToPlayerWithOffset(swipeOffset);
            yield return RotateTo(attackRotation, FramesToDuration(tailSwipeAimStartFrame, tailSwipeTrackUntilFrame));
        }

        if (tailHitbox != null)
        {
            tailHitbox.EnableHitbox();
        }

        yield return new WaitForSeconds(FramesToDuration(tailSwipeSlamHitStartFrame, tailSwipeSlamHitEndFrame));

        if (tailHitbox != null)
        {
            tailHitbox.DisableHitbox();
        }

        yield return WaitUntilFrameFromCurrent(tailSwipeSlamHitEndFrame, tailSwipeSecondHitStartFrame);

        if (tailHitbox != null)
        {
            tailHitbox.EnableHitbox();
        }

        yield return RotateTo(originalRotation, FramesToDuration(tailSwipeSecondHitStartFrame, tailSwipeSecondHitEndFrame));

        if (tailHitbox != null)
        {
            tailHitbox.DisableHitbox();
        }

        float remaining = Mathf.Max(0f, tailSwipeDuration - FrameToSeconds(tailSwipeSecondHitEndFrame));
        yield return new WaitForSeconds(remaining);

        transform.rotation = originalRotation;

        ReturnToIdle();
    }

    private IEnumerator TrackTailToPlayerForDuration(float duration, float extraOffsetY)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            Quaternion targetRotation = GetTailRotationToPlayer(extraOffsetY);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, tailTrackingTurnSpeed * Time.deltaTime);

            yield return null;
        }
    }

    private IEnumerator StepAction(Vector3 direction)
    {
        float distance = Vector3.Dot(direction.normalized, -GetMoveForward()) > 0.7f ? backStepDistance : sideStepDistance;
        yield return StepAction(direction, distance, stepDuration);
    }

    private IEnumerator StepAction(Vector3 direction, float distance, float duration)
    {
        isBusy = true;
        state = DragonState.Acting;

        DisableAllHitboxes();

        yield return FacePlayerForSeconds(0.12f);

        Play(stepAnim);

        yield return MoveInDirectionForSeconds(direction, distance, duration);

        ReturnToIdle();
    }

    private IEnumerator MoveInDirectionForSeconds(Vector3 direction, float distance, float duration)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = -GetMoveForward();
        }

        direction.Normalize();

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
            float curve = Mathf.Sin(t * Mathf.PI);
            float speed = distance / duration;

            MoveDragon(direction * speed * curve * Time.deltaTime);

            yield return null;
        }
    }

    private IEnumerator MoveInDirectionLinearForSeconds(Vector3 direction, float distance, float duration)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = GetMoveForward();
        }

        direction.Normalize();

        float timer = 0f;
        float speed = duration <= 0f ? distance : distance / duration;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            MoveDragon(direction * speed * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator MoveToPositionForSeconds(Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = transform.position;
        float timer = 0f;

        if (duration <= 0f)
        {
            transform.position = targetPosition;
            yield break;
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector3 newPos = Vector3.Lerp(startPosition, targetPosition, t);

            if (lockYPosition)
            {
                newPos.y = startY;
            }

            transform.position = newPos;

            yield return null;
        }

        if (lockYPosition)
        {
            targetPosition.y = startY;
        }

        transform.position = targetPosition;
    }

    private IEnumerator FacePlayerForSeconds(float duration)
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

    private void FacePlayerInstant()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion lookRotation = Quaternion.LookRotation(dir);
        transform.rotation = lookRotation * Quaternion.Euler(0f, modelForwardOffsetY, 0f);
    }

    private void FacePlayerSmooth(float speed)
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion lookRotation = Quaternion.LookRotation(dir);
        Quaternion targetRotation = lookRotation * Quaternion.Euler(0f, modelForwardOffsetY, 0f);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, speed * Time.deltaTime);
    }

    private Vector3 GetDirectionToPlayer()
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

    private Vector3 GetMoveForward()
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

    private void MoveDragon(Vector3 delta)
    {
        transform.position += delta;

        if (lockYPosition)
        {
            Vector3 pos = transform.position;
            pos.y = startY;
            transform.position = pos;
        }
    }

    private IEnumerator RotateTo(Quaternion targetRotation, float duration)
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

    private Vector3 GetRandomSideStepDirection()
    {
        Vector3 forward = GetMoveForward();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        return Random.value > 0.5f ? right : -right;
    }

    private Quaternion GetRotationToPlayerWithOffset(float angleOffset)
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

    private Quaternion GetTailRotationToPlayer(float extraOffsetY)
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

    private float GetTailSwipeAttackOffset()
    {
        if (!chooseTailSwipeDirectionByPlayerPosition || player == null)
        {
            return Random.value > 0.5f ? tailSwipeLeftAttackOffsetY : tailSwipeRightAttackOffsetY;
        }

        Vector3 localPlayer = transform.InverseTransformPoint(player.position);

        if (localPlayer.x < 0f)
        {
            return tailSwipeLeftAttackOffsetY;
        }

        return tailSwipeRightAttackOffsetY;
    }

    private float GetDistanceToPlayer()
    {
        if (player == null) return 999f;

        Vector3 a = transform.position;
        Vector3 b = player.position;

        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    private float FrameToSeconds(int frame)
    {
        return frame / animationFPS;
    }

    private float FramesToDuration(int startFrame, int endFrame)
    {
        return Mathf.Max(0f, endFrame - startFrame) / animationFPS;
    }

    private IEnumerator WaitUntilFrame(int frame)
    {
        yield return new WaitForSeconds(FrameToSeconds(frame));
    }

    private IEnumerator WaitUntilFrameFromCurrent(int currentFrame, int targetFrame)
    {
        int diff = Mathf.Max(0, targetFrame - currentFrame);
        yield return new WaitForSeconds(FrameToSeconds(diff));
    }

    private void Play(string stateName)
    {
        if (animator == null) return;
        if (string.IsNullOrEmpty(stateName)) return;

        animator.CrossFade(stateName, crossFadeTime);
    }

    private void SetAnimatorSpeed(float speed)
    {
        if (animator == null) return;

        animator.speed = speed;
    }

    private void ReturnToIdle()
    {
        DisableAllHitboxes();
        StopAllChargeParticles();
        SetAnimatorSpeed(1f);

        isBusy = false;
        state = DragonState.Idle;

        Play(idleAnim);
    }

    private void DisableAllHitboxes()
    {
        if (breathHitbox != null) breathHitbox.DisableHitbox();
        if (chargeHitbox != null) chargeHitbox.DisableHitbox();
        if (leftArmHitbox != null) leftArmHitbox.DisableHitbox();
        if (rightArmHitbox != null) rightArmHitbox.DisableHitbox();
        if (tailHitbox != null) tailHitbox.DisableHitbox();
    }

    private void StopAllChargeParticles()
    {
        if (chargeHoldParticle != null)
        {
            chargeHoldParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (chargeReadyParticle != null)
        {
            chargeReadyParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (chargeRunParticle != null)
        {
            chargeRunParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void DoRoarEffect()
    {
        if (SafePlayerCamera.Instance != null)
        {
            SafePlayerCamera.Instance.Shake(roarCameraShakeDuration, roarCameraShakeStrength);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, roarStaggerRadius, playerLayer);

        foreach (Collider hit in hits)
        {
            hit.SendMessage("DragonStagger", roarStaggerTime, SendMessageOptions.DontRequireReceiver);
        }
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
        DisableAllHitboxes();
        StopAllChargeParticles();
        SetAnimatorSpeed(1f);

        isBusy = true;
        state = DragonState.Acting;

        yield return FacePlayerForSeconds(0.3f);

        Play(roarAnim);
        DoRoarEffect();

        yield return new WaitForSeconds(roarDuration);

        ReturnToIdle();

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
        DisableAllHitboxes();
        StopAllChargeParticles();
        SetAnimatorSpeed(1f);

        isBusy = true;
        state = DragonState.Down;

        Play(downAnim);

        yield return new WaitForSeconds(downDuration);

        ReturnToIdle();

        StartCoroutine(AILoop());
    }

    private void HandleDeath()
    {
        StopAllCoroutines();

        DisableAllHitboxes();
        StopAllChargeParticles();
        SetAnimatorSpeed(1f);

        state = DragonState.Dead;
        isBusy = true;

        Play(deathAnim);
    }
}