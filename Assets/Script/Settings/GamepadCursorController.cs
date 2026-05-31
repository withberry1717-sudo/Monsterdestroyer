using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Reflection;

public class GamepadCursorController : MonoBehaviour
{
    [Header("Cursor Move")]
    [SerializeField] private bool enableStickCursor = true;

    [Tooltip("ONなら左スティックでカーソルを動かします。OFFなら右スティックで動かします。")]
    [SerializeField] private bool useLeftStickForCursor = true;

    [SerializeField] private float cursorSpeed = 1100f;
    [SerializeField] private float stickDeadZone = 0.12f;
    [SerializeField] private bool onlyWhenCursorVisible = true;
    [SerializeField] private bool onlyWhenCursorUnlocked = true;

    [Header("PS Controller Buttons")]
    [Tooltip("PS4の×。Unity Input SystemではbuttonSouthです。")]
    [SerializeField] private bool crossButtonClicksUI = true;

    [Tooltip("PS4の〇。Unity Input SystemではbuttonEastです。")]
    [SerializeField] private bool circleButtonCancels = true;

    [Header("Cancel Target")]
    [Tooltip("キャンセル時に呼びたいオブジェクト。PauseManagerやSettingManagerを入れる。空なら選択中UIにCancelを送ります。")]
    [SerializeField] private GameObject cancelReceiver;

    [Tooltip("〇を押した時に呼ぶメソッド候補。上から順に探し、最初に見つかった1個だけ呼びます。")]
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

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = false;

    private Vector2 virtualCursorPosition;
    private PointerEventData pointerData;
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    private void Start()
    {
        virtualCursorPosition = GetCurrentMousePositionOrCenter();
        pointerData = new PointerEventData(EventSystem.current);
    }

    private void Update()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null) return;
        if (EventSystem.current == null) return;
        if (!ShouldControlCursor()) return;

        SyncVirtualCursorWithRealMouseWhenStickIdle(gamepad);
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
        if (!enableStickCursor) return false;

        if (onlyWhenCursorVisible && !Cursor.visible)
        {
            return false;
        }

        if (onlyWhenCursorUnlocked && Cursor.lockState == CursorLockMode.Locked)
        {
            return false;
        }

        return true;
    }

    private Vector2 ReadCursorStick(Gamepad gamepad)
    {
        Vector2 stick = useLeftStickForCursor
            ? gamepad.leftStick.ReadValue()
            : gamepad.rightStick.ReadValue();

        if (stick.sqrMagnitude < stickDeadZone * stickDeadZone)
        {
            return Vector2.zero;
        }

        return stick;
    }

    private void SyncVirtualCursorWithRealMouseWhenStickIdle(Gamepad gamepad)
    {
        Vector2 stick = ReadCursorStick(gamepad);
        if (stick != Vector2.zero) return;

        virtualCursorPosition = GetCurrentMousePositionOrCenter();
    }

    private Vector2 GetCurrentMousePositionOrCenter()
    {
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }

        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private void MoveCursor(Gamepad gamepad)
    {
        Vector2 stick = ReadCursorStick(gamepad);
        if (stick == Vector2.zero) return;

        virtualCursorPosition += stick * cursorSpeed * Time.unscaledDeltaTime;
        virtualCursorPosition.x = Mathf.Clamp(virtualCursorPosition.x, 0f, Screen.width);
        virtualCursorPosition.y = Mathf.Clamp(virtualCursorPosition.y, 0f, Screen.height);

        if (Mouse.current != null)
        {
            Mouse.current.WarpCursorPosition(virtualCursorPosition);
        }
    }

    private GameObject GetCurrentCursorTarget()
    {
        if (pointerData == null)
        {
            pointerData = new PointerEventData(EventSystem.current);
        }

        pointerData.Reset();
        pointerData.position = virtualCursorPosition;

        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        if (raycastResults.Count <= 0)
        {
            return null;
        }

        return raycastResults[0].gameObject;
    }

    private void ClickCurrentCursorTarget()
    {
        GameObject target = GetCurrentCursorTarget();

        if (target == null)
        {
            if (showDebugLog) Debug.Log("[GamepadCursor] × pressed, but no UI target.");
            return;
        }

        if (showDebugLog) Debug.Log("[GamepadCursor] × Click: " + target.name);

        PointerEventData clickData = new PointerEventData(EventSystem.current)
        {
            position = virtualCursorPosition,
            button = PointerEventData.InputButton.Left
        };

        ExecuteEvents.ExecuteHierarchy(target, clickData, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.ExecuteHierarchy(target, clickData, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.ExecuteHierarchy(target, clickData, ExecuteEvents.pointerClickHandler);

        EventSystem.current.SetSelectedGameObject(target);
    }

    private void CancelCurrentUI()
    {
        if (showDebugLog) Debug.Log("[GamepadCursor] 〇 Cancel");

        if (cancelReceiver != null && TryCallFirstExistingMethod(cancelReceiver, cancelMethodNames))
        {
            return;
        }

        BaseEventData cancelData = new BaseEventData(EventSystem.current);

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected != null)
        {
            ExecuteEvents.ExecuteHierarchy(selected, cancelData, ExecuteEvents.cancelHandler);
            return;
        }

        GameObject target = GetCurrentCursorTarget();
        if (target != null)
        {
            ExecuteEvents.ExecuteHierarchy(target, cancelData, ExecuteEvents.cancelHandler);
        }
    }

    private bool TryCallFirstExistingMethod(GameObject receiver, string[] methodNames)
    {
        if (receiver == null || methodNames == null) return false;

        MonoBehaviour[] behaviours = receiver.GetComponents<MonoBehaviour>();

        foreach (string methodName in methodNames)
        {
            if (string.IsNullOrEmpty(methodName)) continue;

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null) continue;

                MethodInfo method = behaviour.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    System.Type.EmptyTypes,
                    null
                );

                if (method == null) continue;

                method.Invoke(behaviour, null);

                if (showDebugLog)
                {
                    Debug.Log("[GamepadCursor] 〇 Cancel Method: " + behaviour.GetType().Name + "." + methodName);
                }

                return true;
            }
        }

        return false;
    }
}
