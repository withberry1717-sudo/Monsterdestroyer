using UnityEngine;
using NaughtyCharacter;
using System.Collections;

namespace Retro.ThirdPersonCharacter
{
    [RequireComponent(typeof(PlayerInput), typeof(Animator), typeof(Movement))]
    public class Combat : MonoBehaviour
    {
        private enum ComboStage
        {
            None,
            Light1,
            Light2,

            Heavy1,
            Heavy2,
            HeavyLight,

            ChargedHeavyDone,
            ChargedLight1,
            ChargedLight2,
            ChargedHeavy1,
            ChargedHeavyLight
        }

        private enum AttackEffectType
        {
            None,
            Light,
            Heavy,
            ChargedHeavy,
            ComboFinisher,
            WeakWeakHeavyFinisher,
            HeavyWeakHeavyFinisher,
            ChargedWeakWeakHeavyFinisher,
            ChargedHeavyWeakHeavyFinisher,
            Dash
        }

        [Header("Animator State Names")]
        [SerializeField] private string attackStateName = "RFA_Attack";
        [SerializeField] private string abilityStateName = "RFA_Ability";

        private Animator _animator;
        private PlayerInput _playerInput;
        private Movement _movement;

        public bool AttackInProgress { get; private set; } = false;

        [Header("Hitboxes")]
        [SerializeField] private WeaponHitbox daggerHitbox;
        [SerializeField] private WeaponHitbox swordHitbox;

        [Header("Charge Effects")]
        [SerializeField] private ParticleSystem chargeEffect;
        [SerializeField] private ParticleSystem chargeReadyEffect;
        [SerializeField] private Vector3 chargeEffectLocalPosition = new Vector3(0f, 1.2f, 0.3f);

        private bool chargeReadyEffectPlayed = false;
        private bool maxChargeEffectPlayed = false;

        [Header("Attack Audio")]
        [Tooltip("攻撃SE用AudioSource。空なら自分/子/親から自動取得します。")]
        [SerializeField] private AudioSource attackAudioSource;

        [Range(0f, 1f)]
        [SerializeField] private float attackSfxVolume = 1f;

        [Header("Light Attack Particle / SFX")]
        [Tooltip("弱攻撃の判定が出る瞬間に出すパーティクルPrefab")]
        [SerializeField] private ParticleSystem lightAttackParticlePrefab;

        [Tooltip("弱攻撃の判定が出る瞬間に鳴らすSE")]
        [SerializeField] private AudioClip lightAttackSfx;

        [Tooltip("弱攻撃パーティクルの位置補正。x=左右, y=高さ, z=正面距離")]
        [SerializeField] private Vector3 lightAttackParticleOffset = new Vector3(0f, 1.0f, 1.0f);

        [Tooltip("弱攻撃パーティクルの角度補正。正面がズレる時にYなどを調整")]
        [SerializeField] private Vector3 lightAttackParticleEulerOffset = Vector3.zero;

        [Header("Heavy Attack Particle / SFX")]
        [Tooltip("通常強攻撃の判定が出る瞬間に出すパーティクルPrefab")]
        [SerializeField] private ParticleSystem heavyAttackParticlePrefab;

        [Tooltip("通常強攻撃の判定が出る瞬間に鳴らすSE")]
        [SerializeField] private AudioClip heavyAttackSfx;

        [Tooltip("通常強攻撃パーティクルの位置補正。x=左右, y=高さ, z=正面距離")]
        [SerializeField] private Vector3 heavyAttackParticleOffset = new Vector3(0f, 1.05f, 1.15f);

        [Tooltip("通常強攻撃パーティクルの角度補正。正面がズレる時にYなどを調整")]
        [SerializeField] private Vector3 heavyAttackParticleEulerOffset = Vector3.zero;

        [Header("Charged Heavy Particle / SFX")]
        [Tooltip("溜め強攻撃の判定が出る瞬間に出すパーティクルPrefab")]
        [SerializeField] private ParticleSystem chargedHeavyParticlePrefab;

        [Tooltip("溜め強攻撃の判定が出る瞬間に鳴らすSE")]
        [SerializeField] private AudioClip chargedHeavySfx;

        [Tooltip("溜め強攻撃パーティクルの位置補正。x=左右, y=高さ, z=正面距離")]
        [SerializeField] private Vector3 chargedHeavyParticleOffset = new Vector3(0f, 1.1f, 1.25f);

        [Tooltip("溜め強攻撃パーティクルの角度補正。正面がズレる時にYなどを調整")]
        [SerializeField] private Vector3 chargedHeavyParticleEulerOffset = Vector3.zero;

        [Header("Dash Attack Particle / SFX")]
        [Tooltip("ダッシュ攻撃の判定が出る瞬間に出すパーティクルPrefab。空なら弱攻撃のものを使います。")]
        [SerializeField] private ParticleSystem dashAttackParticlePrefab;

        [Tooltip("ダッシュ攻撃の判定が出る瞬間に鳴らすSE。空なら弱攻撃SEを使います。")]
        [SerializeField] private AudioClip dashAttackSfx;

        [Tooltip("ダッシュ攻撃パーティクルの位置補正。x=左右, y=高さ, z=正面距離")]
        [SerializeField] private Vector3 dashAttackParticleOffset = new Vector3(0f, 1.0f, 1.05f);

        [Tooltip("ダッシュ攻撃パーティクルの角度補正。正面がズレる時にYなどを調整")]
        [SerializeField] private Vector3 dashAttackParticleEulerOffset = Vector3.zero;

        [Header("Combo Finisher Particle / SFX")]
        [Tooltip("弱弱強 / 強弱強 / 溜め強弱弱強 / 溜め強強弱強 の最後の強で出すパーティクル")]
        [SerializeField] private ParticleSystem comboFinisherParticlePrefab;

        [Tooltip("コンボ締めの判定が出る瞬間に鳴らすSE。空なら強攻撃SEを使います。")]
        [SerializeField] private AudioClip comboFinisherSfx;

        [Tooltip("パーティクルを出す位置。空ならプレイヤーの前に出す。設定しても回転はプレイヤー正面基準になります。")]
        [SerializeField] private Transform comboFinisherParticlePoint;

        [Tooltip("パーティクル生成位置の補正。x=左右, y=高さ, z=正面距離")]
        [SerializeField] private Vector3 comboFinisherParticleOffset = new Vector3(0f, 1.0f, 1.0f);

        [Tooltip("コンボ締めパーティクルの角度補正。正面がズレる時にYなどを調整")]
        [SerializeField] private Vector3 comboFinisherParticleEulerOffset = Vector3.zero;

        [Header("Individual Combo Finisher VFX / SFX")]
        [Tooltip("弱弱強の最後の強で出すVFX。空なら共通Combo Finisher、さらに空なら通常強攻撃VFXを使います。")]
        [SerializeField] private ParticleSystem weakWeakHeavyFinisherParticlePrefab;

        [Tooltip("弱弱強の最後の強で鳴らすSE。空なら通常強攻撃SEを使います。")]
        [SerializeField] private AudioClip weakWeakHeavyFinisherSfx;

        [Tooltip("弱弱強フィニッシャーVFXの位置補正。x=左右, y=高さ, z=正面距離")]
        [SerializeField] private Vector3 weakWeakHeavyFinisherParticleOffset = new Vector3(0f, 1.0f, 1.0f);

        [Tooltip("弱弱強フィニッシャーVFXの角度補正。正面がズレる時にYなどを調整")]
        [SerializeField] private Vector3 weakWeakHeavyFinisherParticleEulerOffset = Vector3.zero;

        [Tooltip("強弱強の最後の強で出すVFX。空なら共通Combo Finisher、さらに空なら通常強攻撃VFXを使います。")]
        [SerializeField] private ParticleSystem heavyWeakHeavyFinisherParticlePrefab;

        [Tooltip("強弱強の最後の強で鳴らすSE。空なら通常強攻撃SEを使います。")]
        [SerializeField] private AudioClip heavyWeakHeavyFinisherSfx;

        [Tooltip("強弱強フィニッシャーVFXの位置補正。x=左右, y=高さ, z=正面距離")]
        [SerializeField] private Vector3 heavyWeakHeavyFinisherParticleOffset = new Vector3(0f, 1.0f, 1.0f);

        [Tooltip("強弱強フィニッシャーVFXの角度補正。正面がズレる時にYなどを調整")]
        [SerializeField] private Vector3 heavyWeakHeavyFinisherParticleEulerOffset = Vector3.zero;

        [Tooltip("溜め強→弱弱強の最後の強で出すVFX。空なら共通Combo Finisher、さらに空なら通常強攻撃VFXを使います。")]
        [SerializeField] private ParticleSystem chargedWeakWeakHeavyFinisherParticlePrefab;

        [Tooltip("溜め強→弱弱強の最後の強で鳴らすSE。空なら通常強攻撃SEを使います。")]
        [SerializeField] private AudioClip chargedWeakWeakHeavyFinisherSfx;

        [Tooltip("溜め強→弱弱強フィニッシャーVFXの位置補正。x=左右, y=高さ, z=正面距離")]
        [SerializeField] private Vector3 chargedWeakWeakHeavyFinisherParticleOffset = new Vector3(0f, 1.0f, 1.0f);

        [Tooltip("溜め強→弱弱強フィニッシャーVFXの角度補正。正面がズレる時にYなどを調整")]
        [SerializeField] private Vector3 chargedWeakWeakHeavyFinisherParticleEulerOffset = Vector3.zero;

        [Tooltip("溜め強→強弱強の最後の強で出すVFX。空なら共通Combo Finisher、さらに空なら通常強攻撃VFXを使います。")]
        [SerializeField] private ParticleSystem chargedHeavyWeakHeavyFinisherParticlePrefab;

        [Tooltip("溜め強→強弱強の最後の強で鳴らすSE。空なら通常強攻撃SEを使います。")]
        [SerializeField] private AudioClip chargedHeavyWeakHeavyFinisherSfx;

        [Tooltip("溜め強→強弱強フィニッシャーVFXの位置補正。x=左右, y=高さ, z=正面距離")]
        [SerializeField] private Vector3 chargedHeavyWeakHeavyFinisherParticleOffset = new Vector3(0f, 1.0f, 1.0f);

        [Tooltip("溜め強→強弱強フィニッシャーVFXの角度補正。正面がズレる時にYなどを調整")]
        [SerializeField] private Vector3 chargedHeavyWeakHeavyFinisherParticleEulerOffset = Vector3.zero;

        [Header("Individual Combo Finisher Hitbox Size")]
        [Tooltip("弱弱強フィニッシャー中だけ、剣の当たり判定Transformを何倍にするか。1なら通常サイズ。")]
        [SerializeField] private float weakWeakHeavyFinisherHitboxScale = 1.25f;

        [Tooltip("強弱強フィニッシャー中だけ、剣の当たり判定Transformを何倍にするか。1なら通常サイズ。")]
        [SerializeField] private float heavyWeakHeavyFinisherHitboxScale = 1.25f;

        [Tooltip("溜め強→弱弱強フィニッシャー中だけ、剣の当たり判定Transformを何倍にするか。1なら通常サイズ。")]
        [SerializeField] private float chargedWeakWeakHeavyFinisherHitboxScale = 1.35f;

        [Tooltip("溜め強→強弱強フィニッシャー中だけ、剣の当たり判定Transformを何倍にするか。1なら通常サイズ。")]
        [SerializeField] private float chargedHeavyWeakHeavyFinisherHitboxScale = 1.35f;

        [Tooltip("ON推奨。フィニッシャー判定終了後、HitboxのScaleを元に戻します。")]
        [SerializeField] private bool restoreFinisherHitboxScaleAfterHit = true;

        [Tooltip("生成したパーティクルを何秒後に消すか")]
        [SerializeField] private float comboFinisherParticleDestroyTime = 2.0f;

        [Tooltip("ONならフィニッシャーの判定が出る瞬間にパーティクルを出す")]
        [SerializeField] private bool playFinisherParticleOnHitStart = true;


        [Header("Attack Particle Safety")]
        [Tooltip("ON推奨。一回の攻撃につきパーティクル/SEを一回だけ再生します。多重発生防止用です。")]
        [SerializeField] private bool playAttackEffectOnlyOncePerAttack = true;

        [Tooltip("ON推奨。Prefab側がLoopになっていても、生成した攻撃パーティクルは単発化します。めっちゃ出続ける時の対策です。")]
        [SerializeField] private bool forceAttackParticleLoopOff = true;

        [Tooltip("通常攻撃系パーティクルを何秒後に消すか。長いと重なって多く見えます。0.8〜1.5推奨。")]
        [SerializeField] private float normalAttackParticleDestroyTime = 1.2f;

        [Header("Attack Forward Movement")]
        [SerializeField] private bool useAttackForwardMove = true;

        [SerializeField] private float light1ForwardDistance = 0.35f;
        [SerializeField] private float light2ForwardDistance = 0.45f;
        [SerializeField] private float heavyComboLightForwardDistance = 0.42f;
        [SerializeField] private float dashAttackForwardDistance = 0.35f;
        [SerializeField] private float quickHeavyForwardDistance = 0.55f;
        [SerializeField] private float quickHeavySecondForwardDistance = 0.60f;
        [SerializeField] private float chargedHeavyForwardDistance = 0.85f;
        [SerializeField] private float comboHeavyFinisherForwardDistance = 0.85f;

        [SerializeField] private float attackForwardDuration = 0.22f;
        [SerializeField] private float attackForwardAccelerationTime = 0.05f;
        [SerializeField] private float attackForwardDecelerationTime = 0.10f;

        [Header("Light Attack")]
        [SerializeField] private float light1Start = 0.12f;
        [SerializeField] private float light1End = 0.36f;
        [SerializeField] private float light1DamageMultiplier = 1.0f;

        [SerializeField] private float light2Start = 0.10f;
        [SerializeField] private float light2End = 0.38f;
        [SerializeField] private float light2DamageMultiplier = 1.0f;

        [Tooltip("強弱強の弱部分。基本倍率は上げない")]
        [SerializeField] private float heavyComboLightDamageMultiplier = 1.0f;

        [Header("Cooldown")]
        [SerializeField] private float comboFinishCooldown = 0.32f;

        [Tooltip("強攻撃単押し2連後やコンボ締め後の強攻撃クールタイム")]
        [SerializeField] private float neutralHeavyCooldown = 0.22f;

        [Header("Dash Attack")]
        [SerializeField] private float dashAttackStart = 0.08f;
        [SerializeField] private float dashAttackEnd = 0.34f;
        [SerializeField] private float dashAttackDamageMultiplier = 1.0f;

        [Header("Quick Heavy / 右クリック単押し")]
        [SerializeField] private float heavyTapTime = 0.18f;
        [SerializeField] private float heavyHoldToStartChargeTime = 0.22f;

        [SerializeField] private float quickHeavyAnimationStartNormalized = 0.28f;
        [SerializeField] private float quickHeavySecondAnimationStartNormalized = 0.23f;

        [SerializeField] private float quickHeavyAnimationSpeed = 1.35f;
        [SerializeField] private float quickHeavySecondAnimationSpeed = 1.4f;

        [SerializeField] private float quickHeavyStart = 0.08f;
        [SerializeField] private float quickHeavyEnd = 0.40f;

        [SerializeField] private float quickHeavySecondStart = 0.07f;
        [SerializeField] private float quickHeavySecondEnd = 0.42f;

        [Tooltip("単発強1発目。連打可能にする代わりに低め")]
        [SerializeField] private float quickHeavyDamageMultiplier = 0.85f;

        [Tooltip("単発強2発目。少しだけ上げるが、コンボ締めより弱い")]
        [SerializeField] private float quickHeavySecondDamageMultiplier = 0.90f;

        [Tooltip("単発強1発目の全体硬直。アニメが終わる前に動ける場合はここを上げる")]
        [SerializeField] private float quickHeavyEndDelay = 0.90f;

        [Tooltip("単発強2発目の全体硬直。アニメが終わる前に動ける場合はここを上げる")]
        [SerializeField] private float quickHeavySecondEndDelay = 0.95f;

        [Header("Heavy Movement Unlock")]
        [SerializeField] private float quickHeavyMoveUnlockTime = 0.72f;
        [SerializeField] private float quickHeavySecondMoveUnlockTime = 0.76f;
        [SerializeField] private float heavyFinisherMoveUnlockTime = 0.78f;
        [SerializeField] private float chargedHeavyMoveUnlockTime = 0.76f;

        [Header("Heavy Combo Finisher")]
        [SerializeField] private float heavyFinisherAnimationStartNormalized = 0.18f;
        [SerializeField] private float heavyFinisherAnimationSpeed = 1.25f;
        [SerializeField] private float heavyFinisherStart = 0.08f;
        [SerializeField] private float heavyFinisherEnd = 0.46f;
        [SerializeField] private float heavyFinisherEndDelay = 0.95f;

        [Tooltip("弱弱強の締め")]
        [SerializeField] private float weakWeakHeavyFinisherMultiplier = 1.20f;

        [Tooltip("強弱強の締め。強弱強は当てやすいので少し抑える")]
        [SerializeField] private float heavyWeakHeavyFinisherMultiplier = 1.15f;

        [Tooltip("溜め強→弱弱強の締め")]
        [SerializeField] private float chargedWeakWeakHeavyFinisherMultiplier = 1.25f;

        [Tooltip("溜め強→強弱強の締め。強弱強ルートなので少し抑えめ")]
        [SerializeField] private float chargedHeavyWeakHeavyFinisherMultiplier = 1.20f;

        [Header("Charge Heavy / 右クリック長押し")]
        [SerializeField] private float chargeRequiredTime = 0.45f;
        [SerializeField] private float maxChargeTime = 1.5f;

        [SerializeField] private float chargeHoldNormalizedTime = 0.27f;
        [SerializeField] private float chargeAnimationSlowSpeed = 0.32f;

        [SerializeField] private float chargedAttackStartDelay = 0.12f;
        [SerializeField] private float chargedAttackEndDelay = 0.55f;

        [Tooltip("溜め強本体。専用パーティクルは既存のCharge Effects側で管理")]
        [SerializeField] private float chargedHeavyMinMultiplier = 1.15f;

        [SerializeField] private float chargedHeavyMaxMultiplier = 1.45f;
        [SerializeField] private float chargedHeavyEndDelay = 0.95f;

        [Header("Charge Movement")]
        [SerializeField] private bool allowTurnWhileCharging = true;
        [SerializeField] private float chargeTurnMultiplier = 0.85f;

        [SerializeField] private bool allowMoveWhileCharging = true;
        [SerializeField] private float chargeMoveMultiplier = 0.22f;

        [Header("Charge Blink")]
        [SerializeField] private bool allowBlinkWhileCharging = true;
        [SerializeField] private float chargeBlinkDistanceMultiplier = 0.5f;

        [Header("Combo")]
        [SerializeField] private float comboResetTime = 1.0f;
        [SerializeField] private float comboInputBufferTime = 0.35f;
        [SerializeField] private float attackEndDelay = 0.55f;

        private ComboStage comboStage = ComboStage.None;
        private float comboTimer = 0f;

        private bool bufferedLight = false;
        private bool bufferedHeavy = false;
        private float bufferTimer = 0f;

        private bool isWaitingHeavyDecision = false;
        private float heavyDecisionTimer = 0f;

        private bool isChargingHeavy = false;

        private float chargeHeldTimer = 0f;
        private float chargePowerTimer = 0f;

        private bool isComboCooldown = false;
        private bool isNeutralHeavyCooldown = false;

        private Coroutine comboCooldownRoutine;
        private Coroutine neutralHeavyCooldownRoutine;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _playerInput = GetComponent<PlayerInput>();
            _movement = GetComponent<Movement>();

            if (attackAudioSource == null)
            {
                attackAudioSource = GetComponent<AudioSource>();
            }

            if (attackAudioSource == null)
            {
                attackAudioSource = GetComponentInChildren<AudioSource>();
            }

            if (attackAudioSource == null)
            {
                attackAudioSource = GetComponentInParent<AudioSource>();
            }

            CreateChargeEffectsIfNeeded();
            StopChargeEffects();
        }

        private void Update()
        {
            UpdateComboTimer();
            UpdateBufferTimer();

            if (isWaitingHeavyDecision)
            {
                UpdateHeavyDecision();
                return;
            }

            if (isChargingHeavy)
            {
                UpdateHeavyCharge();
                return;
            }

            if (AttackInProgress)
            {
                ReadBufferedInput();
                return;
            }

            if (_playerInput.AttackInput)
            {
                if (isComboCooldown)
                {
                    Debug.Log("コンボ硬直中");
                    return;
                }

                if (IsNextLightAllowed())
                {
                    if (_movement != null && _movement.CanDashAttack && comboStage == ComboStage.None)
                    {
                        StartDashAttack();
                    }
                    else
                    {
                        StartLightAttack();
                    }
                }
                else
                {
                    Debug.Log("このタイミングでは弱攻撃に派生できません");
                }

                return;
            }

            if (_playerInput.SpecialAttackInput)
            {
                if (isComboCooldown)
                {
                    Debug.Log("コンボ硬直中");
                    return;
                }

                if (IsNextHeavyAllowed())
                {
                    StartHeavyFromComboInput();
                }
                else if (comboStage == ComboStage.None)
                {
                    if (isNeutralHeavyCooldown)
                    {
                        Debug.Log("強攻撃クールタイム中");
                        return;
                    }

                    StartHeavyDecision();
                }
                else
                {
                    Debug.Log("このタイミングでは強攻撃に派生できません");
                }
            }
        }

        private void UpdateComboTimer()
        {
            if (comboStage == ComboStage.None) return;
            if (AttackInProgress) return;
            if (isChargingHeavy) return;
            if (isWaitingHeavyDecision) return;

            comboTimer -= Time.deltaTime;

            if (comboTimer <= 0f)
            {
                if (comboStage == ComboStage.Heavy1)
                {
                    StartNeutralHeavyCooldown();
                }

                ResetCombo();
            }
        }

        private void RefreshComboTimer()
        {
            comboTimer = comboResetTime;
        }

        private void ResetCombo()
        {
            comboStage = ComboStage.None;
            comboTimer = 0f;
            bufferedLight = false;
            bufferedHeavy = false;
        }

        private void UpdateBufferTimer()
        {
            if (!bufferedLight && !bufferedHeavy) return;

            bufferTimer -= Time.deltaTime;

            if (bufferTimer <= 0f)
            {
                bufferedLight = false;
                bufferedHeavy = false;
            }
        }

        private void ReadBufferedInput()
        {
            if (_playerInput.AttackInput)
            {
                if (IsNextLightAllowed() && !isComboCooldown)
                {
                    bufferedLight = true;
                    bufferedHeavy = false;
                    bufferTimer = comboInputBufferTime;
                }
            }

            if (_playerInput.SpecialAttackInput || _playerInput.SpecialAttackReleased)
            {
                if (IsNextHeavyAllowed() && !isComboCooldown)
                {
                    bufferedHeavy = true;
                    bufferedLight = false;
                    bufferTimer = comboInputBufferTime;
                }
            }
        }

        private bool IsNextLightAllowed()
        {
            if (isComboCooldown) return false;

            switch (comboStage)
            {
                case ComboStage.None:
                case ComboStage.Light1:
                case ComboStage.Heavy1:
                case ComboStage.ChargedHeavyDone:
                case ComboStage.ChargedLight1:
                case ComboStage.ChargedHeavy1:
                    return true;

                default:
                    return false;
            }
        }

        private bool IsNextHeavyAllowed()
        {
            if (isComboCooldown) return false;

            switch (comboStage)
            {
                case ComboStage.Light2:
                case ComboStage.Heavy1:
                case ComboStage.HeavyLight:
                case ComboStage.ChargedHeavyDone:
                case ComboStage.ChargedLight2:
                case ComboStage.ChargedHeavyLight:
                    return true;

                default:
                    return false;
            }
        }

        private void StartHeavyDecision()
        {
            isWaitingHeavyDecision = true;
            heavyDecisionTimer = 0f;

            Debug.Log("右クリック入力判定開始");
        }

        private void UpdateHeavyDecision()
        {
            heavyDecisionTimer += Time.deltaTime;

            if (_playerInput.SpecialAttackReleased)
            {
                float heldTime = heavyDecisionTimer;

                isWaitingHeavyDecision = false;
                heavyDecisionTimer = 0f;

                if (heldTime <= heavyTapTime || heldTime < heavyHoldToStartChargeTime)
                {
                    StartQuickHeavyAttackAsNeutral();
                }
                else
                {
                    StartHeavyCharge(heldTime);
                }

                return;
            }

            if (!_playerInput.SpecialAttackHeld)
            {
                isWaitingHeavyDecision = false;
                heavyDecisionTimer = 0f;
                return;
            }

            if (heavyDecisionTimer >= heavyHoldToStartChargeTime)
            {
                float initialChargeTime = heavyDecisionTimer;

                isWaitingHeavyDecision = false;
                heavyDecisionTimer = 0f;

                StartHeavyCharge(initialChargeTime);
            }
        }

        private void StartHeavyCharge(float initialChargeTime = 0f)
        {
            isChargingHeavy = true;
            chargeHeldTimer = Mathf.Max(0f, initialChargeTime);
            chargePowerTimer = Mathf.Min(chargeHeldTimer, maxChargeTime);

            AttackInProgress = true;
            chargeReadyEffectPlayed = false;
            maxChargeEffectPlayed = false;

            if (_movement != null)
            {
                _movement.isAttacking = true;

                if (allowMoveWhileCharging || allowTurnWhileCharging || allowBlinkWhileCharging)
                {
                    _movement.BeginChargeAttackMove(
                        allowMoveWhileCharging,
                        allowTurnWhileCharging,
                        chargeMoveMultiplier,
                        chargeTurnMultiplier,
                        allowBlinkWhileCharging,
                        chargeBlinkDistanceMultiplier
                    );
                }
                else
                {
                    _movement.canMoveWhileAttacking = false;
                    _movement.SetAllowBlinkWhileAttacking(false);
                }
            }

            StartChargeEffect();

            if (_animator != null)
            {
                _animator.speed = chargeAnimationSlowSpeed;
                _animator.Play(abilityStateName, 0, 0f);
            }

            Debug.Log("強攻撃 溜め開始");
        }

        private void UpdateHeavyCharge()
        {
            chargeHeldTimer += Time.deltaTime;
            chargePowerTimer = Mathf.Min(chargeHeldTimer, maxChargeTime);

            if (!chargeReadyEffectPlayed && chargeHeldTimer >= chargeRequiredTime)
            {
                chargeReadyEffectPlayed = true;

                if (chargeReadyEffect != null)
                {
                    chargeReadyEffect.Play();
                }

                Debug.Log("溜め攻撃可能！");
            }

            if (!maxChargeEffectPlayed && chargeHeldTimer >= maxChargeTime)
            {
                maxChargeEffectPlayed = true;

                if (chargeReadyEffect != null)
                {
                    chargeReadyEffect.Play();
                }

                Debug.Log("最大溜め完了！");
            }

            if (_animator != null)
            {
                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

                if (stateInfo.IsName(abilityStateName))
                {
                    if (stateInfo.normalizedTime >= chargeHoldNormalizedTime)
                    {
                        _animator.Play(abilityStateName, 0, chargeHoldNormalizedTime);
                        _animator.speed = chargeAnimationSlowSpeed;
                    }
                }
            }

            if (_playerInput.SpecialAttackReleased)
            {
                bool isCharged = chargeHeldTimer >= chargeRequiredTime;

                isChargingHeavy = false;

                StopChargeEffects();

                if (_movement != null)
                {
                    _movement.EndChargeAttackMove();
                }

                if (_animator != null)
                {
                    _animator.speed = 1f;
                }

                if (isCharged)
                {
                    StartChargedHeavyAttack(chargePowerTimer);
                }
                else
                {
                    StartQuickHeavyAttackAsNeutral();
                }
            }
        }

        private void StartLightAttack()
        {
            bool isSecondLight = false;
            bool isHeavyComboLight = false;

            switch (comboStage)
            {
                case ComboStage.None:
                    comboStage = ComboStage.Light1;
                    break;

                case ComboStage.Light1:
                    comboStage = ComboStage.Light2;
                    isSecondLight = true;
                    break;

                case ComboStage.Heavy1:
                    comboStage = ComboStage.HeavyLight;
                    isHeavyComboLight = true;
                    break;

                case ComboStage.ChargedHeavyDone:
                    comboStage = ComboStage.ChargedLight1;
                    break;

                case ComboStage.ChargedLight1:
                    comboStage = ComboStage.ChargedLight2;
                    isSecondLight = true;
                    break;

                case ComboStage.ChargedHeavy1:
                    comboStage = ComboStage.ChargedHeavyLight;
                    isHeavyComboLight = true;
                    break;

                default:
                    Debug.Log("弱攻撃には派生できません");
                    return;
            }

            RefreshComboTimer();

            float start = isSecondLight ? light2Start : light1Start;
            float end = isSecondLight ? light2End : light1End;
            float multiplier = isSecondLight ? light2DamageMultiplier : light1DamageMultiplier;
            float forwardDistance = isSecondLight ? light2ForwardDistance : light1ForwardDistance;

            if (isHeavyComboLight)
            {
                multiplier = heavyComboLightDamageMultiplier;
                forwardDistance = heavyComboLightForwardDistance;
            }

            if (_movement != null)
            {
                _movement.isAttacking = true;
                _movement.canMoveWhileAttacking = true;
                StartAttackForwardMove(forwardDistance);
            }

            Debug.Log("弱攻撃派生: " + comboStage);

            PlayAttackAnimation(attackStateName, 0f);

            StartCoroutine(
                AttackRoutine(
                    daggerHitbox,
                    start,
                    end,
                    multiplier,
                    attackEndDelay,
                    1f,
                    false,
                    false,
                    false,
                    999f,
                    false,
                    AttackEffectType.Light
                )
            );
        }

        private void StartDashAttack()
        {
            ResetCombo();

            if (_movement != null)
            {
                _movement.isAttacking = true;
                _movement.canMoveWhileAttacking = false;
                StartAttackForwardMove(dashAttackForwardDistance);
            }

            Debug.Log("ダッシュ攻撃");

            PlayAttackAnimation(attackStateName, 0f);

            StartCoroutine(
                AttackRoutine(
                    daggerHitbox,
                    dashAttackStart,
                    dashAttackEnd,
                    dashAttackDamageMultiplier,
                    attackEndDelay,
                    1f,
                    true,
                    false,
                    false,
                    999f,
                    false,
                    AttackEffectType.Dash
                )
            );
        }

        private void StartQuickHeavyAttackAsNeutral()
        {
            comboStage = ComboStage.Heavy1;
            RefreshComboTimer();

            Debug.Log("強攻撃 単押し1発目");

            if (_movement != null)
            {
                _movement.isAttacking = true;
                _movement.canMoveWhileAttacking = false;
                _movement.SetAllowBlinkWhileAttacking(false);
                StartAttackForwardMove(quickHeavyForwardDistance);
            }

            PlayAttackAnimation(abilityStateName, quickHeavyAnimationStartNormalized);

            StartCoroutine(
                AttackRoutine(
                    swordHitbox,
                    quickHeavyStart,
                    quickHeavyEnd,
                    quickHeavyDamageMultiplier,
                    quickHeavyEndDelay,
                    quickHeavyAnimationSpeed,
                    false,
                    false,
                    true,
                    quickHeavyMoveUnlockTime,
                    false,
                    AttackEffectType.Heavy
                )
            );
        }

        private void StartSecondQuickHeavyAttack()
        {
            comboStage = ComboStage.Heavy2;
            RefreshComboTimer();

            Debug.Log("強攻撃 単押し2発目");

            if (_movement != null)
            {
                _movement.isAttacking = true;
                _movement.canMoveWhileAttacking = false;
                _movement.SetAllowBlinkWhileAttacking(false);
                StartAttackForwardMove(quickHeavySecondForwardDistance);
            }

            PlayAttackAnimation(abilityStateName, quickHeavySecondAnimationStartNormalized);

            StartCoroutine(
                AttackRoutine(
                    swordHitbox,
                    quickHeavySecondStart,
                    quickHeavySecondEnd,
                    quickHeavySecondDamageMultiplier,
                    quickHeavySecondEndDelay,
                    quickHeavySecondAnimationSpeed,
                    true,
                    true,
                    true,
                    quickHeavySecondMoveUnlockTime,
                    false,
                    AttackEffectType.Heavy
                )
            );
        }

        private void StartHeavyFromComboInput()
        {
            switch (comboStage)
            {
                case ComboStage.Light2:
                    StartHeavyFinisher("弱弱強", weakWeakHeavyFinisherMultiplier, AttackEffectType.WeakWeakHeavyFinisher);
                    break;

                case ComboStage.Heavy1:
                    StartSecondQuickHeavyAttack();
                    break;

                case ComboStage.HeavyLight:
                    StartHeavyFinisher("強弱強", heavyWeakHeavyFinisherMultiplier, AttackEffectType.HeavyWeakHeavyFinisher);
                    break;

                case ComboStage.ChargedHeavyDone:
                    StartQuickHeavyAfterCharged();
                    break;

                case ComboStage.ChargedLight2:
                    StartHeavyFinisher("溜め強→弱弱強", chargedWeakWeakHeavyFinisherMultiplier, AttackEffectType.ChargedWeakWeakHeavyFinisher);
                    break;

                case ComboStage.ChargedHeavyLight:
                    StartHeavyFinisher("溜め強→強弱強", chargedHeavyWeakHeavyFinisherMultiplier, AttackEffectType.ChargedHeavyWeakHeavyFinisher);
                    break;
            }
        }

        private void StartQuickHeavyAfterCharged()
        {
            comboStage = ComboStage.ChargedHeavy1;
            RefreshComboTimer();

            Debug.Log("溜め強→強 / 強弱強ルート開始");

            if (_movement != null)
            {
                _movement.isAttacking = true;
                _movement.canMoveWhileAttacking = false;
                _movement.SetAllowBlinkWhileAttacking(false);
                StartAttackForwardMove(quickHeavyForwardDistance);
            }

            PlayAttackAnimation(abilityStateName, quickHeavyAnimationStartNormalized);

            StartCoroutine(
                AttackRoutine(
                    swordHitbox,
                    quickHeavyStart,
                    quickHeavyEnd,
                    quickHeavyDamageMultiplier,
                    quickHeavyEndDelay,
                    quickHeavyAnimationSpeed,
                    false,
                    false,
                    true,
                    quickHeavyMoveUnlockTime,
                    false,
                    AttackEffectType.Heavy
                )
            );
        }

        private void StartHeavyFinisher(string comboName, float damageMultiplier, AttackEffectType finisherEffectType)
        {
            Debug.Log("コンボ締め: " + comboName);

            RefreshComboTimer();

            if (_movement != null)
            {
                _movement.isAttacking = true;
                _movement.canMoveWhileAttacking = false;
                _movement.SetAllowBlinkWhileAttacking(false);
                StartAttackForwardMove(comboHeavyFinisherForwardDistance);
            }

            PlayAttackAnimation(abilityStateName, heavyFinisherAnimationStartNormalized);

            StartCoroutine(
                AttackRoutine(
                    swordHitbox,
                    heavyFinisherStart,
                    heavyFinisherEnd,
                    damageMultiplier,
                    heavyFinisherEndDelay,
                    heavyFinisherAnimationSpeed,
                    true,
                    true,
                    true,
                    heavyFinisherMoveUnlockTime,
                    true,
                    finisherEffectType
                )
            );
        }

        private void StartChargedHeavyAttack(float finalChargePowerTime)
        {
            Debug.Log("溜め強攻撃 PowerTime: " + finalChargePowerTime);

            comboStage = ComboStage.ChargedHeavyDone;
            RefreshComboTimer();

            if (_movement != null)
            {
                _movement.isAttacking = true;
                _movement.canMoveWhileAttacking = false;
                _movement.SetAllowBlinkWhileAttacking(false);
                StartAttackForwardMove(chargedHeavyForwardDistance);
            }

            float chargeRate = Mathf.Clamp01(finalChargePowerTime / maxChargeTime);
            float multiplier = Mathf.Lerp(chargedHeavyMinMultiplier, chargedHeavyMaxMultiplier, chargeRate);

            PlayAttackAnimation(abilityStateName, chargeHoldNormalizedTime);

            StartCoroutine(
                AttackRoutine(
                    swordHitbox,
                    chargedAttackStartDelay,
                    chargedAttackEndDelay,
                    multiplier,
                    chargedHeavyEndDelay,
                    1f,
                    false,
                    false,
                    true,
                    chargedHeavyMoveUnlockTime,
                    false,
                    AttackEffectType.ChargedHeavy
                )
            );
        }

        private void StartAttackForwardMove(float distance)
        {
            if (!useAttackForwardMove) return;
            if (_movement == null) return;

            _movement.StartAttackForwardMove(
                distance,
                attackForwardDuration,
                attackForwardAccelerationTime,
                attackForwardDecelerationTime
            );
        }

        private void PlayAttackAnimation(string stateName, float normalizedStartTime)
        {
            if (_animator == null) return;

            _animator.speed = 1f;
            _animator.ResetTrigger("Attack");
            _animator.ResetTrigger("Ability");

            _animator.Play(stateName, 0, Mathf.Clamp01(normalizedStartTime));
        }

        private IEnumerator AttackRoutine(
            WeaponHitbox hitbox,
            float hitStart,
            float hitEnd,
            float damageMultiplier,
            float totalDuration,
            float animationSpeed,
            bool finishComboAfterEnd,
            bool startNeutralHeavyCooldownAfterEnd,
            bool allowLateMovement,
            float lateMovementUnlockTime,
            bool playComboFinisherParticle,
            AttackEffectType attackEffectType
        )
        {
            AttackInProgress = true;

            if (_movement != null)
            {
                _movement.isAttacking = true;

                if (allowLateMovement)
                {
                    _movement.canMoveWhileAttacking = false;
                }
            }

            if (_animator != null)
            {
                _animator.speed = animationSpeed;
            }

            if (hitbox == null)
            {
                Debug.LogError("Hitboxが空です！");
                EndAttack();
                yield break;
            }

            float timer = 0f;
            bool hitboxEnabled = false;
            bool lateMoveEnabled = false;
            bool attackEffectPlayed = false;
            bool hitboxScaleChanged = false;
            Vector3 originalHitboxScale = Vector3.one;
            float hitboxScaleMultiplier = GetFinisherHitboxScaleMultiplier(attackEffectType);

            totalDuration = Mathf.Max(totalDuration, hitEnd + 0.05f);

            while (timer < totalDuration)
            {
                timer += Time.deltaTime;

                if (!hitboxEnabled && timer >= hitStart)
                {
                    hitboxEnabled = true;
                    ApplyTemporaryHitboxScale(hitbox, hitboxScaleMultiplier, ref originalHitboxScale, ref hitboxScaleChanged);
                    hitbox.EnableHitbox(damageMultiplier);
                    Debug.Log("判定ON / 倍率: " + damageMultiplier + " / HitboxScale: " + hitboxScaleMultiplier);

                    if (!playAttackEffectOnlyOncePerAttack || !attackEffectPlayed)
                    {
                        attackEffectPlayed = true;

                        if (playComboFinisherParticle && playFinisherParticleOnHitStart)
                        {
                            PlayAttackEffect(attackEffectType);
                        }
                        else
                        {
                            PlayAttackEffect(attackEffectType);
                        }
                    }
                }

                if (hitboxEnabled && timer >= hitEnd)
                {
                    hitboxEnabled = false;
                    hitbox.DisableHitbox();
                    RestoreTemporaryHitboxScale(hitbox, originalHitboxScale, ref hitboxScaleChanged);
                    Debug.Log("判定OFF");
                }

                if (allowLateMovement && !lateMoveEnabled && timer >= lateMovementUnlockTime)
                {
                    lateMoveEnabled = true;
                    EnableLateAttackMovement();
                }

                yield return null;
            }

            hitbox.DisableHitbox();
            RestoreTemporaryHitboxScale(hitbox, originalHitboxScale, ref hitboxScaleChanged);

            EndAttack();

            if (startNeutralHeavyCooldownAfterEnd)
            {
                StartNeutralHeavyCooldown();
            }

            if (finishComboAfterEnd)
            {
                bufferedLight = false;
                bufferedHeavy = false;
                ResetCombo();
                StartComboCooldown();
                yield break;
            }

            TryConsumeBuffer();
        }

        private void PlayComboFinisherParticle()
        {
            PlayAttackEffect(AttackEffectType.ComboFinisher);
        }

        private void PlayAttackEffect(AttackEffectType effectType)
        {
            ParticleSystem particlePrefab = GetAttackParticlePrefab(effectType);
            AudioClip sfx = GetAttackSfx(effectType);
            Vector3 offset = GetAttackParticleOffset(effectType);
            Vector3 eulerOffset = GetAttackParticleEulerOffset(effectType);
            float destroyTime = GetAttackParticleDestroyTime(effectType);

            PlayAttackSfx(sfx);

            if (particlePrefab == null)
            {
                return;
            }

            Vector3 spawnPosition = GetPlayerFrontParticlePosition(offset, effectType);
            Quaternion spawnRotation = GetPlayerFrontParticleRotation(eulerOffset);

            ParticleSystem particle = Instantiate(
                particlePrefab,
                spawnPosition,
                spawnRotation
            );

            particle.transform.localScale = particlePrefab.transform.localScale;

            if (forceAttackParticleLoopOff)
            {
                ForceParticleLoopOff(particle);
            }

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(true);

            Destroy(particle.gameObject, destroyTime);
        }

        private void ForceParticleLoopOff(ParticleSystem rootParticle)
        {
            if (rootParticle == null) return;

            ParticleSystem[] particles = rootParticle.GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem ps in particles)
            {
                var main = ps.main;
                main.loop = false;
                main.playOnAwake = false;
            }
        }

        private void ApplyTemporaryHitboxScale(
            WeaponHitbox hitbox,
            float scaleMultiplier,
            ref Vector3 originalScale,
            ref bool scaleChanged
        )
        {
            if (hitbox == null) return;
            if (scaleChanged) return;
            if (Mathf.Approximately(scaleMultiplier, 1f)) return;
            if (scaleMultiplier <= 0f) return;

            Transform hitboxTransform = hitbox.transform;
            originalScale = hitboxTransform.localScale;
            hitboxTransform.localScale = originalScale * scaleMultiplier;
            scaleChanged = true;
        }

        private void RestoreTemporaryHitboxScale(
            WeaponHitbox hitbox,
            Vector3 originalScale,
            ref bool scaleChanged
        )
        {
            if (!restoreFinisherHitboxScaleAfterHit) return;
            if (hitbox == null) return;
            if (!scaleChanged) return;

            hitbox.transform.localScale = originalScale;
            scaleChanged = false;
        }

        private float GetFinisherHitboxScaleMultiplier(AttackEffectType effectType)
        {
            switch (effectType)
            {
                case AttackEffectType.WeakWeakHeavyFinisher:
                    return Mathf.Max(0.01f, weakWeakHeavyFinisherHitboxScale);

                case AttackEffectType.HeavyWeakHeavyFinisher:
                    return Mathf.Max(0.01f, heavyWeakHeavyFinisherHitboxScale);

                case AttackEffectType.ChargedWeakWeakHeavyFinisher:
                    return Mathf.Max(0.01f, chargedWeakWeakHeavyFinisherHitboxScale);

                case AttackEffectType.ChargedHeavyWeakHeavyFinisher:
                    return Mathf.Max(0.01f, chargedHeavyWeakHeavyFinisherHitboxScale);

                default:
                    return 1f;
            }
        }

        private ParticleSystem GetAttackParticlePrefab(AttackEffectType effectType)
        {
            switch (effectType)
            {
                case AttackEffectType.Light:
                    return lightAttackParticlePrefab;

                case AttackEffectType.Dash:
                    return dashAttackParticlePrefab != null ? dashAttackParticlePrefab : lightAttackParticlePrefab;

                case AttackEffectType.Heavy:
                    return heavyAttackParticlePrefab;

                case AttackEffectType.ChargedHeavy:
                    return chargedHeavyParticlePrefab != null ? chargedHeavyParticlePrefab : heavyAttackParticlePrefab;

                case AttackEffectType.ComboFinisher:
                    return comboFinisherParticlePrefab != null ? comboFinisherParticlePrefab : heavyAttackParticlePrefab;

                case AttackEffectType.WeakWeakHeavyFinisher:
                    return weakWeakHeavyFinisherParticlePrefab != null
                        ? weakWeakHeavyFinisherParticlePrefab
                        : (comboFinisherParticlePrefab != null ? comboFinisherParticlePrefab : heavyAttackParticlePrefab);

                case AttackEffectType.HeavyWeakHeavyFinisher:
                    return heavyWeakHeavyFinisherParticlePrefab != null
                        ? heavyWeakHeavyFinisherParticlePrefab
                        : (comboFinisherParticlePrefab != null ? comboFinisherParticlePrefab : heavyAttackParticlePrefab);

                case AttackEffectType.ChargedWeakWeakHeavyFinisher:
                    return chargedWeakWeakHeavyFinisherParticlePrefab != null
                        ? chargedWeakWeakHeavyFinisherParticlePrefab
                        : (comboFinisherParticlePrefab != null ? comboFinisherParticlePrefab : heavyAttackParticlePrefab);

                case AttackEffectType.ChargedHeavyWeakHeavyFinisher:
                    return chargedHeavyWeakHeavyFinisherParticlePrefab != null
                        ? chargedHeavyWeakHeavyFinisherParticlePrefab
                        : (comboFinisherParticlePrefab != null ? comboFinisherParticlePrefab : heavyAttackParticlePrefab);

                default:
                    return null;
            }
        }

        private AudioClip GetAttackSfx(AttackEffectType effectType)
        {
            switch (effectType)
            {
                case AttackEffectType.Light:
                    return lightAttackSfx;

                case AttackEffectType.Dash:
                    return dashAttackSfx != null ? dashAttackSfx : lightAttackSfx;

                case AttackEffectType.Heavy:
                    return heavyAttackSfx;

                case AttackEffectType.ChargedHeavy:
                    return chargedHeavySfx != null ? chargedHeavySfx : heavyAttackSfx;

                case AttackEffectType.ComboFinisher:
                    return comboFinisherSfx != null ? comboFinisherSfx : heavyAttackSfx;

                case AttackEffectType.WeakWeakHeavyFinisher:
                    return weakWeakHeavyFinisherSfx != null ? weakWeakHeavyFinisherSfx : heavyAttackSfx;

                case AttackEffectType.HeavyWeakHeavyFinisher:
                    return heavyWeakHeavyFinisherSfx != null ? heavyWeakHeavyFinisherSfx : heavyAttackSfx;

                case AttackEffectType.ChargedWeakWeakHeavyFinisher:
                    return chargedWeakWeakHeavyFinisherSfx != null ? chargedWeakWeakHeavyFinisherSfx : heavyAttackSfx;

                case AttackEffectType.ChargedHeavyWeakHeavyFinisher:
                    return chargedHeavyWeakHeavyFinisherSfx != null ? chargedHeavyWeakHeavyFinisherSfx : heavyAttackSfx;

                default:
                    return null;
            }
        }

        private Vector3 GetAttackParticleOffset(AttackEffectType effectType)
        {
            switch (effectType)
            {
                case AttackEffectType.Light:
                    return lightAttackParticleOffset;

                case AttackEffectType.Dash:
                    return dashAttackParticleOffset;

                case AttackEffectType.Heavy:
                    return heavyAttackParticleOffset;

                case AttackEffectType.ChargedHeavy:
                    return chargedHeavyParticleOffset;

                case AttackEffectType.ComboFinisher:
                    return comboFinisherParticleOffset;

                case AttackEffectType.WeakWeakHeavyFinisher:
                    return weakWeakHeavyFinisherParticleOffset;

                case AttackEffectType.HeavyWeakHeavyFinisher:
                    return heavyWeakHeavyFinisherParticleOffset;

                case AttackEffectType.ChargedWeakWeakHeavyFinisher:
                    return chargedWeakWeakHeavyFinisherParticleOffset;

                case AttackEffectType.ChargedHeavyWeakHeavyFinisher:
                    return chargedHeavyWeakHeavyFinisherParticleOffset;

                default:
                    return Vector3.zero;
            }
        }

        private Vector3 GetAttackParticleEulerOffset(AttackEffectType effectType)
        {
            switch (effectType)
            {
                case AttackEffectType.Light:
                    return lightAttackParticleEulerOffset;

                case AttackEffectType.Dash:
                    return dashAttackParticleEulerOffset;

                case AttackEffectType.Heavy:
                    return heavyAttackParticleEulerOffset;

                case AttackEffectType.ChargedHeavy:
                    return chargedHeavyParticleEulerOffset;

                case AttackEffectType.ComboFinisher:
                    return comboFinisherParticleEulerOffset;

                case AttackEffectType.WeakWeakHeavyFinisher:
                    return weakWeakHeavyFinisherParticleEulerOffset;

                case AttackEffectType.HeavyWeakHeavyFinisher:
                    return heavyWeakHeavyFinisherParticleEulerOffset;

                case AttackEffectType.ChargedWeakWeakHeavyFinisher:
                    return chargedWeakWeakHeavyFinisherParticleEulerOffset;

                case AttackEffectType.ChargedHeavyWeakHeavyFinisher:
                    return chargedHeavyWeakHeavyFinisherParticleEulerOffset;

                default:
                    return Vector3.zero;
            }
        }

        private float GetAttackParticleDestroyTime(AttackEffectType effectType)
        {
            if (effectType == AttackEffectType.ComboFinisher
                || effectType == AttackEffectType.WeakWeakHeavyFinisher
                || effectType == AttackEffectType.HeavyWeakHeavyFinisher
                || effectType == AttackEffectType.ChargedWeakWeakHeavyFinisher
                || effectType == AttackEffectType.ChargedHeavyWeakHeavyFinisher)
            {
                return comboFinisherParticleDestroyTime;
            }

            return normalAttackParticleDestroyTime;
        }

        private Vector3 GetPlayerFrontParticlePosition(Vector3 offset, AttackEffectType effectType)
        {
            if (effectType == AttackEffectType.ComboFinisher && comboFinisherParticlePoint != null)
            {
                return comboFinisherParticlePoint.position
                    + GetFlatRight() * offset.x
                    + Vector3.up * offset.y
                    + GetFlatForward() * offset.z;
            }

            return transform.position
                + GetFlatRight() * offset.x
                + Vector3.up * offset.y
                + GetFlatForward() * offset.z;
        }

        private Quaternion GetPlayerFrontParticleRotation(Vector3 eulerOffset)
        {
            return Quaternion.LookRotation(GetFlatForward(), Vector3.up) * Quaternion.Euler(eulerOffset);
        }

        private Vector3 GetFlatForward()
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            return forward.normalized;
        }

        private Vector3 GetFlatRight()
        {
            Vector3 right = transform.right;
            right.y = 0f;

            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.right;
            }

            return right.normalized;
        }

        private void PlayAttackSfx(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (attackAudioSource == null)
            {
                attackAudioSource = GetComponent<AudioSource>();
            }

            if (attackAudioSource == null)
            {
                attackAudioSource = GetComponentInChildren<AudioSource>();
            }

            if (attackAudioSource == null)
            {
                attackAudioSource = GetComponentInParent<AudioSource>();
            }

            if (attackAudioSource == null)
            {
                Debug.LogWarning("[Combat] Attack AudioSourceが見つからないため、攻撃SEを再生できません。", this);
                return;
            }

            attackAudioSource.PlayOneShot(clip, attackSfxVolume);
        }

        private void EnableLateAttackMovement()
        {
            if (_movement == null) return;

            _movement.isAttacking = true;
            _movement.canMoveWhileAttacking = true;
            _movement.SetAllowBlinkWhileAttacking(false);

            Debug.Log("攻撃終盤：移動解禁");
        }

        private void EndAttack()
        {
            AttackInProgress = false;
            isChargingHeavy = false;
            isWaitingHeavyDecision = false;
            heavyDecisionTimer = 0f;
            chargeHeldTimer = 0f;
            chargePowerTimer = 0f;

            StopChargeEffects();

            if (_movement != null)
            {
                _movement.EndChargeAttackMove();
            }

            if (_animator != null)
            {
                _animator.speed = 1f;
            }

            if (_movement != null)
            {
                _movement.isAttacking = false;
                _movement.canMoveWhileAttacking = false;
                _movement.SetAllowBlinkWhileAttacking(false);
                _movement.StopAttackForwardMove();
            }

            if (daggerHitbox != null)
            {
                daggerHitbox.DisableHitbox();
            }

            if (swordHitbox != null)
            {
                swordHitbox.DisableHitbox();
            }

            if (comboStage != ComboStage.None)
            {
                RefreshComboTimer();
            }
        }

        private void StartComboCooldown()
        {
            if (comboCooldownRoutine != null)
            {
                StopCoroutine(comboCooldownRoutine);
            }

            comboCooldownRoutine = StartCoroutine(ComboCooldownRoutine());
        }

        private IEnumerator ComboCooldownRoutine()
        {
            isComboCooldown = true;

            yield return new WaitForSeconds(comboFinishCooldown);

            isComboCooldown = false;
        }

        private void StartNeutralHeavyCooldown()
        {
            if (neutralHeavyCooldownRoutine != null)
            {
                StopCoroutine(neutralHeavyCooldownRoutine);
            }

            neutralHeavyCooldownRoutine = StartCoroutine(NeutralHeavyCooldownRoutine());
        }

        private IEnumerator NeutralHeavyCooldownRoutine()
        {
            isNeutralHeavyCooldown = true;

            yield return new WaitForSeconds(neutralHeavyCooldown);

            isNeutralHeavyCooldown = false;
        }

        private void TryConsumeBuffer()
        {
            if (bufferedLight && IsNextLightAllowed())
            {
                bufferedLight = false;
                bufferedHeavy = false;

                StartLightAttack();
                return;
            }

            if (bufferedHeavy && IsNextHeavyAllowed())
            {
                bufferedLight = false;
                bufferedHeavy = false;

                StartHeavyFromComboInput();
            }
        }

        private void StartChargeEffect()
        {
            if (chargeEffect != null)
            {
                chargeEffect.Play();
            }

            if (chargeReadyEffect != null)
            {
                chargeReadyEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void StopChargeEffects()
        {
            if (chargeEffect != null)
            {
                chargeEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (chargeReadyEffect != null)
            {
                chargeReadyEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            chargeReadyEffectPlayed = false;
            maxChargeEffectPlayed = false;
        }

        private void CreateChargeEffectsIfNeeded()
        {
            if (chargeEffect == null)
            {
                GameObject chargeObj = new GameObject("Auto_ChargeEffect");
                chargeObj.transform.SetParent(transform);
                chargeObj.transform.localPosition = chargeEffectLocalPosition;
                chargeObj.transform.localRotation = Quaternion.identity;
                chargeObj.transform.localScale = Vector3.one;

                chargeEffect = chargeObj.AddComponent<ParticleSystem>();
                SetupChargeParticle(chargeEffect);
            }

            if (chargeReadyEffect == null)
            {
                GameObject readyObj = new GameObject("Auto_ChargeReadyEffect");
                readyObj.transform.SetParent(transform);
                readyObj.transform.localPosition = chargeEffectLocalPosition;
                readyObj.transform.localRotation = Quaternion.identity;
                readyObj.transform.localScale = Vector3.one;

                chargeReadyEffect = readyObj.AddComponent<ParticleSystem>();
                SetupChargeReadyParticle(chargeReadyEffect);
            }
        }

        private void SetupChargeParticle(ParticleSystem ps)
        {
            var main = ps.main;
            main.duration = 0.8f;
            main.loop = true;
            main.startLifetime = 0.35f;
            main.startSpeed = 0.55f;
            main.startSize = 0.22f;
            main.startColor = new Color(1f, 0.82f, 0.25f, 0.75f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 35f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void SetupChargeReadyParticle(ParticleSystem ps)
        {
            var main = ps.main;
            main.duration = 0.35f;
            main.loop = false;
            main.startLifetime = 0.35f;
            main.startSpeed = 2.2f;
            main.startSize = 0.45f;
            main.startColor = new Color(1f, 0.95f, 0.55f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 35)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void StartDaggerHit()
        {
            if (daggerHitbox != null) daggerHitbox.EnableHitbox(1f);
        }

        public void EndDaggerHit()
        {
            if (daggerHitbox != null) daggerHitbox.DisableHitbox();
        }

        public void StartSwordHit()
        {
            if (swordHitbox != null) swordHitbox.EnableHitbox(1f);
        }

        public void EndSwordHit()
        {
            if (swordHitbox != null) swordHitbox.DisableHitbox();
        }

        public void SetAttackStart()
        {
            AttackInProgress = true;

            if (_movement != null)
            {
                _movement.isAttacking = true;
            }
        }

        public void SetAttackEnd()
        {
            // コルーチン側で終了管理
        }

        public void SetAbilityStart()
        {
            AttackInProgress = true;

            if (_movement != null)
            {
                _movement.isAttacking = true;
                _movement.canMoveWhileAttacking = false;
            }
        }

        public void SetAbilityEnd()
        {
            // コルーチン側で終了管理
        }
    }
}