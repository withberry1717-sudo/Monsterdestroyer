using UnityEngine;
using UnityEngine.UI;

public class DragonUIManager : MonoBehaviour
{
    [Header("連携するドラゴンのHPスクリプト")]
    [SerializeField] private DragonHP dragonHP;

    [Header("HPバーのUI画像 (Imageコンポーネント)")]
    [Tooltip("即座に減るメインのHPバー (赤色など)")]
    [SerializeField] private Image mainHpBar;

    [Tooltip("遅れて減るHPバー (黄色など)")]
    [SerializeField] private Image delayedHpBar;

    [Tooltip("一瞬光るなどのハイライト用バー (白など)")]
    [SerializeField] private Image highlightBar;

    [Header("アニメーション設定")]
    [Tooltip("遅れて減るバーが減り始めるまでの待機時間")]
    [SerializeField] private float delayTime = 0.5f;
    [Tooltip("遅れて減るバーが減る速度")]
    [SerializeField] private float decreaseSpeed = 0.5f;

    private float currentDelayTimer;
    private float targetFillAmount;

    private void Start()
    {
        // 最初はすべて満タン
        if (mainHpBar) mainHpBar.fillAmount = 1f;
        if (delayedHpBar) delayedHpBar.fillAmount = 1f;
        if (highlightBar) highlightBar.fillAmount = 1f;

        targetFillAmount = 1f;
    }

    private void Update()
    {
        if (dragonHP == null) return;

        // 目標のHP割合を計算 (現在のHP / 最大HP)
        targetFillAmount = dragonHP.currentHP / dragonHP.maxHP;

        // 1. メインバーは即座に更新
        if (mainHpBar)
        {
            mainHpBar.fillAmount = targetFillAmount;
        }

        // 2. ハイライトバーも即座に更新（または別の演出に使う）
        if (highlightBar)
        {
            highlightBar.fillAmount = targetFillAmount;
            // ※もし「ダメージを受けた瞬間だけ一瞬表示したい」場合は、Updateではなく
            // Eventでトリガーしてコルーチンで消すなどの処理に変更してください。
        }

        // 3. 遅延バーの処理
        if (delayedHpBar && delayedHpBar.fillAmount > targetFillAmount)
        {
            currentDelayTimer += Time.deltaTime;

            // 待機時間を超えたら、徐々に減らす
            if (currentDelayTimer > delayTime)
            {
                delayedHpBar.fillAmount = Mathf.Lerp(delayedHpBar.fillAmount, targetFillAmount, decreaseSpeed * Time.deltaTime);

                // ほぼ追いついたらピッタリ合わせる
                if (delayedHpBar.fillAmount - targetFillAmount < 0.001f)
                {
                    delayedHpBar.fillAmount = targetFillAmount;
                }
            }
        }
        else
        {
            // 目標値と同じかそれ以下の場合はタイマーをリセット
            currentDelayTimer = 0f;
        }
    }
}