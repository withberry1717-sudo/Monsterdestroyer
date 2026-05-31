using UnityEngine;
using System.Collections.Generic;

public class AttackHitEffect : MonoBehaviour
{
    [Header("Hit Target")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private string dragonTag = "Dragon";
    [SerializeField] private string coreTag = "DragonCore";

    [Header("Weapon Type")]
    [Tooltip("ONならこの攻撃はダガー扱い。ヒットエフェクトを小さくできます。")]
    [SerializeField] private bool isDaggerAttack = false;

    [Header("Effect")]
    [SerializeField] private ParticleSystem hitSparkPrefab;
    [SerializeField] private float effectLifeTime = 1.5f;
    [SerializeField] private float effectScale = 1.0f;

    [Tooltip("ダガー攻撃時のエフェクト倍率。0.5なら通常の半分。")]
    [SerializeField] private float daggerEffectScaleMultiplier = 0.55f;

    [Header("Sound")]
    [SerializeField] private AudioClip hitSE;
    [SerializeField] private float volume = 0.8f;
    [SerializeField] private float pitchMin = 0.95f;
    [SerializeField] private float pitchMax = 1.08f;

    [Tooltip("ダガー攻撃時のSE音量倍率。")]
    [SerializeField] private float daggerVolumeMultiplier = 0.85f;

    [Header("Duplicate Prevention")]
    [SerializeField] private float sameTargetCooldown = 0.15f;

    [Header("Per Attack Limit")]
    [Tooltip("ONなら1回の攻撃中に出るパーティクル数を制限します。")]
    [SerializeField] private bool limitEffectsPerAttack = true;

    [Tooltip("1回の攻撃中に出せる最大パーティクル数。")]
    [SerializeField] private int maxEffectsPerAttack = 1;

    [Tooltip("SEも同じ回数までに制限する。")]
    [SerializeField] private bool alsoLimitSoundPerAttack = true;

    [Header("Auto Reset")]
    [Tooltip("ONなら、ColliderがOFF→ONになった瞬間に攻撃履歴を自動リセットします。二段攻撃でパーティクルが出ない時はON。")]
    [SerializeField] private bool resetWhenColliderTurnsOn = true;

    [Tooltip("このオブジェクト以下のColliderを監視します。空なら自動取得。")]
    [SerializeField] private Collider[] monitoredColliders;

    [Tooltip("ONならリセット時にConsoleへログを出します。確認用。")]
    [SerializeField] private bool logReset = false;

    private readonly Dictionary<Collider, float> lastHitTimes = new Dictionary<Collider, float>();

    private int effectCountThisAttack = 0;
    private int soundCountThisAttack = 0;

    private bool wasHitboxActive = false;

    private void Awake()
    {
        CacheMonitoredColliders();
    }

    private void OnEnable()
    {
        CacheMonitoredColliders();
        ClearHitHistory();
        wasHitboxActive = IsAnyMonitoredColliderActive();
    }

    private void Update()
    {
        if (!resetWhenColliderTurnsOn) return;

        bool isHitboxActive = IsAnyMonitoredColliderActive();

        // ColliderがOFFからONになった瞬間 = 新しい攻撃開始
        if (isHitboxActive && !wasHitboxActive)
        {
            ClearHitHistory();

            if (logReset)
            {
                Debug.Log("[AttackHitEffect] 新しい攻撃としてヒット履歴をリセット: " + gameObject.name, this);
            }
        }

        wasHitboxActive = isHitboxActive;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsValidTarget(other))
        {
            return;
        }

        if (IsRecentlyHit(other))
        {
            return;
        }

        RegisterHit(other);

        Vector3 hitPoint = other.ClosestPoint(transform.position);

        if (hitPoint == Vector3.zero)
        {
            hitPoint = other.transform.position;
        }

        if (CanPlayEffectThisAttack())
        {
            PlaySpark(hitPoint, other);
            effectCountThisAttack++;
        }

        if (CanPlaySoundThisAttack())
        {
            PlaySound(hitPoint);
            soundCountThisAttack++;
        }
    }

    private void CacheMonitoredColliders()
    {
        if (monitoredColliders != null && monitoredColliders.Length > 0)
        {
            return;
        }

        monitoredColliders = GetComponentsInChildren<Collider>(true);
    }

    private bool IsAnyMonitoredColliderActive()
    {
        if (monitoredColliders == null || monitoredColliders.Length == 0)
        {
            CacheMonitoredColliders();
        }

        if (monitoredColliders == null) return false;

        foreach (Collider col in monitoredColliders)
        {
            if (col == null) continue;

            if (col.enabled && col.gameObject.activeInHierarchy)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsValidTarget(Collider other)
    {
        if (other == null) return false;

        if (SafeCompareTag(other.transform, enemyTag)) return true;
        if (SafeCompareTag(other.transform, dragonTag)) return true;
        if (SafeCompareTag(other.transform, coreTag)) return true;

        Transform parent = other.transform.parent;

        while (parent != null)
        {
            if (SafeCompareTag(parent, enemyTag)) return true;
            if (SafeCompareTag(parent, dragonTag)) return true;
            if (SafeCompareTag(parent, coreTag)) return true;

            parent = parent.parent;
        }

        return false;
    }

    private bool SafeCompareTag(Transform target, string tagName)
    {
        if (target == null) return false;
        if (string.IsNullOrEmpty(tagName)) return false;

        try
        {
            return target.CompareTag(tagName);
        }
        catch
        {
            return false;
        }
    }

    private bool IsRecentlyHit(Collider other)
    {
        if (!lastHitTimes.ContainsKey(other))
        {
            return false;
        }

        return Time.time - lastHitTimes[other] < sameTargetCooldown;
    }

    private void RegisterHit(Collider other)
    {
        if (lastHitTimes.ContainsKey(other))
        {
            lastHitTimes[other] = Time.time;
        }
        else
        {
            lastHitTimes.Add(other, Time.time);
        }
    }

    private bool CanPlayEffectThisAttack()
    {
        if (!limitEffectsPerAttack) return true;

        int safeMax = Mathf.Max(0, maxEffectsPerAttack);
        return effectCountThisAttack < safeMax;
    }

    private bool CanPlaySoundThisAttack()
    {
        if (!alsoLimitSoundPerAttack) return true;
        if (!limitEffectsPerAttack) return true;

        int safeMax = Mathf.Max(0, maxEffectsPerAttack);
        return soundCountThisAttack < safeMax;
    }

    private void PlaySpark(Vector3 hitPoint, Collider other)
    {
        if (hitSparkPrefab == null) return;

        Quaternion rotation = Quaternion.identity;

        Vector3 direction = (hitPoint - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            rotation = Quaternion.LookRotation(direction);
        }

        ParticleSystem spark = Instantiate(hitSparkPrefab, hitPoint, rotation);

        float finalScale = effectScale;

        if (isDaggerAttack)
        {
            finalScale *= daggerEffectScaleMultiplier;
        }

        spark.transform.localScale *= finalScale;
        spark.Play();

        Destroy(spark.gameObject, effectLifeTime);
    }

    private void PlaySound(Vector3 hitPoint)
    {
        if (hitSE == null) return;

        GameObject audioObj = new GameObject("HitSE");
        audioObj.transform.position = hitPoint;

        AudioSource audioSource = audioObj.AddComponent<AudioSource>();
        audioSource.clip = hitSE;

        float finalVolume = volume;

        if (isDaggerAttack)
        {
            finalVolume *= daggerVolumeMultiplier;
        }

        audioSource.volume = finalVolume;
        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.spatialBlend = 0.4f;
        audioSource.Play();

        Destroy(audioObj, hitSE.length + 0.2f);
    }

    public void ClearHitHistory()
    {
        lastHitTimes.Clear();
        effectCountThisAttack = 0;
        soundCountThisAttack = 0;
    }

    // CombatやAnimation Eventから明示的に呼びたい時用
    public void BeginNewAttack()
    {
        ClearHitHistory();

        if (logReset)
        {
            Debug.Log("[AttackHitEffect] BeginNewAttackでリセット: " + gameObject.name, this);
        }
    }
}