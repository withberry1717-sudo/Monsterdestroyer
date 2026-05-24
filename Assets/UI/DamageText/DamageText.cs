using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    [SerializeField] private float lifeTime = 0.8f;
    [SerializeField] private float moveSpeed = 120f;

    [SerializeField] private float minDamage = 8f;
    [SerializeField] private float maxDamage = 45f;

    [SerializeField] private float minScale = 1.5f;
    [SerializeField] private float maxScale = 4.0f;

    private TextMeshProUGUI text;
    private float timer;

    void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    public void Setup(float damage)
    {
        text.text = Mathf.CeilToInt(damage).ToString();

        float t = Mathf.InverseLerp(minDamage, maxDamage, damage);

        text.color = Color.Lerp(Color.white, Color.yellow, t);

        transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, t);
    }

    void Update()
    {
        timer += Time.deltaTime;

        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}