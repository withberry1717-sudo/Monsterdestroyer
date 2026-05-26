using UnityEngine;
using System.Collections.Generic;

public class DragonAttackHitbox : MonoBehaviour
{
    [Header("çUåÇê›íË")]
    public float damage = 20f;
    public float knockbackPower = 5f;
    public float staggerTime = 0.5f;

    [Header("ëŒè€")]
    public LayerMask targetLayers;

    [Header("ëΩíiÉqÉbÉgñhé~")]
    public bool hitOnlyOncePerActivation = true;

    private Collider hitboxCollider;
    private readonly HashSet<GameObject> hitTargets = new HashSet<GameObject>();

    void Awake()
    {
        hitboxCollider = GetComponent<Collider>();

        if (hitboxCollider == null)
        {
            Debug.LogWarning($"{name} Ç… Collider Ç™Ç†ÇËÇ‹ÇπÇÒ");
            return;
        }

        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false;
    }

    public void EnableHitbox()
    {
        hitTargets.Clear();

        if (hitboxCollider != null)
            hitboxCollider.enabled = true;
    }

    public void DisableHitbox()
    {
        if (hitboxCollider != null)
            hitboxCollider.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & targetLayers) == 0)
            return;

        GameObject target = other.gameObject;

        if (hitOnlyOncePerActivation && hitTargets.Contains(target))
            return;

        hitTargets.Add(target);

        // ÉvÉåÉCÉÑÅ[ë§Ç… TakeDamage(float) Ç™Ç†ÇÍÇŒåƒÇŒÇÍÇÈ
        target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

        // ÉvÉåÉCÉÑÅ[ë§Ç… DragonStagger(float) Ç™Ç†ÇÍÇŒãØÇﬁ
        target.SendMessage("DragonStagger", staggerTime, SendMessageOptions.DontRequireReceiver);

        // RigidbodyÇ™Ç†ÇÍÇŒåyÇ≠êÅÇ¡îÚÇŒÇ∑
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 dir = (target.transform.position - transform.position).normalized;
            dir.y = 0.2f;
            rb.AddForce(dir * knockbackPower, ForceMode.Impulse);
        }
    }
}