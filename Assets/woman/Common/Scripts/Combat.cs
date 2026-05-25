using UnityEngine;
using NaughtyCharacter;
using System.Collections;

namespace Retro.ThirdPersonCharacter
{
    [RequireComponent(typeof(PlayerInput), typeof(Animator), typeof(Movement))]
    public class Combat : MonoBehaviour
    {
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

        [Header("Light Attack")]
        [SerializeField] private float light1Start = 0.12f;
        [SerializeField] private float light1End = 0.36f;
        [SerializeField] private float light1DamageMultiplier = 1.0f;

        [SerializeField] private float light2Start = 0.10f;
        [SerializeField] private float light2End = 0.38f;
        [SerializeField] private float light2DamageMultiplier = 1.15f;

        [Header("Light Combo Cooldown")]
        [SerializeField] private float lightComboFinishCooldown = 0.45f;

        [Header("Dash Attack")]
        [SerializeField] private float dashAttackStart = 0.08f;
        [SerializeField] private float dashAttackEnd = 0.34f;
        [SerializeField] private float dashAttackDamageMultiplier = 1.35f;

        [Header("Quick Heavy / 右クリック単押し")]
        [SerializeField] private float quickHeavyAnimationStartNormalized = 0.28f;
        [SerializeField] private float quickHeavyAnimationSpeed = 1.4f;
        [SerializeField] private float quickHeavyStart = 0.10f;
        [SerializeField] private float quickHeavyEnd = 0.45f;
        [SerializeField] private float quickHeavyDamageMultiplier = 1.35f;
        [SerializeField] private float quickHeavyEndDelay = 0.75f;

        [Header("Charge Heavy / 右クリック長押し")]
        [SerializeField] private float chargeRequiredTime = 0.45f;

        [Tooltip("Abilityアニメーションの0.9秒地点あたり。Abilityが3.33秒なら0.27前後")]
        [SerializeField] private float chargeHoldNormalizedTime = 0.27f;

        [Tooltip("長押し中のAbility再生速度。小さいほどスロー")]
        [SerializeField] private float chargeAnimationSlowSpeed = 0.32f;

        [SerializeField] private float maxChargeTime = 1.5f;
        [SerializeField] private float chargedAttackStartDelay = 0.12f;
        [SerializeField] private float chargedAttackEndDelay = 0.55f;
        [SerializeField] private float chargedHeavyMinMultiplier = 1.6f;
        [SerializeField] private float chargedHeavyMaxMultiplier = 2.5f;
        [SerializeField] private float chargedHeavyEndDelay = 0.9f;

        [Header("Combo")]
        [SerializeField] private float comboResetTime = 1.0f;
        [SerializeField] private float comboInputBufferTime = 0.35f;
        [SerializeField] private float attackEndDelay = 0.65f;

        private int lightComboStep = 0;
        private float comboTimer = 0f;

        private bool bufferedLight = false;
        private bool bufferedHeavy = false;
        private float bufferTimer = 0f;

        private bool isChargingHeavy = false;
        private float chargeTimer = 0f;

        private bool isLightComboCooldown = false;
        private bool currentAttackBlocksLightBuffer = false;

        private Coroutine currentAttackRoutine;
        private Coroutine lightCooldownRoutine;

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
                if (isLightComboCooldown)
                {
                    Debug.Log("弱攻撃クールダウン中");
                    return;
                }

                if (_movement != null && _movement.CanDashAttack)
                {
                    StartDashAttack();
                }
                else
                {
                    StartLightAttack();
                }

                return;
            }

            if (_playerInput.SpecialAttackInput)
            {
                StartHeavyCharge();
                return;
            }
        }

        private void UpdateComboTimer()
        {
            if (lightComboStep <= 0) return;

            comboTimer -= Time.deltaTime;

            if (comboTimer <= 0f)
            {
                lightComboStep = 0;
            }
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
                if (!currentAttackBlocksLightBuffer && !isLightComboCooldown)
                {
                    bufferedLight = true;
                    bufferedHeavy = false;
                    bufferTimer = comboInputBufferTime;
                }
            }

            if (_playerInput.SpecialAttackInput || _playerInput.SpecialAttackReleased)
            {
                bufferedHeavy = true;
                bufferedLight = false;
                bufferTimer = comboInputBufferTime;
            }
        }

        private void StartHeavyCharge()
        {
            isChargingHeavy = true;
            chargeTimer = 0f;
            AttackInProgress = true;
            currentAttackBlocksLightBuffer = false;
            chargeReadyEffectPlayed = false;

            StartChargeEffect();

            if (_movement != null)
            {
                _movement.isAttacking = true;
            }

            if (_animator != null)
            {
                _animator.speed = chargeAnimationSlowSpeed;
                _animator.Play(abilityStateName, 0, 0f);
            }

            Debug.Log("強攻撃 溜め開始");
        }

        private void UpdateHeavyCharge()
        {
            chargeTimer += Time.deltaTime;

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
                    StartQuickHeavyAttack();
                }
            }
        }

        private void StartLightAttack()
        {
            lightComboStep++;

            if (lightComboStep > 2)
            {
                lightComboStep = 1;
            }

            comboTimer = comboResetTime;

            bool isSecondLight = lightComboStep == 2;

            float start = isSecondLight ? light2Start : light1Start;
            float end = isSecondLight ? light2End : light1End;
            float multiplier = isSecondLight ? light2DamageMultiplier : light1DamageMultiplier;

            currentAttackBlocksLightBuffer = isSecondLight;

            Debug.Log("弱攻撃 " + lightComboStep + "段目");

            PlayAttackAnimation(attackStateName, 0f);

            currentAttackRoutine = StartCoroutine(
                AttackRoutine(
                    daggerHitbox,
                    start,
                    end,
                    multiplier,
                    attackEndDelay,
                    1f,
                    isSecondLight
                )
            );
        }

        private void StartDashAttack()
        {
            lightComboStep = 0;
            comboTimer = 0f;
            currentAttackBlocksLightBuffer = false;

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
                    false
                )
            );
        }

        private void StartQuickHeavyAttack()
        {
            Debug.Log("強攻撃 単押し");

            lightComboStep = 0;
            comboTimer = 0f;
            currentAttackBlocksLightBuffer = false;

            PlayAttackAnimation(abilityStateName, quickHeavyAnimationStartNormalized);

            currentAttackRoutine = StartCoroutine(
                AttackRoutine(
                    swordHitbox,
                    quickHeavyStart,
                    quickHeavyEnd,
                    quickHeavyDamageMultiplier,
                    quickHeavyEndDelay,
                    quickHeavyAnimationSpeed,
                    false
                )
            );
        }

        private void StartChargedHeavyAttack(float chargeTime)
        {
            Debug.Log("溜め強攻撃 Charge: " + chargeTime);

            lightComboStep = 0;
            comboTimer = 0f;
            currentAttackBlocksLightBuffer = false;

            float chargeRate = Mathf.Clamp01(chargeTime / maxChargeTime);
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
                    false
                )
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
            bool startLightCooldownAfterEnd
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

            Debug.Log("判定ON");
            hitbox.EnableHitbox(damageMultiplier);

            yield return new WaitForSeconds(Mathf.Max(0.01f, hitEnd - hitStart));

            Debug.Log("判定OFF");
            hitbox.DisableHitbox();

            yield return new WaitForSeconds(Mathf.Max(0f, endDelay - hitEnd));

            EndAttack();

            if (startLightCooldownAfterEnd)
            {
                bufferedLight = false;
                lightComboStep = 0;
                comboTimer = 0f;
                StartLightComboCooldown();
            }

            TryConsumeBuffer();
        }

        private void EndAttack()
        {
            AttackInProgress = false;
            isChargingHeavy = false;
            currentAttackBlocksLightBuffer = false;

            StopChargeEffects();

            if (_animator != null)
            {
                _animator.speed = 1f;
            }

            if (_movement != null)
            {
                _movement.isAttacking = false;
            }

            if (daggerHitbox != null)
            {
                daggerHitbox.DisableHitbox();
            }

            if (swordHitbox != null)
            {
                swordHitbox.DisableHitbox();
            }
        }

        private void StartLightComboCooldown()
        {
            if (lightCooldownRoutine != null)
            {
                StopCoroutine(lightCooldownRoutine);
            }

            lightCooldownRoutine = StartCoroutine(LightComboCooldownRoutine());
        }

        private IEnumerator LightComboCooldownRoutine()
        {
            isLightComboCooldown = true;

            yield return new WaitForSeconds(lightComboFinishCooldown);

            isLightComboCooldown = false;
        }

        private void TryConsumeBuffer()
        {
            if (bufferedLight && !isLightComboCooldown)
            {
                bufferedLight = false;
                bufferedHeavy = false;

                StartLightAttack();
                return;
            }

            if (bufferedHeavy)
            {
                bufferedLight = false;
                bufferedHeavy = false;

                StartQuickHeavyAttack();
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
                chargeEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (chargeReadyEffect != null)
            {
                chargeReadyEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
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
            if (daggerHitbox != null)
            {
                daggerHitbox.EnableHitbox(1f);
            }
        }

        public void EndDaggerHit()
        {
            if (daggerHitbox != null)
            {
                daggerHitbox.DisableHitbox();
            }
        }

        public void StartSwordHit()
        {
            if (swordHitbox != null)
            {
                swordHitbox.EnableHitbox(1f);
            }
        }

        public void EndSwordHit()
        {
            if (swordHitbox != null)
            {
                swordHitbox.DisableHitbox();
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
            }
        }

        public void SetAbilityEnd()
        {
            // コルーチン側で終了管理
        }
    }
}