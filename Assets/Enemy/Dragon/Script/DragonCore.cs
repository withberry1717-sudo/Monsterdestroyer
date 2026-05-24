using UnityEngine;

public class DragonCore : MonoBehaviour
{
    [Header("ドラゴンの本体HP")]
    public float maxHP = 100f;
    public float currentHP;

    [Header("クリスタルの共有HP")]
    public float maxCrystalHP = 30f;
    public float currentCrystalHP;
    public bool isCrystalBroken = false; // 壊れたかどうかのフラグ

    void Start()
    {
        currentHP = maxHP;
        currentCrystalHP = maxCrystalHP;
    }

    // 本体へのダメージ処理
    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        Debug.Log($"本体に {damage} のダメージ！ 残りHP: {currentHP}");

        if (currentHP <= 0)
        {
            Debug.Log("ドラゴン討伐完了！");
        }
    }

    // クリスタルへのダメージ処理（3つのコライダーからここが呼ばれる）
    public void TakeCrystalDamage(float damage)
    {
        if (isCrystalBroken) return; // 既に壊れていたらこれ以上HPを減らさない

        currentCrystalHP -= damage;
        Debug.Log($"クリスタル共有HPに {damage} のダメージ！ 残り耐久: {currentCrystalHP}");

        if (currentCrystalHP <= 0)
        {
            isCrystalBroken = true;
            Debug.Log("クリスタル完全破壊！！（エフェクト発生など）");
            // ※ここでクリスタルが割れる処理などを実行
        }
    }
}