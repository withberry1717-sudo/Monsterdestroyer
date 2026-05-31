using System.Collections;
using UnityEngine;

public class DragonAnimationEffectPlayer : MonoBehaviour
{
    [System.Serializable]
    public class SfxSetting
    {
        [Tooltip("再生するSEです。")]
        public AudioClip clip;

        [Tooltip("このSEの音量です。最終音量 = Default Volume × この値")]
        [Range(0f, 2f)]
        public float volume = 1f;

        [Tooltip("このSEを何秒遅らせて鳴らすか。")]
        public float delay = 0f;
    }

    [Header("Audio")]
    public AudioSource audioSource;

    [Tooltip("全SE共通の基本音量です。各SEのVolumeと掛け算されます。")]
    [Range(0f, 1f)]
    public float defaultVolume = 1f;

    [Header("Difficulty / Timing Sync")]
    [Tooltip("ONならDragonAIのAnimation Speed倍率に合わせてSE Delayを補正します。Hardでアニメが1.1倍ならDelayは1/1.1になります。")]
    public bool scaleSfxDelayWithDragonAnimationSpeed = true;

    [Tooltip("未設定なら親/子から自動で探します。DragonAIのdifficultyAnimationSpeedMultiplierを読みます。")]
    public DragonAI dragonAI;

    [Tooltip("手動でSE Delay全体を調整する倍率です。1が通常。0.8なら全体的に早く、1.2なら遅く鳴ります。")]
    public float manualDelayMultiplier = 1f;

    [Header("Common SFX")]
    public SfxSetting roar = new SfxSetting();
    public SfxSetting down = new SfxSetting();
    public SfxSetting death = new SfxSetting();
    public SfxSetting step = new SfxSetting();
    public SfxSetting swipe = new SfxSetting();
    public SfxSetting tailSlam = new SfxSetting();
    public SfxSetting tailSwipe = new SfxSetting();

    [Header("Tail Swipe Special SFX")]
    [Tooltip("Tail Swipeの1段目叩きつけ専用SEです。Clipが空ならTail SlamのClipを使います。ただしDelay/Volumeはここ専用です。")]
    public SfxSetting tailSwipeFirstSlam = new SfxSetting();

    [Tooltip("Tail Swipeの2段目回転1ループごとに鳴らすSEです。Clipが空ならTail SwipeのClipを使います。ただしDelay/Volumeはここ専用です。")]
    public SfxSetting tailSwipeSpinLoop = new SfxSetting();

    [Header("Common SFX Continued")]
    public SfxSetting breathCharge = new SfxSetting();
    public SfxSetting breathFire = new SfxSetting();
    public SfxSetting chargeStart = new SfxSetting();
    public SfxSetting chargeRun = new SfxSetting();
    public SfxSetting chargeEnd = new SfxSetting();

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

    [Header("Compatibility: Old Clip Fields")]
    [Tooltip("古いInspector設定を引き継ぐための互換欄です。空なら無視します。新しく設定するなら上のCommon SFX側を使ってください。")]
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

    public bool HasAudioSource => audioSource != null;

    private void Awake()
    {
        FindAudioSourceIfNeeded();
        FindDragonAIIfNeeded();
        MigrateOldClipFieldsIfNeeded();
    }

    private void OnValidate()
    {
        manualDelayMultiplier = Mathf.Max(0f, manualDelayMultiplier);
        ClampSetting(roar);
        ClampSetting(down);
        ClampSetting(death);
        ClampSetting(step);
        ClampSetting(swipe);
        ClampSetting(tailSlam);
        ClampSetting(tailSwipe);
        ClampSetting(tailSwipeFirstSlam);
        ClampSetting(tailSwipeSpinLoop);
        ClampSetting(breathCharge);
        ClampSetting(breathFire);
        ClampSetting(chargeStart);
        ClampSetting(chargeRun);
        ClampSetting(chargeEnd);
    }

    private void ClampSetting(SfxSetting setting)
    {
        if (setting == null) return;
        setting.delay = Mathf.Max(0f, setting.delay);
        setting.volume = Mathf.Max(0f, setting.volume);
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

    private void FindDragonAIIfNeeded()
    {
        if (dragonAI != null) return;

        dragonAI = GetComponent<DragonAI>();

        if (dragonAI == null)
            dragonAI = GetComponentInChildren<DragonAI>();

        if (dragonAI == null)
            dragonAI = GetComponentInParent<DragonAI>();
    }

    private void MigrateOldClipFieldsIfNeeded()
    {
        AssignOldClipIfNewEmpty(roar, roarSfx);
        AssignOldClipIfNewEmpty(down, downSfx);
        AssignOldClipIfNewEmpty(death, deathSfx);
        AssignOldClipIfNewEmpty(step, stepSfx);
        AssignOldClipIfNewEmpty(swipe, swipeSfx);
        AssignOldClipIfNewEmpty(tailSlam, tailSlamSfx);
        AssignOldClipIfNewEmpty(tailSwipe, tailSwipeSfx);
        AssignOldClipIfNewEmpty(breathCharge, breathChargeSfx);
        AssignOldClipIfNewEmpty(breathFire, breathFireSfx);
        AssignOldClipIfNewEmpty(chargeStart, chargeStartSfx);
        AssignOldClipIfNewEmpty(chargeRun, chargeRunSfx);
        AssignOldClipIfNewEmpty(chargeEnd, chargeEndSfx);
    }

    private void AssignOldClipIfNewEmpty(SfxSetting setting, AudioClip oldClip)
    {
        if (setting == null) return;

        if (setting.clip == null && oldClip != null)
        {
            setting.clip = oldClip;
        }
    }

    public void PlayRoar() => Play(roar, roarParticle);
    public void PlayDown() => Play(down, downParticle);
    public void PlayDeath() => Play(death, deathParticle);
    public void PlayStep() => Play(step, stepParticle);
    public void PlaySwipe() => Play(swipe, swipeParticle);
    public void PlayTailSlam() => Play(tailSlam, tailSlamParticle);
    public void PlayTailSwipe() => Play(tailSwipe, tailSwipeParticle);

    // DragonAIから直接呼ぶ用。TailSlamのDelay/Volumeを確実に使う。
    public bool TryPlayTailSlamSfxOnly()
    {
        return TryPlaySfxSetting(tailSlam);
    }

    // TailSlamの当たり判定ONの瞬間に鳴らす用。Delayは無視し、Clip/Volumeだけ使う。
    public bool TryPlayTailSlamSfxOnHitImmediate()
    {
        return TryPlaySfxSettingImmediate(tailSlam);
    }

    public void PlayTailSwipeFirstSlam() => TryPlayTailSwipeFirstSlamSfxOnly();
    public void PlayTailSwipeSpinLoop() => TryPlayTailSwipeSpinLoopSfxOnly();

    // TailSwipe一段目専用。Clipが空ならTailSlamのClipを使うが、Delay/VolumeはtailSwipeFirstSlamを使う。
    public bool TryPlayTailSwipeFirstSlamSfxOnly()
    {
        return TryPlaySpecialSfx(tailSwipeFirstSlam, tailSlam);
    }

    // TailSwipe一段目の当たり判定ONの瞬間に鳴らす用。Delayは無視し、Clipは空ならTailSlamを使う。
    public bool TryPlayTailSwipeFirstSlamSfxOnHitImmediate()
    {
        return TryPlaySpecialSfxImmediate(tailSwipeFirstSlam, tailSlam, tailSlamSfx);
    }

    // TailSwipe二段目回転専用。Clipが空ならTailSwipeのClipを使うが、Delay/VolumeはtailSwipeSpinLoopを使う。
    public bool TryPlayTailSwipeSpinLoopSfxOnly()
    {
        return TryPlaySpecialSfx(tailSwipeSpinLoop, tailSwipe);
    }

    // 互換用。DragonAI古い版から呼ばれても、fallbackClipのDelayにはせず、専用設定のDelay/Volumeを使う。
    public bool TryPlayTailSwipeFirstSlamSfx(AudioClip fallbackClip)
    {
        return TryPlaySpecialSfx(tailSwipeFirstSlam, tailSlam, fallbackClip, tailSlamSfx);
    }

    public bool TryPlayTailSwipeSpinLoopSfx(AudioClip fallbackClip)
    {
        return TryPlaySpecialSfx(tailSwipeSpinLoop, tailSwipe, fallbackClip, tailSwipeSfx);
    }

    public void PlayBreathCharge() => Play(breathCharge, breathChargeParticle);
    public void PlayBreathFire() => Play(breathFire, breathFireParticle);
    public void PlayChargeStart() => Play(chargeStart, chargeStartParticle);
    public void PlayChargeRun() => Play(chargeRun, chargeRunParticle);
    public void PlayChargeEnd() => Play(chargeEnd, chargeEndParticle);

    public void StopChargeRunParticle()
    {
        if (chargeRunParticle != null)
        {
            chargeRunParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    public bool TryPlayCustomSfx(AudioClip clip)
    {
        if (clip == null) return false;

        SfxSetting matchedSetting = FindSettingByClip(clip);

        if (matchedSetting != null)
        {
            return TryPlaySfxSetting(matchedSetting);
        }

        return TryPlayCustomSfx(clip, 0f, 1f);
    }

    public bool TryPlayCustomSfx(AudioClip clip, float delay)
    {
        return TryPlayCustomSfx(clip, delay, 1f);
    }

    public bool TryPlayCustomSfx(AudioClip clip, float delay, float volumeMultiplier)
    {
        if (clip == null) return false;

        FindAudioSourceIfNeeded();

        if (audioSource == null)
        {
            Debug.LogWarning("[DragonAnimationEffectPlayer] AudioSourceがありません。", this);
            return false;
        }

        float finalDelay = GetEffectiveDelay(delay);
        float finalVolume = Mathf.Clamp01(defaultVolume * Mathf.Max(0f, volumeMultiplier));

        if (finalDelay > 0f)
        {
            StartCoroutine(PlayOneShotDelayed(clip, finalDelay, finalVolume));
        }
        else
        {
            audioSource.PlayOneShot(clip, finalVolume);
        }

        return true;
    }

    private bool TryPlaySpecialSfx(SfxSetting specialSetting, SfxSetting defaultClipSetting)
    {
        return TryPlaySpecialSfx(specialSetting, defaultClipSetting, null, null);
    }

    private bool TryPlaySpecialSfx(SfxSetting specialSetting, SfxSetting defaultClipSetting, AudioClip fallbackClip, AudioClip oldFallbackClip)
    {
        if (specialSetting == null) return false;

        AudioClip clip = specialSetting.clip;

        if (clip == null && defaultClipSetting != null)
        {
            clip = defaultClipSetting.clip;
        }

        if (clip == null)
        {
            clip = fallbackClip;
        }

        if (clip == null)
        {
            clip = oldFallbackClip;
        }

        if (clip == null) return false;

        // 重要：Delay/Volumeは必ずspecialSetting側を使う。
        // TailSlamと同じClipでも、TailSwipe一段目のDelayを別にできる。
        return TryPlayCustomSfx(clip, specialSetting.delay, specialSetting.volume);
    }

    private bool TryPlaySfxSettingImmediate(SfxSetting setting)
    {
        if (setting == null) return false;
        if (setting.clip == null) return false;

        return TryPlayCustomSfxImmediate(setting.clip, setting.volume);
    }

    private bool TryPlaySpecialSfxImmediate(SfxSetting specialSetting, SfxSetting defaultClipSetting, AudioClip oldFallbackClip)
    {
        if (specialSetting == null) return false;

        AudioClip clip = specialSetting.clip;

        if (clip == null && defaultClipSetting != null)
        {
            clip = defaultClipSetting.clip;
        }

        if (clip == null)
        {
            clip = oldFallbackClip;
        }

        if (clip == null) return false;

        // 重要：当たり判定ONの瞬間に鳴らすのでDelayは必ず0。
        // VolumeはspecialSetting側を使う。
        return TryPlayCustomSfxImmediate(clip, specialSetting.volume);
    }

    public bool TryPlayCustomSfxImmediate(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip == null) return false;

        FindAudioSourceIfNeeded();

        if (audioSource == null)
        {
            Debug.LogWarning("[DragonAnimationEffectPlayer] AudioSourceがありません。", this);
            return false;
        }

        float finalVolume = Mathf.Clamp01(defaultVolume * Mathf.Max(0f, volumeMultiplier));
        audioSource.PlayOneShot(clip, finalVolume);
        return true;
    }

    public bool TryPlayCustomParticle(ParticleSystem particle)
    {
        if (particle == null) return false;

        particle.Play(true);
        return true;
    }

    public void PlayCustomSfx(AudioClip clip)
    {
        TryPlayCustomSfx(clip);
    }

    public void PlayCustomSfxDelayed(AudioClip clip, float delay)
    {
        TryPlayCustomSfx(clip, delay);
    }

    public void PlayCustomParticle(ParticleSystem particle)
    {
        TryPlayCustomParticle(particle);
    }

    private void Play(SfxSetting sfxSetting, ParticleSystem particle)
    {
        if (particle != null)
        {
            particle.Play(true);
        }

        TryPlaySfxSetting(sfxSetting);
    }

    private bool TryPlaySfxSetting(SfxSetting setting)
    {
        if (setting == null) return false;
        if (setting.clip == null) return false;

        return TryPlayCustomSfx(setting.clip, setting.delay, setting.volume);
    }

    private SfxSetting FindSettingByClip(AudioClip clip)
    {
        if (clip == null) return null;

        if (roar != null && roar.clip == clip) return roar;
        if (down != null && down.clip == clip) return down;
        if (death != null && death.clip == clip) return death;
        if (step != null && step.clip == clip) return step;
        if (swipe != null && swipe.clip == clip) return swipe;
        if (tailSlam != null && tailSlam.clip == clip) return tailSlam;
        if (tailSwipe != null && tailSwipe.clip == clip) return tailSwipe;
        if (breathCharge != null && breathCharge.clip == clip) return breathCharge;
        if (breathFire != null && breathFire.clip == clip) return breathFire;
        if (chargeStart != null && chargeStart.clip == clip) return chargeStart;
        if (chargeRun != null && chargeRun.clip == clip) return chargeRun;
        if (chargeEnd != null && chargeEnd.clip == clip) return chargeEnd;

        return null;
    }

    private float GetEffectiveDelay(float rawDelay)
    {
        float delay = Mathf.Max(0f, rawDelay);
        delay *= Mathf.Max(0f, manualDelayMultiplier);

        if (!scaleSfxDelayWithDragonAnimationSpeed)
        {
            return delay;
        }

        FindDragonAIIfNeeded();

        if (dragonAI == null)
        {
            return delay;
        }

        float animationSpeed = Mathf.Max(0.01f, dragonAI.difficultyAnimationSpeedMultiplier);

        return delay / animationSpeed;
    }

    private IEnumerator PlayOneShotDelayed(AudioClip clip, float delay, float volume)
    {
        yield return new WaitForSeconds(delay);

        if (clip == null) yield break;

        FindAudioSourceIfNeeded();

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
