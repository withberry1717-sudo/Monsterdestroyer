using UnityEngine;
using System.Collections.Generic;

public class DragonAttackHitbox : MonoBehaviour
{
    [Header("攻撃設定")]
    [Tooltip("プレイヤーに与えるダメージ")]
    public float damage = 20f;

    [Tooltip("吹っ飛ばしの強さ")]
    public float knockbackPower = 5f;

    [Tooltip("プレイヤーを怯ませる時間")]
    public float staggerTime = 0.5f;

    [Header("対象")]
    [Tooltip("攻撃を当てる対象Layer。Playerなどを指定")]
    public LayerMask targetLayers;

    [Header("多段ヒット防止")]
    [Tooltip("オンなら、1回の攻撃判定ON中に同じ対象へ1回だけ当たる")]
    public bool hitOnlyOncePerActivation = true;

    [Header("Activation VFX / SFX")]
    [Tooltip("攻撃判定がONになった時に再生するパーティクル")]
    public ParticleSystem activationParticle;

    [Tooltip("攻撃判定がONになっている間だけ再生するループパーティクル")]
    public ParticleSystem loopParticle;

    [Tooltip("攻撃判定がONになった時のSE")]
    public AudioClip activationSfx;

    [Header("Hit VFX / SFX")]
    [Tooltip("ヒットした場所に出すパーティクルPrefab")]
    public GameObject hitParticlePrefab;

    [Tooltip("ヒット時SE")]
    public AudioClip hitSfx;

    [Header("Audio")]
    [Tooltip("AudioSource。未設定なら自分か親から探す")]
    public AudioSource audioSource;

    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    private Collider hitboxCollider;
    private readonly HashSet<GameObject> hitTargets = new HashSet<GameObject>();

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();

        if (hitboxCollider == null)
        {
            Debug.LogWarning($"{name} に Collider がありません");
            return;
        }

        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponentInParent<AudioSource>();
        }
    }

    public void EnableHitbox()
    {
        hitTargets.Clear();

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
        }

        if (activationParticle != null)
        {
            activationParticle.Play();
        }

        if (loopParticle != null)
        {
            loopParticle.Play();
        }

        PlayOneShot(activationSfx);
    }

    public void DisableHitbox()
    {
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }

        if (loopParticle != null)
        {
            loopParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryHit(other);
    }

    private void TryHit(Collider other)
    {
        if (((1 << other.gameObject.layer) & targetLayers) == 0)
        {
            return;
        }

        GameObject target = other.gameObject;

        if (hitOnlyOncePerActivation && hitTargets.Contains(target))
        {
            return;
        }

        hitTargets.Add(target);

        target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        target.SendMessage("DragonStagger", staggerTime, SendMessageOptions.DontRequireReceiver);

        Rigidbody rb = target.GetComponent<Rigidbody>();

        if (rb != null)
        {
            Vector3 dir = target.transform.position - transform.position;
            dir.y = 0.2f;

            if (dir.sqrMagnitude < 0.001f)
            {
                dir = transform.forward;
            }

            rb.AddForce(dir.normalized * knockbackPower, ForceMode.Impulse);
        }

        SpawnHitParticle(other);
        PlayOneShot(hitSfx);
    }

    private void SpawnHitParticle(Collider other)
    {
        if (hitParticlePrefab == null) return;

        Vector3 spawnPos = other.ClosestPoint(transform.position);
        Quaternion spawnRot = Quaternion.LookRotation((spawnPos - transform.position).normalized);

        Instantiate(hitParticlePrefab, spawnPos, spawnRot);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource == null) return;

        audioSource.PlayOneShot(clip, sfxVolume);
    }
}