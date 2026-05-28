using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ClearStampEffect : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private GameObject stampObject;
    [SerializeField] private RectTransform stampRect;
    [SerializeField] private Image stampImage;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip stampSE;

    [Header("ClearPanel表示後の自動再生")]
    [SerializeField] private bool playOnPanelEnable = true;

    [Tooltip("ClearPanelが表示されてから、何秒後にハンコを押すか")]
    [SerializeField] private float delayAfterPanelShown = 0.6f;

    [Header("上からポン設定")]
    [Tooltip("現在置いてある位置から、どれくらい上から落ちてくるか")]
    [SerializeField] private float dropFromY = 120f;

    [Tooltip("現在置いてある大きさを基準にする")]
    [SerializeField] private bool useCurrentTransformAsFinal = true;

    [Header("演出設定")]
    [SerializeField] private float moveDuration = 0.18f;
    [SerializeField] private float startScaleMultiplier = 1.25f;
    [SerializeField] private float overshootScaleMultiplier = 1.08f;
    [SerializeField] private float settleDuration = 0.08f;

    private Coroutine stampCoroutine;

    private Vector2 finalPos;
    private Vector3 finalScale;
    private Quaternion finalRotation;

    private void Awake()
    {
        CacheFinalTransform();
        PrepareHidden();
    }

    private void OnEnable()
    {
        CacheFinalTransform();
        PrepareHidden();

        if (playOnPanelEnable)
        {
            if (stampCoroutine != null)
            {
                StopCoroutine(stampCoroutine);
            }

            stampCoroutine = StartCoroutine(PlayAfterDelay());
        }
    }

    private void OnDisable()
    {
        if (stampCoroutine != null)
        {
            StopCoroutine(stampCoroutine);
            stampCoroutine = null;
        }
    }

    private void CacheFinalTransform()
    {
        if (stampRect == null) return;

        if (useCurrentTransformAsFinal)
        {
            finalPos = stampRect.anchoredPosition;
            finalScale = stampRect.localScale;
            finalRotation = stampRect.localRotation;
        }
    }

    private void PrepareHidden()
    {
        if (stampObject != null)
        {
            stampObject.SetActive(true);
        }

        if (stampRect != null)
        {
            stampRect.anchoredPosition = finalPos + new Vector2(0f, dropFromY);
            stampRect.localScale = finalScale * startScaleMultiplier;
            stampRect.localRotation = finalRotation;
        }

        if (stampImage != null)
        {
            Color color = stampImage.color;
            color.a = 0f;
            stampImage.color = color;
        }
    }

    private IEnumerator PlayAfterDelay()
    {
        if (delayAfterPanelShown > 0f)
        {
            yield return new WaitForSeconds(delayAfterPanelShown);
        }

        PlayStamp();
    }

    public void PlayStamp()
    {
        if (stampCoroutine != null)
        {
            StopCoroutine(stampCoroutine);
        }

        stampCoroutine = StartCoroutine(StampRoutine());
    }

    private IEnumerator StampRoutine()
    {
        if (stampObject != null)
        {
            stampObject.SetActive(true);
        }

        if (stampRect == null || stampImage == null)
        {
            Debug.LogWarning("ClearStampEffect: Stamp Rect または Stamp Image が設定されていません。");
            yield break;
        }

        Vector2 startPos = finalPos + new Vector2(0f, dropFromY);
        Vector3 startScale = finalScale * startScaleMultiplier;
        Vector3 overshootScale = finalScale * overshootScaleMultiplier;

        stampRect.anchoredPosition = startPos;
        stampRect.localScale = startScale;
        stampRect.localRotation = finalRotation;

        Color color = stampImage.color;
        color.a = 0f;
        stampImage.color = color;

        float timer = 0f;

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / moveDuration);

            float eased = 1f - Mathf.Pow(1f - t, 3f);

            stampRect.anchoredPosition = Vector2.Lerp(startPos, finalPos, eased);
            stampRect.localScale = Vector3.Lerp(startScale, overshootScale, eased);

            Color c = stampImage.color;
            c.a = Mathf.Lerp(0f, 1f, eased);
            stampImage.color = c;

            yield return null;
        }

        stampRect.anchoredPosition = finalPos;

        if (audioSource != null && stampSE != null)
        {
            audioSource.PlayOneShot(stampSE);
        }

        timer = 0f;

        while (timer < settleDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / settleDuration);
            stampRect.localScale = Vector3.Lerp(overshootScale, finalScale, t);
            yield return null;
        }

        stampRect.localScale = finalScale;

        Color finalColor = stampImage.color;
        finalColor.a = 1f;
        stampImage.color = finalColor;

        stampCoroutine = null;
    }
}