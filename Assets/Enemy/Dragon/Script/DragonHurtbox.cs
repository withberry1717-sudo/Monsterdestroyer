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
    [Tooltip("尻尾クリスタルなど、クリスタル部位ならオン")]
    public bool isCrystalPart = false;

    [Header("Hit VFX")]
    public ParticleSystem bodyHitParticle;
    public ParticleSystem crystalHitParticle;

    [Header("Hit SFX")]
    public AudioSource audioSource;
    public AudioClip bodyHitSfx;
    public AudioClip crystalHitSfx;

    [Range(0f, 1f)]
    public float hitSfxVolume = 1f;

    private DragonDamageTextSettings damageTextSettings;

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
    }

    public void OnHit(float baseDamage)
    {
        OnHit(baseDamage, transform.position);
    }

    public void OnHit(float baseDamage, Vector3 hitPosition)
    {
        float finalDamage = baseDamage * damageMultiplier;

        if (dragonHP == null)
        {
            Debug.LogWarning($"{name}: DragonHPがセットされていません");
            return;
        }

        ShowDamageText(finalDamage, hitPosition);

        if (isCrystalPart)
        {
            dragonHP.TakeCrystalDamage(finalDamage);
            PlayCrystalHitFeedback();
            return;
        }

        dragonHP.TakeDamage(finalDamage);
        PlayBodyHitFeedback();
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

    private void PlayCrystalHitFeedback()
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