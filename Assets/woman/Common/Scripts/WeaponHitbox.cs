using UnityEngine;
using System.Collections;

public class WeaponHitbox : MonoBehaviour
{
    private Collider _collider;
    private bool _hasHit;
    private float currentDamageMultiplier = 1f;

    [Header("攻撃設定")]
    public float damage = 10f;

    [Header("ダメージ乱数")]
    [Range(0f, 1f)]
    public float damageVariation = 0.15f;

    [Header("演出設定")]
    public float hitStopDuration = 0.1f;

    [Header("サイズ自由調整（アニメーション対策）")]
    public bool fixScale = true;
    public Vector3 weaponScale = new Vector3(1.5f, 1.5f, 1.5f);

    void Start()
    {
        _collider = GetComponent<Collider>();

        if (_collider != null)
        {
            _collider.enabled = false;
        }
    }

    public void EnableHitbox()
    {
        EnableHitbox(1f);
    }

    public void EnableHitbox(float damageMultiplier)
    {
        currentDamageMultiplier = Mathf.Max(0f, damageMultiplier);
        _hasHit = false;

        if (_collider != null)
        {
            _collider.enabled = true;
        }
    }

    public void DisableHitbox()
    {
        if (_collider != null)
        {
            _collider.enabled = false;
        }

        currentDamageMultiplier = 1f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasHit || !other.CompareTag("Enemy")) return;

        _hasHit = true;

        float randomMultiplier = Random.Range(
            1f - damageVariation,
            1f + damageVariation
        );

        float finalDamage = damage * currentDamageMultiplier * randomMultiplier;

        Debug.Log(gameObject.name + " が命中！ Damage: " + finalDamage);

        DragonHurtbox dragonHurtbox = other.GetComponent<DragonHurtbox>();

        if (dragonHurtbox != null)
        {
            dragonHurtbox.OnHit(finalDamage);
        }
        else
        {
            EnemyHP enemyHP = other.GetComponent<EnemyHP>();

            if (enemyHP != null)
            {
                enemyHP.TakeDamage(finalDamage);
            }
        }

        StartCoroutine(DoHitStop(hitStopDuration));
    }

    private IEnumerator DoHitStop(float duration)
    {
        float previousTimeScale = Time.timeScale;

        Time.timeScale = 0.02f;
        yield return new WaitForSecondsRealtime(duration);

        if (Time.timeScale != 0f)
        {
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        }
    }

    void LateUpdate()
    {
        if (fixScale)
        {
            transform.localScale = weaponScale;
        }
    }
}