using UnityEngine;
using NaughtyCharacter;

namespace Retro.ThirdPersonCharacter
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(Aiming))]
    public class AimingController : MonoBehaviour
    {
        private Aiming _aiming;
        private PlayerInput _playerInput;

        [SerializeField] private bool _isAiming;
        [SerializeField] private SpringArm _springArm;

        [Header("Settings")]
        [SerializeField] private float _aimCameraDistance = 3f;
        [SerializeField] private float _regularCameraDistance = 1f;

        private void Awake()
        {
            _aiming = GetComponent<Aiming>();
            _playerInput = GetComponent<PlayerInput>();
        }

        private void Start()
        {
            if (_springArm == null)
            {
                _springArm = FindAnyObjectByType<SpringArm>();
            }

            OnStateChanged();
        }

        private void Update()
        {
            if (_playerInput != null && _playerInput.ChangeCameraModeInput)
            {
                SwitchAim();
            }
        }

        private void SwitchAim()
        {
            _isAiming = !_isAiming;
            OnStateChanged();
        }

        private void OnStateChanged()
        {
            if (_aiming == null) return;

            if (_springArm == null)
            {
                Debug.LogWarning("AimingController: SpringArm が設定されていません。CameraRig / Pivot / SpringArm を確認してください。");
                _aiming.enabled = _isAiming;
                return;
            }

            if (_isAiming)
            {
                _springArm.TargetLength = _aimCameraDistance;
                _aiming.enabled = true;
            }
            else
            {
                _springArm.TargetLength = _regularCameraDistance;
                _aiming.enabled = false;
            }
        }

        private void OnDisable()
        {
            // GameOverやPauseでAimingControllerがOFFになった時、
            // AimingだけONで残るのを防ぐ
            if (_aiming != null)
            {
                _aiming.enabled = false;
            }

            if (_springArm != null)
            {
                _springArm.TargetLength = _regularCameraDistance;
            }

            _isAiming = false;
        }

        private void OnEnable()
        {
            if (_aiming == null)
            {
                _aiming = GetComponent<Aiming>();
            }

            if (_playerInput == null)
            {
                _playerInput = GetComponent<PlayerInput>();
            }

            if (_springArm == null)
            {
                _springArm = FindAnyObjectByType<SpringArm>();
            }

            OnStateChanged();
        }
    }
}