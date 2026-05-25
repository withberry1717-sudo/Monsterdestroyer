using UnityEngine;
using System.Collections.Generic;

public class AttackHitEffect : MonoBehaviour
{
    [Header("Hit Target")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private string dragonTag = "Dragon";
    [SerializeField] private string coreTag = "DragonCore";

    [Header("Effect")]
    [SerializeField] private ParticleSystem hitSparkPrefab;
    [SerializeField] private float effectLifeTime = 1.5f;
    [SerializeField] private float effectScale = 1.0f;

    [Header("Sound")]
    [SerializeField] private AudioClip hitSE;
    [SerializeField] private float volume = 0.8f;
    [SerializeField] private float pitchMin = 0.95f;
    [SerializeField] private float pitchMax = 1.08f;

    [Header("Duplicate Prevention")]
    [SerializeField] private float sameTargetCooldown = 0.15f;

    private readonly Dictionary<Collider, float> lastHitTimes = new Dictionary<Collider, float>();

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

        PlaySpark(hitPoint, other);
        PlaySound(hitPoint);
    }

    private bool IsValidTarget(Collider other)
    {
        if (other == null) return false;

        if (other.CompareTag(enemyTag)) return true;
        if (other.CompareTag(dragonTag)) return true;
        if (other.CompareTag(coreTag)) return true;

        // 親にタグが付いてる場合にも対応
        Transform parent = other.transform.parent;

        while (parent != null)
        {
            if (parent.CompareTag(enemyTag)) return true;
            if (parent.CompareTag(dragonTag)) return true;
            if (parent.CompareTag(coreTag)) return true;

            parent = parent.parent;
        }

        return false;
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
        spark.transform.localScale *= effectScale;
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
        audioSource.volume = volume;
        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.spatialBlend = 0.4f;
        audioSource.Play();

        Destroy(audioObj, hitSE.length + 0.2f);
    }

    public void ClearHitHistory()
    {
        lastHitTimes.Clear();
    }
}