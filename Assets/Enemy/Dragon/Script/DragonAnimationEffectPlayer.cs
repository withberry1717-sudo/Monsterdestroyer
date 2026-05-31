using UnityEngine;

public class DragonAnimationEffectPlayer : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;

    [Range(0f, 1f)]
    public float defaultVolume = 1f;

    [Header("Common SFX")]
    public AudioClip roarSfx;
    public AudioClip downSfx;
    public AudioClip deathSfx;
    public AudioClip stepSfx;
    public AudioClip swipeSfx;
    public AudioClip tailSlamSfx;
    public AudioClip tailSwipeSfx;
    public AudioClip breathChargeSfx;
    public AudioClip breathFireSfx;
    public AudioClip chargeStartSfx;
    public AudioClip chargeRunSfx;
    public AudioClip chargeEndSfx;

    [Header("Common Particles")]
    public ParticleSystem roarParticle;
    public ParticleSystem downParticle;
    public ParticleSystem deathParticle;
    public ParticleSystem stepParticle;
    public ParticleSystem swipeParticle;
    public ParticleSystem tailSlamParticle;
    public ParticleSystem tailSwipeParticle;
    public ParticleSystem breathChargeParticle;
    public ParticleSystem breathFireParticle;
    public ParticleSystem chargeStartParticle;
    public ParticleSystem chargeRunParticle;
    public ParticleSystem chargeEndParticle;

    public bool HasAudioSource => audioSource != null;

    private void Awake()
    {
        FindAudioSourceIfNeeded();
    }

    private void FindAudioSourceIfNeeded()
    {
        if (audioSource != null) return;

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = GetComponentInChildren<AudioSource>();

        if (audioSource == null)
            audioSource = GetComponentInParent<AudioSource>();
    }

    public void PlayRoar() => Play(roarSfx, roarParticle);
    public void PlayDown() => Play(downSfx, downParticle);
    public void PlayDeath() => Play(deathSfx, deathParticle);
    public void PlayStep() => Play(stepSfx, stepParticle);
    public void PlaySwipe() => Play(swipeSfx, swipeParticle);
    public void PlayTailSlam() => Play(tailSlamSfx, tailSlamParticle);
    public void PlayTailSwipe() => Play(tailSwipeSfx, tailSwipeParticle);
    public void PlayBreathCharge() => Play(breathChargeSfx, breathChargeParticle);
    public void PlayBreathFire() => Play(breathFireSfx, breathFireParticle);
    public void PlayChargeStart() => Play(chargeStartSfx, chargeStartParticle);
    public void PlayChargeRun() => Play(chargeRunSfx, chargeRunParticle);
    public void PlayChargeEnd() => Play(chargeEndSfx, chargeEndParticle);

    public void StopChargeRunParticle()
    {
        if (chargeRunParticle != null)
            chargeRunParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    public bool TryPlayCustomSfx(AudioClip clip)
    {
        if (clip == null) return false;

        FindAudioSourceIfNeeded();

        if (audioSource == null)
        {
            Debug.LogWarning("[DragonAnimationEffectPlayer] AudioSourceÇ™Ç†ÇËÇ‹ÇπÇÒÅB", this);
            return false;
        }

        audioSource.PlayOneShot(clip, defaultVolume);
        return true;
    }

    public bool TryPlayCustomParticle(ParticleSystem particle)
    {
        if (particle == null) return false;

        particle.Play();
        return true;
    }

    public void PlayCustomSfx(AudioClip clip)
    {
        TryPlayCustomSfx(clip);
    }

    public void PlayCustomParticle(ParticleSystem particle)
    {
        TryPlayCustomParticle(particle);
    }

    private void Play(AudioClip clip, ParticleSystem particle)
    {
        TryPlayCustomSfx(clip);

        if (particle != null)
            particle.Play();
    }
}