using UnityEngine;

namespace NaughtyCharacter
{
    public class SafePlayerCamera : MonoBehaviour
    {
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

        private Vector2 currentRotation;
        private Vector2 targetRotation;
        private Vector2 rotationVelocity;
        private Vector3 followVelocity;

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
    }
}