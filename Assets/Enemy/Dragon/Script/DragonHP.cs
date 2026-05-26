using UnityEngine;
using System;

public class DragonHP : MonoBehaviour
{
    [Header("ドラゴンの本体HP")]
    [Tooltip("ドラゴン本体の最大HP")]
    public float maxHP = 3000f;

    [Tooltip("現在の本体HP")]
    public float currentHP;

    [Header("尻尾クリスタルのHP")]
    [Tooltip("尻尾クリスタルの最大HP。これが0になると尻尾攻撃を封印してダウンする")]
    public float maxTailCrystalHP = 300f;

    [Tooltip("現在の尻尾クリスタルHP")]
    public float currentTailCrystalHP;

    [Tooltip("尻尾クリスタルが壊れているか")]
    public bool isTailCrystalBroken = false;

    [Header("互換用：昔のCrystal変数")]
    [Tooltip("昔のコードとの互換用。isTailCrystalBrokenと同じ意味")]
    public bool isCrystalBroken = false;

    [Header("状態")]
    [Tooltip("ドラゴンが死亡しているか")]
    public bool isDead = false;

    [Tooltip("HP50%イベントをすでに発動したか")]
    public bool halfHpTriggered = false;

    public event Action OnHalfHP;
    public event Action OnDeath;
    public event Action OnTailCrystalBroken;
    public event Action OnCrystalBroken;

    private void Start()
    {
        currentHP = maxHP;
        currentTailCrystalHP = maxTailCrystalHP;
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0f);

        Debug.Log($"本体に {damage} ダメージ。残りHP: {currentHP}");

        if (!halfHpTriggered && currentHP <= maxHP * 0.5f)
        {
            halfHpTriggered = true;
            Debug.Log("ドラゴンHP50%以下。強化フェーズへ移行");
            OnHalfHP?.Invoke();
        }

        if (currentHP <= 0f)
        {
            isDead = true;
            Debug.Log("ドラゴン討伐完了");
            OnDeath?.Invoke();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameClear();
            }
        }
    }

    public void TakeTailCrystalDamage(float damage)
    {
        if (isDead) return;
        if (isTailCrystalBroken) return;

        currentTailCrystalHP -= damage;
        currentTailCrystalHP = Mathf.Max(currentTailCrystalHP, 0f);

        Debug.Log($"尻尾クリスタルに {damage} ダメージ。残り耐久: {currentTailCrystalHP}");

        if (currentTailCrystalHP <= 0f)
        {
            BreakTailCrystal();
        }
    }

    public void TakeCrystalDamage(float damage)
    {
        TakeTailCrystalDamage(damage);
    }

    private void BreakTailCrystal()
    {
        if (isTailCrystalBroken) return;

        isTailCrystalBroken = true;
        isCrystalBroken = true;

        Debug.Log("尻尾クリスタル破壊。尻尾攻撃を封印してダウン");

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddCrystalBreakBonus();
        }

        OnTailCrystalBroken?.Invoke();
        OnCrystalBroken?.Invoke();
    }
}