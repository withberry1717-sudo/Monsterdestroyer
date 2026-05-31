using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NaughtyCharacter;

namespace Retro.ThirdPersonCharacter
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Combat))]
    [RequireComponent(typeof(CharacterController))]
    public class Movement : MonoBehaviour
    {
        private Animator _animator; private PlayerInput _playerInput; private Combat _combat; private CharacterController _characterController; private TrailRenderer _trailRenderer;

        [Header("Dash Effect")]
        [Tooltip("回避専用のParticleがある場合だけ入れる。chargeEffect / chargeReadyEffect は絶対に入れない")]
        [SerializeField] private ParticleSystem dashParticleSystem;

        [Header("Blink Audio")]
        [Tooltip("ブリンク音を再生するAudioSource。空ならPlayer自身か子から自動取得します。")]
        [SerializeField] private AudioSource blinkAudioSource;

        [Range(0f, 1f)]
        [Tooltip("ブリンク関連SEの音量です。")]
        [SerializeField] private float blinkSfxVolume = 1f;

        [Tooltip("ブリンク開始時に鳴らすSEです。")]
        [SerializeField] private AudioClip blinkStartSfx;

        [Tooltip("ブリンク終了時に鳴らすSEです。空なら鳴りません。")]
        [SerializeField] private AudioClip blinkEndSfx;

        [Tooltip("ブリンクゲージが1つ回復した時に鳴らすSEです。空なら鳴りません。")]
        [SerializeField] private AudioClip blinkRecoverSfx;

        [Header("Blink Audio Timing")]
        [Tooltip("ブリンク開始SEを何秒遅らせて鳴らすか。")]
        [SerializeField] private float blinkStartSfxDelay = 0f;

        [Tooltip("ブリンク終了SEを何秒遅らせて鳴らすか。")]
        [SerializeField] private float blinkEndSfxDelay = 0f;

        [Tooltip("ブリンク回復SEを何秒遅らせて鳴らすか。")]
        [SerializeField] private float blinkRecoverSfxDelay = 0f;

        [Header("Blink Particle Timing")]
        [Tooltip("ブリンクParticleを何秒遅らせて再生するか。0ならブリンク開始と同時です。")]
        [SerializeField] private float blinkParticleDelay = 0f;

        [SerializeField] private Transform _cameraTransform;

        private Vector2 lastMovementInput;
        private Vector3 moveDirection = Vector3.zero;

        public bool isAttacking = false;

        [Header("Attack Movement")]
        public bool canMoveWhileAttacking = false;
        [SerializeField] private float attackMoveSpeedMultiplier = 0.35f;
        [SerializeField] private float attackTurnSpeedMultiplier = 0.7f;

        [Header("Movement Settings")]
        public float gravity = 10;
        public float jumpSpeed = 4;
        public float MaxSpeed = 10;
        public float airControl = 15.0f;
        public float rotationSpeed = 15.0f;
        public float airRotationSpeed = 3.0f;

        [Header("Rotation Fix / 通常移動の向き修正")]
        [Tooltip("ON推奨。カメラのY角だけを使って移動方向を作ります。真後ろ入力時に斜めへブレる問題を防ぎます。")]
        [SerializeField] private bool useCameraYawOnlyForMoveDirection = true;

        [Tooltip("ON推奨。SlerpではなくRotateTowardsで一定角速度で回します。180度反転時のプルプルを防ぎます。")]
        [SerializeField] private bool useStableRotateTowards = true;

        [Tooltip("この角度以上の方向転換は旋回速度を上げます。真後ろ入力対策です。")]
        [SerializeField] private float backTurnBoostAngle = 135f;

        [Tooltip("真後ろなど大きく向きを変える時の旋回速度倍率です。大きいほどすぐ後ろを向きます。")]
        [SerializeField] private float backTurnSpeedMultiplier = 2.2f;

        [Tooltip("目標角度との差がこの角度以下ならピタッと合わせます。小刻みな揺れ防止です。")]
        [SerializeField] private float rotationSnapAngle = 1.5f;

        [Tooltip("この入力未満なら向き変更しません。スティックやAxisの微振動対策です。キーボードなら0.05〜0.15推奨。")]
        [SerializeField] private float moveInputDeadZone = 0.08f;

        private float DecelerationOnStop = 0.00f;

        [Header("Input Settings")]
        public KeyCode dashKey = KeyCode.LeftShift;
        public KeyCode jumpKey = KeyCode.Space;

        [Header("Dash Settings")]
        public float dashSpeed = 30f;
        public float dashTime = 0.2f;

        [Header("Dash Attack")]
        [SerializeField] private float dashAttackInputWindow = 0.25f;

        [Header("Blink Charge Settings")]
        [SerializeField] private int maxBlinkCharges = 2;
        [SerializeField] private float blinkRecoverTime = 1.5f;
        [SerializeField] private float blinkRecoverDelay = 0.4f;

        [Header("Blink UI")]
        [SerializeField] private Image blinkSlot1Fill;
        [SerializeField] private Image blinkSlot2Fill;

        [Header("Charge Blink")]
        [Tooltip("Combat側からONにされた時だけ、攻撃中でもブリンクを許可する")]
        [SerializeField] private bool allowBlinkWhileAttacking = false;

        [Tooltip("溜め中ブリンク距離倍率。0.5なら通常の半分")]
        [SerializeField] private float attackBlinkDistanceMultiplier = 0.5f;

        [Header("Dragon Stagger")]
        [SerializeField] private bool canBeStaggeredByDragon = true;
        [SerializeField] private float staggerStopPower = 0.15f;
        [SerializeField] private string staggerAnimatorTrigger = "BigHit";
        [SerializeField] private bool stopAttackWhenStaggered = true;

        private int currentBlinkCharges;
        private float blinkRecoverTimer = 0f;
        private float blinkRecoverDelayTimer = 0f;

        private bool isDashing = false;
        private float dashAttackWindowTimer = 0f;

        private bool isDragonStaggered = false;
        private Coroutine dragonStaggerRoutine;
        private Coroutine attackForwardMoveRoutine;

        private bool chargeAttackMoveOverrideActive = false;
        private float defaultAttackMoveSpeedMultiplier;
        private float defaultAttackTurnSpeedMultiplier;

        public bool IsDashing => isDashing;
        public bool CanDashAttack => isDashing || dashAttackWindowTimer > 0f;
        public bool IsDragonStaggered => isDragonStaggered;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _playerInput = GetComponent<PlayerInput>();
            _combat = GetComponent<Combat>();
            _characterController = GetComponent<CharacterController>();

            InputSystem.settings.maxEventBytesPerUpdate = 0;

            _trailRenderer = GetComponentInChildren<TrailRenderer>();
            FindBlinkAudioSourceIfNeeded();

            ForceStopTrail();

            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }

            defaultAttackMoveSpeedMultiplier = attackMoveSpeedMultiplier;
            defaultAttackTurnSpeedMultiplier = attackTurnSpeedMultiplier;

            currentBlinkCharges = maxBlinkCharges;
            blinkRecoverTimer = 0f;
            blinkRecoverDelayTimer = 0f;

            string savedBlinkKey = PlayerPrefs.GetString("BlinkKey", dashKey.ToString());

            if (System.Enum.TryParse(savedBlinkKey, out KeyCode loadedKey))
            {
                dashKey = loadedKey;
            }

            UpdateBlinkUI();
        }

        private void OnDisable()
        {
            ForceStopTrail();
            StopAttackForwardMove();
            EndChargeAttackMove();
            SetAllowBlinkWhileAttacking(false);

            if (dragonStaggerRoutine != null)
            {
                StopCoroutine(dragonStaggerRoutine);
                dragonStaggerRoutine = null;
            }

            isDragonStaggered = false;
        }

        public void BeginChargeAttackMove(
            bool allowMove,
            bool allowTurn,
            float moveMultiplier,
            float turnMultiplier,
            bool allowBlink,
            float blinkDistanceMultiplier
        )
        {
            if (isDragonStaggered) return;

            if (!chargeAttackMoveOverrideActive)
            {
                defaultAttackMoveSpeedMultiplier = attackMoveSpeedMultiplier;
                defaultAttackTurnSpeedMultiplier = attackTurnSpeedMultiplier;
            }

            chargeAttackMoveOverrideActive = true;

            isAttacking = true;
            canMoveWhileAttacking = allowMove || allowTurn;

            attackMoveSpeedMultiplier = allowMove ? Mathf.Max(0f, moveMultiplier) : 0f;
            attackTurnSpeedMultiplier = allowTurn ? Mathf.Max(0f, turnMultiplier) : 0f;

            allowBlinkWhileAttacking = allowBlink;
            attackBlinkDistanceMultiplier = Mathf.Clamp(blinkDistanceMultiplier, 0.05f, 1f);
        }

        public void BeginChargeAttackMove(
            bool allowMove,
            bool allowTurn,
            float moveMultiplier,
            float turnMultiplier,
            bool allowBlink
        )
        {
            BeginChargeAttackMove(
                allowMove,
                allowTurn,
                moveMultiplier,
                turnMultiplier,
                allowBlink,
                attackBlinkDistanceMultiplier
            );
        }

        public void BeginChargeAttackMove(
            bool allowMove,
            bool allowTurn,
            float moveMultiplier,
            float turnMultiplier
        )
        {
            BeginChargeAttackMove(
                allowMove,
                allowTurn,
                moveMultiplier,
                turnMultiplier,
                false,
                attackBlinkDistanceMultiplier
            );
        }

        public void SetAllowBlinkWhileAttacking(bool allow)
        {
            allowBlinkWhileAttacking = allow;
        }

        public void EndChargeAttackMove()
        {
            if (!chargeAttackMoveOverrideActive)
            {
                allowBlinkWhileAttacking = false;
                return;
            }

            chargeAttackMoveOverrideActive = false;

            attackMoveSpeedMultiplier = defaultAttackMoveSpeedMultiplier;
            attackTurnSpeedMultiplier = defaultAttackTurnSpeedMultiplier;

            allowBlinkWhileAttacking = false;
            attackBlinkDistanceMultiplier = 0.5f;
        }

        public void ForceStopTrail()
        {
            if (_trailRenderer != null)
            {
                _trailRenderer.enabled = false;
                _trailRenderer.Clear();
            }

            if (dashParticleSystem != null)
            {
                dashParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            isDashing = false;
        }

        public void StartAttackForwardMove(
            float distance,
            float duration,
            float accelerationTime,
            float decelerationTime
        )
        {
            if (_characterController == null) return;
            if (distance <= 0f) return;
            if (duration <= 0f) return;
            if (isDragonStaggered) return;

            if (attackForwardMoveRoutine != null)
            {
                StopCoroutine(attackForwardMoveRoutine);
            }

            attackForwardMoveRoutine = StartCoroutine(
                AttackForwardMoveRoutine(distance, duration, accelerationTime, decelerationTime)
            );
        }

        public void StopAttackForwardMove()
        {
            if (attackForwardMoveRoutine != null)
            {
                StopCoroutine(attackForwardMoveRoutine);
                attackForwardMoveRoutine = null;
            }
        }

        private IEnumerator AttackForwardMoveRoutine(
            float distance,
            float duration,
            float accelerationTime,
            float decelerationTime
        )
        {
            float timer = 0f;
            float movedDistance = 0f;

            Vector3 forwardDirection = transform.forward;
            forwardDirection.y = 0f;
            forwardDirection.Normalize();

            float baseSpeed = distance / duration;

            while (timer < duration && movedDistance < distance)
            {
                if (_characterController == null)
                {
                    yield break;
                }

                if (isDragonStaggered)
                {
                    yield break;
                }

                float deltaTime = Time.deltaTime;
                timer += deltaTime;

                float accelMultiplier = 1f;
                float decelMultiplier = 1f;

                if (accelerationTime > 0f)
                {
                    accelMultiplier = Mathf.Clamp01(timer / accelerationTime);
                    accelMultiplier = Mathf.SmoothStep(0f, 1f, accelMultiplier);
                }

                if (decelerationTime > 0f)
                {
                    float timeUntilEnd = duration - timer;
                    decelMultiplier = Mathf.Clamp01(timeUntilEnd / decelerationTime);
                    decelMultiplier = Mathf.SmoothStep(0f, 1f, decelMultiplier);
                }

                float inertiaMultiplier = Mathf.Min(accelMultiplier, decelMultiplier);
                float moveAmount = baseSpeed * inertiaMultiplier * deltaTime;

                float remainingDistance = distance - movedDistance;

                if (moveAmount > remainingDistance)
                {
                    moveAmount = remainingDistance;
                }

                Vector3 move = forwardDirection * moveAmount;

                _characterController.Move(move);

                movedDistance += moveAmount;

                yield return null;
            }

            attackForwardMoveRoutine = null;
        }

        private void Update()
        {
            if (_animator == null) return;

            if (dashAttackWindowTimer > 0f)
            {
                dashAttackWindowTimer -= Time.deltaTime;
            }

            RecoverBlinkCharge();

            if (isDragonStaggered)
            {
                return;
            }

            if (Input.GetKeyDown(dashKey) && CanBlink())
            {
                Vector3 dashDir = transform.forward;

                if (moveDirection.x != 0 || moveDirection.z != 0)
                {
                    dashDir = new Vector3(moveDirection.x, 0, moveDirection.z).normalized;
                }

                UseBlink();
                StartCoroutine(DashCoroutine(dashDir));
            }

            if (!isDashing)
            {
                Move();
            }
        }

        private bool CanBlink()
        {
            bool canBlinkDuringAttack = isAttacking && allowBlinkWhileAttacking;

            return !isDashing
                && !isDragonStaggered
                && currentBlinkCharges > 0
                && (!isAttacking || canBlinkDuringAttack);
        }

        private void UseBlink()
        {
            currentBlinkCharges--;

            if (currentBlinkCharges < 0)
            {
                currentBlinkCharges = 0;
            }

            if (currentBlinkCharges < maxBlinkCharges)
            {
                blinkRecoverDelayTimer = blinkRecoverDelay;
                blinkRecoverTimer = blinkRecoverTime;
            }

            UpdateBlinkUI();

            Debug.Log("Blink Used. Current Charges: " + currentBlinkCharges);
        }

        private void RecoverBlinkCharge()
        {
            if (currentBlinkCharges >= maxBlinkCharges)
            {
                blinkRecoverTimer = 0f;
                blinkRecoverDelayTimer = 0f;
                UpdateBlinkUI();
                return;
            }

            if (blinkRecoverDelayTimer > 0f)
            {
                blinkRecoverDelayTimer -= Time.deltaTime;
                UpdateBlinkUI();
                return;
            }

            blinkRecoverTimer -= Time.deltaTime;

            if (blinkRecoverTimer <= 0f)
            {
                currentBlinkCharges++;
                PlayBlinkRecoverSfx();

                if (currentBlinkCharges < maxBlinkCharges)
                {
                    blinkRecoverTimer = blinkRecoverTime;
                    blinkRecoverDelayTimer = 0f;
                }
                else
                {
                    blinkRecoverTimer = 0f;
                    blinkRecoverDelayTimer = 0f;
                }
            }

            UpdateBlinkUI();
        }

        private void UpdateBlinkUI()
        {
            float slot1Amount = 0f;
            float slot2Amount = 0f;

            if (currentBlinkCharges >= 1)
            {
                slot1Amount = 1f;
            }

            if (currentBlinkCharges >= 2)
            {
                slot2Amount = 1f;
            }

            if (currentBlinkCharges < maxBlinkCharges && blinkRecoverTime > 0f)
            {
                float recoverProgress = 0f;

                if (blinkRecoverDelayTimer <= 0f)
                {
                    recoverProgress = 1f - Mathf.Clamp01(blinkRecoverTimer / blinkRecoverTime);
                }

                if (currentBlinkCharges == 0)
                {
                    slot1Amount = recoverProgress;
                    slot2Amount = 0f;
                }
                else if (currentBlinkCharges == 1)
                {
                    slot1Amount = 1f;
                    slot2Amount = recoverProgress;
                }
            }

            if (blinkSlot1Fill != null)
            {
                blinkSlot1Fill.fillAmount = slot1Amount;
            }

            if (blinkSlot2Fill != null)
            {
                blinkSlot2Fill.fillAmount = slot2Amount;
            }
        }

        private IEnumerator DashCoroutine(Vector3 dashDir)
        {
            isDashing = true;
            dashAttackWindowTimer = dashAttackInputWindow;

            float currentDashSpeed = dashSpeed;

            if (isAttacking && allowBlinkWhileAttacking)
            {
                currentDashSpeed *= attackBlinkDistanceMultiplier;
            }

            PlayBlinkStartSfx();

            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
                _trailRenderer.enabled = true;
            }

            PlayBlinkParticle();

            _animator.SetFloat("InputX", 0);
            _animator.SetFloat("InputY", 0);
            _animator.SetBool("IsInAir", true);

            if (SafePlayerCamera.Instance != null)
            {
                SafePlayerCamera.Instance.Shake(0.1f, 0.5f);
            }

            float startTime = Time.time;

            while (Time.time < startTime + dashTime)
            {
                if (isDragonStaggered)
                {
                    break;
                }

                _characterController.Move(dashDir * currentDashSpeed * Time.deltaTime);
                yield return null;
            }

            ForceStopTrail();
            PlayBlinkEndSfx();

            _animator.SetBool("IsInAir", false);

            if (!isDragonStaggered)
            {
                dashAttackWindowTimer = dashAttackInputWindow;
            }
            else
            {
                dashAttackWindowTimer = 0f;
            }
        }

        private void Move()
        {
            if (isDragonStaggered)
            {
                return;
            }

            float x = _playerInput.MovementInput.x;
            float y = _playerInput.MovementInput.y;

            bool grounded = _characterController.isGrounded;

            Vector2 rawInput = new Vector2(x, y);
            float inputMagnitude = Mathf.Clamp01(rawInput.magnitude);
            bool hasMoveInput = inputMagnitude > moveInputDeadZone;

            Vector3 desiredMoveDirection = Vector3.zero;

            if (hasMoveInput)
            {
                Vector3 localInputDirection = new Vector3(x, 0f, y);

                if (localInputDirection.sqrMagnitude > 1f)
                {
                    localInputDirection.Normalize();
                }

                if (_cameraTransform != null)
                {
                    if (useCameraYawOnlyForMoveDirection)
                    {
                        // カメラのforward/rightを毎回投影して使うと、真後ろ入力で微妙に斜めへブレることがある。
                        // Yawだけを使うと、S入力は必ずカメラ基準の真後ろ方向になる。
                        Quaternion cameraYaw = Quaternion.Euler(0f, _cameraTransform.eulerAngles.y, 0f);
                        desiredMoveDirection = cameraYaw * localInputDirection;
                    }
                    else
                    {
                        Vector3 camForward = _cameraTransform.forward;
                        Vector3 camRight = _cameraTransform.right;

                        camForward.y = 0f;
                        camRight.y = 0f;

                        camForward.Normalize();
                        camRight.Normalize();

                        desiredMoveDirection = camForward * y + camRight * x;
                    }
                }
                else
                {
                    desiredMoveDirection = localInputDirection;
                }

                desiredMoveDirection.y = 0f;

                if (desiredMoveDirection.sqrMagnitude > 1f)
                {
                    desiredMoveDirection.Normalize();
                }
            }

            Vector3 inputDirection = desiredMoveDirection * MaxSpeed;

            if (isAttacking)
            {
                if (canMoveWhileAttacking)
                {
                    inputDirection *= attackMoveSpeedMultiplier;

                    moveDirection.x = inputDirection.x;
                    moveDirection.z = inputDirection.z;
                }
                else
                {
                    inputDirection = Vector3.zero;

                    float deceleration = 10.0f;
                    moveDirection.x = Mathf.Lerp(moveDirection.x, 0f, deceleration * Time.deltaTime);
                    moveDirection.z = Mathf.Lerp(moveDirection.z, 0f, deceleration * Time.deltaTime);
                }
            }
            else if (grounded)
            {
                moveDirection.x = inputDirection.x;
                moveDirection.z = inputDirection.z;

                if (Input.GetKeyDown(jumpKey))
                {
                    moveDirection.y = jumpSpeed;
                }
            }
            else
            {
                if (hasMoveInput)
                {
                    moveDirection.x = Mathf.Lerp(moveDirection.x, inputDirection.x, airControl * Time.deltaTime);
                    moveDirection.z = Mathf.Lerp(moveDirection.z, inputDirection.z, airControl * Time.deltaTime);
                }
            }

            bool canTurnNow = !isAttacking || canMoveWhileAttacking;

            if (canTurnNow && hasMoveInput && desiredMoveDirection.sqrMagnitude > 0.0001f)
            {
                RotatePlayerToMoveDirection(desiredMoveDirection, grounded);
            }

            moveDirection.y -= gravity * Time.deltaTime;
            _characterController.Move(moveDirection * Time.deltaTime);

            if (isAttacking && canMoveWhileAttacking)
            {
                _animator.SetFloat("InputX", 0);
                _animator.SetFloat("InputY", inputMagnitude * 0.5f);
            }
            else
            {
                _animator.SetFloat("InputX", 0);
                _animator.SetFloat("InputY", isAttacking ? 0 : inputMagnitude);
            }

            _animator.SetBool("IsInAir", !grounded);
        }

        private void RotatePlayerToMoveDirection(Vector3 desiredMoveDirection, bool grounded)
        {
            desiredMoveDirection.y = 0f;

            if (desiredMoveDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(desiredMoveDirection.normalized, Vector3.up);

            float currentRotationSpeed = grounded ? rotationSpeed : airRotationSpeed;

            if (isAttacking && canMoveWhileAttacking)
            {
                currentRotationSpeed *= attackTurnSpeedMultiplier;
            }

            float angle = Quaternion.Angle(transform.rotation, targetRotation);

            if (angle >= backTurnBoostAngle)
            {
                currentRotationSpeed *= backTurnSpeedMultiplier;
            }

            if (angle <= rotationSnapAngle)
            {
                transform.rotation = targetRotation;
                return;
            }

            if (useStableRotateTowards)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    currentRotationSpeed * 90f * Time.deltaTime
                );
            }
            else
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    currentRotationSpeed * Time.deltaTime
                );
            }
        }

        private void FindBlinkAudioSourceIfNeeded()
        {
            if (blinkAudioSource != null) return;

            blinkAudioSource = GetComponent<AudioSource>();

            if (blinkAudioSource == null)
            {
                blinkAudioSource = GetComponentInChildren<AudioSource>();
            }

            if (blinkAudioSource == null)
            {
                blinkAudioSource = GetComponentInParent<AudioSource>();
            }
        }

        private void PlayBlinkStartSfx()
        {
            PlayBlinkSfx(blinkStartSfx, blinkStartSfxDelay);
        }

        private void PlayBlinkEndSfx()
        {
            PlayBlinkSfx(blinkEndSfx, blinkEndSfxDelay);
        }

        private void PlayBlinkRecoverSfx()
        {
            PlayBlinkSfx(blinkRecoverSfx, blinkRecoverSfxDelay);
        }

        private void PlayBlinkSfx(AudioClip clip, float delay)
        {
            if (clip == null) return;

            FindBlinkAudioSourceIfNeeded();

            if (blinkAudioSource == null)
            {
                Debug.LogWarning("[Movement] Blink AudioSourceがありません。PlayerにAudioSourceを追加してください。", this);
                return;
            }

            delay = Mathf.Max(0f, delay);

            if (delay > 0f)
            {
                StartCoroutine(PlayBlinkSfxDelayed(clip, delay));
            }
            else
            {
                blinkAudioSource.PlayOneShot(clip, blinkSfxVolume);
            }
        }

        private IEnumerator PlayBlinkSfxDelayed(AudioClip clip, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (clip == null) yield break;

            FindBlinkAudioSourceIfNeeded();

            if (blinkAudioSource != null)
            {
                blinkAudioSource.PlayOneShot(clip, blinkSfxVolume);
            }
        }

        private void PlayBlinkParticle()
        {
            if (dashParticleSystem == null) return;

            float delay = Mathf.Max(0f, blinkParticleDelay);

            if (delay > 0f)
            {
                StartCoroutine(PlayBlinkParticleDelayed(delay));
            }
            else
            {
                dashParticleSystem.Play(true);
            }
        }

        private IEnumerator PlayBlinkParticleDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (dashParticleSystem != null && isDashing)
            {
                dashParticleSystem.Play(true);
            }
        }

        public void DragonStagger(float time)
        {
            if (!canBeStaggeredByDragon) return;
            if (time <= 0f) return;

            if (dragonStaggerRoutine != null)
            {
                StopCoroutine(dragonStaggerRoutine);
            }

            dragonStaggerRoutine = StartCoroutine(DragonStaggerRoutine(time));
        }

        private IEnumerator DragonStaggerRoutine(float time)
        {
            isDragonStaggered = true;

            ForceStopTrail();
            StopAttackForwardMove();
            EndChargeAttackMove();
            SetAllowBlinkWhileAttacking(false);

            if (stopAttackWhenStaggered)
            {
                isAttacking = false;
                canMoveWhileAttacking = false;
            }

            isDashing = false;
            dashAttackWindowTimer = 0f;

            moveDirection.x *= staggerStopPower;
            moveDirection.z *= staggerStopPower;

            if (_animator != null)
            {
                _animator.SetFloat("InputX", 0f);
                _animator.SetFloat("InputY", 0f);
                _animator.SetBool("IsInAir", false);

                if (!string.IsNullOrEmpty(staggerAnimatorTrigger))
                {
                    _animator.SetTrigger(staggerAnimatorTrigger);
                }
            }

            yield return new WaitForSeconds(time);

            isDragonStaggered = false;
            dragonStaggerRoutine = null;
        }

        private void StopMovementOnAttack()
        {
            var temp = lastMovementInput;
            temp.x -= DecelerationOnStop;
            temp.y -= DecelerationOnStop;
            lastMovementInput = temp;

            _animator.SetFloat("InputX", lastMovementInput.x);
            _animator.SetFloat("InputY", lastMovementInput.y);
        }
    }

}