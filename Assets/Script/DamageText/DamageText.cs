using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ダメージ数字を表示するTextMeshProUGUI")]
    public TextMeshProUGUI damageText;

    [Header("Damage Range")]
    [Tooltip("色とサイズ変化の基準になる最小ダメージ")]
    public float minDamage = 10f;

    [Tooltip("色とサイズ変化の基準になる最大ダメージ")]
    public float maxDamage = 150f;

    [Header("Color")]
    [Tooltip("低ダメージ時の色")]
    public Color lowDamageColor = Color.white;

    [Tooltip("高ダメージ時の色。黄色系がおすすめ")]
    public Color highDamageColor = Color.yellow;

    [Header("Size")]
    [Tooltip("低ダメージ時の文字サイズ")]
    public float minFontSize = 36f;

    [Tooltip("高ダメージ時の文字サイズ")]
    public float maxFontSize = 72f;

    [Tooltip("出現した瞬間の拡大率")]
    public float popScale = 1.35f;

    [Tooltip("消える直前の縮小率")]
    public float endScale = 0.85f;

    [Header("Animation")]
    [Tooltip("表示してから消えるまでの時間")]
    public float lifeTime = 0.8f;

    [Tooltip("上に移動する量")]
    public float floatUpDistance = 70f;

    [Tooltip("横方向に少し流れる量")]
    public float sideMoveDistance = 25f;

    [Tooltip("出現時に少し揺れる強さ")]
    public float shakePower = 10f;

    [Tooltip("回転の最大角度")]
    public float maxRotationAngle = 12f;

    [Header("Critical Style")]
    [Tooltip("このダメージ以上なら強調演出にする")]
    public float bigDamageThreshold = 100f;

    [Tooltip("大ダメージ時に文字を少し長く残す")]
    public float bigDamageLifeBonus = 0.15f;

    private RectTransform rectTransform;
    private Vector2 startPosition;
    private Vector2 sideDirection;
    private float timer;
    private float currentLifeTime;
    private float baseFontSize;
    private float startRotation;
    private bool isBigDamage;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (damageText == null)
        {
            damageText = GetComponent<TextMeshProUGUI>();
        }
    }

    public void Setup(float damage)
    {
        if (damageText == null)
        {
            return;
        }

        timer = 0f;

        float damageRate = Mathf.InverseLerp(minDamage, maxDamage, damage);
        damageRate = Mathf.Clamp01(damageRate);

        isBigDamage = damage >= bigDamageThreshold;
        currentLifeTime = lifeTime + (isBigDamage ? bigDamageLifeBonus : 0f);

        int roundedDamage = Mathf.RoundToInt(damage);
        damageText.text = roundedDamage.ToString();

        Color damageColor = Color.Lerp(lowDamageColor, highDamageColor, damageRate);
        damageColor.a = 1f;
        damageText.color = damageColor;

        baseFontSize = Mathf.Lerp(minFontSize, maxFontSize, damageRate);
        damageText.fontSize = baseFontSize;

        if (rectTransform != null)
        {
            startPosition = rectTransform.anchoredPosition;

            float side = Random.value < 0.5f ? -1f : 1f;
            sideDirection = new Vector2(side, 0f);

            startRotation = Random.Range(-maxRotationAngle, maxRotationAngle);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, startRotation);

            float extraPop = isBigDamage ? 0.25f : 0f;
            rectTransform.localScale = Vector3.one * (popScale + extraPop);
        }
    }

    private void Update()
    {
        if (damageText == null || rectTransform == null)
        {
            return;
        }

        timer += Time.deltaTime;

        float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, currentLifeTime));

        // 上昇は少し減速する感じ
        float upT = 1f - Mathf.Pow(1f - t, 2f);
        Vector2 upMove = Vector2.up * (floatUpDistance * upT);

        // 横移動はふわっと流す
        Vector2 sideMove = sideDirection * (sideMoveDistance * t);

        // 最初だけブルッと揺れる
        float shakeT = Mathf.Clamp01(1f - t * 4f);
        Vector2 shake = Random.insideUnitCircle * (shakePower * shakeT);

        rectTransform.anchoredPosition = startPosition + upMove + sideMove + shake;

        // 最初にポップしてから縮む
        float scaleT = EaseOutBack(t);
        float startScaleValue = isBigDamage ? popScale + 0.25f : popScale;
        float scale = Mathf.Lerp(startScaleValue, endScale, scaleT);
        rectTransform.localScale = Vector3.one * scale;

        // 少し回転を戻す
        float rotation = Mathf.Lerp(startRotation, 0f, t);
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);

        // 0.8秒前後でフェードアウト
        Color c = damageText.color;
        c.a = 1f - Mathf.SmoothStep(0f, 1f, t);
        damageText.color = c;

        if (timer >= currentLifeTime)
        {
            Destroy(gameObject);
        }
    }

    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}