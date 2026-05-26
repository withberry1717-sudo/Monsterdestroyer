using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DragonAI : MonoBehaviour
{
    private enum DragonState { Intro, Idle, Acting, Opening, Down, Dead }
    private enum DragonAction
    {
        Opening,
        WideBreath,
        BeamBreath,
        Charge,
        DoubleCharge,
        Swipe,
        TailSlam,
        TailSwipe,
        BackStepBreath,
        SideStepSwipe,
        SideStepTailSlam,
        SideStepBreath,
        RapidCombo,
        Approach
    }

    [Header("参照設定")]
    [Tooltip("Dragon_AllObjectsについているDragonDragonMotionを入れてください。移動、回転、アニメーション再生を担当します。")]
    public DragonDragonMotion motion;

    [Tooltip("Dragon_AllObjectsについているDragonPhaseControllerを入れてください。HP50%以下の強化状態を管理します。")]
    public DragonPhaseController phase;

    [Tooltip("DragonCoreについているDragonHPを入れてください。本体HP、尻尾クリスタル破壊、死亡イベントを受け取ります。")]
    public DragonHP dragonHP;

    [Tooltip("Dragon_AllObjectsについているDragonAnimationEffectPlayerを入れてください。未設定でもAudioSourceがあればSE再生できます。")]
    public DragonAnimationEffectPlayer effectPlayer;

    [Tooltip("プレイヤー本体のTransformを入れてください。ドラゴンの追跡、向き調整、攻撃対象の基準になります。")]
    public Transform player;

    [Header("イントロ")]
    [Tooltip("戦闘開始後、咆哮する前に待機する秒数です。大きくすると戦闘開始までの間が長くなります。")]
    public float introIdleBeforeRoarTime = 3f;

    [Header("距離判定")]
    [Tooltip("近距離判定の距離です。プレイヤーとの距離がこの値未満なら近距離行動を選びます。大きくすると近距離攻撃を始めやすくなります。")]
    public float closeRange = 7f;

    [Tooltip("中距離判定の距離です。この距離以上で遠距離未満なら中距離行動を選びます。大きくすると中距離行動の範囲が広くなります。")]
    public float middleRange = 15f;

    [Tooltip("遠距離判定の距離です。この距離より遠いと遠距離行動や接近を選びます。大きくすると遠くからでもブレスや接近を選びやすくなります。")]
    public float farRange = 25f;

    [Tooltip("接近行動をやめる距離です。大きくするとプレイヤーから離れた位置で止まり、小さくすると近くまで詰めます。")]
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

    [Header("近接攻撃前の位置調整")]
    [Tooltip("オンにすると、ブレス以外の攻撃前に近接攻撃が届く距離まで接近します。オフにするとその場で攻撃しやすくなります。")]
    public bool approachBeforeNonBreathAttack = true;

    [Tooltip("近接攻撃を開始する距離です。大きくすると遠めから攻撃し、小さくすると近くまで寄ってから攻撃します。")]
    public float meleeAttackStartDistance = 5.5f;

    [Tooltip("近接攻撃前に距離を詰める速度です。大きくすると攻撃前の接近が速くなります。")]
    public float meleeApproachSpeed = 4.0f;

    [Tooltip("近接攻撃前にプレイヤーへ向き直る速度です。大きくすると素早くプレイヤーを向きます。")]
    public float meleeApproachTurnSpeed = 7f;

    [Tooltip("近接攻撃前の接近を諦めるまでの秒数です。大きくすると長く追い、小さくすると早めに攻撃へ移ります。")]
    public float meleeApproachTimeout = 4f;

    [Header("行動間隔")]
    [Tooltip("行動後に次の行動まで待つ最短秒数です。大きくすると攻撃頻度が下がります。")]
    public float minActionInterval = 0.6f;

    [Tooltip("行動後に次の行動まで待つ最長秒数です。大きくすると攻撃の間が長くなります。")]
    public float maxActionInterval = 1.3f;

    [Header("待機・隙行動")]
    [Tooltip("オンにすると、確率で何もせず待機する隙行動を行います。不要ならオフにしてください。")]
    public bool useOpeningIdle = false;

    [Tooltip("通常時に隙行動を選ぶ確率です。0なら出ません。大きくするとプレイヤーが攻撃できる隙が増えます。")]
    [Range(0f, 1f)] public float openingIdleChance = 0f;

    [Tooltip("隙行動の最短時間です。大きくすると短い隙でも長くなります。")]
    public float openingIdleMinTime = 1.0f;

    [Tooltip("隙行動の最長時間です。大きくすると長い隙が発生しやすくなります。")]
    public float openingIdleMaxTime = 2.2f;

    [Tooltip("オンにすると、隙行動中もプレイヤーの方を向きます。オフにすると向きを固定します。")]
    public bool lookAtPlayerDuringOpening = true;

    [Header("行動抽選の重み")]
    [Tooltip("近距離で腕攻撃を選ぶ重みです。大きくすると腕攻撃が出やすくなります。")]
    public int closeSwipeWeight = 4;

    [Tooltip("近距離でTail Slamを選ぶ重みです。大きくすると尻尾叩きつけが出やすくなります。")]
    public int closeTailSlamWeight = 2;

    [Tooltip("近距離でTail Swipeを選ぶ重みです。大きくすると尻尾なぎ払いが出やすくなります。")]
    public int closeTailSwipeWeight = 2;

    [Tooltip("近距離で横ステップ後の腕攻撃を選ぶ重みです。大きくすると回り込み攻撃が増えます。")]
    public int closeSideStepSwipeWeight = 2;

    [Tooltip("近距離でバックステップ後ブレスを選ぶ重みです。大きくすると距離を取ってからブレスを使いやすくなります。")]
    public int closeBackStepBreathWeight = 2;

    [Tooltip("近距離で突進を選ぶ重みです。大きくすると近距離でも突進しやすくなります。突進頻度はCharge Cooldownにも制限されます。")]
    public int closeChargeWeight = 1;

    [Tooltip("中距離で腕攻撃を選ぶ重みです。大きくすると接近して腕攻撃しやすくなります。")]
    public int middleSwipeWeight = 3;

    [Tooltip("中距離でブレスを選ぶ重みです。大きくするとブレス攻撃が増えます。")]
    public int middleBreathWeight = 2;

    [Tooltip("中距離で尻尾攻撃を選ぶ重みです。大きくするとTail SlamやTail Swipeが増えます。")]
    public int middleTailWeight = 2;

    [Tooltip("中距離でステップ系行動を選ぶ重みです。大きくすると横ステップやステップ後攻撃が増えます。")]
    public int middleStepWeight = 2;

    [Tooltip("中距離で突進を選ぶ重みです。大きくすると突進が増えます。突進頻度はCharge Cooldownにも制限されます。")]
    public int middleChargeWeight = 1;

    [Tooltip("遠距離でブレスを選ぶ重みです。大きくすると遠距離からブレスを使いやすくなります。")]
    public int farBreathWeight = 3;

    [Tooltip("遠距離で接近行動を選ぶ重みです。大きくすると遠距離で走って近づきやすくなります。")]
    public int farApproachWeight = 3;

    [Tooltip("遠距離で突進を選ぶ重みです。大きくすると遠距離から突進しやすくなります。")]
    public int farChargeWeight = 1;

    [Header("デバッグ用：行動オンオフ")]
    [Tooltip("オンにすると接近行動を使います。オフにすると遠距離でも歩き・走り接近を選ばなくなります。")]
    public bool enableApproachAction = true;

    [Tooltip("オンにすると腕攻撃を使います。オフにするとSwipeと横ステップ後の腕攻撃、腕攻撃を含む連続攻撃を使いません。")]
    public bool enableSwipeAction = true;

    [Tooltip("オンにするとTail Slamを使います。オフにすると尻尾叩きつけを使いません。")]
    public bool enableTailSlamAction = true;

    [Tooltip("オンにするとTail Swipeを使います。オフにすると尻尾なぎ払いを使いません。")]
    public bool enableTailSwipeAction = true;

    [Tooltip("オンにすると扇状ブレスを使います。オフにすると広範囲ブレスを使いません。")]
    public bool enableWideBreathAction = true;

    [Tooltip("オンにするとビームブレスを使います。オフにすると追尾ビームブレスを使いません。")]
    public bool enableBeamBreathAction = true;

    [Tooltip("オンにすると突進攻撃を使います。Use Charge As Attackもオンである必要があります。")]
    public bool enableChargeAction = true;

    [Tooltip("オンにすると第2形態の二連突進を使います。突進攻撃がオフの場合は使われません。")]
    public bool enableDoubleChargeAction = true;

    [Tooltip("オンにするとバックステップ後ブレスを使います。オフにするとBackStepBreathを選ばなくなります。")]
    public bool enableBackStepBreathAction = true;

    [Tooltip("オンにすると横ステップ後の腕攻撃を使います。腕攻撃がオフの場合は使われません。")]
    public bool enableSideStepSwipeAction = true;

    [Tooltip("オンにすると横ステップ後のTail Slamを使います。Tail Slamがオフの場合は使われません。")]
    public bool enableSideStepTailSlamAction = true;

    [Tooltip("オンにすると横ステップ後ブレスを使います。ブレス2種が両方オフの場合は使われません。")]
    public bool enableSideStepBreathAction = true;

    [Tooltip("オンにすると第2形態の連続攻撃を使います。腕攻撃がオフの場合は使われません。")]
    public bool enableRapidComboAction = true;

    [Header("突進攻撃")]
    [Tooltip("突進中だけ有効にする攻撃判定を入れてください。未設定なら突進してもダメージ判定は出ません。")]
    public DragonAttackHitbox chargeHitbox;

    [Tooltip("オンにすると突進攻撃を使います。オフにすると行動抽選から突進を外します。")]
    public bool useChargeAsAttack = true;

    [Tooltip("突進の最低インターバルです。大きくすると突進頻度が下がり、小さくすると突進しやすくなります。推奨値は10秒前後です。")]
    public float chargeCooldown = 10f;

    [Tooltip("突進の基準速度です。大きくすると突進が速くなり、避けにくくなります。")]
    public float chargeSpeed = 24f;

    [Tooltip("突進でプレイヤーを通り過ぎる距離です。大きくするとプレイヤーの奥まで走り抜けます。")]
    public float chargeOvershootDistance = 4f;

    [Tooltip("突進時間の最小値です。小さすぎると短距離突進が一瞬で終わります。")]
    public float chargeMinDuration = 0.75f;

    [Tooltip("突進時間の最大値です。大きくすると遠距離突進が長く続きます。")]
    public float chargeMaxDuration = 1.8f;

    [Tooltip("この距離未満から突進する場合は、先にバックステップして距離を取ります。大きくするとバックステップしやすくなります。")]
    public float chargeMinStartDistance = 8f;

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

    [Tooltip("近距離突進前のバックステップ距離です。大きくすると突進前に大きく後退します。")]
    public float closeChargeBackStepDistance = 5f;

    [Tooltip("近距離突進前のバックステップ1回分の時間です。大きくするとゆっくり後退します。")]
    public float closeChargeBackStepDuration = 0.38f;

    [Tooltip("近距離突進前のバックステップ回数です。大きくすると突進前に何度も後退します。")]
    public int closeChargeBackStepCount = 2;

    [Tooltip("突進序盤の加速割合です。大きくすると加速時間が長くなり、出始めが緩やかになります。")]
    [Range(0.01f, 0.5f)] public float chargeAccelerationRatio = 0.18f;

    [Tooltip("突進終盤の減速割合です。大きくすると減速時間が長くなり、止まり方が緩やかになります。")]
    [Range(0.01f, 0.5f)] public float chargeDecelerationRatio = 0.22f;

    [Header("突進パーティクル")]
    [Tooltip("突進の溜め中に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem chargeHoldParticle;

    [Tooltip("突進準備完了時に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem chargeReadyParticle;

    [Tooltip("突進中に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem chargeRunParticle;

    [Header("突進サウンド")]
    [Tooltip("突進の溜め中に再生するSEです。未設定なら鳴りません。")]
    public AudioClip chargeHoldSfx;

    [Tooltip("突進準備完了時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip chargeReadySfx;

    [Tooltip("突進開始時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip chargeRunSfx;

    [Tooltip("突進終了時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip chargeEndSfx;

    [Header("ステップ")]
    [Tooltip("横ステップ距離です。大きくすると左右への移動幅が大きくなります。")]
    public float sideStepDistance = 2f;

    [Tooltip("バックステップ距離です。大きくすると後退距離が大きくなります。")]
    public float backStepDistance = 3f;

    [Tooltip("ステップにかかる時間です。大きくするとゆっくり移動し、小さくすると素早く移動します。")]
    public float stepDuration = 0.45f;

    [Tooltip("バックステップ後にブレスを使う確率です。大きくすると後退後ブレスが増えます。")]
    [Range(0f, 1f)] public float afterBackStepBreathChance = 0.75f;

    [Tooltip("バックステップ後に突進へ派生する確率です。大きくすると後退後突進が増えます。低め推奨です。")]
    [Range(0f, 1f)] public float afterBackStepChargeChance = 0.05f;

    [Header("腕攻撃")]
    [Tooltip("ひっかき中に前進する距離です。大きくすると攻撃が届きやすくなります。")]
    public float swipeForwardDistance = 2.4f;

    [Tooltip("ひっかき前進を開始するフレームです。小さくすると早めに前進します。")]
    public int swipeLungeStartFrame = 18;

    [Tooltip("ひっかき前進を終了するフレームです。大きくすると前進時間が長くなります。")]
    public int swipeLungeEndFrame = 48;

    [Tooltip("腕攻撃後に開始位置へ戻る時間です。大きくするとゆっくり戻ります。")]
    public float swipeReturnTime = 0.25f;

    [Tooltip("オンにすると腕攻撃後に攻撃開始位置へ戻ります。オフにすると前進した位置に残ります。")]
    public bool swipeReturnToStartPosition = true;

    [Tooltip("オンにすると腕攻撃の前進方向をプレイヤー方向にします。オフにするとドラゴンの正面方向へ前進します。")]
    public bool swipeLungeTowardPlayer = true;

    [Tooltip("腕攻撃で前進している間にプレイヤー方向を追う強さです。大きくすると攻撃中の向き補正が強くなります。")]
    public float swipeLungeTurnSpeed = 7f;

    [Tooltip("左腕攻撃の判定を入れてください。未設定なら左腕攻撃にダメージ判定は出ません。")]
    public DragonAttackHitbox leftArmHitbox;

    [Tooltip("右腕攻撃の判定を入れてください。未設定なら右腕攻撃にダメージ判定は出ません。")]
    public DragonAttackHitbox rightArmHitbox;

    [Tooltip("ひっかき判定を出し始めるフレームです。小さくすると早く当たり判定が出ます。")]
    public int swipeHitStartFrame = 35;

    [Tooltip("ひっかき判定を消すフレームです。大きくすると当たり判定が長く残ります。")]
    public int swipeHitEndFrame = 55;

    [Tooltip("ひっかきアニメーション全体の長さです。実際のアニメーション長に合わせてください。")]
    public float swipeAnimDuration = 2.0f;

    [Tooltip("腕攻撃開始時に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem swipeParticle;

    [Tooltip("腕攻撃開始時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip swipeSfx;

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

    [Tooltip("扇状ブレスの溜め中に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem wideBreathChargeParticle;

    [Tooltip("扇状ブレスの発射中に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem wideBreathFireParticle;

    [Tooltip("扇状ブレスの溜め中に再生するSEです。未設定なら鳴りません。")]
    public AudioClip wideBreathChargeSfx;

    [Tooltip("扇状ブレスの発射時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip wideBreathFireSfx;

    [Tooltip("扇状ブレスの判定開始フレームです。小さくすると早く判定が出ます。")]
    public int wideBreathStartFrame = 35;

    [Tooltip("扇状ブレスの判定終了フレームです。大きくすると判定が長く残ります。")]
    public int wideBreathEndFrame = 120;

    [Tooltip("扇状ブレス後の隙です。大きくするとブレス後に反撃しやすくなります。")]
    public float wideBreathRecovery = 0.8f;

    [Header("ビームブレス")]
    [Tooltip("ビームブレスの攻撃判定を入れてください。細長いBox Colliderを使うと調整しやすいです。")]
    public DragonAttackHitbox beamBreathHitbox;

    [Tooltip("ビームの向きを制御するTransformを入れてください。通常はBeamBreathPivotを入れます。")]
    public Transform beamBreathPivot;

    [Tooltip("BeamBreathPivotについているDragonBeamBreathAimerを入れてください。ビームの追尾方向を制御します。")]
    public DragonBeamBreathAimer beamBreathAimer;

    [Tooltip("ビームブレスの溜め中に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem beamBreathChargeParticle;

    [Tooltip("ビームブレスの発射中に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem beamBreathFireParticle;

    [Tooltip("ビームブレスの溜め中に再生するSEです。未設定なら鳴りません。")]
    public AudioClip beamBreathChargeSfx;

    [Tooltip("ビームブレスの発射時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip beamBreathFireSfx;

    [Tooltip("ビームブレスの判定開始フレームです。小さくすると早く判定が出ます。")]
    public int beamBreathStartFrame = 45;

    [Tooltip("ビームブレスの判定終了フレームです。大きくすると判定が長く残ります。")]
    public int beamBreathEndFrame = 135;

    [Tooltip("ビームの追尾を始めるフレームです。小さくすると早く追尾し始めます。")]
    public int beamTrackStartFrame = 40;

    [Tooltip("ビームの追尾をやめるフレームです。大きくすると長くプレイヤーを追います。")]
    public int beamTrackEndFrame = 120;

    [Tooltip("ビームブレス後の隙です。大きくすると発射後に反撃しやすくなります。")]
    public float beamBreathRecovery = 1.0f;

    [Header("尻尾攻撃")]
    [Tooltip("尻尾攻撃の判定を入れてください。Tail SlamとTail Swipeで共通使用します。")]
    public DragonAttackHitbox tailHitbox;

    [Tooltip("オンにすると尻尾攻撃時に尻尾側をプレイヤーへ向けます。オフにすると通常の向き補正になります。")]
    public bool tailAttacksTurnTailToPlayer = true;

    [Tooltip("尻尾をプレイヤーへ向けるための基本角度補正です。尻尾ではなく頭が向く場合は180前後を試してください。")]
    public float tailFacePlayerOffsetY = 0f;

    [Tooltip("Tail Slamの叩きつけ位置の角度補正です。狙いが左右にずれる場合に調整してください。")]
    public float tailSlamAttackOffsetY = -35f;

    [Tooltip("Tail Swipe前半で尻尾側を向ける固定角度です。現在は左右ランダムを使わず、この値だけを使います。")]
    public float tailSwipeFixedAttackOffsetY = -55f;

    [Tooltip("尻尾攻撃時にプレイヤー方向へ回転する速度です。大きくすると素早く向きを合わせます。")]
    public float tailTrackingTurnSpeed = 12f;

    [Header("Tail Slam")]
    [Tooltip("Tail Slam全体の長さです。実際のアニメーション長に合わせてください。")]
    public float tailSlamDuration = 4.8f;

    [Tooltip("Tail Slamで狙いを合わせ始めるフレームです。")]
    public int tailSlamAimStartFrame = 5;

    [Tooltip("Tail Slamでプレイヤー追尾を続ける最後のフレームです。大きくすると直前まで狙います。")]
    public int tailSlamTrackUntilFrame = 73;

    [Tooltip("Tail Slamの判定開始フレームです。")]
    public int tailSlamHitStartFrame = 73;

    [Tooltip("Tail Slamの判定終了フレームです。大きくすると判定が長く残ります。")]
    public int tailSlamHitEndFrame = 82;

    [Tooltip("Tail Slam後に正面へ戻り始めるフレームです。")]
    public int tailSlamReturnStartFrame = 105;

    [Tooltip("Tail Slam後に正面へ戻り終わるフレームです。")]
    public int tailSlamReturnEndFrame = 138;

    [Tooltip("尻尾を直接プレイヤーに向けない設定の時に使う角度補正です。")]
    public float tailSlamAngleOffset = -15f;

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

    [Tooltip("Tail Swipe後半の判定開始フレームです。通常は後半回転開始フレームと同じ値にします。")]
    public int tailSwipeSecondHitStartFrame = 104;

    [Tooltip("Tail Swipe後半の判定終了フレームです。大きくすると後半の判定が長く残ります。")]
    public int tailSwipeSecondHitEndFrame = 135;

    [Header("Tail Swipe後半回転")]
    [Tooltip("オンにするとTail Swipe後半で体を反時計回りに回転させながら薙ぎ払います。")]
    public bool tailSwipeSecondHitTurnToPlayer = true;

    [Tooltip("Tail Swipe後半で追加する回転角度です。反時計回りにしたい場合は負の値を使います。逆方向に回る場合は正負を入れ替えてください。")]
    public float tailSwipeSecondHitExtraCounterClockwiseAngle = -80f;

    [Tooltip("Tail Swipe後半でプレイヤー方向を追う速度です。大きくすると素早く向きを合わせます。")]
    public float tailSwipeSecondHitTurnSpeed = 10f;

    [Tooltip("Tail Swipe後半回転を開始するフレームです。104前後が目安です。")]
    public int tailSwipeSecondTurnStartFrame = 104;

    [Tooltip("Tail Swipe後半回転を終了するフレームです。135前後が目安です。")]
    public int tailSwipeSecondTurnEndFrame = 135;

    [Header("尻尾攻撃の演出")]
    [Tooltip("Tail Slam開始時に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem tailSlamParticle;

    [Tooltip("Tail Swipe開始時に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem tailSwipeParticle;

    [Tooltip("Tail Slam開始時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip tailSlamSfx;

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
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private DragonState state = DragonState.Intro;
    private bool isBusy = false;
    private float lastChargeTime = -999f;
    private DragonAction lastAction = DragonAction.Opening;
    private DragonAction secondLastAction = DragonAction.Opening;

    private void Awake()
    {
        if (motion == null) motion = GetComponent<DragonDragonMotion>();
        if (phase == null) phase = GetComponent<DragonPhaseController>();
        if (dragonHP == null) dragonHP = GetComponentInParent<DragonHP>();
        if (effectPlayer == null) effectPlayer = GetComponent<DragonAnimationEffectPlayer>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = GetComponentInParent<AudioSource>();
        if (beamBreathAimer == null && beamBreathPivot != null) beamBreathAimer = beamBreathPivot.GetComponent<DragonBeamBreathAimer>();
        if (motion != null) motion.SetPlayer(player);
        if (beamBreathAimer != null) beamBreathAimer.player = player;
    }

    private void OnEnable()
    {
        if (dragonHP == null) return;
        dragonHP.OnHalfHP += HandleHalfHP;
        dragonHP.OnTailCrystalBroken += HandleTailCrystalBroken;
        dragonHP.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (dragonHP == null) return;
        dragonHP.OnHalfHP -= HandleHalfHP;
        dragonHP.OnTailCrystalBroken -= HandleTailCrystalBroken;
        dragonHP.OnDeath -= HandleDeath;
    }

    private void Start()
    {
        DisableAllHitboxes();
        StopAllSpecialParticles();
        if (motion != null) motion.ResetAnimatorSpeed();
        StartCoroutine(IntroRoutine());
    }

    private void Update()
    {
        if (state == DragonState.Dead || state == DragonState.Down || isBusy || motion == null) return;
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
            if (phase != null) interval = phase.ApplyActionInterval(interval);
            yield return new WaitForSeconds(interval);

            if (state == DragonState.Down || state == DragonState.Dead || isBusy) continue;
            yield return DecideAction(motion.GetDistanceToPlayer());
        }
    }

    private IEnumerator DecideAction(float distance)
    {
        DragonAction action;
        if (ShouldDoOpening()) action = DragonAction.Opening;
        else if (distance > farRange) action = PickFarAction();
        else if (distance >= middleRange) action = PickMiddleAction();
        else action = PickCloseAction();

        yield return ExecuteAction(action);
    }

    private bool ShouldDoOpening()
    {
        if (!useOpeningIdle) return false;
        if (phase != null && phase.ShouldUsePhase2Opening()) return true;
        return Random.value < openingIdleChance;
    }

    private DragonAction PickFarAction()
    {
        WeightedActionPicker picker = new WeightedActionPicker();
        AddAction(picker, DragonAction.WideBreath, farBreathWeight);
        AddAction(picker, DragonAction.BeamBreath, farBreathWeight);
        AddAction(picker, DragonAction.Approach, farApproachWeight);
        if (CanUseCharge()) AddAction(picker, GetPhaseChargeAction(), farChargeWeight);
        return PreventRepeat(picker.Pick());
    }

    private DragonAction PickMiddleAction()
    {
        WeightedActionPicker picker = new WeightedActionPicker();
        AddAction(picker, DragonAction.Swipe, middleSwipeWeight);
        AddAction(picker, DragonAction.WideBreath, middleBreathWeight);
        AddAction(picker, DragonAction.BeamBreath, middleBreathWeight);
        AddAction(picker, DragonAction.SideStepSwipe, middleStepWeight);
        AddAction(picker, DragonAction.SideStepBreath, middleStepWeight);

        if (!IsTailBroken())
        {
            AddAction(picker, DragonAction.TailSlam, middleTailWeight);
            AddAction(picker, DragonAction.TailSwipe, middleTailWeight);
            AddAction(picker, DragonAction.SideStepTailSlam, middleStepWeight);
        }

        if (CanUseCharge()) AddAction(picker, GetPhaseChargeAction(), middleChargeWeight);
        return PreventRepeat(picker.Pick());
    }

    private DragonAction PickCloseAction()
    {
        WeightedActionPicker picker = new WeightedActionPicker();

        if (phase != null && phase.ShouldUseRapidCombo()) AddAction(picker, DragonAction.RapidCombo, 4);
        AddAction(picker, DragonAction.Swipe, closeSwipeWeight);
        AddAction(picker, DragonAction.SideStepSwipe, closeSideStepSwipeWeight);
        AddAction(picker, DragonAction.BackStepBreath, closeBackStepBreathWeight);
        AddAction(picker, DragonAction.WideBreath, 1);

        if (!IsTailBroken())
        {
            AddAction(picker, DragonAction.TailSlam, closeTailSlamWeight);
            AddAction(picker, DragonAction.TailSwipe, closeTailSwipeWeight);
            AddAction(picker, DragonAction.SideStepTailSlam, 1);
        }

        if (CanUseCharge()) AddAction(picker, GetPhaseChargeAction(), closeChargeWeight);
        return PreventRepeat(picker.Pick());
    }

    private void AddAction(WeightedActionPicker picker, DragonAction action, int weight)
    {
        if (!IsActionAllowed(action)) return;
        picker.Add(action, weight);
    }

    private DragonAction GetPhaseChargeAction()
    {
        if (enableDoubleChargeAction && phase != null && phase.ShouldUseDoubleCharge()) return DragonAction.DoubleCharge;
        return DragonAction.Charge;
    }

    private DragonAction PreventRepeat(DragonAction picked)
    {
        if (!IsActionAllowed(picked)) return DragonAction.Opening;
        if (picked != lastAction && picked != secondLastAction) return picked;
        return DragonAction.Opening;
    }

    private bool IsActionAllowed(DragonAction action)
    {
        switch (action)
        {
            case DragonAction.Opening:
                return true;
            case DragonAction.Approach:
                return enableApproachAction;
            case DragonAction.Swipe:
                return enableSwipeAction;
            case DragonAction.TailSlam:
                return enableTailSlamAction && !IsTailBroken();
            case DragonAction.TailSwipe:
                return enableTailSwipeAction && !IsTailBroken();
            case DragonAction.WideBreath:
                return enableWideBreathAction;
            case DragonAction.BeamBreath:
                return enableBeamBreathAction;
            case DragonAction.Charge:
                return enableChargeAction && useChargeAsAttack;
            case DragonAction.DoubleCharge:
                return enableChargeAction && enableDoubleChargeAction && useChargeAsAttack;
            case DragonAction.BackStepBreath:
                return enableBackStepBreathAction && (enableWideBreathAction || enableBeamBreathAction);
            case DragonAction.SideStepSwipe:
                return enableSideStepSwipeAction && enableSwipeAction;
            case DragonAction.SideStepTailSlam:
                return enableSideStepTailSlamAction && enableTailSlamAction && !IsTailBroken();
            case DragonAction.SideStepBreath:
                return enableSideStepBreathAction && (enableWideBreathAction || enableBeamBreathAction);
            case DragonAction.RapidCombo:
                return enableRapidComboAction && enableSwipeAction;
            default:
                return true;
        }
    }

    private IEnumerator ExecuteAction(DragonAction action)
    {
        if (!IsActionAllowed(action))
        {
            yield return OpeningIdle();
            yield break;
        }

        RegisterAction(action);
        switch (action)
        {
            case DragonAction.Opening: yield return OpeningIdle(); break;
            case DragonAction.WideBreath: yield return WideBreathAttack(); break;
            case DragonAction.BeamBreath: yield return BeamBreathAttack(); break;
            case DragonAction.Charge: yield return ChargeAttack(false, true); break;
            case DragonAction.DoubleCharge: yield return DoubleChargeAttack(); break;
            case DragonAction.Swipe: yield return SwipeAttack(Random.value > 0.5f); break;
            case DragonAction.TailSlam: yield return TailSlam(); break;
            case DragonAction.TailSwipe: yield return TailSwipe(); break;
            case DragonAction.BackStepBreath: yield return BackStepThenMaybeBreath(); break;
            case DragonAction.SideStepSwipe: yield return SideStepThenSwipe(); break;
            case DragonAction.SideStepTailSlam: yield return SideStepThenTailSlam(); break;
            case DragonAction.SideStepBreath: yield return SideStepThenBreath(); break;
            case DragonAction.RapidCombo: yield return RapidCombo(); break;
            case DragonAction.Approach: yield return ApproachPlayer(); break;
            default: yield return OpeningIdle(); break;
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
        if (phase != null && phase.isPhase2) duration = phase.GetPhase2OpeningTime();

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            if (lookAtPlayerDuringOpening) motion.FacePlayerSmooth(motion.idleTurnSpeed);
            yield return null;
        }

        ReturnToIdle();
    }

    private IEnumerator ApproachPlayer()
    {
        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();

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
            if (phase != null) speed = phase.ApplySpeed(speed);
            motion.MoveDragon(motion.GetMoveForward() * speed * Time.deltaTime);
            yield return null;
        }

        ReturnToIdle();
    }

    private IEnumerator ApproachMeleeRange()
    {
        if (!approachBeforeNonBreathAttack || player == null) yield break;

        float timer = 0f;
        float checkTimer = 0f;
        bool running = motion.GetDistanceToPlayer() >= switchToRunDistance;
        string moveAnim = running ? motion.runAnim : motion.walkAnim;
        motion.PlayAnim(moveAnim, true);

        while (player != null && motion.GetDistanceToPlayer() > meleeAttackStartDistance)
        {
            timer += Time.deltaTime;
            checkTimer += Time.deltaTime;
            if (timer > meleeApproachTimeout) break;

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

            motion.KeepMoveAnim(moveAnim, ref checkTimer);
            motion.FacePlayerSmooth(meleeApproachTurnSpeed);
            float speed = running ? runChaseSpeed : meleeApproachSpeed;
            if (phase != null) speed = phase.ApplySpeed(speed);
            motion.MoveDragon(motion.GetMoveForward() * speed * Time.deltaTime);
            yield return null;
        }

        yield return motion.FacePlayerForSeconds(0.15f);
    }

    private IEnumerator BackStepThenMaybeBreath()
    {
        float r = Random.value;
        if (CanUseCharge() && r < afterBackStepChargeChance)
        {
            yield return ChargeAttack(true, true);
            yield break;
        }

        yield return StepAction(-motion.GetMoveForward());

        if (r < afterBackStepChargeChance + afterBackStepBreathChance)
        {
            yield return new WaitForSeconds(0.12f);
            yield return RandomEnabledBreathAttack();
        }
        else
        {
            ReturnToIdle();
        }
    }

    private IEnumerator SideStepThenSwipe()
    {
        if (!enableSwipeAction)
        {
            yield return OpeningIdle();
            yield break;
        }

        yield return StepAction(motion.GetRandomSideStepDirection());
        yield return new WaitForSeconds(Random.Range(0.05f, 0.18f));
        yield return SwipeAttack(Random.value > 0.5f);
    }

    private IEnumerator SideStepThenTailSlam()
    {
        if (IsTailBroken() || !enableTailSlamAction)
        {
            if (enableSwipeAction) yield return SideStepThenSwipe();
            else yield return OpeningIdle();
            yield break;
        }

        yield return StepAction(motion.GetRandomSideStepDirection());
        yield return new WaitForSeconds(Random.Range(0.05f, 0.18f));
        yield return TailSlam();
    }

    private IEnumerator SideStepThenBreath()
    {
        if (!enableWideBreathAction && !enableBeamBreathAction)
        {
            yield return OpeningIdle();
            yield break;
        }

        yield return StepAction(motion.GetRandomSideStepDirection());
        yield return new WaitForSeconds(Random.Range(0.05f, 0.18f));
        yield return RandomEnabledBreathAttack();
    }

    private IEnumerator RapidCombo()
    {
        if (!enableSwipeAction)
        {
            yield return OpeningIdle();
            yield break;
        }

        isBusy = true;
        state = DragonState.Acting;
        yield return SwipeAttack(Random.value > 0.5f);
        isBusy = true;
        state = DragonState.Acting;
        yield return new WaitForSeconds(phase != null ? phase.rapidComboInterval : 0.15f);

        if (enableTailSwipeAction && !IsTailBroken() && Random.value > 0.4f) yield return TailSwipe();
        else yield return SwipeAttack(Random.value > 0.5f);

        isBusy = true;
        state = DragonState.Acting;
        yield return new WaitForSeconds(phase != null ? phase.rapidComboRecovery : 1.2f);
        ReturnToIdle();
    }

    private IEnumerator DoubleChargeAttack()
    {
        if (!CanUseCharge())
        {
            if (enableSwipeAction) yield return SwipeAttack(Random.value > 0.5f);
            else yield return OpeningIdle();
            yield break;
        }

        yield return ChargeAttack(true, true);
        isBusy = true;
        state = DragonState.Acting;
        yield return new WaitForSeconds(doubleChargeInterval);
        if (state == DragonState.Dead || state == DragonState.Down) yield break;
        yield return ChargeAttack(false, false);
        isBusy = true;
        state = DragonState.Acting;
        yield return new WaitForSeconds(doubleChargeRecovery);
        ReturnToIdle();
    }

    private IEnumerator RandomEnabledBreathAttack()
    {
        if (enableWideBreathAction && enableBeamBreathAction)
        {
            if (Random.value > 0.5f) yield return WideBreathAttack();
            else yield return BeamBreathAttack();
            yield break;
        }

        if (enableWideBreathAction)
        {
            yield return WideBreathAttack();
            yield break;
        }

        if (enableBeamBreathAction)
        {
            yield return BeamBreathAttack();
            yield break;
        }

        yield return OpeningIdle();
    }

    private IEnumerator WideBreathAttack()
    {
        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();
        StopAllSpecialParticles();
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

        if (wideBreathChargeParticle != null) wideBreathChargeParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (wideBreathFireParticle != null) wideBreathFireParticle.Play();
        PlaySfx(wideBreathFireSfx);
        if (wideBreathHitbox != null) wideBreathHitbox.EnableHitbox();
        yield return new WaitForSeconds(Mathf.Max(0f, end - start));
        if (wideBreathHitbox != null) wideBreathHitbox.DisableHitbox();
        if (wideBreathFireParticle != null) wideBreathFireParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        yield return new WaitForSeconds(Mathf.Max(0f, breathDuration - end));
        yield return new WaitForSeconds(wideBreathRecovery);
        ReturnToIdle();
    }

    private IEnumerator BeamBreathAttack()
    {
        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();
        StopAllSpecialParticles();
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
        float trackStart = motion.FrameToSeconds(beamTrackStartFrame);
        float trackEnd = motion.FrameToSeconds(beamTrackEndFrame);
        float end = motion.FrameToSeconds(beamBreathEndFrame);
        float timer = 0f;
        bool hitboxEnabled = false;
        bool fireParticlePlayed = false;

        while (timer < breathDuration)
        {
            timer += Time.deltaTime;

            if (timer < trackStart) motion.FacePlayerSmooth(motion.actionTurnSpeed);
            if (timer >= trackStart && timer <= trackEnd && beamBreathAimer != null) beamBreathAimer.AimSmooth();

            if (!fireParticlePlayed && timer >= start)
            {
                fireParticlePlayed = true;
                if (beamBreathChargeParticle != null) beamBreathChargeParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
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
        if (beamBreathFireParticle != null) beamBreathFireParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        yield return new WaitForSeconds(beamBreathRecovery);
        ReturnToIdle();
    }

    private IEnumerator ChargeAttack(bool forceCloseBackStep, bool consumeCooldown)
    {
        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();
        StopAllSpecialParticles();
        motion.ResetAnimatorSpeed();
        if (consumeCooldown) lastChargeTime = Time.time;

        float distance = motion.GetDistanceToPlayer();
        if (forceCloseBackStep || distance < chargeMinStartDistance) yield return DoubleBackStepForCloseCharge();

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

        if (chargeHoldParticle != null) chargeHoldParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (chargeReadyParticle != null) chargeReadyParticle.Play();
        PlaySfx(chargeReadySfx);
        motion.ResetAnimatorSpeed();
        motion.PlayAnim(motion.runAnim, true);
        yield return new WaitForSeconds(chargeReadyPauseTime);

        Vector3 chargeDirection = motion.GetDirectionToPlayer();
        float targetDistance = GetChargeTargetDistance(chargeDirection);
        float baseSpeed = phase != null ? phase.ApplyChargeSpeed(chargeSpeed) : chargeSpeed;
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
            float currentDistance = targetDistance * EvaluateChargeMoveCurve(t);
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

    private IEnumerator DoubleBackStepForCloseCharge()
    {
        int count = Mathf.Max(1, closeChargeBackStepCount);
        for (int i = 0; i < count; i++)
        {
            yield return motion.FacePlayerForSeconds(0.08f);
            yield return motion.MoveInDirectionForSeconds(-motion.GetMoveForward(), closeChargeBackStepDistance, closeChargeBackStepDuration, motion.stepAnim);
            yield return new WaitForSeconds(0.08f);
        }
    }

    private float GetChargeTargetDistance(Vector3 chargeDirection)
    {
        if (player == null) return chargeSpeed;
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
        yield return motion.FacePlayerForSeconds(0.25f);

        Vector3 startPosition = transform.position;
        string animName = left ? motion.leftSwipeAnim : motion.rightSwipeAnim;
        DragonAttackHitbox hitbox = left ? leftArmHitbox : rightArmHitbox;
        motion.PlayAnim(animName, true);
        if (swipeParticle != null) swipeParticle.Play();
        PlaySfx(swipeSfx);

        float lungeStart = motion.FrameToSeconds(swipeLungeStartFrame);
        float lungeEnd = motion.FrameToSeconds(swipeLungeEndFrame);
        float hitStart = motion.FrameToSeconds(swipeHitStartFrame);
        float hitEnd = motion.FrameToSeconds(swipeHitEndFrame);
        float timer = 0f;
        float previousLunge = 0f;
        bool hitboxEnabled = false;
        Vector3 fixedLungeDirection = swipeLungeTowardPlayer ? motion.GetDirectionToPlayer() : motion.GetMoveForward();

        while (timer < swipeAnimDuration)
        {
            timer += Time.deltaTime;

            if (timer >= lungeStart && timer <= lungeEnd)
            {
                motion.FacePlayerSmooth(swipeLungeTurnSpeed);
                Vector3 lungeDirection = swipeLungeTowardPlayer ? motion.GetDirectionToPlayer() : fixedLungeDirection;
                float t = Mathf.InverseLerp(lungeStart, lungeEnd, timer);
                float currentLunge = swipeForwardDistance * Mathf.SmoothStep(0f, 1f, t);
                float delta = currentLunge - previousLunge;
                previousLunge = currentLunge;
                motion.MoveDragon(lungeDirection * delta);
            }

            if (!hitboxEnabled && timer >= hitStart)
            {
                hitboxEnabled = true;
                if (hitbox != null) hitbox.EnableHitbox();
            }

            if (hitboxEnabled && timer >= hitEnd)
            {
                hitboxEnabled = false;
                if (hitbox != null) hitbox.DisableHitbox();
            }

            yield return null;
        }

        if (hitbox != null) hitbox.DisableHitbox();
        if (swipeReturnToStartPosition) yield return motion.MoveToPositionForSeconds(startPosition, swipeReturnTime);
        ReturnToIdle();
    }

    private IEnumerator TailSlam()
    {
        if (IsTailBroken())
        {
            yield return SwipeAttack(Random.value > 0.5f);
            yield break;
        }

        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();
        yield return ApproachMeleeRange();
        yield return motion.FacePlayerForSeconds(0.25f);

        Quaternion originalRotation = transform.rotation;
        motion.PlayAnim(motion.tailSlamAnim, true);
        if (tailSlamParticle != null) tailSlamParticle.Play();
        PlaySfx(tailSlamSfx);
        yield return new WaitForSeconds(motion.FrameToSeconds(tailSlamAimStartFrame));

        if (tailAttacksTurnTailToPlayer)
        {
            yield return TrackTailToPlayerForDuration(motion.FramesToDuration(tailSlamAimStartFrame, tailSlamTrackUntilFrame), tailSlamAttackOffsetY);
        }
        else
        {
            yield return motion.RotateTo(motion.GetRotationToPlayerWithOffset(tailSlamAngleOffset), motion.FramesToDuration(tailSlamAimStartFrame, tailSlamTrackUntilFrame));
        }

        if (tailHitbox != null) tailHitbox.EnableHitbox();
        yield return new WaitForSeconds(motion.FramesToDuration(tailSlamHitStartFrame, tailSlamHitEndFrame));
        if (tailHitbox != null) tailHitbox.DisableHitbox();
        yield return new WaitForSeconds(motion.FramesToDuration(tailSlamHitEndFrame, tailSlamReturnStartFrame));
        yield return motion.RotateTo(originalRotation, motion.FramesToDuration(tailSlamReturnStartFrame, tailSlamReturnEndFrame));
        yield return new WaitForSeconds(Mathf.Max(0f, tailSlamDuration - motion.FrameToSeconds(tailSlamReturnEndFrame)));
        transform.rotation = originalRotation;
        ReturnToIdle();
    }

    private IEnumerator TailSwipe()
    {
        if (IsTailBroken())
        {
            yield return SwipeAttack(Random.value > 0.5f);
            yield break;
        }

        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();
        yield return ApproachMeleeRange();
        yield return motion.FacePlayerForSeconds(0.25f);

        motion.PlayAnim(motion.tailSwipeAnim, true);
        if (tailSwipeParticle != null) tailSwipeParticle.Play();
        PlaySfx(tailSwipeSfx);
        yield return new WaitForSeconds(motion.FrameToSeconds(tailSwipeAimStartFrame));

        if (tailAttacksTurnTailToPlayer)
        {
            yield return TrackTailToPlayerForDuration(motion.FramesToDuration(tailSwipeAimStartFrame, tailSwipeTrackUntilFrame), tailSwipeFixedAttackOffsetY);
        }
        else
        {
            yield return motion.RotateTo(motion.GetRotationToPlayerWithOffset(tailSwipeFixedAttackOffsetY), motion.FramesToDuration(tailSwipeAimStartFrame, tailSwipeTrackUntilFrame));
        }

        if (tailHitbox != null) tailHitbox.EnableHitbox();
        yield return new WaitForSeconds(motion.FramesToDuration(tailSwipeSlamHitStartFrame, tailSwipeSlamHitEndFrame));
        if (tailHitbox != null) tailHitbox.DisableHitbox();

        yield return new WaitForSeconds(motion.FramesToDuration(tailSwipeSlamHitEndFrame, tailSwipeSecondHitStartFrame));

        if (tailHitbox != null) tailHitbox.EnableHitbox();
        if (tailSwipeSecondHitTurnToPlayer) yield return TailSwipeSecondTurnToPlayer();
        else yield return new WaitForSeconds(motion.FramesToDuration(tailSwipeSecondHitStartFrame, tailSwipeSecondHitEndFrame));
        if (tailHitbox != null) tailHitbox.DisableHitbox();

        yield return new WaitForSeconds(Mathf.Max(0f, tailSwipeDuration - motion.FrameToSeconds(tailSwipeSecondTurnEndFrame)));
        ReturnToIdle();
    }

    private IEnumerator TailSwipeSecondTurnToPlayer()
    {
        int startFrame = Mathf.Min(tailSwipeSecondTurnStartFrame, tailSwipeSecondTurnEndFrame);
        int endFrame = Mathf.Max(tailSwipeSecondTurnStartFrame, tailSwipeSecondTurnEndFrame);
        float duration = motion.FramesToDuration(startFrame, endFrame);
        float timer = 0f;
        Quaternion startRotation = transform.rotation;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            Quaternion lookPlayer = motion.GetRotationToPlayerWithOffset(0f);
            Quaternion counterClockwiseAdd = Quaternion.Euler(0f, tailSwipeSecondHitExtraCounterClockwiseAngle, 0f);
            Quaternion targetRotation = lookPlayer * counterClockwiseAdd;
            float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, duration));
            Quaternion easedRotation = Quaternion.Slerp(startRotation, targetRotation, Mathf.SmoothStep(0f, 1f, t));
            transform.rotation = Quaternion.Slerp(transform.rotation, easedRotation, tailSwipeSecondHitTurnSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator TrackTailToPlayerForDuration(float duration, float extraOffsetY)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            Quaternion targetRotation = motion.GetTailRotationToPlayer(tailFacePlayerOffsetY, extraOffsetY);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, tailTrackingTurnSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator StepAction(Vector3 direction)
    {
        float distance = Vector3.Dot(direction.normalized, -motion.GetMoveForward()) > 0.7f ? backStepDistance : sideStepDistance;
        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();
        yield return motion.FacePlayerForSeconds(0.12f);
        yield return motion.MoveInDirectionForSeconds(direction, distance, stepDuration, motion.stepAnim);
        ReturnToIdle();
    }

    private void HandleHalfHP()
    {
        if (state == DragonState.Dead) return;
        if (phase != null) phase.EnterPhase2();
        StopAllCoroutines();
        StartCoroutine(HalfHPRoarRoutine());
    }

    private IEnumerator HalfHPRoarRoutine()
    {
        DisableAllHitboxes();
        StopAllSpecialParticles();
        motion.ResetAnimatorSpeed();
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
        motion.ResetAnimatorSpeed();
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
        if (motion != null) motion.ResetAnimatorSpeed();
        state = DragonState.Dead;
        isBusy = true;
        if (motion != null) motion.PlayAnim(motion.deathAnim, true);
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
        if (!useChargeAsAttack) return false;
        return Time.time >= lastChargeTime + chargeCooldown;
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
        if (state != DragonState.Dead && state != DragonState.Down) state = DragonState.Idle;
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
    }

    private void StopAllChargeParticles()
    {
        if (chargeHoldParticle != null) chargeHoldParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (chargeReadyParticle != null) chargeReadyParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (chargeRunParticle != null) chargeRunParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void StopAllBreathParticles()
    {
        if (wideBreathChargeParticle != null) wideBreathChargeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (wideBreathFireParticle != null) wideBreathFireParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (beamBreathChargeParticle != null) beamBreathChargeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (beamBreathFireParticle != null) beamBreathFireParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

        if (audioSource != null) audioSource.PlayOneShot(clip, sfxVolume);
    }

    private class WeightedActionPicker
    {
        private readonly List<DragonAction> actions = new List<DragonAction>();

        public void Add(DragonAction action, int weight)
        {
            int safeWeight = Mathf.Max(0, weight);
            for (int i = 0; i < safeWeight; i++) actions.Add(action);
        }

        public DragonAction Pick()
        {
            if (actions.Count == 0) return DragonAction.Opening;
            return actions[Random.Range(0, actions.Count)];
        }
    }
}
