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
        EmergencyBackStep,
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

    private enum DragonEffectKind
    {
        Roar,
        Down,
        Death,
        Step,
        Swipe,
        TailSlam,
        TailSwipe,
        BreathCharge,
        BreathFire,
        ChargeStart,
        ChargeRun,
        ChargeEnd
    }

    [Header("参照設定")]
    [Tooltip("Dragon_AllObjectsに付いているDragonDragonMotionを入れてください。移動、回転、アニメーション再生を担当します。")]
    public DragonDragonMotion motion;

    [Tooltip("Dragon_AllObjectsに付いているDragonPhaseControllerを入れてください。HP50%以下の強化状態を管理します。")]
    public DragonPhaseController phase;

    [Header("外部難易度反映")]
    [Tooltip("DifficultyApplierなど外部スクリプトから行動速度を変える時に使います。Easy/Normal/Hardの判定自体はこのDragonAI内では行いません。")]
    public bool useExternalDifficultyMultipliers = true;

    [Tooltip("行動後の待ち時間倍率です。1より大きいと行動間隔が長くなり、1より小さいと行動間隔が短くなります。Easyは1.15、Hardは0.85などがおすすめです。")]
    public float difficultyActionIntervalMultiplier = 1f;

    [Tooltip("歩き、走り、突進などの座標移動速度倍率です。Easyは0.9、Hardは1.1などがおすすめです。")]
    public float difficultyMoveSpeedMultiplier = 1f;

    [Tooltip("攻撃・咆哮・ブレスなどAnimator再生速度の倍率です。Easyは0.9、Hardは1.1などがおすすめです。")]
    public float difficultyAnimationSpeedMultiplier = 1f;

    [Header("難易度と判定・演出同期")]
    [Tooltip("ON推奨。HardでAnimator速度を上げた時、攻撃判定・パーティクル・SEの発生タイミングも同じ倍率で前倒しします。")]
    public bool scaleHitboxAndEffectTimingWithAnimationSpeed = true;

    [Tooltip("ONにすると、AIから再生するParticleをLocal Simulationにしてドラゴンの子として動きに追従しやすくします。ブレスや煙が不自然にくっつく場合はOFFにしてください。")]
    public bool forcePlayedParticlesToLocalSimulation = true;

    [Tooltip("ONにすると、Particle再生前にStopしてからPlayします。連続攻撃時に古い再生状態が残る場合はON推奨です。")]
    public bool restartParticleBeforePlay = true;

    [Tooltip("ONにすると、突進の溜め時間も難易度のAnimator速度に合わせて短縮/延長します。OFFならCharge Tell Timeは実時間のままです。突進溜めが異常に長い時はOFF推奨です。")]
    public bool scaleChargeTellTimeWithAnimationSpeed = false;

    [Tooltip("ONにすると、突進直前のReady Pauseも難易度のAnimator速度に合わせて短縮/延長します。OFFなら実時間のままです。")]
    public bool scaleChargeReadyPauseWithAnimationSpeed = false;

    [Header("難易度別 攻撃ディレイ / 突進短縮")]
    [Tooltip("DifficultyApplierから入る追加ディレイです。攻撃行動の開始前にこの秒数だけ待ちます。Hardだけ遅らせたい時に使います。")]
    public float difficultyAttackStartDelay = 0f;

    [Tooltip("ONなら、追加ディレイは攻撃行動だけに適用します。ApproachやOpeningにはかけません。")]
    public bool applyDifficultyAttackStartDelayOnlyToAttackActions = true;

    [Tooltip("ONなら、追加ディレイ中もプレイヤーの方を向きます。")]
    public bool facePlayerDuringDifficultyAttackStartDelay = true;

    [Tooltip("ONなら、Charge Tell Timeを難易度専用値で上書きします。Hardの突進溜めを大幅短縮したい時にON。")]
    public bool useDifficultyChargeTellTimeOverride = false;

    [Tooltip("難易度専用の突進溜め時間です。Use Difficulty Charge Tell Time OverrideがONの時だけ使います。")]
    public float difficultyChargeTellTimeOverride = 0.35f;

    [Tooltip("ONなら、Charge Ready Pause Timeを難易度専用値で上書きします。")]
    public bool useDifficultyChargeReadyPauseOverride = false;

    [Tooltip("難易度専用の突進直前停止時間です。")]
    public float difficultyChargeReadyPauseOverride = 0.05f;

    [Header("Hard限定 突進追尾 / 尻尾直前ディレイ")]
    [Tooltip("DifficultyApplierからHard時だけONにします。ONなら突進中に少しだけプレイヤー方向へ曲がります。")]
    public bool useDifficultyChargeHoming = false;

    [Tooltip("突進追尾の強さです。0なら追尾なし、0.2〜0.5くらいが少し曲がる設定です。")]
    [Range(0f, 1f)]
    public float difficultyChargeHomingStrength = 0.35f;

    [Tooltip("突進追尾で1秒間に曲がれる最大角度です。大きくすると急に曲がります。Hardでも60〜120くらい推奨です。")]
    public float difficultyChargeHomingMaxTurnSpeed = 90f;

    [Tooltip("DifficultyApplierからHard時だけONにします。ONならTailSlam/TailSwipe一段目の判定直前に短いディレイを入れます。")]
    public bool useDifficultyTailPreHitDelay = false;

    [Tooltip("Hard限定。TailSlamの当たり判定が出る直前に入れる追加ディレイです。")]
    public float difficultyTailSlamPreHitDelay = 0.12f;

    [Tooltip("Hard限定。TailSwipe一段目の当たり判定が出る直前に入れる追加ディレイです。")]
    public float difficultyTailSwipeFirstPreHitDelay = 0.12f;

    [Tooltip("尻尾直前ディレイ中にプレイヤーを追う旋回速度です。")]
    public float difficultyTailPreHitTurnSpeed = 22f;

    [Tooltip("ON推奨。尻尾直前ディレイ中はアニメを一時停止して、見た目と当たり判定のズレを減らします。")]
    public bool pauseAnimatorDuringDifficultyTailDelay = true;

    [Tooltip("DragonCoreに付いているDragonHPを入れてください。本体HP、尻尾クリスタル破壊、死亡イベントを受け取ります。")]
    public DragonHP dragonHP;

    [Tooltip("Dragon_AllObjectsに付いているDragonAnimationEffectPlayerを入れてください。SEをまとめて再生したい場合に使います。未設定でもAudioSourceがあれば再生できます。")]
    public DragonAnimationEffectPlayer effectPlayer;

    [Tooltip("プレイヤー本体のTransformを入れてください。ドラゴンが向く方向、追跡、攻撃対象の基準になります。")]
    public Transform player;

    [Header("Player Death Stop / プレイヤー死亡時停止")]
    [Tooltip("PlayerHP。空ならPlayer Transformから自動取得します。HPが0の間、ドラゴンは攻撃せずIdleします。")]
    public PlayerHP playerHP;

    [Tooltip("ONならプレイヤーHPが0になった瞬間、現在の攻撃や移動を中断してIdleで待機します。")]
    public bool stopActionsWhenPlayerDead = true;

    [Tooltip("ONならプレイヤーが復帰した時にAIループを再開します。Hard最終死亡でタイトルへ戻る場合も安全です。")]
    public bool resumeAIWhenPlayerRevived = true;

    private bool pausedBecausePlayerDead = false;

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

    [Header("懐対策バックステップ")]
    [Tooltip("オンにすると、プレイヤーがドラゴンの中心に入り込みすぎた時に高確率でバックステップして間合いを取ります。")]
    public bool enableInnerBackStepAction = true;

    [Tooltip("この距離未満を懐に入り込みすぎた状態として扱います。ひっかきが届かない距離より少し大きめにすると安定します。")]
    public float innerBackStepDistance = 2.4f;

    [Tooltip("懐に入られた時にバックステップを選ぶ確率です。0.8なら高確率で距離を取ります。")]
    [Range(0f, 1f)]
    public float innerBackStepChance = 0.85f;

    [Tooltip("懐対策バックステップの回数です。1から2がおすすめです。")]
    public int innerBackStepCount = 1;

    [Tooltip("懐対策バックステップ1回分の移動距離です。通常バックステップとは別に調整できます。大きくすると懐から大きく離れます。")]
    public float innerBackStepMoveDistance = 4f;

    [Tooltip("懐対策バックステップ1回分の移動時間です。通常バックステップとは別に調整できます。小さくすると素早く下がります。")]
    public float innerBackStepMoveDuration = 0.38f;

    [Tooltip("懐対策バックステップ後の短い硬直です。大きくするとプレイヤーが追撃しやすくなります。")]
    public float innerBackStepRecovery = 0.05f;

    [Tooltip("オンにすると、懐対策バックステップ直後だけ次の行動待ち時間をスキップして、すぐ攻撃や次行動に移ります。")]
    public bool innerBackStepConnectNextActionImmediately = true;

    [Tooltip("懐対策バックステップ直後に次の行動へ移るまでの追加待ち時間です。0ならほぼ即行動します。")]
    public float innerBackStepNextActionDelay = 0f;

    [Tooltip("オンにすると、プレイヤーが懐に入り込みすぎた時は待機中の向き補正を止めます。くるくる回転する挙動を抑えます。")]
    public bool stopIdleTurnWhenPlayerTooClose = true;

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

    [Header("Tail Slam SE Timing")]
    [Tooltip("TailSlamの叩きつけSEを、当たり判定ONより何秒早く鳴らすかです。0なら判定ONと同時。0.03〜0.08くらいがおすすめです。")]
    public float tailSlamImpactSfxAdvanceTime = 0f;

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

    [Tooltip("Tail Swipe開始時に再生するSEです。今回は開始時には基本鳴らさず、2段目回転音のフォールバックとして使います。未設定なら鳴りません。")]
    public AudioClip tailSwipeSfx;

    [Header("Tail Swipe SE 個別設定")]
    [Tooltip("ONならTail Swipe開始時にはSEを鳴らさず、1段目の叩きつけ判定が出た瞬間に専用SEを鳴らします。おすすめON。")]
    public bool useSeparateTailSwipeFirstSlamSfx = true;

    [Tooltip("ONならTail Swipeの1段目叩きつけはTailSlamと同じClipを使います。ただしDelay/VolumeはEffectPlayerの Tail Swipe First Slam 側を使えます。")]
    public bool useTailSlamSfxForTailSwipeFirstHit = true;

    [Tooltip("ONならTail Swipe開始時にTailSwipeSfxを鳴らします。1段目と2段目を別管理したいならOFF推奨。")]
    public bool playTailSwipeStartSfx = false;

    [Tooltip("TailSwipe一段目の叩きつけSEを、当たり判定ONより何秒早く鳴らすかです。0なら判定ONと同時。0.03〜0.08くらいがおすすめです。")]
    public float tailSwipeFirstSlamSfxAdvanceTime = 0f;

    [Header("Tail Swipe 強化設定")]
    [Tooltip("オンにすると、Tail Swipeを強化版として使います。中距離まででだけ発動し、前半の位置合わせ、二段目前の予備動作、回転突進を行います。")]
    public bool useEnhancedTailSwipe = true;

    [Tooltip("強化Tail Swipeを開始できる最小距離です。近すぎる時は不自然な座標移動を避けるため、候補から外します。")]
    public float enhancedTailSwipeMinStartDistance = 3.5f;

    [Tooltip("強化Tail Swipeを開始できる最大距離です。中距離までに制限したいので、middleRange以下を推奨します。")]
    public float enhancedTailSwipeMaxStartDistance = 15f;

    [Tooltip("Tail Swipe前半の位置合わせ移動を使うかどうかです。11から35フレームの間に、尻尾が届きやすい位置へ移動します。")]
    public bool tailSwipeUseRepositionMove = true;

    [Tooltip("Tail Swipe前半の位置合わせ開始フレームです。")]
    public int tailSwipeRepositionStartFrame = 11;

    [Tooltip("Tail Swipe前半の位置合わせ終了フレームです。")]
    public int tailSwipeRepositionEndFrame = 35;

    [Tooltip("位置合わせ後、ドラゴン中心とプレイヤーの距離がこの値に近づくように移動します。尻尾の当たりやすい距離に合わせてください。")]
    public float tailSwipeIdealHitDistance = 6.2f;

    [Tooltip("位置合わせで動ける最大距離です。大きくすると当たりやすくなりますが、ワープ感が出やすくなります。")]
    public float tailSwipeRepositionMaxDistance = 4.0f;

    [Tooltip("位置合わせ移動の横方向補正です。尻尾の当たる位置が左右にずれる場合に調整します。")]
    public float tailSwipeRepositionSideOffset = 0f;

    [Tooltip("位置合わせ移動中の向き補正速度です。大きくすると早く尻尾の角度を合わせます。")]
    public float tailSwipeRepositionTurnSpeed = 18f;

    [Tooltip("一段目後、二段目の回転突進に入る前の予備動作を使うかどうかです。83から102フレームをスローにして時間を作ります。")]
    public bool useTailSwipeSecondTell = true;

    [Tooltip("二段目予備動作の開始フレームです。ここでパーティクルとSEを再生します。")]
    public int tailSwipeSecondTellStartFrame = 83;

    [Tooltip("二段目予備動作の終了フレームです。この後、回転突進に入ります。")]
    public int tailSwipeSecondTellEndFrame = 102;

    [Tooltip("二段目予備動作を実際に見せる秒数です。大きくすると分かりやすい予兆になります。")]
    public float tailSwipeSecondTellDuration = 0.55f;

    [Tooltip("二段目予備動作中に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem tailSwipeSecondTellParticle;

    [Tooltip("二段目予備動作開始時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip tailSwipeSecondTellSfx;

    [Tooltip("Tail Swipeの回転突進を使うかどうかです。103から132フレームの一回転薙ぎ払いをループさせながら突進します。")]
    public bool useTailSwipeSpinDash = true;

    [Tooltip("回転突進専用の攻撃判定です。通常のTail Hitboxとは別にしたい場合に設定してください。未設定ならTail Hitboxを使います。")]
    public DragonAttackHitbox tailSwipeSpinDashHitbox;

    [Tooltip("オン推奨。Tail Swipe二段目の攻撃中は、開始から終了まで常に当たり判定をONにします。")]
    public bool keepTailSwipeSecondHitboxActiveDuringSecondAttack = true;

    [Tooltip("オン推奨。二段目攻撃中にHitbox側が一時的にOFFになっても、毎フレームEnableし直して判定抜けを防ぎます。")]
    public bool forceTailSwipeSecondHitboxEveryFrame = true;

    [Tooltip("回転突進でループさせるアニメーション区間の開始フレームです。")]
    public int tailSwipeSpinLoopStartFrame = 103;

    [Tooltip("回転突進でループさせるアニメーション区間の終了フレームです。")]
    public int tailSwipeSpinLoopEndFrame = 132;

    [Tooltip("回転突進で使うAnimatorレイヤーです。通常は0でOKです。")]
    public int tailSwipeSpinAnimatorLayer = 0;

    [Tooltip("オンにすると、回転突進中にTail Swipeの指定フレーム区間を毎フレーム手動再生します。回転だけしてアニメーションが止まる場合はオンにしてください。")]
    public bool tailSwipeSpinForceAnimationLoop = true;


    [Tooltip("一回転分の実時間です。小さくすると高速回転、大きくするとゆっくり回転します。")]
    public float tailSwipeSpinLoopDuration = 0.55f;

    [Tooltip("一回転ごとに進む固定距離です。大きくすると一回転ごとに大きく前進します。")]
    public float tailSwipeSpinMoveDistancePerLoop = 4.2f;

    [Tooltip("回転突進の最小ループ回数です。")]
    public int tailSwipeSpinMinLoops = 1;

    [Tooltip("回転突進の最大ループ回数です。大きくすると長く追いかけます。")]
    public int tailSwipeSpinMaxLoops = 4;

    [Tooltip("プレイヤーをどれくらい追い越したら止まるかです。大きくするとプレイヤーの奥まで突っ込みます。")]
    public float tailSwipeSpinOvershootDistance = 2.5f;

    [Tooltip("回転突進中の弱い追尾性能です。0なら完全直線、1以上で少しずつプレイヤー方向へ曲がります。")]
    public float tailSwipeSpinHomingStrength = 0.35f;

    [Tooltip("一回転あたりの回転角度です。反時計回りなら360、逆にしたい場合は-360にしてください。")]
    public float tailSwipeSpinRotationPerLoop = 360f;

    [Tooltip("回転突進の最後に残す慣性移動距離です。")]
    public float tailSwipeSpinInertiaDistance = 1.2f;

    [Tooltip("回転突進の最後に残す慣性時間です。この間も回転は続きます。")]
    public float tailSwipeSpinInertiaDuration = 0.25f;

    [Tooltip("回転突進中に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem tailSwipeSpinDashParticle;

    [Tooltip("回転突進開始時、または回転1ループごとに再生するSEです。未設定ならTailSwipeSfxを使います。")]
    public AudioClip tailSwipeSpinDashSfx;

    [Tooltip("ONならTail Swipeの2段目回転攻撃で、1回転するたびにSEを鳴らします。")]
    public bool playTailSwipeSpinSfxEveryLoop = true;

    [Tooltip("回転突進終了時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip tailSwipeSpinDashEndSfx;

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
    [Tooltip("HP50%で第2形態に入る時のダウン時間です。DifficultyApplierから難易度別に変更できます。")]
    public float halfHPDownDuration = 5.5f;

    [Tooltip("旧設定・互換用です。Half HP Down Durationが0以下の場合だけHP50%ダウン時間として使います。")]
    public float downDuration = 9.11f;

    [Header("尻尾切断リアクション")]
    [Tooltip("オンなら尻尾クリスタル破壊時はDownではなくBigHitリアクションを再生します。")]
    public bool useBigHitOnTailBreak = true;

    [Tooltip("尻尾切断時に再生するBigHitアニメーション名です。Animator側のState名と完全一致させてください。")]
    public string tailBreakBigHitAnim = "Big hit";

    [Tooltip("尻尾切断時のBigHit硬直時間です。短くすると尻尾破壊が強すぎなくなります。")]
    public float tailBreakBigHitDuration = 1.2f;

    [Tooltip("ダウン時に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem downParticle;

    [Tooltip("死亡時に再生するパーティクルです。未設定なら何も再生されません。")]
    public ParticleSystem deathParticle;

    [Tooltip("ダウン時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip downSfx;

    [Tooltip("死亡時に再生するSEです。未設定なら鳴りません。")]
    public AudioClip deathSfx;


    [Header("移動アニメーション安全装置")]
    [Tooltip("オン推奨。移動しているのにIdle/停止アニメのまま滑るバグを防ぐため、移動中は歩き/走りアニメとAnimator速度を監視します。")]
    public bool forceMoveAnimationWhileMoving = true;

    [Tooltip("移動中にアニメーション状態を確認する間隔です。短すぎるとアニメが毎回最初からになりやすいので0.2から0.5程度がおすすめです。")]
    public float moveAnimationSafetyCheckInterval = 0.25f;

    [Tooltip("オンにすると、Animator.speedが0のまま移動し始めた時に強制で1へ戻します。Tail Swipeの手動ループ後などの保険です。")]
    public bool forceAnimatorSpeedOneWhenMoving = true;

    [Tooltip("オン推奨。移動中のWalk/Runが非ループ扱いで終わっても、移動している限り自動で再再生します。")]
    public bool keepMoveAnimationAliveWhileMoving = true;

    [Tooltip("移動アニメがこのNormalizedTimeを超えたら、止まる前に同じ移動アニメを0秒から再再生します。3秒前後で止まる場合は0.75〜0.9推奨です。")]
    [Range(0.2f, 0.98f)]
    public float moveAnimationRestartNormalizedTimeWhileMoving = 0.82f;

    [Tooltip("保険として、この秒数ごとに移動アニメを再再生します。クリップが3秒で切れる場合は2.0〜2.5推奨です。")]
    public float moveAnimationHardRefreshInterval = 2.1f;

    [Tooltip("ONならAnimatorへ直接CrossFadeして移動アニメを再再生します。motion.PlayAnimだけで直らない場合の強い保険です。")]
    public bool useDirectAnimatorReplayForMoveAnimation = true;

    [Tooltip("Direct Replay時のCrossFade秒数。小さくすると止まりにくく、大きくすると滑らかですが切り替えが遅れます。")]
    public float directMoveAnimCrossFadeTime = 0.04f;

    [Header("移動アニメ手動ループ 最終対策")]
    [Tooltip("ON推奨。Walk/RunningをAnimatorのLoop設定に頼らず、移動中だけ47フレームを手動で回し続けます。")]
    public bool useFrameDrivenMoveAnimation = true;

    [Tooltip("Walk/Runningの実フレーム数。今回のアニメが47フレームなら47にします。")]
    public int moveAnimationFrameCount = 47;

    [Tooltip("Walk/RunningのFPS。FBXが30fpsなら30です。")]
    public float moveAnimationLoopFPS = 30f;

    [Tooltip("ONなら、座標移動を試みている間は毎フレームWalk/Runningの再生位置を進めます。非ループFBXでも止まりません。")]
    public bool driveMoveAnimationWhileTryingToMove = true;

    [Tooltip("移動アニメの手動再生速度倍率。足が遅く見えたら上げ、速く見えたら下げます。")]
    public float moveAnimationManualSpeedMultiplier = 1f;

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
    private Animator dragonAnimator;
    private bool skipNextActionInterval = false;

    // AIループとダウン処理を明示管理する。StopAllCoroutines後の二重起動や停止バグ対策。
    private Coroutine aiLoopCoroutine;
    private Coroutine downRoutineCoroutine;

    private void Awake()
    {
        dragonAnimator = GetComponent<Animator>();
        if (dragonAnimator == null)
        {
            dragonAnimator = GetComponentInChildren<Animator>();
        }

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

        if (effectPlayer == null)
        {
            effectPlayer = GetComponentInChildren<DragonAnimationEffectPlayer>();
        }

        if (effectPlayer == null)
        {
            effectPlayer = GetComponentInParent<DragonAnimationEffectPlayer>();
        }

        FindAudioSourceIfNeeded();

        if (beamBreathAimer == null && beamBreathPivot != null)
        {
            beamBreathAimer = beamBreathPivot.GetComponent<DragonBeamBreathAimer>();
        }

        if (motion != null)
        {
            motion.SetPlayer(player);
        }

        FindPlayerHPIfNeeded();

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
            ResetAnimatorSpeedHard();
        }

        StartCoroutine(IntroRoutine());
    }

    private void Update()
    {
        if (state == DragonState.Dead) return;

        if (stopActionsWhenPlayerDead && IsPlayerDead())
        {
            EnterPlayerDeadIdle();
            return;
        }

        if (pausedBecausePlayerDead && resumeAIWhenPlayerRevived && !IsPlayerDead())
        {
            ResumeFromPlayerDeadIdle();
        }

        if (state == DragonState.Down) return;
        if (isBusy) return;
        if (motion == null) return;

        if (stopIdleTurnWhenPlayerTooClose && motion.GetDistanceToPlayer() < innerBackStepDistance)
        {
            return;
        }

        motion.FacePlayerSmooth(motion.idleTurnSpeed);
    }


    private void StartAILoopSafely()
    {
        if (state == DragonState.Dead) return;

        if (aiLoopCoroutine != null)
        {
            StopCoroutine(aiLoopCoroutine);
            aiLoopCoroutine = null;
        }

        aiLoopCoroutine = StartCoroutine(AILoop());
    }

    private void PlayAnimationFromStart(string animName)
    {
        if (string.IsNullOrEmpty(animName)) return;

        if (motion != null)
        {
            ResetAnimatorSpeedHard();
            motion.PlayAnim(animName, true);
        }

        if (dragonAnimator != null)
        {
            dragonAnimator.speed = GetDifficultyAnimationSpeedMultiplier();
            dragonAnimator.Play(animName, 0, 0f);
            dragonAnimator.Update(0f);
        }
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

        yield return WaitForAnimSeconds(roarDuration);

        ReturnToIdle();
        StartAILoopSafely();
    }

    private IEnumerator AILoop()
    {
        while (state != DragonState.Dead)
        {
            if (stopActionsWhenPlayerDead && IsPlayerDead())
            {
                EnterPlayerDeadIdle();
                yield return null;
                continue;
            }

            if (state == DragonState.Down || isBusy || player == null || motion == null)
            {
                yield return null;
                continue;
            }

            float interval = Random.Range(minActionInterval, maxActionInterval);

            if (skipNextActionInterval)
            {
                skipNextActionInterval = false;
                interval = Mathf.Max(0f, innerBackStepNextActionDelay);
            }
            else if (phase != null)
            {
                interval = phase.ApplyActionInterval(interval);
            }

            interval = ApplyDifficultyActionInterval(interval);

            if (interval > 0f)
            {
                yield return new WaitForSeconds(interval);
            }
            else
            {
                yield return null;
            }

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

        if (ShouldUseInnerBackStep(distance))
        {
            yield return ExecuteAction(DragonAction.EmergencyBackStep);
            yield break;
        }

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

    private bool ShouldUseInnerBackStep(float distance)
    {
        if (!enableInnerBackStepAction) return false;
        if (distance > innerBackStepDistance) return false;

        return Random.value < innerBackStepChance;
    }


    private bool ShouldUseSwipeCounter(float distance)
    {
        if (!enableSwipeCounterAction) return false;
        if (distance > swipeCounterReachDistance) return false;

        return Random.value < swipeCounterChance;
    }

    private bool CanSelectTailSwipe(float distance)
    {
        if (!enableTailSwipeAction) return false;
        if (distance > tailSwipeReachDistance) return false;

        if (useEnhancedTailSwipe)
        {
            if (distance < enhancedTailSwipeMinStartDistance) return false;
            if (distance > enhancedTailSwipeMaxStartDistance) return false;
        }

        return true;
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

            if (CanSelectTailSwipe(distance))
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

            if (CanSelectTailSwipe(distance))
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
        if (stopActionsWhenPlayerDead && IsPlayerDead())
        {
            EnterPlayerDeadIdle();
            yield break;
        }

        RegisterAction(action);

        yield return WaitDifficultyAttackStartDelayIfNeeded(action);

        switch (action)
        {
            case DragonAction.Opening:
                yield return OpeningIdle();
                break;

            case DragonAction.Approach:
                yield return ApproachPlayer();
                break;

            case DragonAction.EmergencyBackStep:
                yield return EmergencyBackStepOnly();
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

    private IEnumerator WaitDifficultyAttackStartDelayIfNeeded(DragonAction action)
    {
        float delay = Mathf.Max(0f, difficultyAttackStartDelay);

        if (delay <= 0f)
        {
            yield break;
        }

        if (applyDifficultyAttackStartDelayOnlyToAttackActions && !IsAttackActionForDifficultyDelay(action))
        {
            yield break;
        }

        DisableAllHitboxes();
        StopAllSpecialParticles();
        ResetAnimatorSpeedHard();

        if (motion != null)
        {
            motion.PlayAnim(motion.idleAnim, true);
        }

        float timer = 0f;

        while (timer < delay)
        {
            if (state == DragonState.Dead || state == DragonState.Down)
            {
                yield break;
            }

            timer += Time.deltaTime;

            if (facePlayerDuringDifficultyAttackStartDelay && motion != null && player != null)
            {
                motion.FacePlayerSmooth(motion.idleTurnSpeed);
            }

            yield return null;
        }
    }

    private bool IsAttackActionForDifficultyDelay(DragonAction action)
    {
        switch (action)
        {
            case DragonAction.SwipeCounter:
            case DragonAction.TailSlam:
            case DragonAction.TailSwipe:
            case DragonAction.BackStepTailSlam:
            case DragonAction.BackStepTailSwipe:
            case DragonAction.BackStepWideBreath:
            case DragonAction.BackStepBeamBreath:
            case DragonAction.WideBreath:
            case DragonAction.BeamBreath:
            case DragonAction.Charge:
            case DragonAction.DoubleCharge:
                return true;

            default:
                return false;
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
        ResetAnimatorSpeedHard();
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
        ResetAnimatorSpeedHard();

        bool running = motion.GetDistanceToPlayer() >= switchToRunDistance;
        string moveAnim = running ? motion.runAnim : motion.walkAnim;

        PlayMoveAnimSafe(moveAnim);

        float checkTimer = 0f;
        float moveAnimSafetyTimer = 0f;
        float moveAnimLoopTimer = 0f;
        float frameDrivenMoveAnimTimer = 0f;
        StartFrameDrivenMoveAnimation(moveAnim, ref frameDrivenMoveAnimTimer);
        Vector3 previousPosition = motion != null ? motion.transform.position : transform.position;

        while (player != null && motion.GetDistanceToPlayer() > approachStopDistance)
        {
            if (state == DragonState.Down || state == DragonState.Dead)
            {
                yield break;
            }

            float distance = motion.GetDistanceToPlayer();

            bool shouldRun = running;

            if (!running && distance >= switchToRunDistance)
            {
                shouldRun = true;
            }
            else if (running && distance <= runChaseStopDistance)
            {
                shouldRun = false;
            }

            if (shouldRun != running)
            {
                running = shouldRun;
                moveAnim = running ? motion.runAnim : motion.walkAnim;
                PlayMoveAnimSafe(moveAnim);
                checkTimer = 0f;
                moveAnimSafetyTimer = 0f;
                moveAnimLoopTimer = 0f;
                StartFrameDrivenMoveAnimation(moveAnim, ref frameDrivenMoveAnimTimer);
            }

            motion.FacePlayerSmooth(motion.actionTurnSpeed);

            float speed = running ? runChaseSpeed : walkSpeed;

            if (phase != null)
            {
                speed = phase.ApplySpeed(speed);
            }

            speed = ApplyDifficultyMoveSpeed(speed);

            Vector3 moveDelta = motion.GetMoveForward() * speed * Time.deltaTime;
            motion.MoveDragon(moveDelta);

            Vector3 currentPosition = motion != null ? motion.transform.position : transform.position;
            float movedDistance = Vector3.Distance(currentPosition, previousPosition);
            previousPosition = currentPosition;

            bool isActuallyMoving = movedDistance > 0.002f;
            bool isTryingToMove = moveDelta.sqrMagnitude > 0.000001f;

            if (useFrameDrivenMoveAnimation)
            {
                if (driveMoveAnimationWhileTryingToMove ? isTryingToMove : isActuallyMoving)
                {
                    DriveMoveAnimationByFrameTimer(moveAnim, ref frameDrivenMoveAnimTimer);
                }
            }
            else
            {
                checkTimer += Time.deltaTime;
                KeepMoveAnimSafe(moveAnim, ref checkTimer, ref moveAnimSafetyTimer);

                if (isActuallyMoving)
                {
                    ForceMoveAnimationIfNeeded(moveAnim);
                }

                KeepMoveAnimationAliveIfNeeded(moveAnim, ref moveAnimLoopTimer, isActuallyMoving);
            }

            yield return null;
        }

        ReturnToIdle();
    }

    private IEnumerator EmergencyBackStepOnly()
    {
        isBusy = true;
        state = DragonState.Acting;

        DisableAllHitboxes();
        StopAllSpecialParticles();

        if (motion != null)
        {
            ResetAnimatorSpeedHard();
        }

        yield return DoInnerBackSteps();

        if (innerBackStepRecovery > 0f)
        {
            yield return new WaitForSeconds(innerBackStepRecovery);
        }

        if (innerBackStepConnectNextActionImmediately)
        {
            skipNextActionInterval = true;
        }

        ReturnToIdle();
    }

    private IEnumerator DoInnerBackSteps()
    {
        int safeCount = Mathf.Max(0, innerBackStepCount);

        for (int i = 0; i < safeCount; i++)
        {
            yield return motion.FacePlayerForSeconds(0.06f);

            Vector3 backDirection = -motion.GetMoveForward();

            yield return motion.MoveInDirectionForSeconds(
                backDirection,
                innerBackStepMoveDistance,
                innerBackStepMoveDuration,
                motion.stepAnim
            );

            if (backStepInterval > 0f)
            {
                yield return new WaitForSeconds(backStepInterval);
            }
        }
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
        if (IsTailBroken() || !enableTailSlamAction)
        {
            yield return ApproachPlayer();
            yield break;
        }

        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();
        StopAllSpecialParticles();

        yield return DoFixedBackSteps(closeSpecialBackStepCount);

        if (IsTailBroken() || state == DragonState.Down || state == DragonState.Dead)
        {
            ReturnToIdle();
            yield break;
        }

        yield return TailSlam();
    }

    private IEnumerator BackStepThenTailSwipe()
    {
        if (IsTailBroken() || !enableTailSwipeAction)
        {
            yield return ApproachPlayer();
            yield break;
        }

        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();
        StopAllSpecialParticles();

        yield return DoFixedBackSteps(closeSpecialBackStepCount);

        if (IsTailBroken() || state == DragonState.Down || state == DragonState.Dead)
        {
            ReturnToIdle();
            yield break;
        }

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

        float anticipationEndAnimTime = ScaleAnimFrameTime(motion.FrameToSeconds(effectiveAnticipationEndFrame));
        float attackEndAnimTime = ScaleAnimFrameTime(motion.FrameToSeconds(effectiveAttackEndFrame));
        float lungeStart = ScaleAnimFrameTime(motion.FrameToSeconds(swipeLungeStartFrame));
        float lungeEnd = ScaleAnimFrameTime(motion.FrameToSeconds(swipeLungeEndFrame));
        float configuredHitStart = ScaleAnimFrameTime(motion.FrameToSeconds(swipeHitStartFrame));

        float rawAnticipationDuration = Mathf.Max(0.01f, anticipationEndAnimTime);
        float rawAttackDuration = Mathf.Max(0.01f, attackEndAnimTime - anticipationEndAnimTime);
        float targetAnticipationDuration = Mathf.Max(0.01f, ScaleAnimDuration(swipeAnticipationDuration));
        float targetAttackDuration = Mathf.Max(0.01f, ScaleAnimDuration(swipeAttackDuration));

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

        PlayEffect(DragonEffectKind.Swipe, swipeAnticipationSfx, swipeAnticipationParticle);

        if (shouldStretchAnticipation)
        {
            float anticipationAnimatorSpeed = rawAnticipationDuration / targetAnticipationDuration;
            SetAnimatorSpeedScaled(anticipationAnimatorSpeed);
        }
        else
        {
            ResetAnimatorSpeedHard();
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

        PlayEffect(DragonEffectKind.Swipe, swipeSfx, swipeParticle);

        if (shouldStretchAttack)
        {
            float attackAnimatorSpeed = rawAttackDuration / targetAttackDuration;
            SetAnimatorSpeedScaled(attackAnimatorSpeed);
        }
        else
        {
            ResetAnimatorSpeedHard();
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

        ResetAnimatorSpeedHard();

        float afterAttackDuration = Mathf.Max(0f, ScaleAnimDuration(swipeAnimDuration) - attackEndAnimTime);
        if (afterAttackDuration > 0f)
        {
            yield return new WaitForSeconds(afterAttackDuration);
        }

        if (swipeAnticipationParticle != null)
        {
            swipeAnticipationParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        ResetAnimatorSpeedHard();

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

        SetAnimatorSpeedScaled(breathAnimatorSpeed);
        motion.PlayAnim(motion.breathAnim, true);

        PlayEffect(DragonEffectKind.BreathCharge, wideBreathChargeSfx, wideBreathChargeParticle);

        float start = ScaleAnimFrameTime(motion.FrameToSeconds(wideBreathStartFrame), breathAnimatorSpeed);
        float end = ScaleAnimFrameTime(motion.FrameToSeconds(wideBreathEndFrame), breathAnimatorSpeed);

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

        PlayEffect(DragonEffectKind.BreathFire, wideBreathFireSfx, wideBreathFireParticle);

        if (wideBreathHitbox != null) wideBreathHitbox.EnableHitbox();

        yield return new WaitForSeconds(Mathf.Max(0f, end - start));

        if (wideBreathHitbox != null) wideBreathHitbox.DisableHitbox();

        if (wideBreathFireParticle != null)
        {
            wideBreathFireParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, ScaleAnimDuration(breathDuration, breathAnimatorSpeed) - end));
        yield return WaitForAnimSeconds(wideBreathRecovery, breathAnimatorSpeed);

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

        SetAnimatorSpeedScaled(breathAnimatorSpeed);
        motion.PlayAnim(motion.breathAnim, true);

        if (beamBreathAimer != null)
        {
            beamBreathAimer.player = player;
            beamBreathAimer.AimInstant();
        }

        PlayEffect(DragonEffectKind.BreathCharge, beamBreathChargeSfx, beamBreathChargeParticle);

        float start = ScaleAnimFrameTime(motion.FrameToSeconds(beamBreathStartFrame), breathAnimatorSpeed);
        float end = ScaleAnimFrameTime(motion.FrameToSeconds(beamBreathEndFrame), breathAnimatorSpeed);
        float trackStart = ScaleAnimFrameTime(motion.FrameToSeconds(beamTrackStartFrame), breathAnimatorSpeed);
        float trackEnd = ScaleAnimFrameTime(motion.FrameToSeconds(beamTrackEndFrame), breathAnimatorSpeed);

        float timer = 0f;
        bool hitboxEnabled = false;
        bool fireParticlePlayed = false;

        float breathRealDuration = ScaleAnimDuration(breathDuration, breathAnimatorSpeed);

        while (timer < breathRealDuration)
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

                PlayEffect(DragonEffectKind.BreathFire, beamBreathFireSfx, beamBreathFireParticle);
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

        yield return WaitForAnimSeconds(beamBreathRecovery, breathAnimatorSpeed);

        ReturnToIdle();
    }

    private IEnumerator ChargeAttack(bool consumeCooldown)
    {
        isBusy = true;
        state = DragonState.Acting;

        DisableAllHitboxes();
        StopAllSpecialParticles();
        ResetAnimatorSpeedHard();

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
        SetAnimatorSpeedScaled(chargeTellAnimationSpeed);

        PlayEffect(DragonEffectKind.ChargeStart, chargeHoldSfx, chargeHoldParticle);

        float tellTimer = 0f;
        float checkTimer = 0f;

        while (tellTimer < GetChargeTellDuration())
        {
            tellTimer += Time.deltaTime;
            checkTimer += Time.deltaTime;

            motion.KeepMoveAnim(motion.runAnim, ref checkTimer);
            SetAnimatorSpeedScaled(chargeTellAnimationSpeed);
            motion.FacePlayerSmooth(motion.actionTurnSpeed);

            yield return null;
        }

        if (chargeHoldParticle != null)
        {
            chargeHoldParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        PlayEffect(DragonEffectKind.ChargeStart, chargeReadySfx, chargeReadyParticle);

        ResetAnimatorSpeedHard();
        PlayMoveAnimSafe(motion.runAnim);

        yield return WaitForChargeReadyPause();

        Vector3 chargeDirection = motion.GetDirectionToPlayer();
        float targetDistance = GetChargeTargetDistance(chargeDirection);

        float baseSpeed = chargeSpeed;

        if (phase != null)
        {
            baseSpeed = phase.ApplyChargeSpeed(baseSpeed);
        }

        baseSpeed = ApplyDifficultyMoveSpeed(baseSpeed);

        float chargeTime = Mathf.Clamp(
            targetDistance / Mathf.Max(0.01f, baseSpeed),
            ScaleAnimDuration(chargeMinDuration),
            ScaleAnimDuration(chargeMaxDuration)
        );

        float timer = 0f;
        float previousDistance = 0f;
        float runCheckTimer = 0f;

        PlayEffect(DragonEffectKind.ChargeRun, chargeRunSfx, chargeRunParticle);

        if (chargeHitbox != null) chargeHitbox.EnableHitbox();

        PlayMoveAnimSafe(motion.runAnim);
        ResetAnimatorSpeedHard();

        float chargeMoveAnimSafetyTimer = 0f;
        float chargeMoveAnimLoopTimer = 0f;

        while (timer < chargeTime)
        {
            timer += Time.deltaTime;
            runCheckTimer += Time.deltaTime;

            KeepMoveAnimSafe(motion.runAnim, ref runCheckTimer, ref chargeMoveAnimSafetyTimer);

            chargeDirection = ApplyDifficultyChargeHoming(chargeDirection);

            float t = Mathf.Clamp01(timer / chargeTime);
            float eased = EvaluateChargeMoveCurve(t);
            float currentDistance = targetDistance * eased;
            float deltaDistance = currentDistance - previousDistance;
            previousDistance = currentDistance;

            motion.MoveDragon(chargeDirection * deltaDistance);
            KeepMoveAnimationAliveIfNeeded(motion.runAnim, ref chargeMoveAnimLoopTimer, deltaDistance > 0.002f);

            yield return null;
        }

        if (chargeHitbox != null) chargeHitbox.DisableHitbox();

        StopAllChargeParticles();
        ResetAnimatorSpeedHard();
        PlayEffect(DragonEffectKind.ChargeEnd, chargeEndSfx, null);

        yield return WaitForAnimSeconds(chargeRecoveryTime);

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

        yield return WaitForAnimSeconds(doubleChargeInterval);

        if (state == DragonState.Dead || state == DragonState.Down) yield break;

        yield return ChargeAttack(false);

        isBusy = true;
        state = DragonState.Acting;

        yield return WaitForAnimSeconds(doubleChargeRecovery);

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
            ReturnToIdle();
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

        // TailSlamのSEは開始時ではなく、叩きつけ判定ONの瞬間に鳴らす。
        // ここではDelayを使わない。Volume/ClipだけEffectPlayer設定を使う。
        PlayTailSlamStartParticleOnly();

        float timer = 0f;

        float aimStartTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSlamAimStartFrame));
        float trackEndTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSlamTrackUntilFrame));
        float hitStartTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSlamHitStartFrame));
        float hitEndTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSlamHitEndFrame));
        float returnStartTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSlamReturnStartFrame));
        float returnEndTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSlamReturnEndFrame));
        float tailSlamRealDuration = ScaleAnimDuration(tailSlamDuration);

        bool hitboxEnabled = false;
        bool tailSlamSfxPlayed = false;
        bool tailSlamPreHitDelayDone = false;
        bool returnStarted = false;
        Quaternion returnStartRotation = transform.rotation;

        while (timer < tailSlamRealDuration)
        {
            timer += Time.deltaTime;

            if (IsTailBroken() || state == DragonState.Down || state == DragonState.Dead)
            {
                if (tailHitbox != null) tailHitbox.DisableHitbox();
                yield break;
            }

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

            if (!tailSlamPreHitDelayDone && timer >= hitStartTime)
            {
                tailSlamPreHitDelayDone = true;
                yield return WaitDifficultyTailPreHitDelay(
                    GetDifficultyTailSlamPreHitDelay(),
                    tailSlamAttackOffsetY,
                    tailSlamAngleOffset
                );
            }

            float tailSlamSfxTime = Mathf.Max(0f, hitStartTime - Mathf.Max(0f, tailSlamImpactSfxAdvanceTime));

            if (!tailSlamSfxPlayed
                && timer >= tailSlamSfxTime
                && (!ShouldUseDifficultyTailSlamPreHitDelay() || tailSlamPreHitDelayDone))
            {
                tailSlamSfxPlayed = true;
                PlayTailSlamImpactSfx();
            }

            bool shouldEnableHitbox =
                timer >= hitStartTime
                && timer <= hitEndTime
                && (!ShouldUseDifficultyTailSlamPreHitDelay() || tailSlamPreHitDelayDone);

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
            ReturnToIdle();
            yield break;
        }

        isBusy = true;
        state = DragonState.Acting;
        DisableAllHitboxes();
        StopAllSpecialParticles();

        float startDistance = motion.GetDistanceToPlayer();

        if (startDistance > tailSwipeReachDistance)
        {
            ReturnToIdle();
            yield break;
        }

        if (useEnhancedTailSwipe)
        {
            if (startDistance < enhancedTailSwipeMinStartDistance || startDistance > enhancedTailSwipeMaxStartDistance)
            {
                ReturnToIdle();
                yield break;
            }
        }

        ResetAnimatorSpeedHard();
        motion.PlayAnim(motion.tailSwipeAnim, true);

        PlayTailSwipeStartEffect();

        float timer = 0f;

        float aimStartTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSwipeAimStartFrame));
        float trackEndTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSwipeTrackUntilFrame));

        float repositionStartTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSwipeRepositionStartFrame));
        float repositionEndTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSwipeRepositionEndFrame));

        float firstHitStartTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSwipeSlamHitStartFrame));
        float firstHitEndTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSwipeSlamHitEndFrame));

        float secondTellStartTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSwipeSecondTellStartFrame));
        float secondTellEndTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSwipeSecondTellEndFrame));

        float spinLoopStartTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSwipeSpinLoopStartFrame));
        float spinLoopEndTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSwipeSpinLoopEndFrame));

        bool hitboxEnabled = false;
        bool firstSlamSfxPlayed = false;
        bool tailSwipeFirstPreHitDelayDone = false;
        bool repositionStarted = false;

        Vector3 repositionStartPosition = transform.position;
        Vector3 repositionTargetPosition = transform.position;

        float firstPhaseEndTime = useTailSwipeSecondTell ? secondTellStartTime : spinLoopStartTime;

        while (timer < firstPhaseEndTime)
        {
            timer += Time.deltaTime;

            if (IsTailBroken() || state == DragonState.Down || state == DragonState.Dead)
            {
                if (tailHitbox != null) tailHitbox.DisableHitbox();
                if (tailSwipeSpinDashHitbox != null) tailSwipeSpinDashHitbox.DisableHitbox();
                yield break;
            }

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

            if (useEnhancedTailSwipe && tailSwipeUseRepositionMove && timer >= repositionStartTime && timer <= repositionEndTime)
            {
                if (!repositionStarted)
                {
                    repositionStarted = true;
                    repositionStartPosition = transform.position;
                    repositionTargetPosition = CalculateTailSwipeRepositionTarget();
                }

                float moveDuration = Mathf.Max(0.01f, repositionEndTime - repositionStartTime);
                float moveT = Mathf.Clamp01((timer - repositionStartTime) / moveDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, moveT);

                Vector3 desiredPosition = Vector3.Lerp(repositionStartPosition, repositionTargetPosition, smoothT);
                Vector3 delta = desiredPosition - transform.position;
                delta.y = 0f;

                motion.MoveDragon(delta);

                Quaternion targetRotation;

                if (tailAttacksTurnTailToPlayer)
                {
                    targetRotation = motion.GetTailRotationToPlayer(tailFacePlayerOffsetY, tailSwipeFixedAttackOffsetY);
                }
                else
                {
                    targetRotation = motion.GetRotationToPlayerWithOffset(tailSwipeFixedAttackOffsetY);
                }

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, tailSwipeRepositionTurnSpeed * Time.deltaTime);
            }

            if (!tailSwipeFirstPreHitDelayDone && timer >= firstHitStartTime)
            {
                tailSwipeFirstPreHitDelayDone = true;
                yield return WaitDifficultyTailPreHitDelay(
                    GetDifficultyTailSwipeFirstPreHitDelay(),
                    tailSwipeFixedAttackOffsetY,
                    tailSwipeFixedAttackOffsetY
                );
            }

            float tailSwipeFirstSlamSfxTime = Mathf.Max(0f, firstHitStartTime - Mathf.Max(0f, tailSwipeFirstSlamSfxAdvanceTime));

            if (!firstSlamSfxPlayed
                && timer >= tailSwipeFirstSlamSfxTime
                && (!ShouldUseDifficultyTailSwipeFirstPreHitDelay() || tailSwipeFirstPreHitDelayDone))
            {
                firstSlamSfxPlayed = true;
                PlayTailSwipeFirstSlamSfx();
            }

            bool shouldEnableHitbox =
                timer >= firstHitStartTime
                && timer <= firstHitEndTime
                && (!ShouldUseDifficultyTailSwipeFirstPreHitDelay() || tailSwipeFirstPreHitDelayDone);

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

        if (useTailSwipeSecondTell)
        {
            yield return TailSwipeSecondTell(secondTellStartTime, secondTellEndTime);
        }

        if (useTailSwipeSpinDash)
        {
            yield return TailSwipeSpinDash(spinLoopStartTime, spinLoopEndTime);
        }
        else
        {
            yield return TailSwipeOriginalSecondHit();
        }

        if (tailHitbox != null) tailHitbox.DisableHitbox();

        if (tailSwipeSpinDashHitbox != null) tailSwipeSpinDashHitbox.DisableHitbox();

        ResetAnimatorSpeedHard();

        ReturnToIdle();
    }

    private void PlayTailSwipeStartEffect()
    {
        if (tailSwipeParticle != null)
        {
            PrepareParticleForDifficultyPlay(tailSwipeParticle);

            if (restartParticleBeforePlay)
            {
                tailSwipeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            tailSwipeParticle.Play(true);
        }

        if (playTailSwipeStartSfx && tailSwipeSfx != null)
        {
            PlaySfx(tailSwipeSfx);
        }
    }

    private void PlayTailSlamStartParticleOnly()
    {
        ParticleSystem particle = tailSlamParticle != null ? tailSlamParticle : GetCommonParticle(DragonEffectKind.TailSlam);

        if (particle == null) return;

        PrepareParticleForDifficultyPlay(particle);

        if (restartParticleBeforePlay)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        particle.Play(true);
    }

    private void PlayTailSlamImpactSfx()
    {
        // 当たり判定ONの瞬間に鳴らすため、EffectPlayer側のDelayは使わない。
        // Volume/ClipだけEffectPlayer設定を使う。
        if (effectPlayer != null && effectPlayer.TryPlayTailSlamSfxOnHitImmediate())
        {
            return;
        }

        AudioClip clip = tailSlamSfx != null ? tailSlamSfx : GetCommonClip(DragonEffectKind.TailSlam);
        PlaySfxRawNoEffectPlayerDelay(clip);
    }

    private void PlayTailSwipeFirstSlamSfx()
    {
        if (!useSeparateTailSwipeFirstSlamSfx) return;

        // TailSwipe一段目はEffectPlayerの Tail Swipe First Slam 設定を必ず使う。
        // ClipだけTailSlamと同じにできるが、Delay/VolumeはTailSlamとは完全に別管理。
        // 当たり判定ONの瞬間に鳴らすため、EffectPlayer側のDelayは使わない。
        // ClipはTailSlamと同じにできるが、VolumeはTailSwipe First Slam側を使える。
        if (effectPlayer != null && effectPlayer.TryPlayTailSwipeFirstSlamSfxOnHitImmediate())
        {
            return;
        }

        AudioClip clip = useTailSlamSfxForTailSwipeFirstHit ? tailSlamSfx : tailSwipeSfx;
        PlaySfxRawNoEffectPlayerDelay(clip);
    }

    private void PlayTailSwipeSpinLoopSfx()
    {
        if (effectPlayer != null && effectPlayer.TryPlayTailSwipeSpinLoopSfxOnly())
        {
            return;
        }

        AudioClip clip = tailSwipeSpinDashSfx != null ? tailSwipeSpinDashSfx : tailSwipeSfx;
        PlaySfxRawNoEffectPlayerDelay(clip);
    }

    private void PlaySfxRawNoEffectPlayerDelay(AudioClip clip)
    {
        if (clip == null) return;

        FindAudioSourceIfNeeded();

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip, sfxVolume);
        }
    }

    private Vector3 CalculateTailSwipeRepositionTarget()
    {
        if (player == null)
        {
            return transform.position;
        }

        Vector3 toDragon = transform.position - player.position;
        toDragon.y = 0f;

        if (toDragon.sqrMagnitude < 0.001f)
        {
            toDragon = -transform.forward;
        }

        Vector3 desiredFromPlayer = toDragon.normalized * Mathf.Max(0.1f, tailSwipeIdealHitDistance);

        Vector3 side = Vector3.Cross(Vector3.up, desiredFromPlayer.normalized);
        desiredFromPlayer += side * tailSwipeRepositionSideOffset;

        Vector3 desiredPosition = player.position + desiredFromPlayer;
        desiredPosition.y = transform.position.y;

        Vector3 delta = desiredPosition - transform.position;
        delta.y = 0f;

        float maxDistance = Mathf.Max(0f, tailSwipeRepositionMaxDistance);
        if (delta.magnitude > maxDistance)
        {
            delta = delta.normalized * maxDistance;
        }

        return transform.position + delta;
    }

    private IEnumerator TailSwipeSecondTell(float tellStartTime, float tellEndTime)
    {
        if (tailSwipeSecondTellParticle != null)
        {
            PrepareParticleForDifficultyPlay(tailSwipeSecondTellParticle);
            if (restartParticleBeforePlay)
            {
                tailSwipeSecondTellParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            tailSwipeSecondTellParticle.Play(true);
        }

        PlayEffect(DragonEffectKind.TailSwipe, tailSwipeSecondTellSfx, null);

        float rawTellDuration = Mathf.Max(0.01f, tellEndTime - tellStartTime);
        float targetTellDuration = Mathf.Max(rawTellDuration, ScaleAnimDuration(tailSwipeSecondTellDuration));

        float animatorSpeed = rawTellDuration / targetTellDuration;
        SetAnimatorSpeedScaled(animatorSpeed);

        float timer = 0f;

        while (timer < targetTellDuration)
        {
            timer += Time.deltaTime;
            motion.FacePlayerSmooth(tailTrackingTurnSpeed);
            yield return null;
        }

        if (tailSwipeSecondTellParticle != null)
        {
            tailSwipeSecondTellParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        ResetAnimatorSpeedHard();
    }

    private IEnumerator TailSwipeOriginalSecondHit()
    {
        float secondHitStartTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSwipeSecondHitStartFrame));
        float secondHitEndTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSwipeSecondHitEndFrame));
        float secondTurnStartTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSwipeSecondTurnStartFrame));
        float secondTurnEndTime = ScaleAnimFrameTime(motion.FrameToSeconds(tailSwipeSecondTurnEndFrame));

        float timer = secondTurnStartTime;
        float tailSwipeRealDuration = ScaleAnimDuration(tailSwipeDuration);
        bool hitboxEnabled = false;
        bool secondTurnStarted = false;
        Quaternion secondTurnStartRotation = transform.rotation;

        if (keepTailSwipeSecondHitboxActiveDuringSecondAttack && tailHitbox != null)
        {
            hitboxEnabled = true;
            tailHitbox.EnableHitbox();
        }

        while (timer < tailSwipeRealDuration)
        {
            timer += Time.deltaTime;

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

            if (keepTailSwipeSecondHitboxActiveDuringSecondAttack)
            {
                if (tailHitbox != null && forceTailSwipeSecondHitboxEveryFrame)
                {
                    tailHitbox.EnableHitbox();
                }
            }
            else
            {
                bool shouldEnableHitbox = timer >= secondHitStartTime && timer <= secondHitEndTime;

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
            }

            yield return null;
        }

        if (tailHitbox != null) tailHitbox.DisableHitbox();
    }

    private IEnumerator TailSwipeSpinDash(float loopStartTime, float loopEndTime)
    {
        DragonAttackHitbox spinHitbox = tailSwipeSpinDashHitbox != null ? tailSwipeSpinDashHitbox : tailHitbox;

        if (tailSwipeSpinDashParticle != null)
        {
            PrepareParticleForDifficultyPlay(tailSwipeSpinDashParticle);
            if (restartParticleBeforePlay)
            {
                tailSwipeSpinDashParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            tailSwipeSpinDashParticle.Play(true);
        }

        if (!playTailSwipeSpinSfxEveryLoop)
        {
            PlayTailSwipeSpinLoopSfx();
        }

        float rawLoopDuration = Mathf.Max(0.01f, loopEndTime - loopStartTime);
        float loopDuration = Mathf.Max(0.05f, ScaleAnimDuration(tailSwipeSpinLoopDuration));
        float animatorSpeed = rawLoopDuration / loopDuration;

        Vector3 dashDirection = GetFlatDirectionToPlayer();

        float targetDistance = GetTailSwipeSpinDashTargetDistance(dashDirection);
        int requiredLoops = Mathf.CeilToInt(targetDistance / Mathf.Max(0.01f, tailSwipeSpinMoveDistancePerLoop));
        int loopCount = Mathf.Clamp(requiredLoops, Mathf.Max(1, tailSwipeSpinMinLoops), Mathf.Max(1, tailSwipeSpinMaxLoops));

        if (spinHitbox != null)
        {
            spinHitbox.EnableHitbox();
        }

        for (int i = 0; i < loopCount; i++)
        {
            float timer = 0f;
            float previousMove = 0f;
            Quaternion baseRotation = Quaternion.LookRotation(dashDirection, Vector3.up);

            UpdateTailSwipeSpinLoopAnimation(loopStartTime, loopEndTime, 0f, loopDuration, animatorSpeed);

            if (playTailSwipeSpinSfxEveryLoop)
            {
                PlayTailSwipeSpinLoopSfx();
            }

            while (timer < loopDuration)
            {
                timer += Time.deltaTime;

                if (keepTailSwipeSecondHitboxActiveDuringSecondAttack && forceTailSwipeSecondHitboxEveryFrame && spinHitbox != null)
                {
                    spinHitbox.EnableHitbox();
                }

                if (player != null && tailSwipeSpinHomingStrength > 0f)
                {
                    Vector3 toPlayer = GetFlatDirectionToPlayer();
                    dashDirection = Vector3.Slerp(
                        dashDirection,
                        toPlayer,
                        tailSwipeSpinHomingStrength * Time.deltaTime
                    ).normalized;

                    baseRotation = Quaternion.LookRotation(dashDirection, Vector3.up);
                }

                float t = Mathf.Clamp01(timer / loopDuration);
                float currentMove = tailSwipeSpinMoveDistancePerLoop * t;
                float deltaMove = currentMove - previousMove;
                previousMove = currentMove;

                motion.MoveDragon(dashDirection * deltaMove);

                float spinAngle = tailSwipeSpinRotationPerLoop * t;
                transform.rotation = baseRotation * Quaternion.Euler(0f, spinAngle, 0f);

                UpdateTailSwipeSpinLoopAnimation(loopStartTime, loopEndTime, timer, loopDuration, animatorSpeed);

                yield return null;
            }

            UpdateTailSwipeSpinLoopAnimation(loopStartTime, loopEndTime, loopDuration, loopDuration, animatorSpeed);
        }

        float tailSwipeSpinRealInertiaDuration = ScaleAnimDuration(tailSwipeSpinInertiaDuration);

        if (tailSwipeSpinRealInertiaDuration > 0f && tailSwipeSpinInertiaDistance > 0f)
        {
            float timer = 0f;
            float previousMove = 0f;
            Quaternion baseRotation = Quaternion.LookRotation(dashDirection, Vector3.up);

            while (timer < tailSwipeSpinRealInertiaDuration)
            {
                timer += Time.deltaTime;

                if (keepTailSwipeSecondHitboxActiveDuringSecondAttack && forceTailSwipeSecondHitboxEveryFrame && spinHitbox != null)
                {
                    spinHitbox.EnableHitbox();
                }

                float t = Mathf.Clamp01(timer / tailSwipeSpinRealInertiaDuration);
                float eased = 1f - Mathf.Pow(1f - t, 2f);

                float currentMove = tailSwipeSpinInertiaDistance * eased;
                float deltaMove = currentMove - previousMove;
                previousMove = currentMove;

                motion.MoveDragon(dashDirection * deltaMove);

                float spinAngle = tailSwipeSpinRotationPerLoop * t;
                transform.rotation = baseRotation * Quaternion.Euler(0f, spinAngle, 0f);

                float animationLoopTime = Mathf.Repeat(timer, loopDuration);
                UpdateTailSwipeSpinLoopAnimation(loopStartTime, loopEndTime, animationLoopTime, loopDuration, animatorSpeed);

                yield return null;
            }
        }

        if (spinHitbox != null)
        {
            spinHitbox.DisableHitbox();
        }

        if (tailSwipeSpinDashParticle != null)
        {
            tailSwipeSpinDashParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        PlayEffect(DragonEffectKind.TailSwipe, tailSwipeSpinDashEndSfx, null);
        ResetAnimatorSpeedHard();

        if (dragonAnimator != null)
        {
            dragonAnimator.speed = GetDifficultyAnimationSpeedMultiplier();
        }
    }

    private void UpdateTailSwipeSpinLoopAnimation(float loopStartTime, float loopEndTime, float loopTimer, float loopDuration, float animatorSpeed)
    {
        if (motion == null || string.IsNullOrEmpty(motion.tailSwipeAnim))
        {
            return;
        }

        if (!tailSwipeSpinForceAnimationLoop)
        {
            SetAnimatorSpeedScaled(animatorSpeed);
            return;
        }

        if (dragonAnimator == null)
        {
            SetAnimatorSpeedScaled(animatorSpeed);
            motion.PlayAnim(motion.tailSwipeAnim, true);
            return;
        }

        float safeTailSwipeDuration = Mathf.Max(0.01f, ScaleAnimDuration(tailSwipeDuration));
        float startNormalizedTime = Mathf.Clamp01(loopStartTime / safeTailSwipeDuration);
        float endNormalizedTime = Mathf.Clamp01(loopEndTime / safeTailSwipeDuration);

        if (endNormalizedTime <= startNormalizedTime)
        {
            endNormalizedTime = Mathf.Min(0.999f, startNormalizedTime + 0.01f);
        }

        float loopT = Mathf.Clamp01(loopTimer / Mathf.Max(0.01f, loopDuration));
        float normalizedTime = Mathf.Lerp(startNormalizedTime, endNormalizedTime, loopT);

        dragonAnimator.speed = 0f;
        dragonAnimator.Play(motion.tailSwipeAnim, tailSwipeSpinAnimatorLayer, normalizedTime);
        dragonAnimator.Update(0f);
    }

    private float GetTailSwipeSpinDashTargetDistance(Vector3 dashDirection)
    {
        if (player == null)
        {
            return tailSwipeSpinMoveDistancePerLoop;
        }

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        float distanceToPlayerAlongDash = Mathf.Max(0f, Vector3.Dot(toPlayer, dashDirection.normalized));
        return distanceToPlayerAlongDash + tailSwipeSpinOvershootDistance;
    }

    private Vector3 GetFlatDirectionToPlayer()
    {
        if (player == null)
        {
            return transform.forward;
        }

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = transform.forward;
        }

        return direction.normalized;
    }

    private void HandleHalfHP()
    {
        if (state == DragonState.Dead) return;

        if (phase != null)
        {
            phase.EnterPhase2();
        }

        StopAllCoroutines();
        aiLoopCoroutine = null;
        downRoutineCoroutine = null;

        DisableAllHitboxes();
        StopAllSpecialParticles();
        ResetAnimatorSpeedHard();

        // HP50%時は咆哮ではなく、ダウンを最初から再生する。
        downRoutineCoroutine = StartCoroutine(HalfHPDownRoutine());
    }

    private IEnumerator HalfHPDownRoutine()
    {
        DisableAllHitboxes();
        StopAllSpecialParticles();
        ResetAnimatorSpeedHard();

        isBusy = true;
        state = DragonState.Down;

        PlayAnimationFromStart(motion != null ? motion.downAnim : string.Empty);

        if (downParticle != null)
        {
            downParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            downParticle.Play();
        }

        PlayEffect(DragonEffectKind.Down, downSfx, null);

        yield return new WaitForSeconds(GetHalfHPDownDuration());

        if (state == DragonState.Dead)
        {
            yield break;
        }

        ReturnFromDownToAI();
    }

    private void HandleTailCrystalBroken()
    {
        if (state == DragonState.Dead) return;

        // 尻尾切断が入ったら、現在の行動やダウンを止めてリアクションへ移る。
        StopAllCoroutines();
        aiLoopCoroutine = null;
        downRoutineCoroutine = null;

        DisableAllHitboxes();
        StopAllSpecialParticles();
        ResetAnimatorSpeedHard();

        if (useBigHitOnTailBreak)
        {
            downRoutineCoroutine = StartCoroutine(TailBreakBigHitRoutine());
        }
        else
        {
            downRoutineCoroutine = StartCoroutine(DownRoutine());
        }
    }

    private IEnumerator TailBreakBigHitRoutine()
    {
        DisableAllHitboxes();
        StopAllSpecialParticles();
        ResetAnimatorSpeedHard();

        isBusy = true;
        state = DragonState.Down;

        string animName = string.IsNullOrEmpty(tailBreakBigHitAnim) ? string.Empty : tailBreakBigHitAnim;
        PlayAnimationFromStart(animName);

        if (downParticle != null)
        {
            downParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            downParticle.Play();
        }

        PlayEffect(DragonEffectKind.Down, downSfx, null);

        yield return new WaitForSeconds(Mathf.Max(0.05f, tailBreakBigHitDuration));

        if (state == DragonState.Dead)
        {
            yield break;
        }

        ReturnFromDownToAI();
    }

    private IEnumerator DownRoutine()
    {
        DisableAllHitboxes();
        StopAllSpecialParticles();
        ResetAnimatorSpeedHard();

        isBusy = true;
        state = DragonState.Down;

        PlayAnimationFromStart(motion != null ? motion.downAnim : string.Empty);

        if (downParticle != null)
        {
            downParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            downParticle.Play();
        }

        PlayEffect(DragonEffectKind.Down, downSfx, null);

        yield return new WaitForSeconds(GetHalfHPDownDuration());

        if (state == DragonState.Dead)
        {
            yield break;
        }

        ReturnFromDownToAI();
    }

    private void ReturnFromDownToAI()
    {
        if (state == DragonState.Dead)
        {
            return;
        }

        DisableAllHitboxes();
        StopAllSpecialParticles();
        ResetAnimatorSpeedHard();

        state = DragonState.Idle;
        isBusy = false;
        downRoutineCoroutine = null;

        if (motion != null)
        {
            motion.PlayAnim(motion.idleAnim, true);
        }

        StartAILoopSafely();
    }

    private void HandleDeath()
    {
        StopAllCoroutines();

        DisableAllHitboxes();
        StopAllSpecialParticles();

        if (motion != null)
        {
            ResetAnimatorSpeedHard();
        }

        state = DragonState.Dead;
        isBusy = true;

        if (motion != null)
        {
            motion.PlayAnim(motion.deathAnim, true);
        }

        PlayEffect(DragonEffectKind.Death, deathSfx, deathParticle);
    }

    private void FindPlayerHPIfNeeded()
    {
        if (playerHP != null) return;

        if (player != null)
        {
            playerHP = player.GetComponent<PlayerHP>();

            if (playerHP == null)
            {
                playerHP = player.GetComponentInChildren<PlayerHP>();
            }

            if (playerHP == null)
            {
                playerHP = player.GetComponentInParent<PlayerHP>();
            }
        }

        if (playerHP == null)
        {
            playerHP = FindFirstObjectByType<PlayerHP>();
        }
    }

    private bool IsPlayerDead()
    {
        FindPlayerHPIfNeeded();

        if (playerHP == null) return false;

        return playerHP.IsDead;
    }

    private void EnterPlayerDeadIdle()
    {
        if (pausedBecausePlayerDead) return;
        if (state == DragonState.Dead) return;

        pausedBecausePlayerDead = true;

        StopAllCoroutines();
        aiLoopCoroutine = null;
        downRoutineCoroutine = null;

        DisableAllHitboxes();
        StopAllSpecialParticles();
        ResetAnimatorSpeedHard();

        state = DragonState.Idle;
        isBusy = true;
        skipNextActionInterval = false;

        if (motion != null)
        {
            motion.PlayAnim(motion.idleAnim, true);
        }

        Debug.Log("DragonAI: Player HP is 0. Stop attacks and wait in Idle.");
    }

    private void ResumeFromPlayerDeadIdle()
    {
        if (!pausedBecausePlayerDead) return;
        if (state == DragonState.Dead) return;

        pausedBecausePlayerDead = false;

        DisableAllHitboxes();
        StopAllSpecialParticles();
        ResetAnimatorSpeedHard();

        state = DragonState.Idle;
        isBusy = false;

        if (motion != null)
        {
            motion.PlayAnim(motion.idleAnim, true);
        }

        StartAILoopSafely();
        Debug.Log("DragonAI: Player revived. Resume AI.");
    }

    private bool IsTailBroken()
    {
        if (dragonHP == null) return false;

        // 尻尾切断後はTail Slam / Tail Swipe / BackStepTail系を完全に封印する。
        // 古いisTailCrystalBroken系と、新しいIsTailSevered系の両方に対応。
        return dragonHP.IsTailSevered()
            || dragonHP.IsCrystalBroken()
            || dragonHP.isTailCrystalBroken
            || dragonHP.isCrystalBroken;
    }

    private bool CanUseCharge()
    {
        if (!enableChargeAction) return false;
        if (Time.time < lastChargeTime + chargeCooldown) return false;

        return true;
    }



    private bool ShouldUseDifficultyChargeHoming()
    {
        return useExternalDifficultyMultipliers
            && useDifficultyChargeHoming
            && difficultyChargeHomingStrength > 0f
            && player != null
            && motion != null;
    }

    private Vector3 ApplyDifficultyChargeHoming(Vector3 currentDirection)
    {
        currentDirection.y = 0f;

        if (currentDirection.sqrMagnitude < 0.001f)
        {
            currentDirection = transform.forward;
            currentDirection.y = 0f;
        }

        if (currentDirection.sqrMagnitude < 0.001f)
        {
            currentDirection = Vector3.forward;
        }

        currentDirection.Normalize();

        if (!ShouldUseDifficultyChargeHoming())
        {
            return currentDirection;
        }

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.001f)
        {
            return currentDirection;
        }

        Vector3 desiredDirection = toPlayer.normalized;

        float maxRadiansDelta =
            Mathf.Max(0f, difficultyChargeHomingMaxTurnSpeed) * Mathf.Deg2Rad * Time.deltaTime;

        Vector3 limitedDirection = Vector3.RotateTowards(
            currentDirection,
            desiredDirection,
            maxRadiansDelta,
            0f
        ).normalized;

        float homingT = Mathf.Clamp01(difficultyChargeHomingStrength);
        Vector3 result = Vector3.Slerp(currentDirection, limitedDirection, homingT).normalized;

        if (result.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(result, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                Mathf.Max(0f, difficultyChargeHomingMaxTurnSpeed) * Time.deltaTime
            );
        }

        return result.sqrMagnitude > 0.001f ? result : currentDirection;
    }

    private bool ShouldUseDifficultyTailSlamPreHitDelay()
    {
        return useExternalDifficultyMultipliers
            && useDifficultyTailPreHitDelay
            && difficultyTailSlamPreHitDelay > 0f;
    }

    private bool ShouldUseDifficultyTailSwipeFirstPreHitDelay()
    {
        return useExternalDifficultyMultipliers
            && useDifficultyTailPreHitDelay
            && difficultyTailSwipeFirstPreHitDelay > 0f;
    }

    private float GetDifficultyTailSlamPreHitDelay()
    {
        return ShouldUseDifficultyTailSlamPreHitDelay()
            ? Mathf.Max(0f, difficultyTailSlamPreHitDelay)
            : 0f;
    }

    private float GetDifficultyTailSwipeFirstPreHitDelay()
    {
        return ShouldUseDifficultyTailSwipeFirstPreHitDelay()
            ? Mathf.Max(0f, difficultyTailSwipeFirstPreHitDelay)
            : 0f;
    }

    private IEnumerator WaitDifficultyTailPreHitDelay(
        float delay,
        float tailRotationOffsetY,
        float fallbackRotationOffsetY
    )
    {
        delay = Mathf.Max(0f, delay);

        if (delay <= 0f)
        {
            yield break;
        }

        float originalAnimatorSpeed = dragonAnimator != null ? dragonAnimator.speed : 1f;

        if (pauseAnimatorDuringDifficultyTailDelay && dragonAnimator != null)
        {
            dragonAnimator.speed = 0f;
        }

        float timer = 0f;

        while (timer < delay)
        {
            if (state == DragonState.Dead || state == DragonState.Down || IsTailBroken())
            {
                break;
            }

            timer += Time.deltaTime;

            if (motion != null && player != null)
            {
                Quaternion targetRotation;

                if (tailAttacksTurnTailToPlayer)
                {
                    targetRotation = motion.GetTailRotationToPlayer(tailFacePlayerOffsetY, tailRotationOffsetY);
                }
                else
                {
                    targetRotation = motion.GetRotationToPlayerWithOffset(fallbackRotationOffsetY);
                }

                float turnSpeed = Mathf.Max(0f, difficultyTailPreHitTurnSpeed);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );
            }

            yield return null;
        }

        if (pauseAnimatorDuringDifficultyTailDelay && dragonAnimator != null)
        {
            dragonAnimator.speed = originalAnimatorSpeed;
        }
    }

    public void SetDifficultyHardOnlyExtraTuning(
        bool enableChargeHoming,
        float chargeHomingStrength,
        float chargeHomingMaxTurnSpeed,
        bool enableTailPreHitDelay,
        float tailSlamPreHitDelay,
        float tailSwipeFirstPreHitDelay,
        float tailPreHitTurnSpeed,
        bool pauseAnimatorDuringTailDelay
    )
    {
        useDifficultyChargeHoming = enableChargeHoming;
        difficultyChargeHomingStrength = Mathf.Clamp01(chargeHomingStrength);
        difficultyChargeHomingMaxTurnSpeed = Mathf.Max(0f, chargeHomingMaxTurnSpeed);

        useDifficultyTailPreHitDelay = enableTailPreHitDelay;
        difficultyTailSlamPreHitDelay = Mathf.Max(0f, tailSlamPreHitDelay);
        difficultyTailSwipeFirstPreHitDelay = Mathf.Max(0f, tailSwipeFirstPreHitDelay);
        difficultyTailPreHitTurnSpeed = Mathf.Max(0f, tailPreHitTurnSpeed);
        pauseAnimatorDuringDifficultyTailDelay = pauseAnimatorDuringTailDelay;
    }

    public void SetDifficultySpecialAttackTiming(
        float attackStartDelay,
        bool overrideChargeTellTime,
        float chargeTellTimeOverride,
        bool overrideChargeReadyPause,
        float chargeReadyPauseOverride
    )
    {
        difficultyAttackStartDelay = Mathf.Max(0f, attackStartDelay);
        useDifficultyChargeTellTimeOverride = overrideChargeTellTime;
        difficultyChargeTellTimeOverride = Mathf.Max(0f, chargeTellTimeOverride);
        useDifficultyChargeReadyPauseOverride = overrideChargeReadyPause;
        difficultyChargeReadyPauseOverride = Mathf.Max(0f, chargeReadyPauseOverride);
    }

    public void SetDifficultyMultipliers(float actionIntervalMultiplier, float moveSpeedMultiplier, float animationSpeedMultiplier)
    {
        if (!useExternalDifficultyMultipliers)
        {
            return;
        }

        difficultyActionIntervalMultiplier = Mathf.Max(0.05f, actionIntervalMultiplier);
        difficultyMoveSpeedMultiplier = Mathf.Max(0.05f, moveSpeedMultiplier);
        difficultyAnimationSpeedMultiplier = Mathf.Max(0.05f, animationSpeedMultiplier);

        ResetAnimatorSpeedHard();
    }

    public void SetDifficultyHalfHPDownDuration(float duration)
    {
        halfHPDownDuration = Mathf.Max(0.05f, duration);
    }

    public void SetTailBreakBigHitSettings(bool useBigHit, string bigHitAnimName, float duration)
    {
        useBigHitOnTailBreak = useBigHit;

        if (!string.IsNullOrEmpty(bigHitAnimName))
        {
            tailBreakBigHitAnim = bigHitAnimName;
        }

        tailBreakBigHitDuration = Mathf.Max(0.05f, duration);
    }

    public void SetDifficultyActionProfile(float actionSpeedMultiplier)
    {
        actionSpeedMultiplier = Mathf.Max(0.05f, actionSpeedMultiplier);
        SetDifficultyMultipliers(
            1f / actionSpeedMultiplier,
            actionSpeedMultiplier,
            actionSpeedMultiplier
        );
    }


    private float GetChargeTellDuration()
    {
        float baseTellTime = useDifficultyChargeTellTimeOverride ? difficultyChargeTellTimeOverride : chargeTellTime;
        float safeTime = Mathf.Max(0f, baseTellTime);

        if (!scaleChargeTellTimeWithAnimationSpeed)
        {
            return safeTime;
        }

        // chargeTellAnimationSpeed は「溜め姿勢の見た目用スロー倍率」。
        // ここで割ると 1.0 / 0.18 = 5.5秒 になってしまうため、
        // 突進の予兆時間には使わない。難易度のアニメ倍率だけを見る。
        float difficultySpeed = Mathf.Max(0.05f, GetDifficultyAnimationSpeedMultiplier());
        return safeTime / difficultySpeed;
    }

    private WaitForSeconds WaitForChargeReadyPause()
    {
        float baseReadyPause = useDifficultyChargeReadyPauseOverride ? difficultyChargeReadyPauseOverride : chargeReadyPauseTime;
        float safeTime = Mathf.Max(0f, baseReadyPause);

        if (scaleChargeReadyPauseWithAnimationSpeed)
        {
            float difficultySpeed = Mathf.Max(0.05f, GetDifficultyAnimationSpeedMultiplier());
            safeTime /= difficultySpeed;
        }

        return new WaitForSeconds(safeTime);
    }

    private float GetHalfHPDownDuration()
    {
        if (halfHPDownDuration > 0f)
        {
            return halfHPDownDuration;
        }

        return Mathf.Max(0.05f, downDuration);
    }

    private float GetTimingAnimationMultiplier(float baseAnimatorSpeed = 1f)
    {
        float baseSpeed = Mathf.Max(0.05f, baseAnimatorSpeed);

        if (!scaleHitboxAndEffectTimingWithAnimationSpeed)
        {
            return baseSpeed;
        }

        return baseSpeed * GetDifficultyAnimationSpeedMultiplier();
    }

    private float ScaleAnimDuration(float animationSeconds, float baseAnimatorSpeed = 1f)
    {
        return Mathf.Max(0f, animationSeconds) / GetTimingAnimationMultiplier(baseAnimatorSpeed);
    }

    private float ScaleAnimFrameTime(float animationSeconds, float baseAnimatorSpeed = 1f)
    {
        return ScaleAnimDuration(animationSeconds, baseAnimatorSpeed);
    }

    private WaitForSeconds WaitForAnimSeconds(float animationSeconds, float baseAnimatorSpeed = 1f)
    {
        return new WaitForSeconds(ScaleAnimDuration(animationSeconds, baseAnimatorSpeed));
    }

    private float ApplyDifficultyActionInterval(float interval)
    {
        if (!useExternalDifficultyMultipliers) return interval;
        return interval * Mathf.Max(0.05f, difficultyActionIntervalMultiplier);
    }

    private float ApplyDifficultyMoveSpeed(float speed)
    {
        if (!useExternalDifficultyMultipliers) return speed;
        return speed * Mathf.Max(0.05f, difficultyMoveSpeedMultiplier);
    }

    private float GetDifficultyAnimationSpeedMultiplier()
    {
        if (!useExternalDifficultyMultipliers) return 1f;
        return Mathf.Max(0.05f, difficultyAnimationSpeedMultiplier);
    }

    private void SetAnimatorSpeedScaled(float baseSpeed)
    {
        float scaledSpeed = Mathf.Max(0f, baseSpeed) * GetDifficultyAnimationSpeedMultiplier();

        if (motion != null)
        {
            motion.SetAnimatorSpeed(scaledSpeed);
        }

        if (dragonAnimator != null)
        {
            dragonAnimator.speed = scaledSpeed;
        }
    }

    private void ResetAnimatorSpeedHard()
    {
        float speed = GetDifficultyAnimationSpeedMultiplier();

        if (motion != null)
        {
            motion.ResetAnimatorSpeed();

            if (!Mathf.Approximately(speed, 1f))
            {
                motion.SetAnimatorSpeed(speed);
            }
        }

        if (dragonAnimator != null)
        {
            dragonAnimator.speed = speed;
        }
    }

    private void PlayMoveAnimSafe(string animName)
    {
        ReplayMoveAnimation(animName, true);
    }

    private void KeepMoveAnimSafe(string animName, ref float keepTimer, ref float safetyTimer)
    {
        if (motion == null || string.IsNullOrEmpty(animName))
        {
            return;
        }

        if (forceAnimatorSpeedOneWhenMoving)
        {
            ResetAnimatorSpeedHard();
        }

        // 既存の保持処理。現在状態がWalk/Runから外れた時の復帰用。
        motion.KeepMoveAnim(animName, ref keepTimer);

        if (!forceMoveAnimationWhileMoving)
        {
            return;
        }

        safetyTimer += Time.deltaTime;

        if (safetyTimer < Mathf.Max(0.05f, moveAnimationSafetyCheckInterval))
        {
            return;
        }

        safetyTimer = 0f;
        ForceMoveAnimationIfNeeded(animName);
    }

    private void StartFrameDrivenMoveAnimation(string animName, ref float playbackTimer)
    {
        playbackTimer = 0f;

        if (!useFrameDrivenMoveAnimation)
        {
            return;
        }

        ApplyFrameDrivenMoveAnimation(animName, 0f);
    }

    private void DriveMoveAnimationByFrameTimer(string animName, ref float playbackTimer)
    {
        if (!useFrameDrivenMoveAnimation) return;
        if (dragonAnimator == null) return;
        if (string.IsNullOrEmpty(animName)) return;

        ResetAnimatorSpeedHard();

        float fps = Mathf.Max(1f, moveAnimationLoopFPS > 0f ? moveAnimationLoopFPS : (motion != null ? motion.animationFPS : 30f));
        int frameCount = Mathf.Max(2, moveAnimationFrameCount);
        float loopDuration = frameCount / fps;

        playbackTimer += Time.deltaTime * Mathf.Max(0.01f, moveAnimationManualSpeedMultiplier);

        if (playbackTimer >= loopDuration)
        {
            playbackTimer %= loopDuration;
        }

        float normalizedTime = Mathf.Clamp01(playbackTimer / Mathf.Max(0.01f, loopDuration));
        ApplyFrameDrivenMoveAnimation(animName, normalizedTime);
    }

    private void ApplyFrameDrivenMoveAnimation(string animName, float normalizedTime)
    {
        if (dragonAnimator == null) return;
        if (string.IsNullOrEmpty(animName)) return;

        // CrossFadeを繰り返すと0.5秒ほど無姿勢/停止に見えることがあるので、
        // 移動中はPlayで再生位置を直接指定して疑似ループさせる。
        dragonAnimator.speed = GetDifficultyAnimationSpeedMultiplier();
        dragonAnimator.Play(animName, 0, normalizedTime);
        dragonAnimator.Update(0f);
    }

    private void KeepMoveAnimationAliveIfNeeded(string animName, ref float loopTimer, bool isActuallyMoving)
    {
        if (!keepMoveAnimationAliveWhileMoving) return;
        if (!isActuallyMoving) return;
        if (string.IsNullOrEmpty(animName)) return;

        loopTimer += Time.deltaTime;

        if (forceAnimatorSpeedOneWhenMoving)
        {
            ResetAnimatorSpeedHard();
        }

        if (dragonAnimator == null)
        {
            if (loopTimer >= Mathf.Max(0.2f, moveAnimationHardRefreshInterval))
            {
                ReplayMoveAnimation(animName, true);
                loopTimer = 0f;
            }

            return;
        }

        AnimatorStateInfo current = dragonAnimator.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo next = dragonAnimator.GetNextAnimatorStateInfo(0);

        bool inTransition = dragonAnimator.IsInTransition(0);
        bool currentIsMoveAnim = current.IsName(animName);
        bool nextIsMoveAnim = inTransition && next.IsName(animName);

        if (!currentIsMoveAnim && !nextIsMoveAnim)
        {
            ReplayMoveAnimation(animName, true);
            loopTimer = 0f;
            return;
        }

        // 非ループアニメが終端に近づいたら、止まる前に0秒から再再生する。
        // Walk/RunクリップのLoop TimeがOFFでも、移動中は疑似ループできる。
        if (currentIsMoveAnim && !current.loop && current.normalizedTime >= moveAnimationRestartNormalizedTimeWhileMoving)
        {
            ReplayMoveAnimation(animName, true);
            loopTimer = 0f;
            return;
        }

        // クリップ名判定が取れていても3秒前後で止まる環境向けの保険。
        if (loopTimer >= Mathf.Max(0.2f, moveAnimationHardRefreshInterval))
        {
            ReplayMoveAnimation(animName, true);
            loopTimer = 0f;
        }
    }

    private void ForceMoveAnimationIfNeeded(string animName)
    {
        if (!forceMoveAnimationWhileMoving) return;
        if (motion == null || string.IsNullOrEmpty(animName)) return;

        if (dragonAnimator == null)
        {
            motion.PlayAnim(animName, true);
            return;
        }

        if (forceAnimatorSpeedOneWhenMoving && dragonAnimator.speed <= 0.01f)
        {
            dragonAnimator.speed = GetDifficultyAnimationSpeedMultiplier();
        }

        AnimatorStateInfo current = dragonAnimator.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo next = dragonAnimator.GetNextAnimatorStateInfo(0);

        bool currentIsMoveAnim = current.IsName(animName);
        bool nextIsMoveAnim = dragonAnimator.IsInTransition(0) && next.IsName(animName);

        if (!currentIsMoveAnim && !nextIsMoveAnim)
        {
            ReplayMoveAnimation(animName, true);
        }
    }

    private void ReplayMoveAnimation(string animName, bool resetToStart)
    {
        if (motion == null || string.IsNullOrEmpty(animName))
        {
            return;
        }

        ResetAnimatorSpeedHard();

        if (useDirectAnimatorReplayForMoveAnimation && dragonAnimator != null)
        {
            float normalizedTime = resetToStart ? 0f : float.NegativeInfinity;
            float fade = Mathf.Max(0f, directMoveAnimCrossFadeTime);

            dragonAnimator.CrossFadeInFixedTime(animName, fade, 0, normalizedTime);
            dragonAnimator.Update(0f);

            // DragonDragonMotion側の現在アニメ名も合わせるために呼ぶ。
            // force=trueなので、内部のcurrentAnimNameが別名でも確実に更新される。
            motion.PlayAnim(animName, true);
            return;
        }

        motion.PlayAnim(animName, true);

        if (dragonAnimator != null)
        {
            dragonAnimator.Update(0f);
        }
    }

    private void ReturnToIdle()
    {
        if (state == DragonState.Dead)
        {
            return;
        }

        if (stopActionsWhenPlayerDead && IsPlayerDead())
        {
            EnterPlayerDeadIdle();
            return;
        }

        DisableAllHitboxes();
        StopAllSpecialParticles();
        ResetAnimatorSpeedHard();

        state = DragonState.Idle;
        isBusy = false;

        if (motion != null)
        {
            motion.PlayAnim(motion.idleAnim, true);
        }
    }

    private void DisableAllHitboxes()
    {
        if (chargeHitbox != null) chargeHitbox.DisableHitbox();
        if (leftArmHitbox != null) leftArmHitbox.DisableHitbox();
        if (rightArmHitbox != null) rightArmHitbox.DisableHitbox();
        if (tailHitbox != null) tailHitbox.DisableHitbox();
        if (tailSwipeSpinDashHitbox != null) tailSwipeSpinDashHitbox.DisableHitbox();
        if (wideBreathHitbox != null) wideBreathHitbox.DisableHitbox();
        if (beamBreathHitbox != null) beamBreathHitbox.DisableHitbox();
    }

    private void StopAllSpecialParticles()
    {
        StopAllChargeParticles();
        StopAllBreathParticles();
        StopTailSwipeSpecialParticles();
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

    private void StopTailSwipeSpecialParticles()
    {
        if (tailSwipeSecondTellParticle != null)
        {
            tailSwipeSecondTellParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (tailSwipeSpinDashParticle != null)
        {
            tailSwipeSpinDashParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void DoRoarEffect()
    {
        PlayEffect(DragonEffectKind.Roar, roarSfx, roarParticle);

        Collider[] hits = Physics.OverlapSphere(transform.position, roarStaggerRadius, playerLayer);

        foreach (Collider hit in hits)
        {
            hit.SendMessage("DragonStagger", roarStaggerTime, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void PlayEffect(DragonEffectKind kind, AudioClip overrideClip, ParticleSystem overrideParticle)
    {
        ParticleSystem particle = overrideParticle != null ? overrideParticle : GetCommonParticle(kind);
        if (particle != null)
        {
            PrepareParticleForDifficultyPlay(particle);

            if (restartParticleBeforePlay)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            particle.Play(true);
        }

        AudioClip clip = overrideClip != null ? overrideClip : GetCommonClip(kind);
        PlaySfx(clip);
    }

    private void PrepareParticleForDifficultyPlay(ParticleSystem particle)
    {
        if (particle == null) return;
        if (!forcePlayedParticlesToLocalSimulation) return;

        ParticleSystem[] systems = particle.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in systems)
        {
            if (ps == null) continue;

            ParticleSystem.MainModule main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
        }
    }

    private AudioClip GetCommonClip(DragonEffectKind kind)
    {
        if (effectPlayer == null) return null;

        switch (kind)
        {
            case DragonEffectKind.Roar: return effectPlayer.roarSfx;
            case DragonEffectKind.Down: return effectPlayer.downSfx;
            case DragonEffectKind.Death: return effectPlayer.deathSfx;
            case DragonEffectKind.Step: return effectPlayer.stepSfx;
            case DragonEffectKind.Swipe: return effectPlayer.swipeSfx;
            case DragonEffectKind.TailSlam: return effectPlayer.tailSlamSfx;
            case DragonEffectKind.TailSwipe: return effectPlayer.tailSwipeSfx;
            case DragonEffectKind.BreathCharge: return effectPlayer.breathChargeSfx;
            case DragonEffectKind.BreathFire: return effectPlayer.breathFireSfx;
            case DragonEffectKind.ChargeStart: return effectPlayer.chargeStartSfx;
            case DragonEffectKind.ChargeRun: return effectPlayer.chargeRunSfx;
            case DragonEffectKind.ChargeEnd: return effectPlayer.chargeEndSfx;
            default: return null;
        }
    }

    private ParticleSystem GetCommonParticle(DragonEffectKind kind)
    {
        if (effectPlayer == null) return null;

        switch (kind)
        {
            case DragonEffectKind.Roar: return effectPlayer.roarParticle;
            case DragonEffectKind.Down: return effectPlayer.downParticle;
            case DragonEffectKind.Death: return effectPlayer.deathParticle;
            case DragonEffectKind.Step: return effectPlayer.stepParticle;
            case DragonEffectKind.Swipe: return effectPlayer.swipeParticle;
            case DragonEffectKind.TailSlam: return effectPlayer.tailSlamParticle;
            case DragonEffectKind.TailSwipe: return effectPlayer.tailSwipeParticle;
            case DragonEffectKind.BreathCharge: return effectPlayer.breathChargeParticle;
            case DragonEffectKind.BreathFire: return effectPlayer.breathFireParticle;
            case DragonEffectKind.ChargeStart: return effectPlayer.chargeStartParticle;
            case DragonEffectKind.ChargeRun: return effectPlayer.chargeRunParticle;
            case DragonEffectKind.ChargeEnd: return effectPlayer.chargeEndParticle;
            default: return null;
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;

        if (effectPlayer != null && effectPlayer.TryPlayCustomSfx(clip))
        {
            return;
        }

        FindAudioSourceIfNeeded();

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip, sfxVolume);
        }
        else
        {
            Debug.LogWarning("[DragonAI] AudioSourceが見つからないのでSEを再生できません。", this);
        }
    }

    private void FindAudioSourceIfNeeded()
    {
        if (audioSource != null) return;

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = GetComponentInChildren<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponentInParent<AudioSource>();
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
