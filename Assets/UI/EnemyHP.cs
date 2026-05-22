using UnityEngine;

public class EnemyHP : MonoBehaviour
{
    [Header("HP設定")]
    [SerializeField] private int maxHP = 15;
    private int currentHP;

    void Start()
    {
        currentHP = maxHP;
        Debug.Log($"{gameObject.name} のHPが設定されました。初期HP: {currentHP}");
    }

    // ダメージを受ける関数（プレイヤー側から呼ばれる）
    public void TakeDamage(int damage)
    {
        if (currentHP <= 0) return;

        currentHP -= damage;
        Debug.Log($"{gameObject.name} に {damage} ダメージ！ 残りHP: {currentHP}");

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // 死亡処理
    private void Die()
    {
        Debug.Log($"{gameObject.name} を撃破しました！");

        // とりあえずゲームから消去する
        Destroy(gameObject);
    }
}