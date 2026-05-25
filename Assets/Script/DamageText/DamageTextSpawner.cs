using UnityEngine;

public class DamageTextSpawner : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private DamageText damageTextPrefab;

    public void ShowDamage(float damage, Vector3 worldPosition)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);

        DamageText damageText = Instantiate(damageTextPrefab, canvas.transform);
        damageText.transform.position = screenPos;
        damageText.Setup(damage);
    }
}