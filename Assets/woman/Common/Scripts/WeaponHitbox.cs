using UnityEngine;
using System.Collections;

public class WeaponHitbox : MonoBehaviour
{
    private Collider _collider;
    private bool _hasHit; // 二度当たり防止用

    [Header("攻撃設定")]
    // 倍率計算（1.5倍など）を綺麗にするため、int（整数）から float（小数）に変更しました！
    public float damage = 10f;
    [Header("ダメージ乱数")]
    [Range(0f, 1f)]
    public float damageVariation = 0.15f;

    [Header("演出設定")]
    public float hitStopDuration = 0.1f;

    [Header("サイズ自由調整（アニメーション対策）")]
    [Tooltip("インスペクターのサイズ設定を強制適用するかどうか")]
    public bool fixScale = true;

    [Tooltip("ここを変えると武器の大きさがリアルタイムに変わります")]
    public Vector3 weaponScale = new Vector3(1.5f, 1.5f, 1.5f);

    void Start()
    {
        _collider = GetComponent<Collider>();
        _collider.enabled = false;
    }

    public void EnableHitbox()
    {
        _collider.enabled = true;
        _hasHit = false; // 攻撃開始時に判定をリセット
    }

    public void DisableHitbox()
    {
        _collider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 敵（Enemyタグ）に当たった時の処理
        if (_hasHit || !other.CompareTag("Enemy")) return;

        _hasHit = true;
        Debug.Log(gameObject.name + " が命中！");

        // 【超重要】新しく作ったドラゴンの当たり判定を探す！
        DragonHurtbox dragonHurtbox = other.GetComponent<DragonHurtbox>();
        if (dragonHurtbox != null)
        {
            // ランダム倍率を作る
            float randomMultiplier = Random.Range(
                1f - damageVariation,
                1f + damageVariation
            );

            // 最終ダメージ
            float finalDamage = damage * randomMultiplier;

            // ドラゴンへ送る
            dragonHurtbox.OnHit(finalDamage);
        }
        else
        {
            // ※もしドラゴン以外の古い敵（EnemyHP）がいてもエラーにならないように残しておきます
            EnemyHP enemyHP = other.GetComponent<EnemyHP>();
            if (enemyHP != null)
            {
                float randomMultiplier = Random.Range(
                    1f - damageVariation,
                    1f + damageVariation
                );

                float finalDamage = damage * randomMultiplier;

                enemyHP.TakeDamage(finalDamage);
            }
        }

        // ヒットストップ
        StartCoroutine(DoHitStop(hitStopDuration));
    }

    private IEnumerator DoHitStop(float duration)
    {
        Time.timeScale = 0.02f; // ゲーム全体の時間をほぼ停止
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f; // 時間を元に戻す
    }

    void LateUpdate()
    {
        if (fixScale)
        {
            transform.localScale = weaponScale;
        }
    }
}