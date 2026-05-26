using UnityEngine;

public class DamageTextSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ダメージテキストを表示するCanvas")]
    public Canvas canvas;

    [Tooltip("DamageText.cs が付いたUIプレハブ")]
    public GameObject damageTextPrefab;

    [Header("Position")]
    [Tooltip("表示位置の横方向のランダム幅")]
    public float randomX = 20f;

    [Tooltip("表示位置の縦方向のランダム幅")]
    public float randomY = 10f;

    private RectTransform canvasRect;
    private Camera mainCamera;

    private void Awake()
    {
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
        }

        if (canvas != null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
        }

        mainCamera = Camera.main;
    }

    public void SpawnDamageText(float damage, Vector3 worldPosition)
    {
        if (canvas == null || canvasRect == null)
        {
            Debug.LogWarning($"{name}: Canvasが設定されていません");
            return;
        }

        if (damageTextPrefab == null)
        {
            Debug.LogWarning($"{name}: Damage Text Prefabが設定されていません");
            return;
        }

        GameObject obj = Instantiate(damageTextPrefab, canvas.transform);

        RectTransform rect = obj.GetComponent<RectTransform>();

        if (rect != null)
        {
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(mainCamera, worldPosition);

            Vector2 localPosition;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
                out localPosition
            );

            localPosition += new Vector2(
                Random.Range(-randomX, randomX),
                Random.Range(0f, randomY)
            );

            rect.anchoredPosition = localPosition;
        }

        DamageText damageText = obj.GetComponent<DamageText>();

        if (damageText != null)
        {
            damageText.Setup(damage);
        }
    }
}