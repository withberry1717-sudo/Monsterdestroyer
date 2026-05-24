using UnityEngine;

public class DebugPlayerDamageZone : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float hitInterval = 1.0f;

    private float lastHitTime = -999f;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time < lastHitTime + hitInterval) return;

        PlayerHP playerHP = other.GetComponentInParent<PlayerHP>();

        if (playerHP != null)
        {
            playerHP.TakeDamage(damage);
            lastHitTime = Time.time;
            Debug.Log("Debug attack hit player: " + damage);
        }
    }
}