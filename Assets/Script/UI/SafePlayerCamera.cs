using UnityEngine;
using System.Collections;

namespace NaughtyCharacter
{
    public class SafePlayerCamera : MonoBehaviour
    {
        public static SafePlayerCamera Instance;

        [Header("References")]
        public Transform Rig;
        public Transform Pivot;
        public Transform Target;
        public Camera Camera;

        [Header("Camera Feel")]
        [SerializeField] private float sensitivityX = 2.0f;
        [SerializeField] private float sensitivityY = 1.4f;
        [SerializeField] private float rotationSmoothTime = 0.06f;
        [SerializeField] private float followSmoothTime = 0.04f;

        [Header("Pitch Limit")]
        [SerializeField] private float minPitch = -25f;
        [SerializeField] private float maxPitch = 55f;

        [Header("Camera Shake")]
        [SerializeField] private bool enableCameraShake = true;
        [SerializeField] private Transform shakeTarget;
        [SerializeField] private float defaultShakeDuration = 0.25f;
        [SerializeField] private float defaultShakeStrength = 0.15f;

        private Vector2 currentRotation;
        private Vector2 targetRotation;
        private Vector2 rotationVelocity;
        private Vector3 followVelocity;

        private Vector3 originalShakeLocalPosition;
        private Coroutine shakeCoroutine;

        private void Awake()
        {
            Instance = this;

            if (Camera == null)
            {
                Camera = GetComponentInChildren<Camera>();
            }

            if (shakeTarget == null)
            {
                if (Camera != null)
                {
                    shakeTarget = Camera.transform;
                }
                else
                {
                    shakeTarget = transform;
                }
            }

            originalShakeLocalPosition = shakeTarget.localPosition;
        }

        private void LateUpdate()
        {
            if (Target == null || Rig == null || Pivot == null)
            {
                return;
            }

            Rig.position = Vector3.SmoothDamp(
                Rig.position,
                Target.position,
                ref followVelocity,
                followSmoothTime
            );

            // カーソルがロックされていない時は視点操作しない
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            Vector2 cameraInput = new Vector2(
                Input.GetAxis("Mouse X"),
                Input.GetAxis("Mouse Y")
            );

            targetRotation.y += cameraInput.x * sensitivityX;
            targetRotation.x -= cameraInput.y * sensitivityY;
            targetRotation.x = Mathf.Clamp(targetRotation.x, minPitch, maxPitch);

            currentRotation = Vector2.SmoothDamp(
                currentRotation,
                targetRotation,
                ref rotationVelocity,
                rotationSmoothTime
            );

            Rig.localRotation = Quaternion.Euler(0f, currentRotation.y, 0f);
            Pivot.localRotation = Quaternion.Euler(currentRotation.x, 0f, 0f);
        }

        public void Shake()
        {
            Shake(defaultShakeDuration, defaultShakeStrength);
        }

        public void Shake(float duration, float strength)
        {
            if (!enableCameraShake) return;
            if (shakeTarget == null) return;

            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }

            shakeCoroutine = StartCoroutine(ShakeRoutine(duration, strength));
        }

        private IEnumerator ShakeRoutine(float duration, float strength)
        {
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;

                Vector3 randomOffset = Random.insideUnitSphere * strength;
                randomOffset.z = 0f;

                shakeTarget.localPosition = originalShakeLocalPosition + randomOffset;

                yield return null;
            }

            shakeTarget.localPosition = originalShakeLocalPosition;
            shakeCoroutine = null;
        }
    }
}