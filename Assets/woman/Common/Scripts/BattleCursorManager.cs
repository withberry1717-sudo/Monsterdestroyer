using UnityEngine;

public class BattleCursorManager : MonoBehaviour
{
    private static bool shouldLockCursor = true;

    private void Start()
    {
        LockCursor();
    }

    private void LateUpdate()
    {
        ApplyCursorState();
    }

    public static void LockCursor()
    {
        shouldLockCursor = true;
        ApplyCursorState();
    }

    public static void UnlockCursor()
    {
        shouldLockCursor = false;
        ApplyCursorState();
    }

    private static void ApplyCursorState()
    {
        if (shouldLockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}