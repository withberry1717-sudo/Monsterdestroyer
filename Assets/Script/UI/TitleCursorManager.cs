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

    // 毎フレームチェックし、他のスクリプトがカーソルを隠そうとしても強制的に表示する
    private void Update()
    {
        if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
        {
            ApplyCursorSettings();
        }

        // エディタ実行時、画面を1回クリックした瞬間に確実にカーソルを自由にする
        if (Input.GetMouseButtonDown(0))
        {
            ApplyCursorSettings();
        }
    }

    private void ApplyCursorSettings()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused)
        {
            ApplyCursorSettings();
        }
    }
}