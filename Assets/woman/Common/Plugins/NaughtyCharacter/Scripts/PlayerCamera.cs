using UnityEngine;

namespace NaughtyCharacter
{
    public class PlayerCamera : MonoBehaviour
    {
        public float ControlRotationSensitivity = 2.0f;
        public Transform Rig;
        public Transform Pivot;
        public Transform Target;
        public Camera Camera;

        private Vector3 _cameraVelocity;
        private Vector2 controlRotation;

        private void LateUpdate()
        {
            if (Target == null || Rig == null || Pivot == null) return;

            SetPosition(Target.position);

            Vector2 cameraInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            float pitchAngle = controlRotation.x;
            pitchAngle -= cameraInput.y * ControlRotationSensitivity;

            float yawAngle = controlRotation.y;
            yawAngle += cameraInput.x * ControlRotationSensitivity;

            controlRotation = new Vector2(pitchAngle, yawAngle);

            UpdateControlRotation();
            SetControlRotation(controlRotation);
        }

        public void SetPosition(Vector3 position)
        {
            Rig.position = position;
        }

        public void SetControlRotation(Vector2 rotation)
        {
            Quaternion rigTargetLocalRotation = Quaternion.Euler(0.0f, rotation.y, 0.0f);
            Quaternion pivotTargetLocalRotation = Quaternion.Euler(rotation.x, 0.0f, 0.0f);

            Rig.localRotation = rigTargetLocalRotation;
            Pivot.localRotation = pivotTargetLocalRotation;
        }

        public void UpdateControlRotation()
        {
            float pitchAngle = controlRotation.x;
            pitchAngle = Mathf.Clamp(pitchAngle, -45f, 75f);

            float yawAngle = controlRotation.y;

            controlRotation = new Vector2(pitchAngle, yawAngle);
        }
    }
}