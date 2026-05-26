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
    [Tooltip("本体にダメージが入った時のエフェクト")]
    public ParticleSystem bodyHitParticle;

    [Tooltip("クリスタルにダメージが入った時のエフェクト")]
    public ParticleSystem crystalHitParticle;

    [Header("Hit SFX")]
    [Tooltip("AudioSource。未設定なら自分か親から探す")]
    public AudioSource audioSource;

    [Tooltip("本体にダメージが入った時のSE")]
    public AudioClip bodyHitSfx;

    [Tooltip("クリスタルにダメージが入った時のSE")]
    public AudioClip crystalHitSfx;

    [Range(0f, 1f)]
    public float hitSfxVolume = 1f;

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
    }

    public void OnHit(float baseDamage)
    {
        float finalDamage = baseDamage * damageMultiplier;

        if (dragonHP == null)
        {
            Debug.LogWarning($"{name}: DragonHPがセットされていません");
            return;
        }

        if (isCrystalPart)
        {
            dragonHP.TakeCrystalDamage(finalDamage);
            PlayCrystalHitFeedback();
            return;
        }

        dragonHP.TakeDamage(finalDamage);
        PlayBodyHitFeedback();
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