using UnityEngine;

public static class FPSlimit
{
    // ゲーム起動時に、最初のシーンが読み込まれる前に自動で実行される呪文
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitFPS()
    {
        // 1. 垂直同期（VSync）をオフにする（これがオンだとFPS固定が効かない仕様のため）
        QualitySettings.vSyncCount = 0;

        // 2. FPSを60に制限
        Application.targetFrameRate = 60;

        Debug.Log("【軽量化】FPSを60に固定しました（すべてのシーンに自動適用中）");
    }
}