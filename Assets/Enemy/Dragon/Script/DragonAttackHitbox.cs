using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class DragonAttackHitbox : MonoBehaviour
{
    [Header("攻撃設定")]
    [Tooltip("プレイヤーに与える基本ダメージ")]
    public float damage = 20f;

    [Tooltip("難易度などで上書きされるダメージ倍率。通常はDifficultyApplierから設定されます。")]
    [SerializeField] private float difficultyDamageMultiplier = 1f;

    [Tooltip("吹っ飛ばしの強さ")]
    public float knockbackPower = 5f;

    [Tooltip("上方向への吹っ飛び補正")]
    public float knockbackUpPower = 0.2f;

    [Tooltip("プレイヤーを怯ませる時間")]
    public float staggerTime = 0.5f;

    [Header("対象")]
    [Tooltip("攻撃を当てる対象Layer。Playerを指定")]
    public LayerMask targetLayers;

    [Header("多段ヒット防止")]
    [Tooltip("オンなら、1回の攻撃判定ON中に同じ対象へ1回だけ当たる")]
    public bool hitOnlyOncePerActivation = true;

    [Header("判定設定")]
    [Tooltip("子オブジェクトのColliderもまとめて攻撃判定として使う")]
    public bool useChildColliders = true;

    [Tooltip("開始時にColliderを自動でOFFにする")]
    public bool disableOnAwake = true;

    [Tooltip("デバッグログを出す")]
    public bool debugLog = true;

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

    private Rigidbody rb;
    private Collider[] hitboxColliders;
    private readonly HashSet<GameObject> hitTargets = new HashSet<GameObject>();

    public float CurrentDifficultyDamageMultiplier => difficultyDamageMultiplier;
    public float CurrentFinalDamage => damage * difficultyDamageMultiplier;

    private void Awake()
    {
        SetupRigidbody();
        CacheColliders();
        SetupAudio();

        if (targetLayers.value == 0)
        {
            Debug.LogWarning($"{name}: Target Layers が Nothing です。Playerを指定してください。");
        }
    }

    private void SetupRigidbody()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = false;
        rb.isKinematic = true;
    }

    private void CacheColliders()
    {
        if (useChildColliders)
        {
            hitboxColliders = GetComponentsInChildren<Collider>(true);
        }
        else
        {
            Collider ownCollider = GetComponent<Collider>();
            hitboxColliders = ownCollider != null
                ? new Collider[] { ownCollider }
                : new Collider[0];
        }

        if (hitboxColliders == null || hitboxColliders.Length == 0)
        {
            Debug.LogWarning($"{name}: Colliderが見つかりません。親か子にBox Colliderなどを付けてください。");
            return;
        }

        foreach (Collider col in hitboxColliders)
        {
            if (col == null) continue;

            col.isTrigger = true;

            if (disableOnAwake)
            {
                col.enabled = false;
            }
        }
    }

    private void SetupAudio()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponentInParent<AudioSource>();
        }
    }

    public void SetDifficultyDamageMultiplier(float multiplier)
    {
        difficultyDamageMultiplier = Mathf.Max(0f, multiplier);

        if (debugLog)
        {
            Debug.Log($"{name}: Dragon damage multiplier set to {difficultyDamageMultiplier}. Final damage = {CurrentFinalDamage}", this);
        }
    }

    public void EnableHitbox()
    {
        hitTargets.Clear();

        if (hitboxColliders == null || hitboxColliders.Length == 0)
        {
            CacheColliders();
        }

        foreach (Collider col in hitboxColliders)
        {
            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = true;
            }
        }

        if (activationParticle != null)
        {
            activationParticle.Play(true);
        }

        if (loopParticle != null)
        {
            loopParticle.Play(true);
        }

        PlayOneShot(activationSfx);

        if (debugLog)
        {
            Debug.Log($"{name}: Dragon attack hitbox ON");
        }
    }

    public void DisableHitbox()
    {
        if (hitboxColliders != null)
        {
            foreach (Collider col in hitboxColliders)
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }
        }

        if (loopParticle != null)
        {
            loopParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (debugLog)
        {
            Debug.Log($"{name}: Dragon attack hitbox OFF");
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
        if (other == null) return;

        if (((1 << other.gameObject.layer) & targetLayers.value) == 0)
        {
            return;
        }

        GameObject targetRoot = GetTargetRoot(other);

        if (targetRoot == null)
        {
            return;
        }

        if (hitOnlyOncePerActivation && hitTargets.Contains(targetRoot))
        {
            return;
        }

        hitTargets.Add(targetRoot);

        Vector3 hitPosition = other.ClosestPoint(transform.position);

        Vector3 knockDir = targetRoot.transform.position - transform.position;
        knockDir.y = knockbackUpPower;

        if (knockDir.sqrMagnitude < 0.001f)
        {
            knockDir = transform.forward + Vector3.up * knockbackUpPower;
        }

        knockDir.Normalize();

        float finalDamage = CurrentFinalDamage;

        if (debugLog)
        {
            Debug.Log($"{name}: hit {other.name} / root {targetRoot.name} / base damage {damage} / difficulty x{difficultyDamageMultiplier} / final damage {finalDamage}");
        }

        other.SendMessageUpwards(
            "TakeDamage",
            finalDamage,
            SendMessageOptions.DontRequireReceiver
        );

        other.SendMessageUpwards(
            "DragonStagger",
            staggerTime,
            SendMessageOptions.DontRequireReceiver
        );

        other.SendMessageUpwards(
            "DragonKnockback",
            knockDir,
            SendMessageOptions.DontRequireReceiver
        );

        ApplyRigidbodyKnockback(other, knockDir);

        SpawnHitParticle(hitPosition);
        PlayOneShot(hitSfx);
    }

    private GameObject GetTargetRoot(Collider other)
    {
        if (other.attachedRigidbody != null)
        {
            return other.attachedRigidbody.gameObject;
        }

        if (other.transform.root != null)
        {
            return other.transform.root.gameObject;
        }

        return other.gameObject;
    }

    private void ApplyRigidbodyKnockback(Collider other, Vector3 knockDir)
    {
        Rigidbody targetRb = other.attachedRigidbody;

        if (targetRb == null)
        {
            targetRb = other.GetComponentInParent<Rigidbody>();
        }

        if (targetRb == null)
        {
            if (debugLog)
            {
                Debug.Log($"{name}: 対象にRigidbodyなし。CharacterControllerならDragonKnockback側で処理します。");
            }

            return;
        }

        if (targetRb.isKinematic)
        {
            if (debugLog)
            {
                Debug.Log($"{name}: 対象RigidbodyがKinematicなのでAddForceは効きません。DragonKnockback側で処理します。");
            }

            return;
        }

        targetRb.AddForce(knockDir * knockbackPower, ForceMode.Impulse);
    }

    private void SpawnHitParticle(Vector3 spawnPos)
    {
        if (hitParticlePrefab == null) return;

        Vector3 dir = spawnPos - transform.position;

        if (dir.sqrMagnitude < 0.001f)
        {
            dir = transform.forward;
        }

        Quaternion spawnRot = Quaternion.LookRotation(dir.normalized);
        Instantiate(hitParticlePrefab, spawnPos, spawnRot);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource == null) return;

        audioSource.PlayOneShot(clip, sfxVolume);
    }

    private void OnDisable()
    {
        DisableHitbox();
    }
}
