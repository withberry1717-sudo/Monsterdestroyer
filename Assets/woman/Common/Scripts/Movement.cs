using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Retro.ThirdPersonCharacter
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Combat))]
    [RequireComponent(typeof(CharacterController))]
    public class Movement : MonoBehaviour
    {
        private Animator _animator;
        private PlayerInput _playerInput;
        private Combat _combat;
        private CharacterController _characterController;
        private TrailRenderer _trailRenderer;
        private ParticleSystem _particleSystem;

        [SerializeField] private Transform _cameraTransform;

        private Vector2 lastMovementInput;
        private Vector3 moveDirection = Vector3.zero;

        public bool isAttacking = false;

        [Header("Movement Settings")]
        public float gravity = 10;
        public float jumpSpeed = 4;
        public float MaxSpeed = 10;
        public float airControl = 15.0f;
        public float rotationSpeed = 15.0f;
        public float airRotationSpeed = 3.0f;

        private float DecelerationOnStop = 0.00f;

        [Header("Input Settings")]
        public KeyCode dashKey = KeyCode.LeftShift;
        public KeyCode jumpKey = KeyCode.Space;

        [Header("Dash Settings")]
        public float dashSpeed = 30f;
        public float dashTime = 0.2f;

        [Header("Blink Charge Settings")]
        [SerializeField] private int maxBlinkCharges = 2;
        [SerializeField] private float blinkRecoverTime = 1.5f;

        [Header("Blink UI")]
        [SerializeField] private Image blinkSlot1Fill;
        [SerializeField] private Image blinkSlot2Fill;

        private int currentBlinkCharges;
        private float blinkRecoverTimer = 0f;
        private bool isDashing = false;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _playerInput = GetComponent<PlayerInput>();
            _combat = GetComponent<Combat>();
            _characterController = GetComponent<CharacterController>();

            InputSystem.settings.maxEventBytesPerUpdate = 0;

            _trailRenderer = GetComponentInChildren<TrailRenderer>();
            _particleSystem = GetComponentInChildren<ParticleSystem>();

            if (_trailRenderer != null)
            {
                _trailRenderer.enabled = false;
            }

            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }

            currentBlinkCharges = maxBlinkCharges;
            UpdateBlinkUI();
        }

        private void Update()
        {
            if (_animator == null) return;

            RecoverBlinkCharge();

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
            return !isDashing && !isAttacking && currentBlinkCharges > 0;
        }

        private void UseBlink()
        {
            currentBlinkCharges--;

            if (currentBlinkCharges < maxBlinkCharges && blinkRecoverTimer <= 0f)
            {
                blinkRecoverTimer = blinkRecoverTime;
            }

            UpdateBlinkUI();
        }

        private void RecoverBlinkCharge()
        {
            if (currentBlinkCharges >= maxBlinkCharges) return;

            blinkRecoverTimer -= Time.deltaTime;

            if (blinkRecoverTimer <= 0f)
            {
                currentBlinkCharges++;

                if (currentBlinkCharges < maxBlinkCharges)
                {
                    blinkRecoverTimer = blinkRecoverTime;
                }
                else
                {
                    blinkRecoverTimer = 0f;
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

            // 回復中のゲージをじわっと増やす
            if (currentBlinkCharges < maxBlinkCharges && blinkRecoverTime > 0f)
            {
                float recoverProgress = 1f - Mathf.Clamp01(blinkRecoverTimer / blinkRecoverTime);

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

            if (_trailRenderer != null) _trailRenderer.enabled = true;
            if (_particleSystem != null) _particleSystem.Play();

            _animator.SetFloat("InputX", 0);
            _animator.SetFloat("InputY", 0);
            _animator.SetBool("IsInAir", true);

            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.1f, 0.5f);

            float startTime = Time.time;

            while (Time.time < startTime + dashTime)
            {
                _characterController.Move(dashDir * dashSpeed * Time.deltaTime);
                yield return null;
            }

            if (_trailRenderer != null) _trailRenderer.enabled = false;

            _animator.SetBool("IsInAir", false);

            isDashing = false;
        }

        private void Move()
        {
            var x = _playerInput.MovementInput.x;
            var y = _playerInput.MovementInput.y;

            bool grounded = _characterController.isGrounded;

            Vector3 inputDirection = Vector3.zero;

            if (_cameraTransform != null)
            {
                Vector3 camForward = _cameraTransform.forward;
                Vector3 camRight = _cameraTransform.right;
                camForward.y = 0;
                camRight.y = 0;
                camForward.Normalize();
                camRight.Normalize();

                Vector3 targetDirection = camForward * y + camRight * x;
                if (targetDirection.magnitude > 1.0f)
                {
                    targetDirection.Normalize();
                }

                inputDirection = targetDirection * MaxSpeed;
            }
            else
            {
                Vector3 targetDirection = new Vector3(x, 0, y);
                if (targetDirection.magnitude > 1.0f)
                {
                    targetDirection.Normalize();
                }

                inputDirection = targetDirection * MaxSpeed;
            }

            if (isAttacking)
            {
                inputDirection = Vector3.zero;

                float deceleration = 10.0f;
                moveDirection.x = Mathf.Lerp(moveDirection.x, 0, deceleration * Time.deltaTime);
                moveDirection.z = Mathf.Lerp(moveDirection.z, 0, deceleration * Time.deltaTime);
            }
            else if (grounded)
            {
                moveDirection.x = inputDirection.x;
                moveDirection.z = inputDirection.z;

                if (Input.GetKeyDown(jumpKey))
                    moveDirection.y = jumpSpeed;
            }
            else
            {
                if (inputDirection.magnitude > 0.1f)
                {
                    moveDirection.x = Mathf.Lerp(moveDirection.x, inputDirection.x, airControl * Time.deltaTime);
                    moveDirection.z = Mathf.Lerp(moveDirection.z, inputDirection.z, airControl * Time.deltaTime);
                }
            }

            Vector3 lookDirection = new Vector3(moveDirection.x, 0, moveDirection.z);

            if (lookDirection != Vector3.zero && !isAttacking)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

                float currentRotationSpeed = grounded ? rotationSpeed : airRotationSpeed;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, currentRotationSpeed * Time.deltaTime);
            }

            moveDirection.y -= gravity * Time.deltaTime;
            _characterController.Move(moveDirection * Time.deltaTime);

            float inputMagnitude = new Vector2(x, y).magnitude;

            _animator.SetFloat("InputX", 0);
            _animator.SetFloat("InputY", isAttacking ? 0 : inputMagnitude);
            _animator.SetBool("IsInAir", !grounded);
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