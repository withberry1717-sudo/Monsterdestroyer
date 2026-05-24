using UnityEngine;

public class DragonHP : MonoBehaviour
{
    [Header("ドラゴンの本体HP")]
    public float maxHP = 100f;
    public float currentHP;

    [Header("クリスタルの共有HP")]
    public float maxCrystalHP = 30f;
    public float currentCrystalHP;
    public bool isCrystalBroken = false;

    void Start()
    {
        currentHP = maxHP;
        currentCrystalHP = maxCrystalHP;
    }

    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        Debug.Log($"本体に {damage} のダメージ！ 残りHP: {currentHP}");

        if (currentHP <= 0)
        {
            Debug.Log("ドラゴン討伐完了！");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameClear();
            }
        }
    }

    public void TakeCrystalDamage(float damage)
    {
        if (isCrystalBroken) return;

        currentCrystalHP -= damage;
        Debug.Log($"クリスタル共有HPに {damage} のダメージ！ 残り耐久: {currentCrystalHP}");

        if (currentCrystalHP <= 0)
        {
            isCrystalBroken = true;
            Debug.Log("クリスタル完全破壊！！");

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddCrystalBreakBonus();
            }
        }
    }
}