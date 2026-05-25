using UnityEngine;

namespace Retro.ThirdPersonCharacter
{
    public class PlayerInput : MonoBehaviour
    {
        private bool _attackInput;
        private bool _specialAttackInput;
        private bool _specialAttackHeld;
        private bool _specialAttackReleased;

        private Vector2 _movementInput;
        private bool _jumpInput;
        private bool _changeCameraModeInput;

        public bool AttackInput => _attackInput;
        public bool SpecialAttackInput => _specialAttackInput;
        public bool SpecialAttackHeld => _specialAttackHeld;
        public bool SpecialAttackReleased => _specialAttackReleased;

        public Vector2 MovementInput => _movementInput;
        public bool JumpInput => _jumpInput;
        public bool ChangeCameraModeInput => _changeCameraModeInput;

        private void Update()
        {
            _attackInput = Input.GetMouseButtonDown(0);

            _specialAttackInput = Input.GetMouseButtonDown(1);
            _specialAttackHeld = Input.GetMouseButton(1);
            _specialAttackReleased = Input.GetMouseButtonUp(1);

            _movementInput.Set(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            _jumpInput = Input.GetButton("Jump");
            _changeCameraModeInput = Input.GetKeyDown(KeyCode.F);
        }
    }
}