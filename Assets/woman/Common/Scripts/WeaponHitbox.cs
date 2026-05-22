using UnityEngine;
using System.Collections; // ★時間をコントロールするコルーチンに必要です！

public class WeaponHitbox : MonoBehaviour
{
    private Collider _collider;
    private bool _hasHit; // 二度当たり防止用

    [Header("攻撃設定")]
    public int damage = 1;

    [Header("演出設定")]
    public float hitStopDuration = 0.1f; // 最初から体感しやすい0.1秒にしておきます

    [Header("★サイズ自由調整（アニメーション対策）")]
    [Tooltip("インスペクターのサイズ設定を強制適用するかどうか")]
    public bool fixScale = true;

    [Tooltip("ここを変えると武器の大きさがリアルタイムに変わります")]
    public Vector3 weaponScale = new Vector3(1.5f, 1.5f, 1.5f); // いつもの綺麗なXYZ入力が出ます！

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
        // 敵に当たった時の処理
        if (_hasHit || !other.CompareTag("Enemy")) return;

        _hasHit = true;
        Debug.Log(gameObject.name + " が命中！");

        // 当たった相手（Enemy）からEnemyHPコンポーネントを取得してダメージを与える
        EnemyHP enemyHP = other.GetComponent<EnemyHP>();
        if (enemyHP != null)
        {
            enemyHP.TakeDamage(damage);
        }

        // ★HitStop.cs（幽霊ファイル）は使いません！武器自身が直接時間を一瞬止めます
        StartCoroutine(DoHitStop(hitStopDuration));
    }

    // ★ここにヒットストップの処理を直接内蔵しました
    private IEnumerator DoHitStop(float duration)
    {
        Time.timeScale = 0.02f; // ゲーム全体の時間をほぼ停止（1/50の遅さに）
        yield return new WaitForSecondsRealtime(duration); // 現実の秒数で待つ
        Time.timeScale = 1f; // 時間を元に戻す
    }

    // アニメーションの上書きに打ち勝ってサイズを維持する魔法のタイミング
    void LateUpdate()
    {
        if (fixScale)
        {
            transform.localScale = weaponScale;
        }
    }
}