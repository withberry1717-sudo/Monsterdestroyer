
using UnityEngine;
using NaughtyCharacter;
using System.Collections;
using System.Collections.Generic;

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

        private enum FinisherType
        {
            None,
            WeakWeakHeavy,
            HeavyWeakHeavy,
            ChargedWeakWeakHeavy,
            ChargedHeavyWeakHeavy
        }

        [System.Serializable]
        private class AttackEffectSettings
        {
            [Tooltip("攻撃時に出すパーティクルPrefab。空なら何も出ません。")]
            public ParticleSystem particlePrefab;

            [Tooltip("攻撃時に鳴らすSE。コンボ締め専用SEが空なら強攻撃SEを使います。")]
            public AudioClip sfx;

            [Tooltip("プレイヤー基準の出現位置。X=左右、Y=上下、Z=正面距離。")]
            public Vector3 offset = new Vector3(0f, 1.0f, 0.8f);

            [Tooltip("正面がズレる時の角度補正。主にYを90/-90/180で調整。")]
            public Vector3 eulerOffset = Vector3.zero;

            [Tooltip("生成したパーティクルを消すまでの秒数。")]
            public float destroyTime = 1.5f;

            [Tooltip("攻撃判定ONから何秒遅らせてパーティクルを出すか。0なら判定ONと同時です。")]
            public float particleDelay = 0f;

            [Tooltip("攻撃判定ONから何秒遅らせてSEを鳴らすか。0なら判定ONと同時です。")]
            public float sfxDelay = 0f;

            [Tooltip("生成するパーティクルのサイズ倍率。通常攻撃は1、フィニッシャーや最大溜め攻撃だけ大きくするのがおすすめです。")]
            public Vector3 particleScale = Vector3.one;
        }

        private struct ColliderCache
        {
            public Vector3 localScale;
            public Vector3 boxSize;
            public Vector3 boxCenter;
            public float capsuleRadius;
            public float capsuleHeight;
            public Vector3 capsuleCenter;
            public float sphereRadius;
            public Vector3 sphereCenter;
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

        [Header("Actual Collider Size Control")]
        [Tooltip("空ならWeaponHitboxの子からColliderを自動取得します。今回みたいにdagger/swordについているCapsuleColliderを直接拡大したい場合は、ここにドラッグすると確実です。")]
        [SerializeField] private Collider[] daggerAttackColliders;

        [Tooltip("空ならWeaponHitboxの子からColliderを自動取得します。今回みたいにdagger/swordについているCapsuleColliderを直接拡大したい場合は、ここにドラッグすると確実です。")]
        [SerializeField] private Collider[] swordAttackColliders;

        [Tooltip("ONなら、上のCollider配列が空の時にWeaponHitbox配下のColliderを自動で探します。")]
        [SerializeField] private bool autoFindCollidersFromWeaponHitbox = true;

        [Tooltip("ONなら、コンボ締めの判定中だけColliderのRadius/Height/Sizeを拡大します。")]
        [SerializeField] private bool scaleColliderForComboFinisher = true;

        [Tooltip("Colliderを拡大した時、判定を少し前に寄せる量です。0で中心は変えません。CapsuleColliderの場合はローカルZ方向へ動きます。")]
        [SerializeField] private float finisherColliderForwardCenterOffset = 0.15f;

        private readonly Dictionary<Collider, ColliderCache> colliderCache = new Dictionary<Collider, ColliderCache>();

        [Header("Attack Audio")]
        [SerializeField] private AudioSource attackAudioSource;
        [Range(0f, 1f)]
        [SerializeField] private float attackSfxVolume = 1f;

        [Header("Attack VFX / SFX")]
        [SerializeField]
        private AttackEffectSettings lightAttackEffect = new AttackEffectSettings
        {
            offset = new Vector3(0f, 1.0f, 0.55f),
            destroyTime = 1.2f
        };

        [SerializeField]
        private AttackEffectSettings heavyAttackEffect = new AttackEffectSettings
        {
            offset = new Vector3(0f, 1.05f, 0.75f),
            destroyTime = 1.4f
        };

        [SerializeField]
        private AttackEffectSettings chargedHeavyEffect = new AttackEffectSettings
        {
            offset = new Vector3(0f, 1.1f, 0.85f),
            destroyTime = 1.6f
        };

        [SerializeField]
        private AttackEffectSettings maxChargedHeavyEffect = new AttackEffectSettings
        {
            offset = new Vector3(0f, 1.15f, 0.95f),
            destroyTime = 2.0f,
            particleScale = new Vector3(1.35f, 1.35f, 1.35f)
        };

        [SerializeField]
        private AttackEffectSettings dashAttackEffect = new AttackEffectSettings
        {
            offset = new Vector3(0f, 1.0f, 0.65f),
            destroyTime = 1.2f
        };

        [Header("Individual Combo Finisher VFX / SFX")]
        [SerializeField]
        private AttackEffectSettings weakWeakHeavyFinisherEffect = new AttackEffectSettings
        {
            offset = new Vector3(0f, 1.1f, 0.85f),
            destroyTime = 1.8f
        };

        [SerializeField]
        private AttackEffectSettings heavyWeakHeavyFinisherEffect = new AttackEffectSettings
        {
            offset = new Vector3(0f, 1.1f, 0.85f),
            destroyTime = 1.8f
        };

        [SerializeField]
        private AttackEffectSettings chargedWeakWeakHeavyFinisherEffect = new AttackEffectSettings
        {
            offset = new Vector3(0f, 1.15f, 0.95f),
            destroyTime = 2.0f
        };

        [SerializeField]
        private AttackEffectSettings chargedHeavyWeakHeavyFinisherEffect = new AttackEffectSettings
        {
            offset = new Vector3(0f, 1.15f, 0.95f),
            destroyTime = 2.0f
        };

        [Header("Individual Combo Finisher Hitbox Scale")]
        [SerializeField] private float weakWeakHeavyHitboxScale = 1.35f;
        [SerializeField] private float heavyWeakHeavyHitboxScale = 1.35f;
        [SerializeField] private float chargedWeakWeakHeavyHitboxScale = 1.5f;
        [SerializeField] private float chargedHeavyWeakHeavyHitboxScale = 1.5f;

        [Header("Attack Particle Safety")]
        [Tooltip("ONなら、1回の攻撃中にVFX/SFXを1回だけ出します。")]
        [SerializeField] private bool playAttackEffectOnlyOncePerAttack = true;

        [Tooltip("ONなら、Prefab側がLoopでも生成時にLoopをOFFにして出っぱなしを防ぎます。")]
        [SerializeField] private bool forceSpawnedParticleLoopOff = true;

        [Header("Charge Effects")]
        [SerializeField] private ParticleSystem chargeEffect;
        [SerializeField] private ParticleSystem chargeReadyEffect;

        [Tooltip("チャージ中エフェクトのローカル座標です。プレイヤー本体基準で調整できます。")]
        [SerializeField] private Vector3 chargeEffectLocalPosition = new Vector3(0f, 1.2f, 0.3f);

        [Tooltip("チャージ中エフェクトのローカル角度です。向きがズレる時に調整してください。")]
        [SerializeField] private Vector3 chargeEffectLocalEuler = Vector3.zero;

        [Tooltip("チャージ中エフェクトのローカルサイズです。")]
        [SerializeField] private Vector3 chargeEffectLocalScale = Vector3.one;

        [Tooltip("チャージ完了エフェクトのローカル座標です。空間的にずれる時に調整してください。")]
        [SerializeField] private Vector3 chargeReadyEffectLocalPosition = new Vector3(0f, 1.25f, 0.35f);

        [Tooltip("チャージ完了エフェクトのローカル角度です。向きがズレる時に調整してください。")]
        [SerializeField] private Vector3 chargeReadyEffectLocalEuler = Vector3.zero;

        [Tooltip("チャージ完了エフェクトのローカルサイズです。")]
        [SerializeField] private Vector3 chargeReadyEffectLocalScale = Vector3.one;

        [Tooltip("ONなら、外部から入れたchargeEffectもチャージ中はLoop扱いにして、止まっても再再生します。")]
        [SerializeField] private bool keepChargeEffectLoopingWhileCharging = true;

        [Tooltip("ON推奨。Shockwave系など一瞬で終わるVFXを、チャージ中だけ一定間隔でStop→Playし直して繰り返します。")]
        [SerializeField] private bool replayChargeEffectByInterval = true;

        [Tooltip("チャージ中VFXを何秒ごとに再再生するかです。0.25〜0.6くらいがおすすめです。")]
        [SerializeField] private float chargeEffectReplayInterval = 0.35f;

        [Tooltip("ONなら、chargeEffect配下の子ParticleSystemもまとめてLoop設定にします。")]
        [SerializeField] private bool forceChargeEffectChildrenLoop = true;

        [Header("Charge SFX")]
        [Tooltip("チャージ開始時に鳴らすSEです。")]
        [SerializeField] private AudioClip chargeStartSfx;

        [Tooltip("チャージ中に一定間隔で鳴らすSEです。空なら鳴りません。")]
        [SerializeField] private AudioClip chargeLoopSfx;

        [Tooltip("Charge Loop Sfxを何秒ごとに鳴らすかです。0.4〜0.8くらいがおすすめです。")]
        [SerializeField] private float chargeLoopSfxInterval = 0.6f;

        [Tooltip("チャージ完了した瞬間に鳴らすSEです。")]
        [SerializeField] private AudioClip chargeReadySfx;

        private bool chargeReadyEffectPlayed = false;
        private bool maxChargeEffectPlayed = false;
        private float chargeEffectReplayTimer = 0f;

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

        [Header("Max Charged Heavy")]
        [Tooltip("最大溜め攻撃時だけColliderを大きくする倍率です。1なら通常サイズです。")]
        [SerializeField] private float maxChargedHeavyHitboxScale = 1.35f;

        [Tooltip("ONなら最大溜め攻撃の当たり判定を Max Charged Heavy Hitbox Scale で大きくします。")]
        [SerializeField] private bool scaleColliderForMaxChargedHeavy = true;

        [Header("Charge Movement / 溜め中の移動・旋回")]
        [Tooltip("ONなら溜め中も向きを変えられます。OFFにすると溜め開始時の向きで固定されます。")]
        [SerializeField] private bool allowTurnWhileCharging = true;

        [Tooltip("溜め中の向き変更速度倍率です。小さいほどゆっくり向きを変えます。0.2〜0.45くらいがおすすめです。")]
        [Range(0f, 1f)]
        [SerializeField] private float chargeTurnMultiplier = 0.35f;

        [Tooltip("ONなら溜め中も少しだけ移動できます。OFFにすると溜め中は移動できません。")]
        [SerializeField] private bool allowMoveWhileCharging = true;

        [Tooltip("溜め中の移動速度倍率です。小さいほどほぼその場で溜めます。0.08〜0.16くらいがおすすめです。")]
        [Range(0f, 1f)]
        [SerializeField] private float chargeMoveMultiplier = 0.12f;

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
        private Coroutine chargeLoopSfxRoutine;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _playerInput = GetComponent<PlayerInput>();
            _movement = GetComponent<Movement>();

            if (attackAudioSource == null)
            {
                attackAudioSource = GetComponent<AudioSource>();
                if (attackAudioSource == null)
                {
                    attackAudioSource = GetComponentInChildren<AudioSource>();
                }
            }

            CacheAllColliderDefaults();

            ApplyChargeEffectTransforms();
            CreateChargeEffectsIfNeeded();
            ApplyChargeEffectTransforms();
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

            KeepChargeEffectAlive();

            if (!chargeReadyEffectPlayed && chargeHeldTimer >= chargeRequiredTime)
            {
                chargeReadyEffectPlayed = true;

                if (chargeReadyEffect != null)
                {
                    ApplyChargeEffectTransforms();
                    chargeReadyEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    chargeReadyEffect.Play(true);
                }

                PlaySfx(chargeReadySfx);

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
                    lightAttackEffect,
                    FinisherType.None,
                    1f
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
                    dashAttackEffect,
                    FinisherType.None,
                    1f
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
                    heavyAttackEffect,
                    FinisherType.None,
                    1f
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
                    heavyAttackEffect,
                    FinisherType.None,
                    1f
                )
            );
        }

        private void StartHeavyFromComboInput()
        {
            switch (comboStage)
            {
                case ComboStage.Light2:
                    StartHeavyFinisher("弱弱強", weakWeakHeavyFinisherMultiplier, FinisherType.WeakWeakHeavy);
                    break;

                case ComboStage.Heavy1:
                    StartSecondQuickHeavyAttack();
                    break;

                case ComboStage.HeavyLight:
                    StartHeavyFinisher("強弱強", heavyWeakHeavyFinisherMultiplier, FinisherType.HeavyWeakHeavy);
                    break;

                case ComboStage.ChargedHeavyDone:
                    StartQuickHeavyAfterCharged();
                    break;

                case ComboStage.ChargedLight2:
                    StartHeavyFinisher("溜め強→弱弱強", chargedWeakWeakHeavyFinisherMultiplier, FinisherType.ChargedWeakWeakHeavy);
                    break;

                case ComboStage.ChargedHeavyLight:
                    StartHeavyFinisher("溜め強→強弱強", chargedHeavyWeakHeavyFinisherMultiplier, FinisherType.ChargedHeavyWeakHeavy);
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
                    heavyAttackEffect,
                    FinisherType.None,
                    1f
                )
            );
        }

        private void StartHeavyFinisher(string comboName, float damageMultiplier, FinisherType finisherType)
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
                    GetFinisherEffect(finisherType),
                    finisherType,
                    GetFinisherHitboxScale(finisherType)
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

            bool isMaxCharged = finalChargePowerTime >= maxChargeTime - 0.01f;
            AttackEffectSettings chargedEffectToUse = GetChargedHeavyEffect(isMaxCharged);
            float chargedHitboxScale = (isMaxCharged && scaleColliderForMaxChargedHeavy)
                ? Mathf.Max(1f, maxChargedHeavyHitboxScale)
                : 1f;

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
                    chargedEffectToUse,
                    FinisherType.None,
                    chargedHitboxScale
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
            AttackEffectSettings attackEffect,
            FinisherType finisherType,
            float hitboxSizeMultiplier
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
            bool effectPlayed = false;

            totalDuration = Mathf.Max(totalDuration, hitEnd + 0.05f);

            while (timer < totalDuration)
            {
                timer += Time.deltaTime;

                if (!hitboxEnabled && timer >= hitStart)
                {
                    hitboxEnabled = true;

                    bool shouldScaleThisAttack = hitboxSizeMultiplier > 1.0001f
                        && ((finisherType != FinisherType.None && scaleColliderForComboFinisher)
                            || (finisherType == FinisherType.None && scaleColliderForMaxChargedHeavy));

                    if (shouldScaleThisAttack)
                    {
                        ApplyColliderScale(hitbox, hitboxSizeMultiplier);
                    }

                    hitbox.EnableHitbox(damageMultiplier);
                    Debug.Log("判定ON / 倍率: " + damageMultiplier + " / 判定サイズ倍率: " + hitboxSizeMultiplier);

                    if (!effectPlayed || !playAttackEffectOnlyOncePerAttack)
                    {
                        effectPlayed = true;
                        PlayAttackEffect(attackEffect, finisherType);
                    }
                }

                if (hitboxEnabled && timer >= hitEnd)
                {
                    hitboxEnabled = false;
                    hitbox.DisableHitbox();
                    RestoreColliderScale(hitbox);
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
            RestoreColliderScale(hitbox);

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

        private AttackEffectSettings GetFinisherEffect(FinisherType finisherType)
        {
            switch (finisherType)
            {
                case FinisherType.WeakWeakHeavy:
                    return weakWeakHeavyFinisherEffect;

                case FinisherType.HeavyWeakHeavy:
                    return heavyWeakHeavyFinisherEffect;

                case FinisherType.ChargedWeakWeakHeavy:
                    return chargedWeakWeakHeavyFinisherEffect;

                case FinisherType.ChargedHeavyWeakHeavy:
                    return chargedHeavyWeakHeavyFinisherEffect;

                default:
                    return heavyAttackEffect;
            }
        }

        private float GetFinisherHitboxScale(FinisherType finisherType)
        {
            switch (finisherType)
            {
                case FinisherType.WeakWeakHeavy:
                    return Mathf.Max(1f, weakWeakHeavyHitboxScale);

                case FinisherType.HeavyWeakHeavy:
                    return Mathf.Max(1f, heavyWeakHeavyHitboxScale);

                case FinisherType.ChargedWeakWeakHeavy:
                    return Mathf.Max(1f, chargedWeakWeakHeavyHitboxScale);

                case FinisherType.ChargedHeavyWeakHeavy:
                    return Mathf.Max(1f, chargedHeavyWeakHeavyHitboxScale);

                default:
                    return 1f;
            }
        }

        private AttackEffectSettings GetChargedHeavyEffect(bool isMaxCharged)
        {
            if (!isMaxCharged)
            {
                return chargedHeavyEffect;
            }

            bool maxHasAnyEffect = maxChargedHeavyEffect != null
                && (maxChargedHeavyEffect.particlePrefab != null || maxChargedHeavyEffect.sfx != null);

            return maxHasAnyEffect ? maxChargedHeavyEffect : chargedHeavyEffect;
        }

        private void PlayAttackEffect(AttackEffectSettings effect, FinisherType finisherType)
        {
            if (effect == null)
            {
                return;
            }

            ParticleSystem prefab = effect.particlePrefab;
            AudioClip clip = ResolveAttackSfx(effect, finisherType);

            float particleDelay = Mathf.Max(0f, effect.particleDelay);
            float sfxDelay = Mathf.Max(0f, effect.sfxDelay);

            if (prefab != null)
            {
                if (particleDelay > 0f)
                {
                    StartCoroutine(SpawnAttackParticleDelayed(prefab, effect, particleDelay));
                }
                else
                {
                    SpawnAttackParticle(prefab, effect);
                }
            }

            if (clip != null)
            {
                if (sfxDelay > 0f)
                {
                    StartCoroutine(PlaySfxDelayed(clip, sfxDelay));
                }
                else
                {
                    PlaySfx(clip);
                }
            }
        }

        private AudioClip ResolveAttackSfx(AttackEffectSettings effect, FinisherType finisherType)
        {
            if (effect == null) return null;

            AudioClip clip = effect.sfx;

            if (finisherType != FinisherType.None && clip == null && heavyAttackEffect != null)
            {
                clip = heavyAttackEffect.sfx;
            }

            if (effect == maxChargedHeavyEffect && clip == null && chargedHeavyEffect != null)
            {
                clip = chargedHeavyEffect.sfx;
            }

            return clip;
        }

        private IEnumerator SpawnAttackParticleDelayed(ParticleSystem prefab, AttackEffectSettings effect, float delay)
        {
            yield return new WaitForSeconds(delay);
            SpawnAttackParticle(prefab, effect);
        }

        private IEnumerator PlaySfxDelayed(AudioClip clip, float delay)
        {
            yield return new WaitForSeconds(delay);
            PlaySfx(clip);
        }

        private void SpawnAttackParticle(ParticleSystem prefab, AttackEffectSettings effect)
        {
            if (prefab == null || effect == null) return;

            Vector3 spawnPosition = transform.position
                + transform.forward * effect.offset.z
                + transform.right * effect.offset.x
                + transform.up * effect.offset.y;

            Vector3 flatForward = transform.forward;
            flatForward.y = 0f;

            if (flatForward.sqrMagnitude < 0.0001f)
            {
                flatForward = transform.forward;
            }

            Quaternion spawnRotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up)
                * Quaternion.Euler(effect.eulerOffset);

            ParticleSystem particle = Instantiate(prefab, spawnPosition, spawnRotation);

            Vector3 safeScale = effect.particleScale;
            if (safeScale == Vector3.zero) safeScale = Vector3.one;
            particle.transform.localScale = Vector3.Scale(particle.transform.localScale, safeScale);

            if (forceSpawnedParticleLoopOff)
            {
                ForceParticleLoopOff(particle);
            }

            particle.Play(true);

            float destroyTime = Mathf.Max(0.05f, effect.destroyTime + Mathf.Max(0f, effect.particleDelay));
            Destroy(particle.gameObject, destroyTime);
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;

            if (attackAudioSource == null)
            {
                attackAudioSource = GetComponent<AudioSource>();
                if (attackAudioSource == null)
                {
                    attackAudioSource = GetComponentInChildren<AudioSource>();
                }
            }

            if (attackAudioSource != null)
            {
                attackAudioSource.PlayOneShot(clip, attackSfxVolume);
            }
        }

        private void ForceParticleLoopOff(ParticleSystem root)
        {
            if (root == null) return;

            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem ps in systems)
            {
                ParticleSystem.MainModule main = ps.main;
                main.loop = false;
                main.playOnAwake = false;
            }
        }

        private void CacheAllColliderDefaults()
        {
            CacheColliderDefaults(daggerAttackColliders);
            CacheColliderDefaults(swordAttackColliders);

            if (autoFindCollidersFromWeaponHitbox)
            {
                CacheColliderDefaults(GetEffectiveColliders(daggerHitbox, daggerAttackColliders));
                CacheColliderDefaults(GetEffectiveColliders(swordHitbox, swordAttackColliders));
            }
        }

        private Collider[] GetEffectiveColliders(WeaponHitbox hitbox, Collider[] manualColliders)
        {
            if (manualColliders != null && manualColliders.Length > 0)
            {
                return manualColliders;
            }

            if (!autoFindCollidersFromWeaponHitbox || hitbox == null)
            {
                return null;
            }

            return hitbox.GetComponentsInChildren<Collider>(true);
        }

        private void CacheColliderDefaults(Collider[] colliders)
        {
            if (colliders == null) return;

            foreach (Collider col in colliders)
            {
                if (col == null) continue;
                if (colliderCache.ContainsKey(col)) continue;

                ColliderCache cache = new ColliderCache
                {
                    localScale = col.transform.localScale
                };

                if (col is BoxCollider box)
                {
                    cache.boxSize = box.size;
                    cache.boxCenter = box.center;
                }
                else if (col is CapsuleCollider capsule)
                {
                    cache.capsuleRadius = capsule.radius;
                    cache.capsuleHeight = capsule.height;
                    cache.capsuleCenter = capsule.center;
                }
                else if (col is SphereCollider sphere)
                {
                    cache.sphereRadius = sphere.radius;
                    cache.sphereCenter = sphere.center;
                }

                colliderCache.Add(col, cache);
            }
        }

        private void ApplyColliderScale(WeaponHitbox hitbox, float multiplier)
        {
            multiplier = Mathf.Max(1f, multiplier);

            Collider[] colliders = GetEffectiveColliders(hitbox, hitbox == swordHitbox ? swordAttackColliders : daggerAttackColliders);

            if (colliders == null || colliders.Length == 0)
            {
                Debug.LogWarning("拡大するColliderが見つかりません。Combatの Sword Attack Colliders に sword / dagger の CapsuleCollider を入れてください。", this);
                return;
            }

            CacheColliderDefaults(colliders);

            foreach (Collider col in colliders)
            {
                if (col == null) continue;
                if (!colliderCache.TryGetValue(col, out ColliderCache cache)) continue;

                if (col is BoxCollider box)
                {
                    box.size = cache.boxSize * multiplier;
                    box.center = cache.boxCenter + Vector3.forward * finisherColliderForwardCenterOffset * (multiplier - 1f);
                }
                else if (col is CapsuleCollider capsule)
                {
                    capsule.radius = cache.capsuleRadius * multiplier;
                    capsule.height = cache.capsuleHeight * multiplier;
                    capsule.center = cache.capsuleCenter + Vector3.forward * finisherColliderForwardCenterOffset * (multiplier - 1f);
                }
                else if (col is SphereCollider sphere)
                {
                    sphere.radius = cache.sphereRadius * multiplier;
                    sphere.center = cache.sphereCenter + Vector3.forward * finisherColliderForwardCenterOffset * (multiplier - 1f);
                }
                else
                {
                    col.transform.localScale = cache.localScale * multiplier;
                }
            }
        }

        private void RestoreColliderScale(WeaponHitbox hitbox)
        {
            Collider[] colliders = GetEffectiveColliders(hitbox, hitbox == swordHitbox ? swordAttackColliders : daggerAttackColliders);
            RestoreColliderScale(colliders);
        }

        private void RestoreColliderScale(Collider[] colliders)
        {
            if (colliders == null) return;

            foreach (Collider col in colliders)
            {
                if (col == null) continue;
                if (!colliderCache.TryGetValue(col, out ColliderCache cache)) continue;

                col.transform.localScale = cache.localScale;

                if (col is BoxCollider box)
                {
                    box.size = cache.boxSize;
                    box.center = cache.boxCenter;
                }
                else if (col is CapsuleCollider capsule)
                {
                    capsule.radius = cache.capsuleRadius;
                    capsule.height = cache.capsuleHeight;
                    capsule.center = cache.capsuleCenter;
                }
                else if (col is SphereCollider sphere)
                {
                    sphere.radius = cache.sphereRadius;
                    sphere.center = cache.sphereCenter;
                }
            }
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
                RestoreColliderScale(daggerHitbox);
            }

            if (swordHitbox != null)
            {
                swordHitbox.DisableHitbox();
                RestoreColliderScale(swordHitbox);
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
            ApplyChargeEffectTransforms();
            chargeEffectReplayTimer = 0f;

            if (chargeEffect != null)
            {
                PrepareChargeEffectLoopSettings();
                ReplayChargeEffect();
            }

            if (chargeReadyEffect != null)
            {
                chargeReadyEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            PlaySfx(chargeStartSfx);
            StartChargeLoopSfx();
        }

        private void KeepChargeEffectAlive()
        {
            if (!keepChargeEffectLoopingWhileCharging) return;
            if (!isChargingHeavy) return;
            if (chargeEffect == null) return;

            ApplyChargeEffectTransforms();
            PrepareChargeEffectLoopSettings();

            if (replayChargeEffectByInterval)
            {
                chargeEffectReplayTimer -= Time.deltaTime;

                if (chargeEffectReplayTimer <= 0f)
                {
                    ReplayChargeEffect();
                    chargeEffectReplayTimer = Mathf.Max(0.05f, chargeEffectReplayInterval);
                }

                return;
            }

            if (!IsAnyParticleSystemPlaying(chargeEffect))
            {
                chargeEffect.Play(true);
            }
        }

        private void PrepareChargeEffectLoopSettings()
        {
            if (chargeEffect == null) return;

            ParticleSystem[] systems = forceChargeEffectChildrenLoop
                ? chargeEffect.GetComponentsInChildren<ParticleSystem>(true)
                : new ParticleSystem[] { chargeEffect };

            foreach (ParticleSystem ps in systems)
            {
                if (ps == null) continue;

                ParticleSystem.MainModule main = ps.main;
                main.loop = !replayChargeEffectByInterval;
                main.playOnAwake = false;
            }
        }

        private void ReplayChargeEffect()
        {
            if (chargeEffect == null) return;

            chargeEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            chargeEffect.Play(true);
        }

        private bool IsAnyParticleSystemPlaying(ParticleSystem root)
        {
            if (root == null) return false;

            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem ps in systems)
            {
                if (ps != null && ps.isPlaying)
                {
                    return true;
                }
            }

            return false;
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

            StopChargeLoopSfx();

            chargeReadyEffectPlayed = false;
            maxChargeEffectPlayed = false;
        }

        private void ApplyChargeEffectTransforms()
        {
            if (chargeEffect != null)
            {
                chargeEffect.transform.SetParent(transform, false);
                chargeEffect.transform.localPosition = chargeEffectLocalPosition;
                chargeEffect.transform.localRotation = Quaternion.Euler(chargeEffectLocalEuler);
                chargeEffect.transform.localScale = chargeEffectLocalScale == Vector3.zero ? Vector3.one : chargeEffectLocalScale;
            }

            if (chargeReadyEffect != null)
            {
                chargeReadyEffect.transform.SetParent(transform, false);
                chargeReadyEffect.transform.localPosition = chargeReadyEffectLocalPosition;
                chargeReadyEffect.transform.localRotation = Quaternion.Euler(chargeReadyEffectLocalEuler);
                chargeReadyEffect.transform.localScale = chargeReadyEffectLocalScale == Vector3.zero ? Vector3.one : chargeReadyEffectLocalScale;
            }
        }

        private void StartChargeLoopSfx()
        {
            StopChargeLoopSfx();

            if (chargeLoopSfx == null) return;
            if (chargeLoopSfxInterval <= 0f) return;

            chargeLoopSfxRoutine = StartCoroutine(ChargeLoopSfxRoutine());
        }

        private void StopChargeLoopSfx()
        {
            if (chargeLoopSfxRoutine != null)
            {
                StopCoroutine(chargeLoopSfxRoutine);
                chargeLoopSfxRoutine = null;
            }
        }

        private IEnumerator ChargeLoopSfxRoutine()
        {
            while (isChargingHeavy)
            {
                PlaySfx(chargeLoopSfx);
                yield return new WaitForSeconds(Mathf.Max(0.05f, chargeLoopSfxInterval));
            }

            chargeLoopSfxRoutine = null;
        }

        private void CreateChargeEffectsIfNeeded()
        {
            if (chargeEffect == null)
            {
                GameObject chargeObj = new GameObject("Auto_ChargeEffect");
                chargeObj.transform.SetParent(transform);
                chargeObj.transform.localPosition = chargeEffectLocalPosition;
                chargeObj.transform.localRotation = Quaternion.Euler(chargeEffectLocalEuler);
                chargeObj.transform.localScale = chargeEffectLocalScale == Vector3.zero ? Vector3.one : chargeEffectLocalScale;

                chargeEffect = chargeObj.AddComponent<ParticleSystem>();
                SetupChargeParticle(chargeEffect);
            }

            if (chargeReadyEffect == null)
            {
                GameObject readyObj = new GameObject("Auto_ChargeReadyEffect");
                readyObj.transform.SetParent(transform);
                readyObj.transform.localPosition = chargeReadyEffectLocalPosition;
                readyObj.transform.localRotation = Quaternion.Euler(chargeReadyEffectLocalEuler);
                readyObj.transform.localScale = chargeReadyEffectLocalScale == Vector3.zero ? Vector3.one : chargeReadyEffectLocalScale;

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
            if (daggerHitbox != null)
            {
                daggerHitbox.DisableHitbox();
                RestoreColliderScale(daggerHitbox);
            }
        }

        public void StartSwordHit()
        {
            if (swordHitbox != null) swordHitbox.EnableHitbox(1f);
        }

        public void EndSwordHit()
        {
            if (swordHitbox != null)
            {
                swordHitbox.DisableHitbox();
                RestoreColliderScale(swordHitbox);
            }
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
