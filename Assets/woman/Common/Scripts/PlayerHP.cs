using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerHP : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameClearPanel;
    [SerializeField] private UnityEngine.UI.Image hpBarFill;
    [SerializeField] private UnityEngine.UI.Image hpHighlightFill;
    [SerializeField] private UnityEngine.UI.Image hpDelayFill;

    [Header("HP遅延バー")]
    [SerializeField] private float hpDelayWait = 0.35f;
    [SerializeField] private float hpDelaySpeed = 1.5f;

    private Coroutine hpDelayCoroutine;

    [Header("被弾設定")]
    [SerializeField] private float heavyDamageThreshold = 25f;

    [Header("小ダメージ")]
    [SerializeField] private float lightInvincibleTime = 0.5f;
    [SerializeField] private float lightControlLockTime = 0.25f;

    [Header("大ダメージ")]
    [SerializeField] private float heavyInvincibleTime = 1.2f;
    [SerializeField] private float heavyControlLockTime = 0.5f;
    [SerializeField] private float knockbackPower = 4f;
    [SerializeField] private float knockbackUpPower = 1.2f;
    [SerializeField] private float knockbackDuration = 0.25f;

    [Header("点滅")]
    [SerializeField] private float blinkInterval = 0.08f;

    [Header("Game Over 演出")]
    [SerializeField] private CanvasGroup gameOverCanvasGroup;
    [SerializeField] private float gameOverFadeDelay = 0.8f;
    [SerializeField] private float gameOverFadeDuration = 2.0f;

    [Header("Game Over Buttons")]
    [SerializeField] private Button[] gameOverButtons;

    private float currentHp;
    private bool isGameOver = false;
    private bool isGameClear = false;
    private bool isInvincible = false;

    private Animator _animator;
    private Retro.ThirdPersonCharacter.Movement _movement;
    private CharacterController _characterController;
    private Renderer[] _renderers;

    void Start()
    {
        currentHp = maxHp;
        _animator = GetComponent<Animator>();
        _movement = GetComponent<Retro.ThirdPersonCharacter.Movement>();
        _characterController = GetComponent<CharacterController>();
        _renderers = GetComponentsInChildren<Renderer>();
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0f;
            gameOverCanvasGroup.interactable = false;
            gameOverCanvasGroup.blocksRaycasts = false;
        }

        UpdateHPUI();
        Debug.Log("Game Start! Player HP: " + currentHp);
        float hpRatio = currentHp / maxHp;

        if (hpDelayFill != null)
        {
            hpDelayFill.fillAmount = hpRatio;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isGameOver || isGameClear || isInvincible) return;

        currentHp -= damage;
        UpdateHPUI();

        Debug.Log("Hit! Damage: " + damage + " | Remaining HP: " + currentHp);

        if (currentHp <= 0)
        {
            GameOver();
            return;
        }

        if (damage >= heavyDamageThreshold)
        {
            StartCoroutine(HeavyDamageRoutine());
        }
        else
        {
            StartCoroutine(LightDamageRoutine());
        }
    }

    private IEnumerator LightDamageRoutine()
    {
        isInvincible = true;

        if (_movement != null) _movement.enabled = false;
        if (_animator != null) _animator.SetTrigger("TakingDamage");

        yield return new WaitForSeconds(lightControlLockTime);

        if (_movement != null) _movement.enabled = true;

        yield return StartCoroutine(BlinkRoutine(lightInvincibleTime - lightControlLockTime));

        SetRenderersVisible(true);
        isInvincible = false;
    }

    private IEnumerator HeavyDamageRoutine()
    {
        isInvincible = true;

        if (_movement != null) _movement.enabled = false;
        if (_animator != null) _animator.SetTrigger("KnockDown");

        yield return StartCoroutine(KnockbackRoutine());

        yield return new WaitForSeconds(heavyControlLockTime);


        if (_movement != null) _movement.enabled = true;

        yield return StartCoroutine(BlinkRoutine(heavyInvincibleTime - heavyControlLockTime));

        SetRenderersVisible(true);
        isInvincible = false;
    }

    private IEnumerator KnockbackRoutine()
    {
        float timer = 0f;

        Vector3 knockDir = -transform.forward;
        knockDir.y = knockbackUpPower;

        while (timer < knockbackDuration)
        {
            timer += Time.deltaTime;

            if (_characterController != null)
            {
                _characterController.Move(knockDir * knockbackPower * Time.deltaTime);
            }

            yield return null;
        }
    }

    private IEnumerator BlinkRoutine(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            SetRenderersVisible(false);
            yield return new WaitForSeconds(blinkInterval);

            SetRenderersVisible(true);
            yield return new WaitForSeconds(blinkInterval);

            timer += blinkInterval * 2f;
        }
    }

    private void SetRenderersVisible(bool visible)
    {
        foreach (Renderer r in _renderers)
        {
            r.enabled = visible;
        }
    }

    void UpdateHPUI()
    {
        if (hpText != null)
        {
            hpText.text = "HP: " + Mathf.CeilToInt(currentHp);
        }

        float hpRatio = Mathf.Clamp01(currentHp / maxHp);

        // 本体HPバーは即座に減る
        if (hpBarFill != null)
        {
            hpBarFill.fillAmount = hpRatio;
        }

        // ハイライトも即座に減る
        if (hpHighlightFill != null)
        {
            hpHighlightFill.fillAmount = hpRatio;
        }

        // 遅延バーは後からじわっと減る
        if (hpDelayFill != null)
        {
            if (hpDelayCoroutine != null)
            {
                StopCoroutine(hpDelayCoroutine);
            }

            hpDelayCoroutine = StartCoroutine(DelayHPBarRoutine(hpRatio));
        }
    }

    public void Revive()
    {
        ResetGameOverButtons();
        isGameOver = false;
        isInvincible = false;

        currentHp = maxHp * 1f;
        UpdateHPUI();
        if (hpDelayCoroutine != null)
        {
            StopCoroutine(hpDelayCoroutine);
            hpDelayCoroutine = null;
        }

        if (hpDelayFill != null)
        {
            hpDelayFill.fillAmount = currentHp / maxHp;
        }

        SetRenderersVisible(true);

        if (_movement != null) _movement.enabled = true;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (GameManager.Instance != null)
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddDeathPenalty();
            }
        }
        if (_animator != null)
        {
            _animator.Play("RFA_Movement");
        }
        Debug.Log("Player Revived");
    }

    public void BackToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
    }

    void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        isInvincible = true;

        if (hpText != null) hpText.text = "HP: 0";

        SetRenderersVisible(true);

        if (_movement != null) _movement.enabled = false;

        if (_animator != null)
        {
            _animator.ResetTrigger("TakingDamage");
            _animator.ResetTrigger("KnockDown");
            _animator.SetTrigger("Die");
        }

        StartCoroutine(GameOverFadeRoutine());

        Debug.Log("GAME OVER. Restarting...");
    }

    void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    private IEnumerator GameOverFadeRoutine()
    {
        yield return new WaitForSeconds(gameOverFadeDelay);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0f;
            gameOverCanvasGroup.interactable = false;
            gameOverCanvasGroup.blocksRaycasts = false;
        }

        float timer = 0f;

        while (timer < gameOverFadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / gameOverFadeDuration;

            // 最初ゆっくり、後半じわっと出る
            float eased = Mathf.SmoothStep(0f, 1f, t);

            if (gameOverCanvasGroup != null)
            {
                gameOverCanvasGroup.alpha = eased;
            }

            yield return null;
        }

        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 1f;

            ResetGameOverButtons();
            gameOverCanvasGroup.interactable = true;
            gameOverCanvasGroup.blocksRaycasts = true;
        }
    }
    private void ResetGameOverButtons()
    {
        // 選択中のUIを解除する
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (gameOverButtons == null) return;

        foreach (Button button in gameOverButtons)
        {
            if (button == null) continue;

            // Buttonの状態を強制リセット
            button.interactable = false;
            button.interactable = true;
        }
    }
    private IEnumerator DelayHPBarRoutine(float targetFillAmount)
    {
        // 少し待ってから減り始める
        yield return new WaitForSeconds(hpDelayWait);

        while (hpDelayFill != null && hpDelayFill.fillAmount > targetFillAmount)
        {
            hpDelayFill.fillAmount = Mathf.MoveTowards(
                hpDelayFill.fillAmount,
                targetFillAmount,
                hpDelaySpeed * Time.deltaTime
            );

            yield return null;
        }

        if (hpDelayFill != null)
        {
            hpDelayFill.fillAmount = targetFillAmount;
        }
    }
}