using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class GamepadCursorController : MonoBehaviour
{
    [Header("Cursor Move")]
    [SerializeField] private bool enableCursorControl = true;
    [SerializeField] private bool useLeftStickForCursor = true;

    [Tooltip("基本カーソル速度。おすすめ 750〜950")]
    [SerializeField] private float cursorSpeed = 850f;

    [Tooltip("この値以下のスティック入力は無視。おすすめ 0.20〜0.25")]
    [Range(0f, 0.8f)]
    [SerializeField] private float stickDeadZone = 0.22f;

    [Tooltip("少し倒した時の低速倍率。おすすめ 0.25〜0.45")]
    [SerializeField] private float slowSpeedMultiplier = 0.35f;

    [Tooltip("大きく倒した時の高速倍率。おすすめ 1.3〜1.8")]
    [SerializeField] private float fastSpeedMultiplier = 1.6f;

    [Tooltip("大きいほど細かい操作がしやすい。おすすめ 1.5〜2.0")]
    [SerializeField] private float stickCurvePower = 1.7f;

    [Tooltip("動き出しの加速時間。おすすめ 0.12〜0.22")]
    [SerializeField] private float accelerationTime = 0.18f;

    [Tooltip("止まる時の減速時間。おすすめ 0.05〜0.10")]
    [SerializeField] private float decelerationTime = 0.08f;

    [Header("Active Condition")]
    [SerializeField] private bool onlyWhenCursorVisible = true;
    [SerializeField] private bool onlyWhenCursorUnlocked = true;

    [Header("PS Controller Buttons")]
    [Tooltip("PS4の×。Unity Input SystemではbuttonSouth")]
    [SerializeField] private bool crossButtonClicksUI = true;

    [Tooltip("PS4の〇。Unity Input SystemではbuttonEast")]
    [SerializeField] private bool circleButtonCancels = true;

    [Header("Cancel Target")]
    [Tooltip("キャンセル時に呼びたいオブジェクト。PauseManagerやSettingManagerを入れる。空なら選択中UIへCancelを送る")]
    [SerializeField] private GameObject cancelReceiver;

    [SerializeField]
    private string[] cancelMethodNames =
    {
        "CloseSettings",
        "ClosePause",
        "ClosePauseMenu",
        "ResumeGame",
        "TogglePause",
        "Back",
        "Cancel"
    };

    [Header("Click Safety")]
    [Tooltip("×長押しやUI切替直後の二重クリック防止")]
    [SerializeField] private float clickCooldown = 0.16f;

    [Tooltip("クリック後、このフレーム数だけ実マウス座標を再同期します。パネル切替後の判定ズレ対策")]
    [SerializeField] private int syncFramesAfterClick = 3;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = false;

    private Vector2 currentCursorVelocity;
    private Vector2 velocitySmoothRef;
    private Vector2 cursorPosition;

    private float nextClickAllowedTime = 0f;
    private int forceSyncFrameCount = 0;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    private void OnEnable()
    {
        ForceSyncToRealMouse();
    }

    private void Start()
    {
        ForceSyncToRealMouse();
    }

    private void Update()
    {
        if (!enableCursorControl) return;
        if (EventSystem.current == null) return;

        Gamepad gamepad = Gamepad.current;
        if (gamepad == null) return;

        if (!ShouldControlCursor())
        {
            currentCursorVelocity = Vector2.zero;
            ForceSyncToRealMouse();
            return;
        }

        UpdateCursorPositionFromRealMouseFirst();

        MoveCursor(gamepad);

        if (crossButtonClicksUI && gamepad.buttonSouth.wasPressedThisFrame)
        {
            ClickCurrentCursorTarget();
        }

        if (circleButtonCancels && gamepad.buttonEast.wasPressedThisFrame)
        {
            CancelCurrentUI();
        }
    }

    private bool ShouldControlCursor()
    {
        if (onlyWhenCursorVisible && !Cursor.visible) return false;
        if (onlyWhenCursorUnlocked && Cursor.lockState == CursorLockMode.Locked) return false;
        return true;
    }

    private Vector2 ReadCursorStick(Gamepad gamepad)
    {
        return useLeftStickForCursor
            ? gamepad.leftStick.ReadValue()
            : gamepad.rightStick.ReadValue();
    }

    private void UpdateCursorPositionFromRealMouseFirst()
    {
        // ここが重要。
        // 仮想カーソル座標を持ち続けず、毎フレーム実マウス座標から始める。
        // これでパネル切替・Canvas Scaler・解像度変更後もRaycast座標がズレにくい。
        Vector2 realMousePosition = GetRealMousePosition();

        if (forceSyncFrameCount > 0)
        {
            cursorPosition = realMousePosition;
            forceSyncFrameCount--;
            return;
        }

        cursorPosition = realMousePosition;
    }

    private void MoveCursor(Gamepad gamepad)
    {
        Vector2 stick = ReadCursorStick(gamepad);
        float magnitude = stick.magnitude;

        Vector2 targetVelocity = Vector2.zero;

        if (magnitude > stickDeadZone)
        {
            Vector2 direction = stick.normalized;

            float normalized = Mathf.InverseLerp(stickDeadZone, 1f, magnitude);
            normalized = Mathf.Clamp01(normalized);

            float curved = Mathf.Pow(normalized, stickCurvePower);

            float speedMultiplier = Mathf.Lerp(
                slowSpeedMultiplier,
                fastSpeedMultiplier,
                curved
            );

            targetVelocity = direction * cursorSpeed * speedMultiplier;
        }

        float smoothTime = targetVelocity.sqrMagnitude > currentCursorVelocity.sqrMagnitude
            ? accelerationTime
            : decelerationTime;

        currentCursorVelocity = Vector2.SmoothDamp(
            currentCursorVelocity,
            targetVelocity,
            ref velocitySmoothRef,
            smoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );

        cursorPosition += currentCursorVelocity * Time.unscaledDeltaTime;

        cursorPosition.x = Mathf.Clamp(cursorPosition.x, 0f, Screen.width);
        cursorPosition.y = Mathf.Clamp(cursorPosition.y, 0f, Screen.height);

        WarpRealMouse(cursorPosition);
    }

    private Vector2 GetRealMousePosition()
    {
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }

        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private void WarpRealMouse(Vector2 position)
    {
        if (Mouse.current != null)
        {
            Mouse.current.WarpCursorPosition(position);
            InputSystem.QueueStateEvent(Mouse.current, new UnityEngine.InputSystem.LowLevel.MouseState
            {
                position = position
            });
        }
    }

    private void ForceSyncToRealMouse()
    {
        cursorPosition = GetRealMousePosition();
        currentCursorVelocity = Vector2.zero;
        velocitySmoothRef = Vector2.zero;
    }

    private GameObject GetCurrentCursorTarget()
    {
        if (EventSystem.current == null) return null;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = cursorPosition,
            button = PointerEventData.InputButton.Left
        };

        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        if (raycastResults.Count <= 0) return null;

        return raycastResults[0].gameObject;
    }

    private void ClickCurrentCursorTarget()
    {
        if (Time.unscaledTime < nextClickAllowedTime)
        {
            return;
        }

        nextClickAllowedTime = Time.unscaledTime + clickCooldown;

        // クリック直前に必ず実カーソルと同期
        cursorPosition = GetRealMousePosition();

        GameObject target = GetCurrentCursorTarget();

        if (target == null)
        {
            if (showDebugLog) Debug.Log("[GamepadCursor] × pressed, no UI target.");
            return;
        }

        if (showDebugLog) Debug.Log("[GamepadCursor] × Click: " + target.name);

        PointerEventData clickData = new PointerEventData(EventSystem.current)
        {
            position = cursorPosition,
            button = PointerEventData.InputButton.Left,
            clickCount = 1
        };

        // button.onClick.Invoke() は呼ばない。
        // pointerClickHandlerだけにしないと、ボタンによっては二重クリックになる。
        ExecuteEvents.ExecuteHierarchy(target, clickData, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.ExecuteHierarchy(target, clickData, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.ExecuteHierarchy(target, clickData, ExecuteEvents.pointerClickHandler);

        EventSystem.current.SetSelectedGameObject(target);

        // パネルが開閉した直後に座標がズレやすいので数フレーム同期する
        forceSyncFrameCount = syncFramesAfterClick;
    }

    private void CancelCurrentUI()
    {
        if (Time.unscaledTime < nextClickAllowedTime)
        {
            return;
        }

        nextClickAllowedTime = Time.unscaledTime + clickCooldown;

        if (showDebugLog) Debug.Log("[GamepadCursor] 〇 Cancel");

        if (cancelReceiver != null)
        {
            foreach (string methodName in cancelMethodNames)
            {
                if (string.IsNullOrEmpty(methodName)) continue;
                cancelReceiver.SendMessage(methodName, SendMessageOptions.DontRequireReceiver);
            }

            forceSyncFrameCount = syncFramesAfterClick;
            return;
        }

        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected != null)
        {
            ExecuteEvents.ExecuteHierarchy(
                selected,
                new BaseEventData(EventSystem.current),
                ExecuteEvents.cancelHandler
            );

            forceSyncFrameCount = syncFramesAfterClick;
            return;
        }

        GameObject target = GetCurrentCursorTarget();

        if (target != null)
        {
            ExecuteEvents.ExecuteHierarchy(
                target,
                new BaseEventData(EventSystem.current),
                ExecuteEvents.cancelHandler
            );
        }

        forceSyncFrameCount = syncFramesAfterClick;
    }
}
