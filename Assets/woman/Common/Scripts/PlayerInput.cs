using UnityEngine;
using UnityEngine.EventSystems;

namespace Retro.ThirdPersonCharacter
{
    public class PlayerInput : MonoBehaviour
    {
        public enum GamepadBlinkButton
        {
            LeftShoulder,
            RightShoulder,
            LeftTrigger,
            RightTrigger,
            South,
            East,
            West,
            North,
            Select,
            Start,
            LeftStick,
            RightStick,
            DpadUp,
            DpadDown,
            DpadLeft,
            DpadRight
        }

        private const string BlinkKey = "BlinkKey";
        private const string GamepadPrefix = "Gamepad:";

        private bool _attackInput;
        private bool _specialAttackInput;
        private bool _specialAttackHeld;
        private bool _specialAttackReleased;
        private bool _blinkInput;
        private bool _pauseInput;
        private bool _lockOnInput;

        private Vector2 _movementInput;
        private Vector2 _lookInput;
        private bool _jumpInput;
        private bool _changeCameraModeInput;

        [Header("Keyboard / Mouse")]
        [SerializeField] private KeyCode defaultKeyboardBlinkKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode keyboardPauseKey = KeyCode.Escape;
        [SerializeField] private KeyCode keyboardLockOnKey = KeyCode.T;

        [Header("Gamepad Default")]
        [Tooltip("弱攻撃。PS4でいうR1です。")]
        [SerializeField] private bool useRightShoulderForLightAttack = true;

        [Tooltip("強攻撃。デフォルトはR2です。")]
        [SerializeField] private bool useRightTriggerForHeavyAttack = true;

        [Tooltip("ブリンク。デフォルトはL1です。設定画面で変更できます。")]
        [SerializeField] private GamepadBlinkButton defaultGamepadBlinkButton = GamepadBlinkButton.LeftShoulder;

        [Tooltip("ポーズ。一般的なStart/Optionsボタンです。")]
        [SerializeField] private bool useStartButtonForPause = true;

        [Tooltip("ロックオン。Rスティック押し込みです。")]
        [SerializeField] private bool useRightStickButtonForLockOn = true;

        [Header("Gamepad Stick")]
        [SerializeField] private float leftStickDeadZone = 0.18f;
        [SerializeField] private float rightStickDeadZone = 0.12f;

        public bool AttackInput => _attackInput;
        public bool SpecialAttackInput => _specialAttackInput;
        public bool SpecialAttackHeld => _specialAttackHeld;
        public bool SpecialAttackReleased => _specialAttackReleased;
        public bool BlinkInput => _blinkInput;
        public bool PauseInput => _pauseInput;
        public bool LockOnInput => _lockOnInput;

        public Vector2 MovementInput => _movementInput;
        public Vector2 LookInput => _lookInput;
        public bool JumpInput => _jumpInput;
        public bool ChangeCameraModeInput => _changeCameraModeInput;

        private void OnDisable()
        {
            ClearAllInputs();
        }

        public void ClearActionInputs()
        {
            _attackInput = false;
            _specialAttackInput = false;
            _specialAttackHeld = false;
            _specialAttackReleased = false;
            _blinkInput = false;
            _jumpInput = false;
            _pauseInput = false;
            _lockOnInput = false;
        }

        public void ClearAllInputs()
        {
            ClearActionInputs();
            _movementInput = Vector2.zero;
            _lookInput = Vector2.zero;
            _changeCameraModeInput = false;
        }

        private void Update()
        {
            ReadKeyboardMouseInput();
            ReadGamepadInput();
            BlockActionInputWhenPointerIsOverUI();
        }

        private void ReadKeyboardMouseInput()
        {
            _attackInput = Input.GetMouseButtonDown(0);
            _specialAttackInput = Input.GetMouseButtonDown(1);
            _specialAttackHeld = Input.GetMouseButton(1);
            _specialAttackReleased = Input.GetMouseButtonUp(1);

            _blinkInput = GetSavedBlinkKeyboardDown();
            _pauseInput = Input.GetKeyDown(keyboardPauseKey);
            _lockOnInput = Input.GetKeyDown(keyboardLockOnKey);

            _movementInput.Set(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            _lookInput = Vector2.zero;

            _jumpInput = Input.GetButton("Jump");
            _changeCameraModeInput = Input.GetKeyDown(KeyCode.F);
        }

        private void ReadGamepadInput()
        {
            var gamepad = UnityEngine.InputSystem.Gamepad.current;
            if (gamepad == null) return;

            Vector2 stickMove = gamepad.leftStick.ReadValue();
            if (stickMove.magnitude >= leftStickDeadZone)
            {
                _movementInput = Vector2.ClampMagnitude(stickMove, 1f);
            }

            Vector2 stickLook = gamepad.rightStick.ReadValue();
            if (stickLook.magnitude >= rightStickDeadZone)
            {
                _lookInput = Vector2.ClampMagnitude(stickLook, 1f);
            }

            if (useRightShoulderForLightAttack && gamepad.rightShoulder.wasPressedThisFrame)
            {
                _attackInput = true;
            }

            if (useRightTriggerForHeavyAttack)
            {
                if (gamepad.rightTrigger.wasPressedThisFrame)
                {
                    _specialAttackInput = true;
                }

                if (gamepad.rightTrigger.isPressed)
                {
                    _specialAttackHeld = true;
                }

                if (gamepad.rightTrigger.wasReleasedThisFrame)
                {
                    _specialAttackReleased = true;
                }
            }

            if (GetSavedBlinkGamepadDown(gamepad))
            {
                _blinkInput = true;
            }

            if (useStartButtonForPause && gamepad.startButton.wasPressedThisFrame)
            {
                _pauseInput = true;
            }

            if (useRightStickButtonForLockOn && gamepad.rightStickButton.wasPressedThisFrame)
            {
                _lockOnInput = true;
                _changeCameraModeInput = true;
            }

            if (gamepad.buttonSouth.wasPressedThisFrame)
            {
                _jumpInput = true;
            }
        }

        private void BlockActionInputWhenPointerIsOverUI()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                _attackInput = false;
                _specialAttackInput = false;
                _specialAttackHeld = false;
                _specialAttackReleased = false;
                _blinkInput = false;
            }
        }

        private bool GetSavedBlinkKeyboardDown()
        {
            string saved = PlayerPrefs.GetString(BlinkKey, defaultKeyboardBlinkKey.ToString());

            if (saved.StartsWith(GamepadPrefix))
            {
                return false;
            }

            if (System.Enum.TryParse(saved, out KeyCode key))
            {
                return Input.GetKeyDown(key);
            }

            return Input.GetKeyDown(defaultKeyboardBlinkKey);
        }

        private bool GetSavedBlinkGamepadDown(UnityEngine.InputSystem.Gamepad gamepad)
        {
            string saved = PlayerPrefs.GetString(BlinkKey, GamepadPrefix + defaultGamepadBlinkButton.ToString());

            if (saved.StartsWith(GamepadPrefix))
            {
                string buttonName = saved.Substring(GamepadPrefix.Length);
                if (System.Enum.TryParse(buttonName, out GamepadBlinkButton button))
                {
                    return IsGamepadButtonDown(gamepad, button);
                }
            }

            return IsGamepadButtonDown(gamepad, defaultGamepadBlinkButton);
        }

        public static string ToGamepadBlinkPrefsValue(GamepadBlinkButton button)
        {
            return GamepadPrefix + button.ToString();
        }

        public static string FormatBlinkPrefsValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "L1 / LeftShift";

            if (value.StartsWith(GamepadPrefix))
            {
                string button = value.Substring(GamepadPrefix.Length);
                return "Gamepad " + ToDisplayGamepadButtonName(button);
            }

            return value;
        }

        private static string ToDisplayGamepadButtonName(string button)
        {
            switch (button)
            {
                case "LeftShoulder": return "L1";
                case "RightShoulder": return "R1";
                case "LeftTrigger": return "L2";
                case "RightTrigger": return "R2";
                case "South": return "A / Cross";
                case "East": return "B / Circle";
                case "West": return "X / Square";
                case "North": return "Y / Triangle";
                case "Select": return "Select / Share";
                case "Start": return "Start / Options";
                case "LeftStick": return "L3";
                case "RightStick": return "R3";
                case "DpadUp": return "D-Pad Up";
                case "DpadDown": return "D-Pad Down";
                case "DpadLeft": return "D-Pad Left";
                case "DpadRight": return "D-Pad Right";
                default: return button;
            }
        }

        private static bool IsGamepadButtonDown(UnityEngine.InputSystem.Gamepad gamepad, GamepadBlinkButton button)
        {
            if (gamepad == null) return false;

            switch (button)
            {
                case GamepadBlinkButton.LeftShoulder: return gamepad.leftShoulder.wasPressedThisFrame;
                case GamepadBlinkButton.RightShoulder: return gamepad.rightShoulder.wasPressedThisFrame;
                case GamepadBlinkButton.LeftTrigger: return gamepad.leftTrigger.wasPressedThisFrame;
                case GamepadBlinkButton.RightTrigger: return gamepad.rightTrigger.wasPressedThisFrame;
                case GamepadBlinkButton.South: return gamepad.buttonSouth.wasPressedThisFrame;
                case GamepadBlinkButton.East: return gamepad.buttonEast.wasPressedThisFrame;
                case GamepadBlinkButton.West: return gamepad.buttonWest.wasPressedThisFrame;
                case GamepadBlinkButton.North: return gamepad.buttonNorth.wasPressedThisFrame;
                case GamepadBlinkButton.Select: return gamepad.selectButton.wasPressedThisFrame;
                case GamepadBlinkButton.Start: return gamepad.startButton.wasPressedThisFrame;
                case GamepadBlinkButton.LeftStick: return gamepad.leftStickButton.wasPressedThisFrame;
                case GamepadBlinkButton.RightStick: return gamepad.rightStickButton.wasPressedThisFrame;
                case GamepadBlinkButton.DpadUp: return gamepad.dpad.up.wasPressedThisFrame;
                case GamepadBlinkButton.DpadDown: return gamepad.dpad.down.wasPressedThisFrame;
                case GamepadBlinkButton.DpadLeft: return gamepad.dpad.left.wasPressedThisFrame;
                case GamepadBlinkButton.DpadRight: return gamepad.dpad.right.wasPressedThisFrame;
                default: return false;
            }
        }
    }
}
