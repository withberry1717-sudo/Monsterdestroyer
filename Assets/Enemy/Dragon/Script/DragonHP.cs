using UnityEngine;
using System;

public class DragonHP : MonoBehaviour
{
    [Header("ドラゴンの本体HP")]
    public float maxHP = 3000f;
    public float currentHP;

    [Header("クリスタルの共有HP")]
    public float maxCrystalHP = 300f;
    public float currentCrystalHP;
    public bool isCrystalBroken = false;

    [Header("状態")]
    public bool isDead = false;
    public bool halfHpTriggered = false;

    public event Action OnHalfHP;
    public event Action OnDeath;
    public event Action OnCrystalBroken;

    void Start()
    {
        currentHP = maxHP;
        currentCrystalHP = maxCrystalHP;
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0f);

        Debug.Log($"本体に {damage} のダメージ！ 残りHP: {currentHP}");

        if (!halfHpTriggered && currentHP <= maxHP * 0.5f)
        {
            halfHpTriggered = true;
            Debug.Log("ドラゴンHP50%以下！強化フェーズへ");
            OnHalfHP?.Invoke();
        }

        if (currentHP <= 0f)
        {
            isDead = true;
            Debug.Log("ドラゴン討伐完了！");
            OnDeath?.Invoke();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameClear();
            }
        }
    }

    public void TakeCrystalDamage(float damage)
    {
        if (isDead) return;
        if (isCrystalBroken) return;

        currentCrystalHP -= damage;
        currentCrystalHP = Mathf.Max(currentCrystalHP, 0f);

        Debug.Log($"クリスタル共有HPに {damage} のダメージ！ 残り耐久: {currentCrystalHP}");

        if (currentCrystalHP <= 0f)
        {
            isCrystalBroken = true;
            Debug.Log("クリスタル完全破壊！！");

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddCrystalBreakBonus();
            }

            OnCrystalBroken?.Invoke();
        }
    }
}