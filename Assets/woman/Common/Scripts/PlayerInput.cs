using UnityEngine;
using UnityEngine.EventSystems; // ★追加：UI判定を使うための合言葉

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
            // まずは普通にクリック入力を取得する
            _attackInput = Input.GetMouseButtonDown(0);
            _specialAttackInput = Input.GetMouseButtonDown(1);
            _specialAttackHeld = Input.GetMouseButton(1);
            _specialAttackReleased = Input.GetMouseButtonUp(1);

            // ★追加：もしマウスがUI（クリアパネルやボタン）の上にある場合は、強制的にクリックを無かったことにする
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                _attackInput = false;
                _specialAttackInput = false;
                // 必要であれば Held や Released も false にできます
            }

            // 移動などのキーボード入力はそのまま
            _movementInput.Set(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            _jumpInput = Input.GetButton("Jump");
            _changeCameraModeInput = Input.GetKeyDown(KeyCode.F);
        }
    }
}