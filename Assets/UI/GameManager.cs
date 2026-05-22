using UnityEngine;
using System.Collections;

public class HitStop : MonoBehaviour
{
    public static HitStop Instance;

    private void Awake()
    {
        Instance = this;
    }

    // どこからでも HitStop.Instance.Stop(秒数); で呼べる関数
    public void Stop(float duration)
    {
        // 既にヒットストップ中なら上書きしない
        if (Time.timeScale < 1f) return;

        StartCoroutine(DoHitStop(duration));
    }

    private IEnumerator DoHitStop(float duration)
    {
        // ゲーム内の時間を「0.05倍」にして、ほぼ時を止める
        // （※完全に0にすると他の処理がバグりやすいので、極端なスローにするのが現場のコツです）
        Time.timeScale = 0.05f;

        // 「現実世界の時計」で指定された時間だけ待つ
        yield return new WaitForSecondsRealtime(duration);

        // ゲーム内の時間を通常の「1倍」に戻す
        Time.timeScale = 1f;
    }
}