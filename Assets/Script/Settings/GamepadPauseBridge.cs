using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Reflection;

public class GamepadPauseBridge : MonoBehaviour
{
    [Header("References")]
    [Tooltip("PauseManagerが付いているGameObject。空ならこのGameObjectを使います。")]
    [SerializeField] private GameObject pauseReceiver;

    [Header("PS4 Options Button")]
    [Tooltip("ONならPS4のOptions/startButtonで、Escと同じポーズ処理を呼びます。")]
    [SerializeField] private bool optionsButtonActsAsEscape = true;

    [Tooltip("上から順に探し、見つかった最初の1個だけ呼びます。ResumeGameは絶対に入れないでください。")]
    [SerializeField]
    private string[] pauseMethodNames =
    {
        "TogglePause",
        "TogglePauseMenu",
        "OnEscape",
        "PauseGame",
        "OpenPauseMenu"
    };

    [Header("Safety")]
    [Tooltip("ON推奨。Optionsを押した瞬間に選択中ボタンがSubmit扱いされるのを防ぎます。")]
    [SerializeField] private bool clearSelectedObjectBeforePause = true;

    [SerializeField] private float inputCooldown = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    private float lastPressTime = -999f;

    private void Reset()
    {
        pauseReceiver = gameObject;
    }

    private void Awake()
    {
        if (pauseReceiver == null)
        {
            pauseReceiver = gameObject;
        }
    }

    private void Update()
    {
        if (!optionsButtonActsAsEscape) return;

        Gamepad gamepad = Gamepad.current;
        if (gamepad == null) return;

        if (gamepad.startButton.wasPressedThisFrame)
        {
            if (Time.unscaledTime < lastPressTime + inputCooldown) return;
            lastPressTime = Time.unscaledTime;

            TriggerPauseLikeEscape();
        }
    }

    private void TriggerPauseLikeEscape()
    {
        if (clearSelectedObjectBeforePause && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (pauseReceiver == null)
        {
            if (showDebugLog) Debug.LogWarning("[GamepadPauseBridge] Pause Receiverが空です。", this);
            return;
        }

        Component[] components = pauseReceiver.GetComponents<Component>();

        foreach (string methodName in pauseMethodNames)
        {
            if (string.IsNullOrEmpty(methodName)) continue;

            foreach (Component component in components)
            {
                if (component == null) continue;

                MethodInfo method = component.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (method == null) continue;
                if (method.GetParameters().Length != 0) continue;

                if (showDebugLog)
                {
                    Debug.Log("[GamepadPauseBridge] Options -> " + component.GetType().Name + "." + methodName);
                }

                method.Invoke(component, null);
                return;
            }
        }

        if (showDebugLog)
        {
            Debug.LogWarning("[GamepadPauseBridge] ポーズ用メソッドが見つかりません。Pause Method Namesに実際の関数名を入れてください。", this);
        }
    }
}
