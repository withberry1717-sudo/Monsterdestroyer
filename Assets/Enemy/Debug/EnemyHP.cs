using UnityEngine;

public class EnemyHP : MonoBehaviour
{
    [Header("HP設定")]
    [SerializeField] private float maxHP = 15f;

    private float currentHP;

    [Header("Damage UI")]
    [SerializeField] private DamageTextSpawner damageTextSpawner;

    void Start()
    {
        currentHP = maxHP;

        // 自動取得
        if (damageTextSpawner == null)
        {
            damageTextSpawner = FindAnyObjectByType<DamageTextSpawner>();
        }

        Debug.Log($"{gameObject.name} のHPが設定されました。初期HP: {currentHP}");
    }

    // ダメージを受ける関数
    public void TakeDamage(float damage)
    {
        if (currentHP <= 0) return;

        currentHP -= damage;

        Debug.Log($"{gameObject.name} に {damage} ダメージ！ 残りHP: {currentHP}");

        // ダメージUI表示
        if (damageTextSpawner != null)
        {
            damageTextSpawner.ShowDamage(damage, transform.position);
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // 死亡処理
    private void Die()
    {
        Debug.Log($"{gameObject.name} を撃破しました！");

        Destroy(gameObject);
    }
}