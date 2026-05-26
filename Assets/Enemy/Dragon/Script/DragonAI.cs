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
        SideStepBreath,
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

    [Header("Intro")]
    [Tooltip("開始後、咆哮する前に待機する秒数")]
    public float introIdleBeforeRoarTime = 3f;

    [Header("Animation State Names")]
    [Tooltip("待機アニメーションのState名")]
    public string idleAnim = "Idle_Battle";

    [Tooltip("歩きアニメーションのState名")]
    public string walkAnim = "walk";

    [Tooltip("走り、突進に使うアニメーションのState名")]
    public string runAnim = "Running";

    [Tooltip("咆哮アニメーションのState名")]
    public string roarAnim = "Roar";

    [Tooltip("ブレスアニメーションのState名")]
    public string breathAnim = "Breath";

    [Tooltip("左腕ひっかきのState名")]
    public string leftSwipeAnim = "Arm Swipe_Left";

    [Tooltip("右腕ひっかきのState名")]
    public string rightSwipeAnim = "Arm Swipe_Right";

    [Tooltip("軽いひるみアニメーションのState名")]
    public string bigHitAnim = "Big hit";

    [Tooltip("死亡アニメーションのState名")]
    public string deathAnim = "Death";

    [Tooltip("ダウンアニメーションのState名")]
    public string downAnim = "Down";

    [Tooltip("ステップアニメーションのState名")]
    public string stepAnim = "Step_L_R_B";

    [Tooltip("尻尾叩きつけアニメーションのState名")]
    public string tailSlamAnim = "Tail Slam";

    [Tooltip("尻尾なぎ払いアニメーションのState名")]
    public string tailSwipeAnim = "Tail Swipe";

    [Header("Animation Control")]
    [Tooltip("アニメーション切り替えの滑らかさ")]
    public float crossFadeTime = 0.12f;

    [Tooltip("FBXアニメーションのフレームレート")]
    public float animationFPS = 30f;

    [Tooltip("移動アニメーションが止まった時だけ再確認する間隔。小さすぎるとアニメがリセットされやすい")]
    public float moveAnimCheckInterval = 0.25f;

    [Tooltip("移動アニメーションがこの再生位置を超えたら再再生する。Loop TimeがONなら基本使われない")]
    [Range(0.5f, 0.99f)] public float moveAnimRestartNormalizedTime = 0.95f;

    [Header("Range")]
    [Tooltip("近距離判定")]
    public float closeRange = 7f;

    [Tooltip("中距離判定")]
    public float middleRange = 15f;

    [Tooltip("遠距離判定")]
    public float farRange = 25f;

    [Tooltip("接近行動をやめる距離")]
    public float approachStopDistance = 8f;

    [Header("Approach")]
    [Tooltip("歩き接近速度")]
    public float walkSpeed = 3.2f;

    [Tooltip("走り接近速度")]
    public float runChaseSpeed = 6.2f;

    [Tooltip("接近中、この距離以上離れたらRunningに切り替える")]
    public float switchToRunDistance = 14f;

    [Tooltip("Running追跡をやめてwalkに戻す距離")]
    public float runChaseStopDistance = 8f;

    [Header("Melee Attack Position")]
    [Tooltip("ブレス以外の攻撃前に近距離まで接近する")]
    public bool approachBeforeNonBreathAttack = true;

    [Tooltip("近接攻撃を開始する距離")]
    public float meleeAttackStartDistance = 5.5f;

    [Tooltip("近接攻撃前の接近速度")]
    public float meleeApproachSpeed = 4.0f;

    [Tooltip("近接攻撃前の旋回速度")]
    public float meleeApproachTurnSpeed = 7f;

    [Tooltip("近接攻撃前の接近を諦めるまでの秒数")]
    public float meleeApproachTimeout = 4f;

    [Header("Escape Tackle")]
    [Tooltip("近接攻撃しようとしている時にプレイヤーが離れたら突進に切り替える")]
    public bool tackleWhenTargetEscapes = true;

    [Tooltip("この距離以上離れたら突進へ切り替える")]
    public float tackleEscapeDistance = 11f;

    [Tooltip("近接攻撃の準備中だけ突進切り替えを許可する")]
    public bool useTackleDuringMeleeAttacks = true;

    [Header("Action Interval")]
    [Tooltip("行動後の最短待機時間")]
    public float minActionInterval = 0.35f;

    [Tooltip("行動後の最長待機時間")]
    public float maxActionInterval = 0.9f;

    [Header("Repeat Prevention")]
    [Tooltip("同じ行動を連続で出しにくくする")]
    public bool preventSameActionRepeat = true;

    [Tooltip("直前2回の行動を避ける")]
    public bool avoidLastTwoActions = true;

    [Tooltip("他に選べる行動がない時は連続行動を許可する")]
    public bool allowRepeatIfNoOtherChoice = true;

    [Header("Facing")]
    [Tooltip("待機中にプレイヤーを見る速度")]
    public float idleTurnSpeed = 8f;

    [Tooltip("攻撃前にプレイヤーを見る速度")]
    public float actionTurnSpeed = 6f;

    [Tooltip("攻撃前にプレイヤーを見る時間")]
    public float facePlayerBeforeActionTime = 0.25f;

    [Header("Charge Attack")]
    [Tooltip("突進中だけ有効にする攻撃判定")]
    public DragonAttackHitbox chargeHitbox;

    [Tooltip("突進の基準速度")]
    public float chargeSpeed = 24f;

    [Tooltip("プレイヤーをどれくらい通り過ぎるか")]
    public float chargeOvershootDistance = 4f;

    [Tooltip("突進時間の最小値")]
    public float chargeMinDuration = 0.75f;

    [Tooltip("突進時間の最大値")]
    public float chargeMaxDuration = 1.8f;

    [Tooltip("この距離未満から突進する場合だけ2回バックステップする")]
    public float chargeMinStartDistance = 8f;

    [Tooltip("突進前の溜め時間")]
    public float chargeTellTime = 1.0f;

    [Tooltip("溜め中のRunningアニメーション速度。小さいほどスロー")]
    public float chargeTellAnimationSpeed = 0.18f;

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
    [Tooltip("溜め中に再生するパーティクル")]
    public ParticleSystem chargeHoldParticle;

    [Tooltip("溜め完了時に再生するパーティクル")]
    public ParticleSystem chargeReadyParticle;

    [Tooltip("突進中に再生するパーティクル")]
    public ParticleSystem chargeRunParticle;

    [Header("Step")]
    [Tooltip("横ステップ距離")]
    public float sideStepDistance = 2f;

    [Tooltip("バックステップ距離")]
    public float backStepDistance = 3f;

    [Tooltip("ステップ時間")]
    public float stepDuration = 0.45f;

    [Tooltip("横ステップ後にひっかきを出す確率")]
    [Range(0f, 1f)] public float sideStepSwipeChance = 0.45f;

    [Tooltip("横ステップ後に尻尾攻撃を出す確率")]
    [Range(0f, 1f)] public float sideStepTailChance = 0.35f;

    [Tooltip("横ステップ後にブレスを出す確率")]
    [Range(0f, 1f)] public float sideStepBreathChance = 0.20f;

    [Tooltip("バックステップ後にブレスを使う確率")]
    [Range(0f, 1f)] public float afterBackStepBreathChance = 0.75f;

    [Tooltip("バックステップ後に突進を使う確率。低め推奨")]
    [Range(0f, 1f)] public float afterBackStepChargeChance = 0.08f;

    [Header("Swipe Forward And Return")]
    [Tooltip("ひっかき中に前進する距離。届かないなら上げる")]
    public float swipeForwardDistance = 2.4f;

    [Tooltip("ひっかき前進を開始するフレーム")]
    public int swipeLungeStartFrame = 18;

    [Tooltip("ひっかき前進を終了するフレーム。Hit開始少し後くらいがよい")]
    public int swipeLungeEndFrame = 48;

    [Tooltip("攻撃後に元の位置へ戻る時間")]
    public float swipeReturnTime = 0.25f;

    [Tooltip("オンならひっかき後に攻撃開始位置へ戻る")]
    public bool swipeReturnToStartPosition = true;

    [Tooltip("オンならひっかき前進方向をプレイヤー方向にする。オフならドラゴンの前方向にする")]
    public bool swipeLungeTowardPlayer = true;

    [Tooltip("ひっかき前進中にプレイヤー方向を追う強さ")]
    public float swipeLungeTurnSpeed = 7f;

    [Header("Breath")]
    [Tooltip("ブレス攻撃判定")]
    public DragonAttackHitbox breathHitbox;

    [Tooltip("ブレス判定を出し始めるフレーム")]
    public int breathStartFrame = 30;

    [Tooltip("ブレス判定を消すフレーム")]
    public int breathEndFrame = 120;

    [Tooltip("ブレス全体の長さ")]
    public float breathDuration = 4.2f;

    [Tooltip("ブレス前にプレイヤーを見る時間")]
    public float breathTurnTime = 0.35f;

    [Header("Arm Hitboxes")]
    [Tooltip("左腕攻撃判定")]
    public DragonAttackHitbox leftArmHitbox;

    [Tooltip("右腕攻撃判定")]
    public DragonAttackHitbox rightArmHitbox;

    [Tooltip("ひっかき判定を出し始めるフレーム")]
    public int swipeHitStartFrame = 35;

    [Tooltip("ひっかき判定を消すフレーム")]
    public int swipeHitEndFrame = 55;

    [Tooltip("ひっかきアニメーション全体の長さ")]
    public float swipeAnimDuration = 2.0f;

    [Header("Tail Hitbox")]
    [Tooltip("尻尾攻撃判定")]
    public DragonAttackHitbox tailHitbox;

    [Header("Tail Rotation Control")]
    [Tooltip("尻尾攻撃時に尻尾側をプレイヤーへ向ける")]
    public bool tailAttacksTurnTailToPlayer = true;

    [Tooltip("尻尾をプレイヤーへ向ける基本補正。頭が向くなら180を試す")]
    public float tailFacePlayerOffsetY = 0f;

    [Tooltip("Tail Slamの叩きつけ位置補正。もっと左に寄せたいならマイナスを大きくする")]
    public float tailSlamAttackOffsetY = -35f;

    [Tooltip("Tail Swipe左側補正。もっと左に寄せたいならマイナスを大きくする")]
    public float tailSwipeLeftAttackOffsetY = -55f;

    [Tooltip("Tail Swipe右側補正。右に行きすぎるなら小さくする")]
    public float tailSwipeRightAttackOffsetY = 15f;

    [Tooltip("尻尾がプレイヤーを追尾する回転速度")]
    public float tailTrackingTurnSpeed = 12f;

    [Header("Tail Slam")]
    [Tooltip("Tail Slam全体の長さ")]
    public float tailSlamDuration = 4.8f;

    [Tooltip("Tail Slamで向き合わせ開始フレーム")]
    public int tailSlamAimStartFrame = 5;

    [Tooltip("Tail Slamでプレイヤー追尾を続ける最終フレーム")]
    public int tailSlamTrackUntilFrame = 73;

    [Tooltip("Tail Slamの判定開始フレーム")]
    public int tailSlamHitStartFrame = 73;

    [Tooltip("Tail Slamの判定終了フレーム")]
    public int tailSlamHitEndFrame = 82;

    [Tooltip("Tail Slamで正面に戻り始めるフレーム")]
    public int tailSlamReturnStartFrame = 105;

    [Tooltip("Tail Slamで正面に戻り終わるフレーム")]
    public int tailSlamReturnEndFrame = 138;

    [Tooltip("尻尾を向けない設定の時の角度補正")]
    public float tailSlamAngleOffset = -15f;

    [Header("Tail Swipe")]
    [Tooltip("Tail Swipe全体の長さ")]
    public float tailSwipeDuration = 4.6f;

    [Tooltip("Tail Swipeで向き合わせ開始フレーム")]
    public int tailSwipeAimStartFrame = 8;

    [Tooltip("Tail Swipeでプレイヤー追尾を続ける最終フレーム")]
    public int tailSwipeTrackUntilFrame = 77;

    [Tooltip("Tail Swipeの1段目判定開始フレーム")]
    public int tailSwipeSlamHitStartFrame = 77;

    [Tooltip("Tail Swipeの1段目判定終了フレーム")]
    public int tailSwipeSlamHitEndFrame = 87;

    [Tooltip("Tail Swipeの2段目判定開始フレーム")]
    public int tailSwipeSecondHitStartFrame = 104;

    [Tooltip("Tail Swipeの2段目判定終了フレーム")]
    public int tailSwipeSecondHitEndFrame = 133;

    [Tooltip("プレイヤー位置でTail Swipeの左右を選ぶ")]
    public bool chooseTailSwipeDirectionByPlayerPosition = true;

    [Header("Roar")]
    [Tooltip("咆哮アニメーションの長さ")]
    public float roarDuration = 2.8f;

    [Tooltip("咆哮でプレイヤーをひるませる半径")]
    public float roarStaggerRadius = 15f;

    [Tooltip("咆哮でプレイヤーをひるませる時間")]
    public float roarStaggerTime = 1.0f;

    [Tooltip("プレイヤーのLayer")]
    public LayerMask playerLayer;

    [Tooltip("咆哮時のカメラ揺れ時間")]
    public float roarCameraShakeDuration = 0.5f;

    [Tooltip("咆哮時のカメラ揺れ強さ")]
    public float roarCameraShakeStrength = 0.15f;

    [Header("Down")]
    [Tooltip("ダウン時間")]
    public float downDuration = 9.11f;

    [Header("Phase 2")]
    [Tooltip("HP50パーセント以下で強化状態になったか")]
    public bool isPhase2 = false;

    [Tooltip("第2形態の速度倍率")]
    public float phase2SpeedMultiplier = 1.15f;

    [Tooltip("第2形態の行動間隔倍率")]
    public float phase2ActionIntervalMultiplier = 0.7f;

    [Tooltip("第2形態でコンボを選ぶ確率")]
    [Range(0f, 1f)] public float phase2ComboChance = 0.6f;

    private DragonState state = DragonState.Intro;
    private bool isBusy = false;
    private float startY;
    private DragonAction lastAction = DragonAction.None;
    private DragonAction secondLastAction = DragonAction.None;
    private string currentAnimName = "";
    private float currentAnimSpeed = 1f;

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
        SetAnimatorSpeedSafe(1f);
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

        PlayAnim(idleAnim, true);

        float idleTimer = 0f;

        while (idleTimer < introIdleBeforeRoarTime)
        {
            idleTimer += Time.deltaTime;
            FacePlayerSmooth(idleTurnSpeed);
            yield return null;
        }

        yield return FacePlayerForSeconds(0.25f);

        PlayAnim(roarAnim, true);
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

            yield return DecideAction(GetDistanceToPlayer());
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
                DragonAction.SideStepBreath,
                DragonAction.Charge
            }
            : new DragonAction[]
            {
                DragonAction.Breath,
                DragonAction.Breath,
                DragonAction.Breath,
                DragonAction.Approach,
                DragonAction.SideStepBreath
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
                DragonAction.SideStepBreath,
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
                DragonAction.SideStepBreath,
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
                    DragonAction.SideStepSwipe,
                    DragonAction.SideStepTailSlam,
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
                    DragonAction.SideStepSwipe,
                    DragonAction.SideStepTailSlam,
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
                DragonAction.SideStepBreath,
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
                DragonAction.SideStepBreath,
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

            case DragonAction.SideStepBreath:
                yield return SideStepThenBreath();
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
        string moveAnim = running ? runAnim : walkAnim;

        PlayAnim(moveAnim, true);

        float checkTimer = 0f;

        while (player != null && GetDistanceToPlayer() > approachStopDistance)
        {
            float distance = GetDistanceToPlayer();

            if (distance >= switchToRunDistance && !running)
            {
                running = true;
                moveAnim = runAnim;
                PlayAnim(moveAnim, true);
                checkTimer = 0f;
            }
            else if (running && distance <= runChaseStopDistance)
            {
                running = false;
                moveAnim = walkAnim;
                PlayAnim(moveAnim, true);
                checkTimer = 0f;
            }

            checkTimer += Time.deltaTime;
            KeepMoveAnim(moveAnim, ref checkTimer);

            FacePlayerSmooth(actionTurnSpeed);

            float speed = running ? runChaseSpeed : walkSpeed;
            MoveDragon(GetMoveForward() * speed * Time.deltaTime);

            yield return null;
        }

        ReturnToIdle();
    }

    private IEnumerator ApproachMeleeRange()
    {
        if (!approachBeforeNonBreathAttack) yield break;
        if (player == null) yield break;

        float timer = 0f;
        float checkTimer = 0f;

        bool running = GetDistanceToPlayer() >= switchToRunDistance;
        string moveAnim = running ? runAnim : walkAnim;

        PlayAnim(moveAnim, true);

        while (player != null && GetDistanceToPlayer() > meleeAttackStartDistance)
        {
            timer += Time.deltaTime;
            checkTimer += Time.deltaTime;

            if (timer > meleeApproachTimeout)
            {
                break;
            }

            float distance = GetDistanceToPlayer();

            if (distance >= switchToRunDistance && !running)
            {
                running = true;
                moveAnim = runAnim;
                PlayAnim(moveAnim, true);
                checkTimer = 0f;
            }
            else if (running && distance <= runChaseStopDistance)
            {
                running = false;
                moveAnim = walkAnim;
                PlayAnim(moveAnim, true);
                checkTimer = 0f;
            }

            KeepMoveAnim(moveAnim, ref checkTimer);
            FacePlayerSmooth(meleeApproachTurnSpeed);

            float speed = running ? runChaseSpeed : meleeApproachSpeed;
            MoveDragon(GetMoveForward() * speed * Time.deltaTime);

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

    private IEnumerator SideStepThenBreath()
    {
        yield return StepAction(GetRandomSideStepDirection());
        yield return new WaitForSeconds(Random.Range(0.05f, 0.18f));
        yield return BreathAttack();
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

    private IEnumerator DoubleBackStepForCloseCharge()
    {
        int count = Mathf.Max(1, closeChargeBackStepCount);

        for (int i = 0; i < count; i++)
        {
            yield return FacePlayerForSeconds(0.08f);

            PlayAnim(stepAnim, true);

            Vector3 backDirection = -GetMoveForward();

            yield return MoveInDirectionForSeconds(
                backDirection,
                closeChargeBackStepDistance,
                closeChargeBackStepDuration,
                stepAnim
            );

            yield return new WaitForSeconds(0.08f);
        }
    }

    private IEnumerator BreathAttack()
    {
        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();

        yield return FacePlayerForSeconds(breathTurnTime);

        PlayAnim(breathAnim, true);

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
        SetAnimatorSpeedSafe(1f);

        float distance = GetDistanceToPlayer();

        if (forceCloseBackStep || distance < chargeMinStartDistance)
        {
            yield return DoubleBackStepForCloseCharge();
        }

        yield return FacePlayerForSeconds(0.15f);

        PlayAnim(runAnim, true);
        SetAnimatorSpeedSafe(chargeTellAnimationSpeed);

        if (chargeHoldParticle != null)
        {
            chargeHoldParticle.Play();
        }

        float tellTimer = 0f;
        float checkTimer = 0f;

        while (tellTimer < chargeTellTime)
        {
            tellTimer += Time.deltaTime;
            checkTimer += Time.deltaTime;

            KeepMoveAnim(runAnim, ref checkTimer);
            SetAnimatorSpeedSafe(chargeTellAnimationSpeed);
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

        SetAnimatorSpeedSafe(1f);
        PlayAnim(runAnim, true);

        yield return new WaitForSeconds(chargeReadyPauseTime);

        Vector3 chargeDirection = GetDirectionToPlayer();
        float targetDistance = GetChargeTargetDistance(chargeDirection);
        float baseSpeed = isPhase2 ? chargeSpeed * phase2SpeedMultiplier : chargeSpeed;
        float chargeTime = Mathf.Clamp(targetDistance / Mathf.Max(0.01f, baseSpeed), chargeMinDuration, chargeMaxDuration);

        float timer = 0f;
        float previousDistance = 0f;
        float runCheckTimer = 0f;

        if (chargeRunParticle != null)
        {
            chargeRunParticle.Play();
        }

        if (chargeHitbox != null)
        {
            chargeHitbox.EnableHitbox();
        }

        PlayAnim(runAnim, true);
        SetAnimatorSpeedSafe(1f);

        while (timer < chargeTime)
        {
            timer += Time.deltaTime;
            runCheckTimer += Time.deltaTime;

            KeepMoveAnim(runAnim, ref runCheckTimer);
            SetAnimatorSpeedSafe(1f);

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
        SetAnimatorSpeedSafe(1f);

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
            return Mathf.Lerp(t, 1f, eased);
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

        PlayAnim(animName, true);

        float lungeStart = FrameToSeconds(swipeLungeStartFrame);
        float lungeEnd = FrameToSeconds(swipeLungeEndFrame);
        float hitStart = FrameToSeconds(swipeHitStartFrame);
        float hitEnd = FrameToSeconds(swipeHitEndFrame);

        float timer = 0f;
        float previousLunge = 0f;
        bool hitboxEnabled = false;

        Vector3 fixedLungeDirection = swipeLungeTowardPlayer ? GetDirectionToPlayer() : GetMoveForward();

        while (timer < swipeAnimDuration)
        {
            timer += Time.deltaTime;

            if (timer >= lungeStart && timer <= lungeEnd)
            {
                FacePlayerSmooth(swipeLungeTurnSpeed);

                Vector3 lungeDirection = swipeLungeTowardPlayer ? GetDirectionToPlayer() : fixedLungeDirection;

                float t = Mathf.InverseLerp(lungeStart, lungeEnd, timer);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                float currentLunge = swipeForwardDistance * eased;
                float delta = currentLunge - previousLunge;
                previousLunge = currentLunge;

                MoveDragon(lungeDirection * delta);
            }

            if (!hitboxEnabled && timer >= hitStart)
            {
                hitboxEnabled = true;

                if (hitbox != null)
                {
                    hitbox.EnableHitbox();
                }
            }

            if (hitboxEnabled && timer >= hitEnd)
            {
                hitboxEnabled = false;

                if (hitbox != null)
                {
                    hitbox.DisableHitbox();
                }
            }

            yield return null;
        }

        if (hitbox != null)
        {
            hitbox.DisableHitbox();
        }

        if (swipeReturnToStartPosition)
        {
            yield return MoveToPositionForSeconds(startPosition, swipeReturnTime);
        }

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

        PlayAnim(tailSlamAnim, true);

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

        PlayAnim(tailSwipeAnim, true);

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

        PlayAnim(stepAnim, true);

        yield return MoveInDirectionForSeconds(direction, distance, duration, stepAnim);

        ReturnToIdle();
    }

    private IEnumerator MoveInDirectionForSeconds(Vector3 direction, float distance, float duration, string moveAnim)
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
            float speed = distance / duration;

            MoveDragon(direction * speed * curve * Time.deltaTime);

            yield return null;
        }
    }

    private IEnumerator MoveInDirectionForSeconds(Vector3 direction, float distance, float duration)
    {
        yield return MoveInDirectionForSeconds(direction, distance, duration, walkAnim);
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

    private void PlayAnim(string stateName, bool force)
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

    private void KeepMoveAnim(string stateName, ref float checkTimer)
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

    private void SetAnimatorSpeedSafe(float speed)
    {
        if (animator == null) return;

        currentAnimSpeed = speed;
        animator.speed = speed;
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

    private void ReturnToIdle()
    {
        DisableAllHitboxes();
        StopAllChargeParticles();
        SetAnimatorSpeedSafe(1f);

        isBusy = false;
        state = DragonState.Idle;

        PlayAnim(idleAnim, true);
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
        SetAnimatorSpeedSafe(1f);

        isBusy = true;
        state = DragonState.Acting;

        yield return FacePlayerForSeconds(0.3f);

        PlayAnim(roarAnim, true);
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
        SetAnimatorSpeedSafe(1f);

        isBusy = true;
        state = DragonState.Down;

        PlayAnim(downAnim, true);

        yield return new WaitForSeconds(downDuration);

        ReturnToIdle();

        StartCoroutine(AILoop());
    }

    private void HandleDeath()
    {
        StopAllCoroutines();

        DisableAllHitboxes();
        StopAllChargeParticles();
        SetAnimatorSpeedSafe(1f);

        state = DragonState.Dead;
        isBusy = true;

        PlayAnim(deathAnim, true);
    }
}