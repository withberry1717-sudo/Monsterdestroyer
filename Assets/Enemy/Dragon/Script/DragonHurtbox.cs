using UnityEngine;

public class DragonHurtbox : MonoBehaviour
{
    [Header("本体のHP管理スクリプトを登録")]
    [Tooltip("DragonCoreについているDragonHPを入れる")]
    public DragonHP dragonHP;

    [Header("ダメージ倍率設定")]
    [Tooltip("この部位に当たった時のダメージ倍率")]
    public float damageMultiplier = 1.0f;

    [Header("部位設定")]
    [Tooltip("尻尾切断用の部位ならオン。昔のクリスタル部位扱いと同じです。")]
    public bool isCrystalPart = false;

    [Tooltip("尻尾切断後、このDragonHurtbox自体を無効化します。尻尾切断部位ならオン推奨です。")]
    [SerializeField] private bool disableThisHurtboxWhenTailSevered = true;

    [Tooltip("尻尾切断後、このGameObjectについているColliderを自動でOFFにします。")]
    [SerializeField] private bool disableCollidersWhenTailSevered = true;

    [Header("Hit VFX")]
    [Tooltip("本体にヒットした時のパーティクル")]
    public ParticleSystem bodyHitParticle;

    [Tooltip("尻尾切断部位にヒットした時のパーティクル")]
    public ParticleSystem crystalHitParticle;

    [Header("Hit SFX")]
    [Tooltip("効果音再生用AudioSource。未設定なら自分か親から探します。")]
    public AudioSource audioSource;

    [Tooltip("本体にヒットした時の効果音")]
    public AudioClip bodyHitSfx;

    [Tooltip("尻尾切断部位にヒットした時の効果音")]
    public AudioClip crystalHitSfx;

    [Range(0f, 1f)]
    [Tooltip("ヒット効果音の音量")]
    public float hitSfxVolume = 1f;

    private DragonDamageTextSettings damageTextSettings;
    private Collider[] cachedColliders;
    private bool tailSeverHurtboxDisabled = false;

    private void Awake()
    {
        if (dragonHP == null)
        {
            dragonHP = GetComponentInParent<DragonHP>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponentInParent<AudioSource>();
        }

        damageTextSettings = GetComponentInParent<DragonDamageTextSettings>();
        cachedColliders = GetComponents<Collider>();
    }

    private void OnEnable()
    {
        tailSeverHurtboxDisabled = false;
    }

    public void OnHit(float baseDamage)
    {
        OnHit(baseDamage, transform.position);
    }

    public void OnHit(float baseDamage, Vector3 hitPosition)
    {
        if (dragonHP == null)
        {
            Debug.LogWarning($"{name}: DragonHPがセットされていません");
            return;
        }

        if (dragonHP.isDead) return;

        if (isCrystalPart)
        {
            HandleTailSeverPartHit(baseDamage, hitPosition);
            return;
        }

        HandleBodyHit(baseDamage, hitPosition);
    }

    private void HandleTailSeverPartHit(float baseDamage, Vector3 hitPosition)
    {
        if (dragonHP.IsTailSevered())
        {
            DisableTailSeverHurtboxIfNeeded();
            return;
        }

        float severPartDamage = Mathf.Max(0f, baseDamage * damageMultiplier);

        ShowDamageText(severPartDamage, hitPosition);
        dragonHP.TakeTailSeverPartDamage(severPartDamage);
        PlayTailSeverPartHitFeedback();

        if (dragonHP.IsTailSevered())
        {
            DisableTailSeverHurtboxIfNeeded();
        }
    }

    private void HandleBodyHit(float baseDamage, Vector3 hitPosition)
    {
        float bodyDamage = Mathf.Max(0f, baseDamage * damageMultiplier);

        ShowDamageText(bodyDamage, hitPosition);
        dragonHP.TakeDamage(bodyDamage);
        PlayBodyHitFeedback();
    }

    private void DisableTailSeverHurtboxIfNeeded()
    {
        if (!isCrystalPart) return;
        if (tailSeverHurtboxDisabled) return;

        tailSeverHurtboxDisabled = true;

        if (disableCollidersWhenTailSevered && cachedColliders != null)
        {
            foreach (Collider col in cachedColliders)
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }
        }

        if (disableThisHurtboxWhenTailSevered)
        {
            enabled = false;
        }
    }

    private void ShowDamageText(float damage, Vector3 hitPosition)
    {
        if (damageTextSettings == null) return;
        if (damageTextSettings.damageTextSpawner == null) return;

        Vector3 spawnPosition = hitPosition + Vector3.up * damageTextSettings.heightOffset;
        damageTextSettings.damageTextSpawner.SpawnDamageText(damage, spawnPosition);
    }

    private void PlayBodyHitFeedback()
    {
        if (bodyHitParticle != null)
        {
            bodyHitParticle.Play();
        }

        PlayOneShot(bodyHitSfx);
    }

    private void PlayTailSeverPartHitFeedback()
    {
        if (crystalHitParticle != null)
        {
            crystalHitParticle.Play();
        }

        PlayOneShot(crystalHitSfx);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource == null) return;

        audioSource.PlayOneShot(clip, hitSfxVolume);
    }
}