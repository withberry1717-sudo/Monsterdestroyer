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
            HeavyLight,
            ChargedHeavyDone,
            ChargedLight1,
            ChargedLight2,
            ChargedHeavy1,
            ChargedHeavyLight
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

        [Header("Attack Forward Movement")]
        [SerializeField] private bool useAttackForwardMove = true;

        [SerializeField] private float light1ForwardDistance = 0.35f;
        [SerializeField] private float light2ForwardDistance = 0.45f;
        [SerializeField] private float heavyComboLightForwardDistance = 0.42f;
        [SerializeField] private float dashAttackForwardDistance = 0.35f;
        [SerializeField] private float quickHeavyForwardDistance = 0.55f;
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

        [Tooltip("強攻撃単押しを連打できないようにするクールタイム。短め")]
        [SerializeField] private float neutralHeavyCooldown = 0.18f;

        [Header("Dash Attack")]
        [SerializeField] private float dashAttackStart = 0.08f;
        [SerializeField] private float dashAttackEnd = 0.34f;
        [SerializeField] private float dashAttackDamageMultiplier = 1.0f;

        [Header("Quick Heavy / 右クリック単押し")]
        [Tooltip("この秒数以内に右クリックを離したら、溜めではなく強単発にする")]
        [SerializeField] private float heavyTapTime = 0.18f;

        [Tooltip("この秒数以上右クリックを押しっぱなしにしたら、溜め開始")]
        [SerializeField] private float heavyHoldToStartChargeTime = 0.22f;

        [SerializeField] private float quickHeavyAnimationStartNormalized = 0.28f;
        [SerializeField] private float quickHeavyAnimationSpeed = 1.35f;
        [SerializeField] private float quickHeavyStart = 0.08f;
        [SerializeField] private float quickHeavyEnd = 0.40f;

        [Tooltip("単押し強。基本倍率は上げない")]
        [SerializeField] private float quickHeavyDamageMultiplier = 1.0f;

        [Tooltip("単発強の硬直。短めにして強弱強がつながりやすくする")]
        [SerializeField] private float quickHeavyEndDelay = 0.55f;

        [Header("Heavy Combo Finisher")]
        [SerializeField] private float heavyFinisherAnimationStartNormalized = 0.18f;
        [SerializeField] private float heavyFinisherAnimationSpeed = 1.25f;
        [SerializeField] private float heavyFinisherStart = 0.08f;
        [SerializeField] private float heavyFinisherEnd = 0.46f;
        [SerializeField] private float heavyFinisherEndDelay = 0.70f;

        [Tooltip("弱弱強の締め。少しだけ強い")]
        [SerializeField] private float weakWeakHeavyFinisherMultiplier = 1.20f;

        [Tooltip("強弱強の締め。強弱強は当てやすいので少し抑える")]
        [SerializeField] private float heavyWeakHeavyFinisherMultiplier = 1.15f;

        [Tooltip("溜め強→弱弱強の締め")]
        [SerializeField] private float chargedWeakWeakHeavyFinisherMultiplier = 1.25f;

        [Tooltip("溜め強→強弱強の締め。強弱強ルートなので少し抑えめ")]
        [SerializeField] private float chargedHeavyWeakHeavyFinisherMultiplier = 1.20f;

        [Header("Charge Heavy / 右クリック長押し")]
        [Tooltip("この秒数以上押してから離すと、溜め強として扱う")]
        [SerializeField] private float chargeRequiredTime = 0.45f;

        [Tooltip("最大溜めになるまでの時間。最大後もボタンを離すまで溜め状態を維持できる")]
        [SerializeField] private float maxChargeTime = 1.5f;

        [SerializeField] private float chargeHoldNormalizedTime = 0.27f;
        [SerializeField] private float chargeAnimationSlowSpeed = 0.32f;

        [SerializeField] private float chargedAttackStartDelay = 0.12f;
        [SerializeField] private float chargedAttackEndDelay = 0.55f;

        [Tooltip("溜め強本体。倍率は高すぎないようにする")]
        [SerializeField] private float chargedHeavyMinMultiplier = 1.15f;

        [Tooltip("最大溜め強本体")]
        [SerializeField] private float chargedHeavyMaxMultiplier = 1.45f;

        [SerializeField] private float chargedHeavyEndDelay = 0.65f;

        [Header("Charge Movement")]
        [SerializeField] private bool allowTurnWhileCharging = true;
        [SerializeField] private float chargeTurnMultiplier = 0.85f;

        [SerializeField] private bool allowMoveWhileCharging = true;
        [SerializeField] private float chargeMoveMultiplier = 0.22f;

        [Header("Charge Blink")]
        [Tooltip("ONなら溜め中でもブリンクできる")]
        [SerializeField] private bool allowBlinkWhileCharging = true;

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
        private float chargeTimer = 0f;

        private bool isComboCooldown = false;
        private bool isNeutralHeavyCooldown = false;

        private Coroutine currentAttackRoutine;
        private Coroutine comboCooldownRoutine;
        private Coroutine neutralHeavyCooldownRoutine;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _playerInput = GetComponent<PlayerInput>();
            _movement = GetComponent<Movement>();

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

                return;
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
            chargeTimer = Mathf.Max(0f, initialChargeTime);
            AttackInProgress = true;
            chargeReadyEffectPlayed = false;

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
                        allowBlinkWhileCharging
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
            if (chargeTimer < maxChargeTime)
            {
                chargeTimer += Time.deltaTime;

                if (chargeTimer > maxChargeTime)
                {
                    chargeTimer = maxChargeTime;
                }
            }

            if (!chargeReadyEffectPlayed && chargeTimer >= chargeRequiredTime)
            {
                chargeReadyEffectPlayed = true;

                if (chargeReadyEffect != null)
                {
                    chargeReadyEffect.Play();
                }

                Debug.Log("溜め完了！");
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
                bool isCharged = chargeTimer >= chargeRequiredTime;

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
                    StartChargedHeavyAttack(chargeTimer);
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

            currentAttackRoutine = StartCoroutine(
                AttackRoutine(
                    daggerHitbox,
                    start,
                    end,
                    multiplier,
                    attackEndDelay,
                    1f,
                    false,
                    false
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

            currentAttackRoutine = StartCoroutine(
                AttackRoutine(
                    daggerHitbox,
                    dashAttackStart,
                    dashAttackEnd,
                    dashAttackDamageMultiplier,
                    attackEndDelay,
                    1f,
                    true,
                    false
                )
            );
        }

        private void StartQuickHeavyAttackAsNeutral()
        {
            comboStage = ComboStage.Heavy1;
            RefreshComboTimer();

            Debug.Log("強攻撃 単押し / 強弱強の1段目");

            if (_movement != null)
            {
                _movement.isAttacking = true;
                _movement.canMoveWhileAttacking = false;
                StartAttackForwardMove(quickHeavyForwardDistance);
            }

            PlayAttackAnimation(abilityStateName, quickHeavyAnimationStartNormalized);

            currentAttackRoutine = StartCoroutine(
                AttackRoutine(
                    swordHitbox,
                    quickHeavyStart,
                    quickHeavyEnd,
                    quickHeavyDamageMultiplier,
                    quickHeavyEndDelay,
                    quickHeavyAnimationSpeed,
                    false,
                    true
                )
            );
        }

        private void StartHeavyFromComboInput()
        {
            switch (comboStage)
            {
                case ComboStage.Light2:
                    StartHeavyFinisher("弱弱強", weakWeakHeavyFinisherMultiplier);
                    break;

                case ComboStage.HeavyLight:
                    StartHeavyFinisher("強弱強", heavyWeakHeavyFinisherMultiplier);
                    break;

                case ComboStage.ChargedHeavyDone:
                    StartQuickHeavyAfterCharged();
                    break;

                case ComboStage.ChargedLight2:
                    StartHeavyFinisher("溜め強→弱弱強", chargedWeakWeakHeavyFinisherMultiplier);
                    break;

                case ComboStage.ChargedHeavyLight:
                    StartHeavyFinisher("溜め強→強弱強", chargedHeavyWeakHeavyFinisherMultiplier);
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
                StartAttackForwardMove(quickHeavyForwardDistance);
            }

            PlayAttackAnimation(abilityStateName, quickHeavyAnimationStartNormalized);

            currentAttackRoutine = StartCoroutine(
                AttackRoutine(
                    swordHitbox,
                    quickHeavyStart,
                    quickHeavyEnd,
                    quickHeavyDamageMultiplier,
                    quickHeavyEndDelay,
                    quickHeavyAnimationSpeed,
                    false,
                    false
                )
            );
        }

        private void StartHeavyFinisher(string comboName, float damageMultiplier)
        {
            Debug.Log("コンボ締め: " + comboName);

            RefreshComboTimer();

            if (_movement != null)
            {
                _movement.isAttacking = true;
                _movement.canMoveWhileAttacking = false;
                StartAttackForwardMove(comboHeavyFinisherForwardDistance);
            }

            PlayAttackAnimation(abilityStateName, heavyFinisherAnimationStartNormalized);

            currentAttackRoutine = StartCoroutine(
                AttackRoutine(
                    swordHitbox,
                    heavyFinisherStart,
                    heavyFinisherEnd,
                    damageMultiplier,
                    heavyFinisherEndDelay,
                    heavyFinisherAnimationSpeed,
                    true,
                    true
                )
            );
        }

        private void StartChargedHeavyAttack(float finalChargeTime)
        {
            Debug.Log("溜め強攻撃 Charge: " + finalChargeTime);

            comboStage = ComboStage.ChargedHeavyDone;
            RefreshComboTimer();

            if (_movement != null)
            {
                _movement.isAttacking = true;
                _movement.canMoveWhileAttacking = false;
                _movement.SetAllowBlinkWhileAttacking(false);
                StartAttackForwardMove(chargedHeavyForwardDistance);
            }

            float chargeRate = Mathf.Clamp01(finalChargeTime / maxChargeTime);
            float multiplier = Mathf.Lerp(chargedHeavyMinMultiplier, chargedHeavyMaxMultiplier, chargeRate);

            PlayAttackAnimation(abilityStateName, chargeHoldNormalizedTime);

            currentAttackRoutine = StartCoroutine(
                AttackRoutine(
                    swordHitbox,
                    chargedAttackStartDelay,
                    chargedAttackEndDelay,
                    multiplier,
                    chargedHeavyEndDelay,
                    1f,
                    false,
                    false
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
            float endDelay,
            float animationSpeed,
            bool finishComboAfterEnd,
            bool startNeutralHeavyCooldownAfterEnd
        )
        {
            AttackInProgress = true;

            if (_movement != null)
            {
                _movement.isAttacking = true;
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

            yield return new WaitForSeconds(hitStart);

            Debug.Log("判定ON / 倍率: " + damageMultiplier);
            hitbox.EnableHitbox(damageMultiplier);

            yield return new WaitForSeconds(Mathf.Max(0.01f, hitEnd - hitStart));

            Debug.Log("判定OFF");
            hitbox.DisableHitbox();

            yield return new WaitForSeconds(Mathf.Max(0f, endDelay - hitEnd));

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

        private void EndAttack()
        {
            AttackInProgress = false;
            isChargingHeavy = false;
            isWaitingHeavyDecision = false;
            heavyDecisionTimer = 0f;

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
                return;
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