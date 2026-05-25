using UnityEngine;
using UnityEngine.EventSystems;

public class TitleCursorManager : MonoBehaviour
{
    private void Start()
    {
        ApplyCursorSettings();
    }

    private void OnEnable()
    {
        ApplyCursorSettings();
    }

    // 毎フレームチェックし、他のスクリプトがカーソルを隠そうとしても強制的に引っ張り出す
    private void Update()
    {
        if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // エディタ実行時、画面を1回クリックした瞬間に確実にカーソルを自由にする
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void ApplyCursorSettings()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

#if !UNITY_EDITOR
        Application.FocusChanged += OnApplicationFocusChanged;
#endif

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

#if !UNITY_EDITOR
    private void OnApplicationFocusChanged(bool focused)
    {
        if (focused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void OnDestroy()
    {
        Application.FocusChanged -= OnApplicationFocusChanged;
    }
#endif
}