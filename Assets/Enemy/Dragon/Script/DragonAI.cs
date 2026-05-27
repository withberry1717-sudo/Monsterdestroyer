using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DragonAI : MonoBehaviour
{
    private enum DragonState
    {
        Intro,
        Idle,
        Acting,
        Opening,
        Down,
        Dead
    }

    private enum DragonAction
    {
        None,
        Opening,
        Approach,
        SwipeCounter,
        TailSlam,
        TailSwipe,
        BackStepTailSlam,
        BackStepTailSwipe,
        BackStepWideBreath,
        BackStepBeamBreath,
        WideBreath,
        BeamBreath,
        Charge,
        DoubleCharge
    }

    [Header("参照設定")]
    [Tooltip("Dragon_AllObjectsに付いているDragonDragonMotionを入れてください。移動、回転、アニメーション再生を担当します。")]
    public DragonDragonMotion motion;

    [Tooltip("Dragon_AllObjectsに付いているDragonPhaseControllerを入れてください。HP50%以下の強化状態を管理します。")]
    public DragonPhaseController phase;

    [Tooltip("DragonCoreに付いているDragonHPを入れてください。本体HP、尻尾クリスタル破壊、死亡イベントを受け取ります。")]
    public DragonHP dragonHP;

    [Tooltip("Dragon_AllObjectsに付いているDragonAnimationEffectPlayerを入れてください。SEをまとめて再生したい場合に使います。未設定でもAudioSourceがあれば再生できます。")]
    public DragonAnimationEffectPlayer effectPlayer;

    [Tooltip("プレイヤー本体のTransformを入れてください。ドラゴンが向く方向、追跡、攻撃対象の基準になります。")]
    public Transform player;

    [Header("デバッグ用：行動オンオフ")]
    [Tooltip("接近行動を使うかどうかです。オフにすると遠距離で近づかなくなるため、単体テスト時以外はオン推奨です。")]
    public bool enableApproachAction = true;

    [Tooltip("懐にいるプレイヤーへのひっかき反撃を使うかどうかです。オンでも、プレイヤーが届く範囲にいる時だけ候補になります。")]
    public bool enableSwipeCounterAction = true;

    [Tooltip("Tail Slamを使うかどうかです。尻尾クリスタル破壊後は自動で使わなくなります。")]
    public bool enableTailSlamAction = true;

    [Tooltip("Tail Swipeを使うかどうかです。尻尾クリスタル破壊後は自動で使わなくなります。")]
    public bool enableTailSwipeAction = true;

    [Tooltip("近距離でバックステップしてから尻尾攻撃する行動を使うかどうかです。密着対策として使います。")]
    public bool enableBackStepTailAction = true;

    [Tooltip("近距離でバックステップしてからブレスする行動を使うかどうかです。密着され続ける時の距離取りとして使います。")]
    public bool enableBackStepBreathAction = true;

    [Tooltip("扇状ブレスを使うかどうかです。近距離では直接使わず、必要ならバックステップ後に使います。")]
    public bool enableWideBreathAction = true;

    [Tooltip("ビームブレスを使うかどうかです。近距離では使いません。")]
    public bool enableBeamBreathAction = true;

    [Tooltip("突進を使うかどうかです。近距離から突進する場合は先にバックステップします。")]
    public bool enableChargeAction = true;

    [Tooltip("HP50%以下で二連突進を使うかどうかです。使う場合も突進クールタイムの影響を受けます。")]
    public bool enableDoubleChargeAction = true;

    [Header("イントロ")]
    [Tooltip("戦闘開始後、咆哮する前に待機する秒数です。大きくすると戦闘開始までの間が長くなります。")]
    public float introIdleBeforeRoarTime = 3f;

    [Header("距離判定")]
    [Tooltip("近距離判定の距離です。この距離未満では近距離行動を選びます。近距離ではブレスを吐きません。")]
    public float closeRange = 7f;

    [Tooltip("中距離判定の距離です。この距離以上で遠距離未満なら中距離行動を選びます。")]
    public float middleRange = 15f;

    [Tooltip("遠距離判定の距離です。この距離より遠いと遠距離行動や接近を選びます。")]
    public float farRange = 25f;

    [Tooltip("接近行動をやめる距離です。大きくすると離れた位置で止まり、小さくすると近くまで詰めます。")]
    public float approachStopDistance = 8f;

    [Header("接近行動")]
    [Tooltip("歩き接近の速度です。大きくすると歩き接近が速くなります。")]
    public float walkSpeed = 3.2f;

    [Tooltip("走り接近の速度です。大きくすると遠距離からの追跡が速くなります。")]
    public float runChaseSpeed = 6.2f;

    [Tooltip("この距離以上離れていると走りアニメーションで追跡します。大きくすると走り始める距離が遠くなります。")]
    public float switchToRunDistance = 14f;

    [Tooltip("走り追跡をやめて歩きに戻す距離です。大きくすると早めに歩きへ戻ります。")]
    public float runChaseStopDistance = 8f;

    [Header("行動間隔")]
    [Tooltip("行動後に次の行動まで待つ最短秒数です。大きくすると攻撃頻度が下がります。")]
    public float minActionInterval = 0.45f;

    [Tooltip("行動後に次の行動まで待つ最長秒数です。大きくすると攻撃の間が長くなります。")]
    public float maxActionInterval = 1.0f;

    [Header("待機・隙行動")]
    [Tooltip("オンにすると、確率で何もせず待機する隙行動を行います。不要ならオフにしてください。")]
    public bool useOpeningIdle = false;

    [Tooltip("通常時に隙行動を選ぶ確率です。0なら出ません。大きくするとプレイヤーが攻撃できる隙が増えます。")]
    [Range(0f, 1f)]
    public float openingIdleChance = 0f;

    [Tooltip("隙行動の最短時間です。大きくすると短い隙でも長くなります。")]
    public float openingIdleMinTime = 0.8f;

    [Tooltip("隙行動の最長時間です。大きくすると長い隙が発生しやすくなります。")]
    public float openingIdleMaxTime = 1.6f;

    [Tooltip("オンにすると、隙行動中もプレイヤーの方を向きます。オフにすると向きを固定します。")]
    public bool lookAtPlayerDuringOpening = true;

    [Header("シンプル行動抽選の重み")]
    [Tooltip("近距離でTail Slamを選ぶ重みです。大きくすると尻尾叩きつけが出やすくなります。")]
    public int closeTailSlamWeight = 3;

    [Tooltip("近距離でTail Swipeを選ぶ重みです。大きくすると尻尾なぎ払いが出やすくなります。")]
    public int closeTailSwipeWeight = 3;

    [Tooltip("近距離で突進を選ぶ重みです。近距離突進は先にバックステップします。高すぎると理不尽になりやすいです。")]
    public int closeChargeWeight = 1;

    [Tooltip("近距離で2回バックステップしてからTail Slamを選ぶ重みです。密着された時に距離を作って尻尾攻撃を出します。")]
    public int closeBackStepTailSlamWeight = 2;

    [Tooltip("近距離で2回バックステップしてからTail Swipeを選ぶ重みです。密着された時に距離を作って薙ぎ払いを出します。")]
    public int closeBackStepTailSwipeWeight = 2;

    [Tooltip("近距離で2回バックステップしてから扇状ブレスを選ぶ重みです。近距離で直接ブレスを吐かずに距離を取ってから使います。")]
    public int closeBackStepWideBreathWeight = 2;

    [Tooltip("近距離で2回バックステップしてからビームブレスを選ぶ重みです。近距離で直接ブレスを吐かずに距離を取ってから使います。")]
    public int closeBackStepBeamBreathWeight = 2;

    [Tooltip("中距離でTail Slamを選ぶ重みです。尻尾が届く距離でのみ候補になります。")]
    public int middleTailSlamWeight = 2;

    [Tooltip("中距離でTail Swipeを選ぶ重みです。尻尾が届く距離でのみ候補になります。")]
    public int middleTailSwipeWeight = 2;

    [Tooltip("中距離で扇状ブレスを選ぶ重みです。大きくすると広範囲ブレスが増えます。")]
    public int middleWideBreathWeight = 2;

    [Tooltip("中距離でビームブレスを選ぶ重みです。大きくすると追尾ビームが増えます。")]
    public int middleBeamBreathWeight = 2;

    [Tooltip("中距離で突進を選ぶ重みです。大きくすると中距離から突進しやすくなります。")]
    public int middleChargeWeight = 1;

    [Tooltip("遠距離で接近行動を選ぶ重みです。大きくすると遠距離で近づきやすくなります。")]
    public int farApproachWeight = 3;

    [Tooltip("遠距離で扇状ブレスを選ぶ重みです。大きくすると遠距離から広範囲ブレスを使いやすくなります。")]
    public int farWideBreathWeight = 2;

    [Tooltip("遠距離でビームブレスを選ぶ重みです。大きくすると遠距離からビームブレスを使いやすくなります。")]
    public int farBeamBreathWeight = 3;

    [Tooltip("遠距離で突進を選ぶ重みです。大きくすると遠距離から突進しやすくなります。")]
    public int farChargeWeight = 1;

    [Header("ひっかき反撃")]
    [Tooltip("ひっかき反撃が発生する最大距離です。この距離以内にプレイヤーがいる時だけ、ひっかきが候補になります。")]
    public float swipeCounterReachDistance = 4.2f;

    [Tooltip("プレイヤーが懐にいる時に、ひっかき反撃を出す確率です。0.35なら中確率です。")]
    [Range(0f, 1f)]
    public float swipeCounterChance = 0.35f;

    [Tooltip("ひっかき反撃前にプレイヤーへ向き直る時間です。大きくすると発生前にしっかり向き直ります。")]
    public float swipeCounterFaceTime = 0.12f;

    [Tooltip("オン推奨。解析した実フレームを使います。オンなら予備動作は9フレーム目まで、攻撃判定は9から20フレーム目までになります。古いInspector値の影響を受けにくくします。")]
    public bool useAnalyzedSwipeFrames = true;

    [Tooltip("解析したひっかき予備動作終了フレームです。今回は9フレーム目までを予備動作として引き延ばします。")]
    public int analyzedSwipeAnticipationEndFrame = 9;

    [Tooltip("解析したひっかき攻撃終了フレームです。今回は9から20フレーム目までを攻撃判定にします。")]
    public int analyzedSwipeAttackEndFrame = 20;

    [Tooltip("旧設定です。Use Analyzed Swipe Framesがオフの時だけ使います。ひっかきの予備動作が終わるフレームです。")]
    public int swipeAnticipationEndFrame = 9;

    [Tooltip("ひっかきの予備動作を実際に見せる秒数です。大きくすると腕を構えてから攻撃するまでが長くなり、理不尽さが減ります。")]
    public float swipeAnticipationDuration = 0.75f;

    [Tooltip("オンにすると、予備動作区間だけAnimatorを遅くして9フレーム目までを引き延ばします。")]
    public bool stretchSwipeAnticipation = true;

    [Tooltip("9から20フレーム目までの攻撃部分を実際に見せる秒数です。予備動作だけでなく攻撃部分も少し伸ばすことで、見た目と判定のバランスを取りやすくします。")]
    public float swipeAttackDuration = 0.38f;

    [Tooltip("オンにすると、攻撃区間だけAnimatorを遅くして9から20フレーム目の攻撃を引き延ばします。")]
    public bool stretchSwipeAttack = true;

    [Tooltip("予備動作中にプレイヤーへ向き直る速度です。大きくすると予備動作中にしっかり正面を向きます。")]
    public float swipeAnticipationTurnSpeed = 8f;

    [Tooltip("予備動作中に再生する警告パーティクルです。腕、爪、地面のどこに置いてもOKです。未設定なら何も出ません。")]
    public ParticleSystem swipeAnticipationParticle;

    [Tooltip("予備動作開始時に鳴らす警告SEです。未設定なら鳴りません。")]
    public AudioClip swipeAnticipationSfx;

    [Tooltip("ひっかき中に前進する距離です。今回は距離詰め用途ではないため、0から1程度がおすすめです。")]
    public float swipeForwardDistance = 0f;

    [Tooltip("ひっかき前進を開始するフレームです。Swipe Forward Distanceが0ならほぼ影響しません。")]
    public int swipeLungeStartFrame = 18;

    [Tooltip("ひっかき前進を終了するフレームです。Swipe Forward Distanceが0ならほぼ影響しません。")]
    public int swipeLungeEndFrame = 45;

    [Tooltip("ひっかき中の向き補正の強さです。大きくすると攻撃中にプレイヤー方向へ向きやすくなります。")]
    public float swipeLungeTurnSpeed = 5f;

    [Tooltip("オンなら、予備動作終了と同時にひっかき判定をONにします。理不尽さを減らすため、基本はオン推奨です。")]
    public bool swipeHitboxStartsAtAttackPhase = true;

    [Tooltip("予備動作終了後、当たり判定を出すまでの遅延秒数です。0なら攻撃に入った瞬間に判定が出ます。")]
    public float swipeHitboxDelayAfterAnticipation = 0f;

    [Tooltip("Swipe Hitbox Starts At Attack Phase がオフの時だけ使います。ひっかき判定を出し始めるフレームです。Use Analyzed Swipe Framesがオンの時は基本使いません。")]
    public int swipeHitStartFrame = 9;

    [Tooltip("旧設定です。Use Analyzed Swipe Framesがオフの時だけ主に使います。ひっかき判定を消すフレームです。")]
    public int swipeHitEndFrame = 20;

    [Tooltip("ひっかきアニメーション全体の長さです。実際のアニメーション長に合わせてください。")]
    public float swipeAnimDuration = 1.6f;

    [Tooltip("左腕攻撃の判定を入れてください。未設定なら左腕攻撃にダメージ判定は出ません。")]
    public DragonAttackHitbox leftArmHitbox;

    [Tooltip("右腕攻撃の判定を入れてください。未設定なら右腕攻撃にダメージ判定は出ません。")]
    public DragonAttackHitbox rightArmHitbox;

    [Tooltip("腕攻撃開始時に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem swipeParticle;

    [Tooltip("腕攻撃開始時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip swipeSfx;

    [Header("バックステップ")]
    [Tooltip("ブレスや近距離突進の前に行うバックステップの最小回数です。")]
    public int randomBackStepMinCount = 1;

    [Tooltip("ブレスや近距離突進の前に行うバックステップの最大回数です。")]
    public int randomBackStepMaxCount = 3;

    [Tooltip("バックステップ1回分の距離です。大きくすると大きく後退します。")]
    public float backStepDistance = 4f;

    [Tooltip("バックステップ1回分の時間です。大きくするとゆっくり後退します。")]
    public float backStepDuration = 0.38f;

    [Tooltip("バックステップ同士の間隔です。大きくすると連続バックステップの間が長くなります。")]
    public float backStepInterval = 0.08f;

    [Tooltip("近距離から尻尾攻撃やブレスへ移る時の固定バックステップ回数です。2なら必ず2回下がってから攻撃します。")]
    public int closeSpecialBackStepCount = 2;

    [Tooltip("ブレス前にバックステップする距離判定です。この距離未満でブレスを選んだ場合、先に1から3回バックステップします。")]
    public float breathBackStepIfCloserThan = 10f;

    [Header("突進攻撃")]
    [Tooltip("突進中だけ有効にする攻撃判定を入れてください。未設定なら突進してもダメージ判定は出ません。")]
    public DragonAttackHitbox chargeHitbox;

    [Tooltip("突進の最低インターバルです。大きくすると突進頻度が下がり、小さくすると突進しやすくなります。推奨値は10秒前後です。")]
    public float chargeCooldown = 10f;

    [Tooltip("突進の基準速度です。大きくすると突進が速くなり、避けにくくなります。")]
    public float chargeSpeed = 24f;

    [Tooltip("プレイヤーをどれくらい通り過ぎるかです。大きくするとプレイヤーの奥まで走り抜けます。")]
    public float chargeOvershootDistance = 4f;

    [Tooltip("突進時間の最小値です。小さすぎると短距離突進が一瞬で終わります。")]
    public float chargeMinDuration = 0.75f;

    [Tooltip("突進時間の最大値です。大きくすると遠距離突進が長く続きます。")]
    public float chargeMaxDuration = 1.8f;

    [Tooltip("この距離未満から突進する場合は、先に1から3回バックステップします。")]
    public float chargeBackStepIfCloserThan = 9f;

    [Tooltip("突進前の溜め時間です。大きくすると予兆が長くなり、避けやすくなります。")]
    public float chargeTellTime = 1.0f;

    [Tooltip("突進溜め中のアニメーション速度です。小さくするとスローになり、溜めているように見えます。")]
    public float chargeTellAnimationSpeed = 0.18f;

    [Tooltip("溜め完了後、突進前に止まる時間です。大きくすると発射前の間が長くなります。")]
    public float chargeReadyPauseTime = 0.15f;

    [Tooltip("突進後の硬直時間です。大きくすると突進後の隙が増えます。")]
    public float chargeRecoveryTime = 0.45f;

    [Tooltip("二連突進の1回目と2回目の間隔です。大きくすると2回目までの間が長くなります。")]
    public float doubleChargeInterval = 0.55f;

    [Tooltip("二連突進後の大きめの隙です。大きくすると第2形態の二連突進後に反撃しやすくなります。")]
    public float doubleChargeRecovery = 1.4f;

    [Tooltip("突進序盤の加速割合です。大きくすると加速時間が長くなり、出始めが緩やかになります。")]
    [Range(0.01f, 0.5f)]
    public float chargeAccelerationRatio = 0.18f;

    [Tooltip("突進終盤の減速割合です。大きくすると減速時間が長くなり、止まり方が緩やかになります。")]
    [Range(0.01f, 0.5f)]
    public float chargeDecelerationRatio = 0.22f;

    [Header("突進演出")]
    [Tooltip("突進の溜め中に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem chargeHoldParticle;

    [Tooltip("突進準備完了時に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem chargeReadyParticle;

    [Tooltip("突進中に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem chargeRunParticle;

    [Tooltip("突進の溜め中に再生するSEです。未設定なら鳴りません。")]
    public AudioClip chargeHoldSfx;

    [Tooltip("突進準備完了時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip chargeReadySfx;

    [Tooltip("突進開始時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip chargeRunSfx;

    [Tooltip("突進終了時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip chargeEndSfx;

    [Header("ブレス共通")]
    [Tooltip("ブレス前にプレイヤーへ向き直る時間です。大きくすると発射前にしっかり向き直ります。")]
    public float breathTurnTime = 0.35f;

    [Tooltip("ブレスモーション全体の長さです。実際のアニメーション長に合わせてください。")]
    public float breathDuration = 4.2f;

    [Tooltip("ブレス中のAnimator速度です。1で通常速度です。小さくするとスロー、大きくすると高速になります。")]
    public float breathAnimatorSpeed = 1f;

    [Header("扇状ブレス")]
    [Tooltip("扇状ブレスの攻撃判定を入れてください。広いBox Colliderを使うと調整しやすいです。")]
    public DragonAttackHitbox wideBreathHitbox;

    [Tooltip("扇状ブレスの判定開始フレームです。小さくすると早く判定が出ます。")]
    public int wideBreathStartFrame = 35;

    [Tooltip("扇状ブレスの判定終了フレームです。大きくすると判定が長く残ります。")]
    public int wideBreathEndFrame = 120;

    [Tooltip("扇状ブレス後の隙です。大きくするとブレス後に反撃しやすくなります。")]
    public float wideBreathRecovery = 0.9f;

    [Tooltip("扇状ブレスの溜め中に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem wideBreathChargeParticle;

    [Tooltip("扇状ブレスの発射中に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem wideBreathFireParticle;

    [Tooltip("扇状ブレスの溜め中に再生するSEです。未設定なら鳴りません。")]
    public AudioClip wideBreathChargeSfx;

    [Tooltip("扇状ブレスの発射時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip wideBreathFireSfx;

    [Header("ビームブレス")]
    [Tooltip("ビームブレスの攻撃判定を入れてください。細長いBox Colliderを使うと調整しやすいです。")]
    public DragonAttackHitbox beamBreathHitbox;

    [Tooltip("ビームの向きを制御するTransformを入れてください。通常はBeamBreathPivotを入れます。")]
    public Transform beamBreathPivot;

    [Tooltip("BeamBreathPivotに付いているDragonBeamBreathAimerを入れてください。ビームの追尾方向を制御します。")]
    public DragonBeamBreathAimer beamBreathAimer;

    [Tooltip("ビームブレスの判定開始フレームです。小さくすると早く判定が出ます。")]
    public int beamBreathStartFrame = 45;

    [Tooltip("ビームブレスの判定終了フレームです。大きくすると判定が長く残ります。")]
    public int beamBreathEndFrame = 135;

    [Tooltip("ビームの追尾を始めるフレームです。小さくすると早く追尾し始めます。")]
    public int beamTrackStartFrame = 40;

    [Tooltip("ビームの追尾をやめるフレームです。大きくすると長くプレイヤーを追います。")]
    public int beamTrackEndFrame = 120;

    [Tooltip("ビームブレス後の隙です。大きくすると発射後に反撃しやすくなります。")]
    public float beamBreathRecovery = 1.2f;

    [Tooltip("ビームブレスの溜め中に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem beamBreathChargeParticle;

    [Tooltip("ビームブレスの発射中に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem beamBreathFireParticle;

    [Tooltip("ビームブレスの溜め中に再生するSEです。未設定なら鳴りません。")]
    public AudioClip beamBreathChargeSfx;

    [Tooltip("ビームブレスの発射時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip beamBreathFireSfx;

    [Header("尻尾攻撃の判定")]
    [Tooltip("尻尾攻撃の判定を入れてください。Tail SlamとTail Swipeで共通使用します。")]
    public DragonAttackHitbox tailHitbox;

    [Header("尻尾攻撃の距離")]
    [Tooltip("Tail Slamを選べる最大距離です。この距離より遠い場合はTail Slamを選びません。")]
    public float tailSlamReachDistance = 8.5f;

    [Tooltip("Tail Swipeを選べる最大距離です。この距離より遠い場合はTail Swipeを選びません。")]
    public float tailSwipeReachDistance = 8.5f;

    [Header("尻尾攻撃の向き調整")]
    [Tooltip("オンにすると尻尾攻撃時に尻尾側をプレイヤーへ向けます。オフにすると通常の向き補正になります。")]
    public bool tailAttacksTurnTailToPlayer = true;

    [Tooltip("尻尾をプレイヤーへ向けるための基本角度補正です。尻尾ではなく頭が向く場合は180前後を試してください。")]
    public float tailFacePlayerOffsetY = 0f;

    [Tooltip("Tail Slamの叩きつけ位置の角度補正です。狙いが左右にずれる場合に調整してください。")]
    public float tailSlamAttackOffsetY = 0f;

    [Tooltip("Tail Swipe前半で尻尾側を向ける固定角度です。左右ランダムは使わず、この値を基準にします。")]
    public float tailSwipeFixedAttackOffsetY = -10f;

    [Tooltip("尻尾攻撃時にプレイヤー方向へ回転する速度です。大きくすると素早く向きを合わせます。")]
    public float tailTrackingTurnSpeed = 16f;

    [Header("Tail Slam")]
    [Tooltip("Tail Slam全体の長さです。実際のアニメーション長に合わせてください。")]
    public float tailSlamDuration = 4.8f;

    [Tooltip("Tail Slamで狙いを合わせ始めるフレームです。")]
    public int tailSlamAimStartFrame = 5;

    [Tooltip("Tail Slamでプレイヤー追尾を続ける最後のフレームです。大きくすると直前まで狙います。")]
    public int tailSlamTrackUntilFrame = 88;

    [Tooltip("Tail Slamの判定開始フレームです。")]
    public int tailSlamHitStartFrame = 73;

    [Tooltip("Tail Slamの判定終了フレームです。大きくすると判定が長く残ります。")]
    public int tailSlamHitEndFrame = 82;

    [Tooltip("Tail Slam後にプレイヤー正面へ戻り始めるフレームです。遅くすると戻りジャンプの違和感が減ります。")]
    public int tailSlamReturnStartFrame = 120;

    [Tooltip("Tail Slam後にプレイヤー正面へ戻り終わるフレームです。大きくするとゆっくり戻ります。")]
    public int tailSlamReturnEndFrame = 155;

    [Tooltip("尻尾を直接プレイヤーに向けない設定の時に使う角度補正です。")]
    public float tailSlamAngleOffset = 0f;

    [Tooltip("Tail Slam開始時に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem tailSlamParticle;

    [Tooltip("Tail Slam開始時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip tailSlamSfx;

    [Header("Tail Swipe")]
    [Tooltip("Tail Swipe全体の長さです。実際のアニメーション長に合わせてください。")]
    public float tailSwipeDuration = 4.6f;

    [Tooltip("Tail Swipeで狙いを合わせ始めるフレームです。")]
    public int tailSwipeAimStartFrame = 8;

    [Tooltip("Tail Swipeでプレイヤー追尾を続ける最後のフレームです。大きくすると直前まで狙います。")]
    public int tailSwipeTrackUntilFrame = 77;

    [Tooltip("Tail Swipeの1段目判定開始フレームです。")]
    public int tailSwipeSlamHitStartFrame = 77;

    [Tooltip("Tail Swipeの1段目判定終了フレームです。")]
    public int tailSwipeSlamHitEndFrame = 87;

    [Tooltip("Tail Swipeの2段目判定開始フレームです。通常は後半回転開始フレームと近い値にします。")]
    public int tailSwipeSecondHitStartFrame = 100;

    [Tooltip("Tail Swipeの2段目判定終了フレームです。大きくすると後半の判定が長く残ります。")]
    public int tailSwipeSecondHitEndFrame = 135;

    [Tooltip("Tail Swipe後半で追加する回転角度です。反時計回りにしたい場合は負の値を使います。逆方向に回る場合は正負を入れ替えてください。")]
    public float tailSwipeSecondHitExtraCounterClockwiseAngle = -310f;

    [Tooltip("Tail Swipe後半でプレイヤー方向を追う速度です。大きくすると素早く向きを合わせます。")]
    public float tailSwipeSecondHitTurnSpeed = 900f;

    [Tooltip("Tail Swipe後半回転を開始するフレームです。")]
    public int tailSwipeSecondTurnStartFrame = 100;

    [Tooltip("Tail Swipe後半回転を終了するフレームです。")]
    public int tailSwipeSecondTurnEndFrame = 135;

    [Tooltip("Tail Swipe開始時に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem tailSwipeParticle;

    [Tooltip("Tail Swipe開始時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip tailSwipeSfx;

    [Header("咆哮")]
    [Tooltip("咆哮アニメーションの長さです。実際のアニメーション長に合わせてください。")]
    public float roarDuration = 2.8f;

    [Tooltip("咆哮でプレイヤーを怯ませる半径です。大きくすると広い範囲に怯みが入ります。")]
    public float roarStaggerRadius = 15f;

    [Tooltip("咆哮でプレイヤーを怯ませる時間です。大きくすると長く操作不能になります。")]
    public float roarStaggerTime = 1.0f;

    [Tooltip("咆哮の対象にするプレイヤーLayerです。Playerレイヤーを指定してください。")]
    public LayerMask playerLayer;

    [Tooltip("咆哮時に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem roarParticle;

    [Tooltip("咆哮時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip roarSfx;

    [Header("ダウン・死亡")]
    [Tooltip("尻尾クリスタル破壊後のダウン時間です。大きくするとプレイヤーが攻撃できる時間が長くなります。")]
    public float downDuration = 9.11f;

    [Tooltip("ダウン時に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem downParticle;

    [Tooltip("死亡時に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem deathParticle;

    [Tooltip("ダウン時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip downSfx;

    [Tooltip("死亡時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip deathSfx;

    [Header("共通サウンド")]
    [Tooltip("SEを再生するAudioSourceです。未設定の場合は自分または親から自動で探します。")]
    public AudioSource audioSource;

    [Tooltip("このスクリプトから再生するSEの音量です。大きくするとSEが大きくなります。")]
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    private DragonState state = DragonState.Intro;
    private bool isBusy = false;
    private float lastChargeTime = -999f;
    private DragonAction lastAction = DragonAction.None;
    private DragonAction secondLastAction = DragonAction.None;

    private void Awake()
    {
        if (motion == null)
        {
            motion = GetComponent<DragonDragonMotion>();
        }

        if (phase == null)
        {
            phase = GetComponent<DragonPhaseController>();
        }

        if (dragonHP == null)
        {
            dragonHP = GetComponentInParent<DragonHP>();
        }

        if (effectPlayer == null)
        {
            effectPlayer = GetComponent<DragonAnimationEffectPlayer>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponentInParent<AudioSource>();
        }

        if (beamBreathAimer == null && beamBreathPivot != null)
        {
            beamBreathAimer = beamBreathPivot.GetComponent<DragonBeamBreathAimer>();
        }

        if (motion != null)
        {
            motion.SetPlayer(player);
        }

        if (beamBreathAimer != null)
        {
            beamBreathAimer.player = player;
        }
    }

    private void OnEnable()
    {
        if (dragonHP != null)
        {
            dragonHP.OnHalfHP += HandleHalfHP;
            dragonHP.OnTailCrystalBroken += HandleTailCrystalBroken;
            dragonHP.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (dragonHP != null)
        {
            dragonHP.OnHalfHP -= HandleHalfHP;
            dragonHP.OnTailCrystalBroken -= HandleTailCrystalBroken;
            dragonHP.OnDeath -= HandleDeath;
        }
    }

    private void Start()
    {
        DisableAllHitboxes();
        StopAllSpecialParticles();

        if (motion != null)
        {
            motion.ResetAnimatorSpeed();
        }

        StartCoroutine(IntroRoutine());
    }

    private void Update()
    {
        if (state == DragonState.Dead) return;
        if (state == DragonState.Down) return;
        if (isBusy) return;
        if (motion == null) return;

        motion.FacePlayerSmooth(motion.idleTurnSpeed);
    }

    private IEnumerator IntroRoutine()
    {
        state = DragonState.Intro;
        isBusy = true;

        motion.PlayAnim(motion.idleAnim, true);

        float timer = 0f;

        while (timer < introIdleBeforeRoarTime)
        {
            timer += Time.deltaTime;
            motion.FacePlayerSmooth(motion.idleTurnSpeed);
            yield return null;
        }

        yield return motion.FacePlayerForSeconds(0.25f);

        motion.PlayAnim(motion.roarAnim, true);
        DoRoarEffect();

        yield return new WaitForSeconds(roarDuration);

        ReturnToIdle();
        StartCoroutine(AILoop());
    }

    private IEnumerator AILoop()
    {
        while (state != DragonState.Dead)
        {
            if (state == DragonState.Down || isBusy || player == null || motion == null)
            {
                yield return null;
                continue;
            }

            float interval = Random.Range(minActionInterval, maxActionInterval);

            if (phase != null)
            {
                interval = phase.ApplyActionInterval(interval);
            }

            yield return new WaitForSeconds(interval);

            if (state == DragonState.Down || state == DragonState.Dead || isBusy)
            {
                continue;
            }

            yield return DecideAction();
        }
    }

    private IEnumerator DecideAction()
    {
        float distance = motion.GetDistanceToPlayer();

        if (ShouldDoOpening())
        {
            yield return ExecuteAction(DragonAction.Opening);
            yield break;
        }

        if (ShouldUseSwipeCounter(distance))
        {
            yield return ExecuteAction(DragonAction.SwipeCounter);
            yield break;
        }

        DragonAction action;

        if (distance < closeRange)
        {
            action = PickCloseAction(distance);
        }
        else if (distance < farRange)
        {
            action = PickMiddleAction(distance);
        }
        else
        {
            action = PickFarAction(distance);
        }

        yield return ExecuteAction(action);
    }

    private bool ShouldDoOpening()
    {
        if (!useOpeningIdle) return false;

        if (phase != null && phase.ShouldUsePhase2Opening())
        {
            return true;
        }

        return Random.value < openingIdleChance;
    }

    private bool ShouldUseSwipeCounter(float distance)
    {
        if (!enableSwipeCounterAction) return false;
        if (distance > swipeCounterReachDistance) return false;

        return Random.value < swipeCounterChance;
    }

    private DragonAction PickCloseAction(float distance)
    {
        WeightedActionPicker picker = new WeightedActionPicker();

        if (!IsTailBroken())
        {
            if (enableTailSlamAction && distance <= tailSlamReachDistance)
            {
                picker.Add(DragonAction.TailSlam, closeTailSlamWeight);
            }

            if (enableTailSwipeAction && distance <= tailSwipeReachDistance)
            {
                picker.Add(DragonAction.TailSwipe, closeTailSwipeWeight);
            }

            if (enableBackStepTailAction && enableTailSlamAction)
            {
                picker.Add(DragonAction.BackStepTailSlam, closeBackStepTailSlamWeight);
            }

            if (enableBackStepTailAction && enableTailSwipeAction)
            {
                picker.Add(DragonAction.BackStepTailSwipe, closeBackStepTailSwipeWeight);
            }
        }

        if (enableBackStepBreathAction && enableWideBreathAction)
        {
            picker.Add(DragonAction.BackStepWideBreath, closeBackStepWideBreathWeight);
        }

        if (enableBackStepBreathAction && enableBeamBreathAction)
        {
            picker.Add(DragonAction.BackStepBeamBreath, closeBackStepBeamBreathWeight);
        }

        if (CanUseCharge())
        {
            picker.Add(GetChargeAction(), closeChargeWeight);
        }

        if (enableApproachAction)
        {
            picker.Add(DragonAction.Approach, 1);
        }

        return PreventRepeat(picker.Pick());
    }

    private DragonAction PickMiddleAction(float distance)
    {
        WeightedActionPicker picker = new WeightedActionPicker();

        if (!IsTailBroken())
        {
            if (enableTailSlamAction && distance <= tailSlamReachDistance)
            {
                picker.Add(DragonAction.TailSlam, middleTailSlamWeight);
            }

            if (enableTailSwipeAction && distance <= tailSwipeReachDistance)
            {
                picker.Add(DragonAction.TailSwipe, middleTailSwipeWeight);
            }
        }

        if (enableWideBreathAction)
        {
            picker.Add(DragonAction.WideBreath, middleWideBreathWeight);
        }

        if (enableBeamBreathAction)
        {
            picker.Add(DragonAction.BeamBreath, middleBeamBreathWeight);
        }

        if (CanUseCharge())
        {
            picker.Add(GetChargeAction(), middleChargeWeight);
        }

        if (enableApproachAction && distance > approachStopDistance + 1f)
        {
            picker.Add(DragonAction.Approach, 1);
        }

        return PreventRepeat(picker.Pick());
    }

    private DragonAction PickFarAction(float distance)
    {
        WeightedActionPicker picker = new WeightedActionPicker();

        if (enableApproachAction)
        {
            picker.Add(DragonAction.Approach, farApproachWeight);
        }

        if (enableWideBreathAction)
        {
            picker.Add(DragonAction.WideBreath, farWideBreathWeight);
        }

        if (enableBeamBreathAction)
        {
            picker.Add(DragonAction.BeamBreath, farBeamBreathWeight);
        }

        if (CanUseCharge())
        {
            picker.Add(GetChargeAction(), farChargeWeight);
        }

        return PreventRepeat(picker.Pick());
    }

    private DragonAction GetChargeAction()
    {
        if (enableDoubleChargeAction && phase != null && phase.ShouldUseDoubleCharge())
        {
            return DragonAction.DoubleCharge;
        }

        return DragonAction.Charge;
    }

    private DragonAction PreventRepeat(DragonAction picked)
    {
        if (picked == DragonAction.None)
        {
            return enableApproachAction ? DragonAction.Approach : DragonAction.Opening;
        }

        if (picked != lastAction && picked != secondLastAction)
        {
            return picked;
        }

        if (picked == DragonAction.Charge || picked == DragonAction.DoubleCharge)
        {
            return enableApproachAction ? DragonAction.Approach : DragonAction.Opening;
        }

        return picked;
    }

    private IEnumerator ExecuteAction(DragonAction action)
    {
        RegisterAction(action);

        switch (action)
        {
            case DragonAction.Opening:
                yield return OpeningIdle();
                break;

            case DragonAction.Approach:
                yield return ApproachPlayer();
                break;

            case DragonAction.SwipeCounter:
                yield return SwipeCounterAttack();
                break;

            case DragonAction.TailSlam:
                yield return TailSlam();
                break;

            case DragonAction.TailSwipe:
                yield return TailSwipe();
                break;

            case DragonAction.BackStepTailSlam:
                yield return BackStepThenTailSlam();
                break;

            case DragonAction.BackStepTailSwipe:
                yield return BackStepThenTailSwipe();
                break;

            case DragonAction.BackStepWideBreath:
                yield return BackStepThenWideBreath();
                break;

            case DragonAction.BackStepBeamBreath:
                yield return BackStepThenBeamBreath();
                break;

            case DragonAction.WideBreath:
                yield return WideBreathAttack();
                break;

            case DragonAction.BeamBreath:
                yield return BeamBreathAttack();
                break;

            case DragonAction.Charge:
                yield return ChargeAttack(true);
                break;

            case DragonAction.DoubleCharge:
                yield return DoubleChargeAttack();
                break;

            default:
                yield return OpeningIdle();
                break;
        }
    }

    private void RegisterAction(DragonAction action)
    {
        secondLastAction = lastAction;
        lastAction = action;
    }

    private IEnumerator OpeningIdle()
    {
        isBusy = true;
        state = DragonState.Opening;

        DisableAllHitboxes();
        StopAllSpecialParticles();
        motion.ResetAnimatorSpeed();
        motion.PlayAnim(motion.idleAnim, true);

        float duration = Random.Range(openingIdleMinTime, openingIdleMaxTime);

        if (phase != null && phase.isPhase2)
        {
            duration = phase.GetPhase2OpeningTime();
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            if (lookAtPlayerDuringOpening)
            {
                motion.FacePlayerSmooth(motion.idleTurnSpeed);
            }

            yield return null;
        }

        ReturnToIdle();
    }

    private IEnumerator ApproachPlayer()
    {
        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();
        StopAllSpecialParticles();

        bool running = motion.GetDistanceToPlayer() >= switchToRunDistance;
        string moveAnim = running ? motion.runAnim : motion.walkAnim;

        motion.PlayAnim(moveAnim, true);

        float checkTimer = 0f;

        while (player != null && motion.GetDistanceToPlayer() > approachStopDistance)
        {
            float distance = motion.GetDistanceToPlayer();

            if (distance >= switchToRunDistance && !running)
            {
                running = true;
                moveAnim = motion.runAnim;
                motion.PlayAnim(moveAnim, true);
                checkTimer = 0f;
            }
            else if (running && distance <= runChaseStopDistance)
            {
                running = false;
                moveAnim = motion.walkAnim;
                motion.PlayAnim(moveAnim, true);
                checkTimer = 0f;
            }

            checkTimer += Time.deltaTime;
            motion.KeepMoveAnim(moveAnim, ref checkTimer);
            motion.FacePlayerSmooth(motion.actionTurnSpeed);

            float speed = running ? runChaseSpeed : walkSpeed;

            if (phase != null)
            {
                speed = phase.ApplySpeed(speed);
            }

            motion.MoveDragon(motion.GetMoveForward() * speed * Time.deltaTime);

            yield return null;
        }

        ReturnToIdle();
    }

    private IEnumerator DoRandomBackSteps()
    {
        int minCount = Mathf.Max(0, randomBackStepMinCount);
        int maxCount = Mathf.Max(minCount, randomBackStepMaxCount);
        int count = Random.Range(minCount, maxCount + 1);

        for (int i = 0; i < count; i++)
        {
            yield return motion.FacePlayerForSeconds(0.06f);

            Vector3 backDirection = -motion.GetMoveForward();

            yield return motion.MoveInDirectionForSeconds(
                backDirection,
                backStepDistance,
                backStepDuration,
                motion.stepAnim
            );

            yield return new WaitForSeconds(backStepInterval);
        }
    }

    private IEnumerator DoFixedBackSteps(int count)
    {
        int safeCount = Mathf.Max(0, count);

        for (int i = 0; i < safeCount; i++)
        {
            yield return motion.FacePlayerForSeconds(0.06f);

            Vector3 backDirection = -motion.GetMoveForward();

            yield return motion.MoveInDirectionForSeconds(
                backDirection,
                backStepDistance,
                backStepDuration,
                motion.stepAnim
            );

            yield return new WaitForSeconds(backStepInterval);
        }
    }

    private IEnumerator BackStepThenTailSlam()
    {
        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();
        StopAllSpecialParticles();

        yield return DoFixedBackSteps(closeSpecialBackStepCount);
        yield return TailSlam();
    }

    private IEnumerator BackStepThenTailSwipe()
    {
        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();
        StopAllSpecialParticles();

        yield return DoFixedBackSteps(closeSpecialBackStepCount);
        yield return TailSwipe();
    }

    private IEnumerator BackStepThenWideBreath()
    {
        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();
        StopAllSpecialParticles();

        yield return DoFixedBackSteps(closeSpecialBackStepCount);
        yield return WideBreathAttack(false);
    }

    private IEnumerator BackStepThenBeamBreath()
    {
        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();
        StopAllSpecialParticles();

        yield return DoFixedBackSteps(closeSpecialBackStepCount);
        yield return BeamBreathAttack(false);
    }

    private IEnumerator SwipeCounterAttack()
    {
        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();
        StopAllSpecialParticles();

        if (motion.GetDistanceToPlayer() > swipeCounterReachDistance)
        {
            ReturnToIdle();
            yield break;
        }

        yield return motion.FacePlayerForSeconds(swipeCounterFaceTime);

        bool left = Random.value > 0.5f;
        string animName = left ? motion.leftSwipeAnim : motion.rightSwipeAnim;
        DragonAttackHitbox hitbox = left ? leftArmHitbox : rightArmHitbox;

        motion.PlayAnim(animName, true);

        int effectiveAnticipationEndFrame = useAnalyzedSwipeFrames
            ? analyzedSwipeAnticipationEndFrame
            : swipeAnticipationEndFrame;

        int effectiveAttackEndFrame = useAnalyzedSwipeFrames
            ? analyzedSwipeAttackEndFrame
            : swipeHitEndFrame;

        effectiveAnticipationEndFrame = Mathf.Max(0, effectiveAnticipationEndFrame);
        effectiveAttackEndFrame = Mathf.Max(effectiveAnticipationEndFrame + 1, effectiveAttackEndFrame);

        float anticipationEndAnimTime = motion.FrameToSeconds(effectiveAnticipationEndFrame);
        float attackEndAnimTime = motion.FrameToSeconds(effectiveAttackEndFrame);
        float lungeStart = motion.FrameToSeconds(swipeLungeStartFrame);
        float lungeEnd = motion.FrameToSeconds(swipeLungeEndFrame);
        float configuredHitStart = motion.FrameToSeconds(swipeHitStartFrame);

        float rawAnticipationDuration = Mathf.Max(0.01f, anticipationEndAnimTime);
        float rawAttackDuration = Mathf.Max(0.01f, attackEndAnimTime - anticipationEndAnimTime);
        float targetAnticipationDuration = Mathf.Max(0.01f, swipeAnticipationDuration);
        float targetAttackDuration = Mathf.Max(0.01f, swipeAttackDuration);

        bool shouldStretchAnticipation = stretchSwipeAnticipation && targetAnticipationDuration > rawAnticipationDuration;
        bool shouldStretchAttack = stretchSwipeAttack && targetAttackDuration > rawAttackDuration;

        if (!shouldStretchAnticipation)
        {
            targetAnticipationDuration = rawAnticipationDuration;
        }

        if (!shouldStretchAttack)
        {
            targetAttackDuration = rawAttackDuration;
        }

        if (swipeAnticipationParticle != null)
        {
            swipeAnticipationParticle.Play();
        }

        PlaySfx(swipeAnticipationSfx);

        if (shouldStretchAnticipation)
        {
            float anticipationAnimatorSpeed = rawAnticipationDuration / targetAnticipationDuration;
            motion.SetAnimatorSpeed(anticipationAnimatorSpeed);
        }
        else
        {
            motion.ResetAnimatorSpeed();
        }

        float anticipationTimer = 0f;

        while (anticipationTimer < targetAnticipationDuration)
        {
            anticipationTimer += Time.deltaTime;
            motion.FacePlayerSmooth(swipeAnticipationTurnSpeed);
            yield return null;
        }

        if (swipeAnticipationParticle != null)
        {
            swipeAnticipationParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (swipeParticle != null)
        {
            swipeParticle.Play();
        }

        PlaySfx(swipeSfx);

        if (shouldStretchAttack)
        {
            float attackAnimatorSpeed = rawAttackDuration / targetAttackDuration;
            motion.SetAnimatorSpeed(attackAnimatorSpeed);
        }
        else
        {
            motion.ResetAnimatorSpeed();
        }

        float attackAnimTimer = anticipationEndAnimTime;
        float realAttackPhaseTimer = 0f;
        float previousLunge = 0f;
        bool hitboxEnabled = false;

        float hitDelayAfterAttackStart = swipeHitboxStartsAtAttackPhase
            ? Mathf.Max(0f, swipeHitboxDelayAfterAnticipation)
            : Mathf.Max(0f, configuredHitStart - anticipationEndAnimTime);

        float hitboxEndAfterAttackStart = Mathf.Max(0.03f, targetAttackDuration);

        if (hitDelayAfterAttackStart <= 0f)
        {
            hitboxEnabled = true;
            if (hitbox != null) hitbox.EnableHitbox();
        }

        while (realAttackPhaseTimer < targetAttackDuration)
        {
            float deltaTime = Time.deltaTime;
            realAttackPhaseTimer += deltaTime;

            float attackT = Mathf.Clamp01(realAttackPhaseTimer / targetAttackDuration);
            attackAnimTimer = Mathf.Lerp(anticipationEndAnimTime, attackEndAnimTime, attackT);

            if (swipeForwardDistance > 0f && attackAnimTimer >= lungeStart && attackAnimTimer <= lungeEnd)
            {
                motion.FacePlayerSmooth(swipeLungeTurnSpeed);

                Vector3 lungeDirection = motion.GetDirectionToPlayer();

                float t = Mathf.InverseLerp(lungeStart, lungeEnd, attackAnimTimer);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                float currentLunge = swipeForwardDistance * eased;
                float delta = currentLunge - previousLunge;
                previousLunge = currentLunge;

                motion.MoveDragon(lungeDirection * delta);
            }

            if (!hitboxEnabled && realAttackPhaseTimer >= hitDelayAfterAttackStart)
            {
                hitboxEnabled = true;
                if (hitbox != null) hitbox.EnableHitbox();
            }

            if (hitboxEnabled && realAttackPhaseTimer >= hitboxEndAfterAttackStart)
            {
                hitboxEnabled = false;
                if (hitbox != null) hitbox.DisableHitbox();
            }

            yield return null;
        }

        if (hitbox != null)
        {
            hitbox.DisableHitbox();
        }

        motion.ResetAnimatorSpeed();

        float afterAttackDuration = Mathf.Max(0f, swipeAnimDuration - attackEndAnimTime);
        if (afterAttackDuration > 0f)
        {
            yield return new WaitForSeconds(afterAttackDuration);
        }

        if (swipeAnticipationParticle != null)
        {
            swipeAnticipationParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        motion.ResetAnimatorSpeed();

        ReturnToIdle();
    }

    private IEnumerator WideBreathAttack(bool allowAutoBackStep = true)
    {
        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();
        StopAllSpecialParticles();

        if (allowAutoBackStep && motion.GetDistanceToPlayer() < breathBackStepIfCloserThan)
        {
            yield return DoRandomBackSteps();
        }

        yield return motion.FacePlayerForSeconds(breathTurnTime);

        motion.SetAnimatorSpeed(breathAnimatorSpeed);
        motion.PlayAnim(motion.breathAnim, true);

        if (wideBreathChargeParticle != null) wideBreathChargeParticle.Play();
        PlaySfx(wideBreathChargeSfx);

        float start = motion.FrameToSeconds(wideBreathStartFrame);
        float end = motion.FrameToSeconds(wideBreathEndFrame);

        float timer = 0f;

        while (timer < start)
        {
            timer += Time.deltaTime;
            motion.FacePlayerSmooth(motion.actionTurnSpeed);
            yield return null;
        }

        if (wideBreathChargeParticle != null)
        {
            wideBreathChargeParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (wideBreathFireParticle != null) wideBreathFireParticle.Play();
        PlaySfx(wideBreathFireSfx);

        if (wideBreathHitbox != null) wideBreathHitbox.EnableHitbox();

        yield return new WaitForSeconds(Mathf.Max(0f, end - start));

        if (wideBreathHitbox != null) wideBreathHitbox.DisableHitbox();

        if (wideBreathFireParticle != null)
        {
            wideBreathFireParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, breathDuration - end));
        yield return new WaitForSeconds(wideBreathRecovery);

        ReturnToIdle();
    }

    private IEnumerator BeamBreathAttack(bool allowAutoBackStep = true)
    {
        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();
        StopAllSpecialParticles();

        if (allowAutoBackStep && motion.GetDistanceToPlayer() < breathBackStepIfCloserThan)
        {
            yield return DoRandomBackSteps();
        }

        yield return motion.FacePlayerForSeconds(breathTurnTime);

        motion.SetAnimatorSpeed(breathAnimatorSpeed);
        motion.PlayAnim(motion.breathAnim, true);

        if (beamBreathAimer != null)
        {
            beamBreathAimer.player = player;
            beamBreathAimer.AimInstant();
        }

        if (beamBreathChargeParticle != null) beamBreathChargeParticle.Play();
        PlaySfx(beamBreathChargeSfx);

        float start = motion.FrameToSeconds(beamBreathStartFrame);
        float end = motion.FrameToSeconds(beamBreathEndFrame);
        float trackStart = motion.FrameToSeconds(beamTrackStartFrame);
        float trackEnd = motion.FrameToSeconds(beamTrackEndFrame);

        float timer = 0f;
        bool hitboxEnabled = false;
        bool fireParticlePlayed = false;

        while (timer < breathDuration)
        {
            timer += Time.deltaTime;

            if (timer < trackStart)
            {
                motion.FacePlayerSmooth(motion.actionTurnSpeed);
            }

            if (timer >= trackStart && timer <= trackEnd)
            {
                if (beamBreathAimer != null)
                {
                    beamBreathAimer.AimSmooth();
                }
            }

            if (!fireParticlePlayed && timer >= start)
            {
                fireParticlePlayed = true;

                if (beamBreathChargeParticle != null)
                {
                    beamBreathChargeParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }

                if (beamBreathFireParticle != null) beamBreathFireParticle.Play();
                PlaySfx(beamBreathFireSfx);
            }

            if (!hitboxEnabled && timer >= start)
            {
                hitboxEnabled = true;
                if (beamBreathHitbox != null) beamBreathHitbox.EnableHitbox();
            }

            if (hitboxEnabled && timer >= end)
            {
                hitboxEnabled = false;
                if (beamBreathHitbox != null) beamBreathHitbox.DisableHitbox();
            }

            yield return null;
        }

        if (beamBreathHitbox != null) beamBreathHitbox.DisableHitbox();

        if (beamBreathFireParticle != null)
        {
            beamBreathFireParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        yield return new WaitForSeconds(beamBreathRecovery);

        ReturnToIdle();
    }

    private IEnumerator ChargeAttack(bool consumeCooldown)
    {
        isBusy = true;
        state = DragonState.Acting;

        DisableAllHitboxes();
        StopAllSpecialParticles();
        motion.ResetAnimatorSpeed();

        if (consumeCooldown)
        {
            lastChargeTime = Time.time;
        }

        if (motion.GetDistanceToPlayer() < chargeBackStepIfCloserThan)
        {
            yield return DoRandomBackSteps();
        }

        yield return motion.FacePlayerForSeconds(0.15f);

        motion.PlayAnim(motion.runAnim, true);
        motion.SetAnimatorSpeed(chargeTellAnimationSpeed);

        if (chargeHoldParticle != null) chargeHoldParticle.Play();
        PlaySfx(chargeHoldSfx);

        float tellTimer = 0f;
        float checkTimer = 0f;

        while (tellTimer < chargeTellTime)
        {
            tellTimer += Time.deltaTime;
            checkTimer += Time.deltaTime;

            motion.KeepMoveAnim(motion.runAnim, ref checkTimer);
            motion.SetAnimatorSpeed(chargeTellAnimationSpeed);
            motion.FacePlayerSmooth(motion.actionTurnSpeed);

            yield return null;
        }

        if (chargeHoldParticle != null)
        {
            chargeHoldParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (chargeReadyParticle != null) chargeReadyParticle.Play();
        PlaySfx(chargeReadySfx);

        motion.ResetAnimatorSpeed();
        motion.PlayAnim(motion.runAnim, true);

        yield return new WaitForSeconds(chargeReadyPauseTime);

        Vector3 chargeDirection = motion.GetDirectionToPlayer();
        float targetDistance = GetChargeTargetDistance(chargeDirection);

        float baseSpeed = chargeSpeed;

        if (phase != null)
        {
            baseSpeed = phase.ApplyChargeSpeed(baseSpeed);
        }

        float chargeTime = Mathf.Clamp(targetDistance / Mathf.Max(0.01f, baseSpeed), chargeMinDuration, chargeMaxDuration);

        float timer = 0f;
        float previousDistance = 0f;
        float runCheckTimer = 0f;

        if (chargeRunParticle != null) chargeRunParticle.Play();
        PlaySfx(chargeRunSfx);

        if (chargeHitbox != null) chargeHitbox.EnableHitbox();

        motion.PlayAnim(motion.runAnim, true);
        motion.ResetAnimatorSpeed();

        while (timer < chargeTime)
        {
            timer += Time.deltaTime;
            runCheckTimer += Time.deltaTime;

            motion.KeepMoveAnim(motion.runAnim, ref runCheckTimer);

            float t = Mathf.Clamp01(timer / chargeTime);
            float eased = EvaluateChargeMoveCurve(t);
            float currentDistance = targetDistance * eased;
            float deltaDistance = currentDistance - previousDistance;
            previousDistance = currentDistance;

            motion.MoveDragon(chargeDirection * deltaDistance);

            yield return null;
        }

        if (chargeHitbox != null) chargeHitbox.DisableHitbox();

        StopAllChargeParticles();
        motion.ResetAnimatorSpeed();
        PlaySfx(chargeEndSfx);

        yield return new WaitForSeconds(chargeRecoveryTime);

        ReturnToIdle();
    }

    private IEnumerator DoubleChargeAttack()
    {
        if (!CanUseCharge())
        {
            yield return ApproachPlayer();
            yield break;
        }

        yield return ChargeAttack(true);

        isBusy = true;
        state = DragonState.Acting;

        yield return new WaitForSeconds(doubleChargeInterval);

        if (state == DragonState.Dead || state == DragonState.Down) yield break;

        yield return ChargeAttack(false);

        isBusy = true;
        state = DragonState.Acting;

        yield return new WaitForSeconds(doubleChargeRecovery);

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

    private IEnumerator TailSlam()
    {
        if (IsTailBroken())
        {
            yield return ApproachPlayer();
            yield break;
        }

        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();
        StopAllSpecialParticles();

        if (motion.GetDistanceToPlayer() > tailSlamReachDistance)
        {
            ReturnToIdle();
            yield break;
        }

        motion.PlayAnim(motion.tailSlamAnim, true);

        if (tailSlamParticle != null) tailSlamParticle.Play();
        PlaySfx(tailSlamSfx);

        float timer = 0f;

        float aimStartTime = motion.FrameToSeconds(tailSlamAimStartFrame);
        float trackEndTime = motion.FrameToSeconds(tailSlamTrackUntilFrame);
        float hitStartTime = motion.FrameToSeconds(tailSlamHitStartFrame);
        float hitEndTime = motion.FrameToSeconds(tailSlamHitEndFrame);
        float returnStartTime = motion.FrameToSeconds(tailSlamReturnStartFrame);
        float returnEndTime = motion.FrameToSeconds(tailSlamReturnEndFrame);

        bool hitboxEnabled = false;
        bool returnStarted = false;
        Quaternion returnStartRotation = transform.rotation;

        while (timer < tailSlamDuration)
        {
            timer += Time.deltaTime;

            if (timer >= aimStartTime && timer <= trackEndTime)
            {
                Quaternion targetRotation;

                if (tailAttacksTurnTailToPlayer)
                {
                    targetRotation = motion.GetTailRotationToPlayer(tailFacePlayerOffsetY, tailSlamAttackOffsetY);
                }
                else
                {
                    targetRotation = motion.GetRotationToPlayerWithOffset(tailSlamAngleOffset);
                }

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, tailTrackingTurnSpeed * Time.deltaTime);
            }

            if (timer >= returnStartTime && timer <= returnEndTime)
            {
                if (!returnStarted)
                {
                    returnStarted = true;
                    returnStartRotation = transform.rotation;
                }

                Quaternion targetRotation = motion.GetRotationToPlayerWithOffset(0f);

                float returnDuration = Mathf.Max(0.01f, returnEndTime - returnStartTime);
                float t = Mathf.Clamp01((timer - returnStartTime) / returnDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                Quaternion easedRotation = Quaternion.Slerp(returnStartRotation, targetRotation, smoothT);
                transform.rotation = Quaternion.Slerp(transform.rotation, easedRotation, tailTrackingTurnSpeed * Time.deltaTime);
            }

            bool shouldEnableHitbox = timer >= hitStartTime && timer <= hitEndTime;

            if (shouldEnableHitbox && !hitboxEnabled)
            {
                hitboxEnabled = true;
                if (tailHitbox != null) tailHitbox.EnableHitbox();
            }
            else if (!shouldEnableHitbox && hitboxEnabled)
            {
                hitboxEnabled = false;
                if (tailHitbox != null) tailHitbox.DisableHitbox();
            }

            yield return null;
        }

        if (tailHitbox != null) tailHitbox.DisableHitbox();

        ReturnToIdle();
    }

    private IEnumerator TailSwipe()
    {
        if (IsTailBroken())
        {
            yield return ApproachPlayer();
            yield break;
        }

        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();
        StopAllSpecialParticles();

        if (motion.GetDistanceToPlayer() > tailSwipeReachDistance)
        {
            ReturnToIdle();
            yield break;
        }

        motion.PlayAnim(motion.tailSwipeAnim, true);

        if (tailSwipeParticle != null) tailSwipeParticle.Play();
        PlaySfx(tailSwipeSfx);

        float timer = 0f;

        float aimStartTime = motion.FrameToSeconds(tailSwipeAimStartFrame);
        float trackEndTime = motion.FrameToSeconds(tailSwipeTrackUntilFrame);
        float firstHitStartTime = motion.FrameToSeconds(tailSwipeSlamHitStartFrame);
        float firstHitEndTime = motion.FrameToSeconds(tailSwipeSlamHitEndFrame);
        float secondHitStartTime = motion.FrameToSeconds(tailSwipeSecondHitStartFrame);
        float secondHitEndTime = motion.FrameToSeconds(tailSwipeSecondHitEndFrame);
        float secondTurnStartTime = motion.FrameToSeconds(tailSwipeSecondTurnStartFrame);
        float secondTurnEndTime = motion.FrameToSeconds(tailSwipeSecondTurnEndFrame);

        bool hitboxEnabled = false;
        bool secondTurnStarted = false;
        Quaternion secondTurnStartRotation = transform.rotation;

        while (timer < tailSwipeDuration)
        {
            timer += Time.deltaTime;

            if (timer >= aimStartTime && timer <= trackEndTime)
            {
                Quaternion targetRotation;

                if (tailAttacksTurnTailToPlayer)
                {
                    targetRotation = motion.GetTailRotationToPlayer(tailFacePlayerOffsetY, tailSwipeFixedAttackOffsetY);
                }
                else
                {
                    targetRotation = motion.GetRotationToPlayerWithOffset(tailSwipeFixedAttackOffsetY);
                }

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, tailTrackingTurnSpeed * Time.deltaTime);
            }

            if (timer >= secondTurnStartTime && timer <= secondTurnEndTime)
            {
                if (!secondTurnStarted)
                {
                    secondTurnStarted = true;
                    secondTurnStartRotation = transform.rotation;
                }

                Quaternion lookPlayer = motion.GetRotationToPlayerWithOffset(0f);
                Quaternion counterClockwiseAdd = Quaternion.Euler(0f, tailSwipeSecondHitExtraCounterClockwiseAngle, 0f);
                Quaternion targetRotation = lookPlayer * counterClockwiseAdd;

                float turnDuration = Mathf.Max(0.01f, secondTurnEndTime - secondTurnStartTime);
                float turnT = Mathf.Clamp01((timer - secondTurnStartTime) / turnDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, turnT);

                Quaternion easedRotation = Quaternion.Slerp(secondTurnStartRotation, targetRotation, smoothT);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    easedRotation,
                    tailSwipeSecondHitTurnSpeed * Time.deltaTime
                );
            }

            bool shouldEnableHitbox =
                (timer >= firstHitStartTime && timer <= firstHitEndTime) ||
                (timer >= secondHitStartTime && timer <= secondHitEndTime);

            if (shouldEnableHitbox && !hitboxEnabled)
            {
                hitboxEnabled = true;
                if (tailHitbox != null) tailHitbox.EnableHitbox();
            }
            else if (!shouldEnableHitbox && hitboxEnabled)
            {
                hitboxEnabled = false;
                if (tailHitbox != null) tailHitbox.DisableHitbox();
            }

            yield return null;
        }

        if (tailHitbox != null) tailHitbox.DisableHitbox();

        ReturnToIdle();
    }

    private void HandleHalfHP()
    {
        if (state == DragonState.Dead) return;

        if (phase != null)
        {
            phase.EnterPhase2();
        }

        StopAllCoroutines();
        StartCoroutine(HalfHPRoarRoutine());
    }

    private IEnumerator HalfHPRoarRoutine()
    {
        DisableAllHitboxes();
        StopAllSpecialParticles();

        if (motion != null)
        {
            motion.ResetAnimatorSpeed();
        }

        isBusy = true;
        state = DragonState.Acting;

        yield return motion.FacePlayerForSeconds(0.3f);

        motion.PlayAnim(motion.roarAnim, true);
        DoRoarEffect();

        yield return new WaitForSeconds(roarDuration);

        ReturnToIdle();
        StartCoroutine(AILoop());
    }

    private void HandleTailCrystalBroken()
    {
        if (state == DragonState.Dead) return;

        StopAllCoroutines();
        StartCoroutine(DownRoutine());
    }

    private IEnumerator DownRoutine()
    {
        DisableAllHitboxes();
        StopAllSpecialParticles();

        if (motion != null)
        {
            motion.ResetAnimatorSpeed();
        }

        isBusy = true;
        state = DragonState.Down;

        motion.PlayAnim(motion.downAnim, true);

        if (downParticle != null) downParticle.Play();
        PlaySfx(downSfx);

        yield return new WaitForSeconds(downDuration);

        ReturnToIdle();
        StartCoroutine(AILoop());
    }

    private void HandleDeath()
    {
        StopAllCoroutines();

        DisableAllHitboxes();
        StopAllSpecialParticles();

        if (motion != null)
        {
            motion.ResetAnimatorSpeed();
        }

        state = DragonState.Dead;
        isBusy = true;

        if (motion != null)
        {
            motion.PlayAnim(motion.deathAnim, true);
        }

        if (deathParticle != null) deathParticle.Play();
        PlaySfx(deathSfx);
    }

    private bool IsTailBroken()
    {
        return dragonHP != null && dragonHP.isTailCrystalBroken;
    }

    private bool CanUseCharge()
    {
        if (!enableChargeAction) return false;
        if (Time.time < lastChargeTime + chargeCooldown) return false;

        return true;
    }

    private void ReturnToIdle()
    {
        DisableAllHitboxes();
        StopAllSpecialParticles();

        if (motion != null)
        {
            motion.ResetAnimatorSpeed();
            motion.PlayAnim(motion.idleAnim, true);
        }

        isBusy = false;

        if (state != DragonState.Dead && state != DragonState.Down)
        {
            state = DragonState.Idle;
        }
    }

    private void DisableAllHitboxes()
    {
        if (chargeHitbox != null) chargeHitbox.DisableHitbox();
        if (leftArmHitbox != null) leftArmHitbox.DisableHitbox();
        if (rightArmHitbox != null) rightArmHitbox.DisableHitbox();
        if (tailHitbox != null) tailHitbox.DisableHitbox();
        if (wideBreathHitbox != null) wideBreathHitbox.DisableHitbox();
        if (beamBreathHitbox != null) beamBreathHitbox.DisableHitbox();
    }

    private void StopAllSpecialParticles()
    {
        StopAllChargeParticles();
        StopAllBreathParticles();
        StopAllSwipeParticles();
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

    private void StopAllBreathParticles()
    {
        if (wideBreathChargeParticle != null)
        {
            wideBreathChargeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (wideBreathFireParticle != null)
        {
            wideBreathFireParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (beamBreathChargeParticle != null)
        {
            beamBreathChargeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (beamBreathFireParticle != null)
        {
            beamBreathFireParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void StopAllSwipeParticles()
    {
        if (swipeAnticipationParticle != null)
        {
            swipeAnticipationParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (swipeParticle != null)
        {
            swipeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void DoRoarEffect()
    {
        if (roarParticle != null) roarParticle.Play();
        PlaySfx(roarSfx);

        Collider[] hits = Physics.OverlapSphere(transform.position, roarStaggerRadius, playerLayer);

        foreach (Collider hit in hits)
        {
            hit.SendMessage("DragonStagger", roarStaggerTime, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;

        if (effectPlayer != null)
        {
            effectPlayer.PlayCustomSfx(clip);
            return;
        }

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip, sfxVolume);
        }
    }

    private class WeightedActionPicker
    {
        private readonly List<DragonAction> actions = new List<DragonAction>();

        public void Add(DragonAction action, int weight)
        {
            int safeWeight = Mathf.Max(0, weight);

            for (int i = 0; i < safeWeight; i++)
            {
                actions.Add(action);
            }
        }

        public DragonAction Pick()
        {
            if (actions.Count == 0)
            {
                return DragonAction.Opening;
            }

            return actions[Random.Range(0, actions.Count)];
        }
    }
}
