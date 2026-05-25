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

    [Header("被弾画面フラッシュ")]
    [SerializeField] private CanvasGroup damageFlashCanvasGroup;
    [SerializeField] private float damageFlashMaxAlpha = 0.7f;
    [SerializeField] private float heavyDamageFlashMaxAlpha = 0.9f;
    [SerializeField] private float damageFlashFadeInTime = 0.03f;
    [SerializeField] private float damageFlashHoldTime = 0.04f;
    [SerializeField] private float damageFlashFadeOutTime = 0.35f;

    [Header("赤ふち自動生成")]
    [SerializeField] private bool autoCreateDamageFlashEdges = true;
    [SerializeField] private float damageEdgeThickness = 120f;
    [SerializeField] private Color damageEdgeColor = new Color(1f, 0f, 0f, 0.8f);

    [Header("Game Over 演出")]
    [SerializeField] private CanvasGroup gameOverCanvasGroup;
    [SerializeField] private float gameOverFadeDelay = 0.8f;
    [SerializeField] private float gameOverFadeDuration = 2.0f;

    [Header("Game Over Buttons")]
    [SerializeField] private Button[] gameOverButtons;

    [Header("Game Over時に止めるスクリプト 手動追加用")]
    [SerializeField] private MonoBehaviour[] disableOnGameOver;

    private float currentHp;
    private bool isGameOver = false;
    private bool isGameClear = false;
    private bool isInvincible = false;

    private Coroutine hpDelayCoroutine;
    private Coroutine damageFlashCoroutine;

    private Animator _animator;
    private Retro.ThirdPersonCharacter.Movement _movement;
    private CharacterController _characterController;
    private Renderer[] _renderers;

    private MonoBehaviour _combat;
    private MonoBehaviour _aiming;
    private MonoBehaviour _aimingController;
    private MonoBehaviour _safePlayerCamera;

    void Start()
    {
        currentHp = maxHp;

        _animator = GetComponent<Animator>();
        _movement = GetComponent<Retro.ThirdPersonCharacter.Movement>();
        _characterController = GetComponent<CharacterController>();
        _renderers = GetComponentsInChildren<Renderer>();

        FindScriptsForGameOver();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (gameOverCanvasGroup == null && gameOverPanel != null)
        {
            gameOverCanvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
        }

        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0f;
            gameOverCanvasGroup.interactable = false;
            gameOverCanvasGroup.blocksRaycasts = false;
        }

        SetupDamageFlash();

        if (_movement != null)
        {
            _movement.ForceStopTrail();
        }

        BattleCursorManager.LockCursor();

        UpdateHPUI();

        if (hpDelayFill != null)
        {
            hpDelayFill.fillAmount = currentHp / maxHp;
        }

        Debug.Log("Game Start! Player HP: " + currentHp);
    }

    private void SetupDamageFlash()
    {
        if (damageFlashCanvasGroup == null)
        {
            Debug.LogWarning("Damage Flash Canvas Group が未設定です。PlayerHPのInspectorにDamageFlashPanelを入れてください。");
            return;
        }

        damageFlashCanvasGroup.alpha = 0f;
        damageFlashCanvasGroup.interactable = false;
        damageFlashCanvasGroup.blocksRaycasts = false;

        Image parentImage = damageFlashCanvasGroup.GetComponent<Image>();
        if (parentImage != null)
        {
            parentImage.enabled = false;
        }

        if (autoCreateDamageFlashEdges)
        {
            CreateDamageFlashEdgesIfNeeded();
        }
    }

    private void CreateDamageFlashEdgesIfNeeded()
    {
        if (damageFlashCanvasGroup == null) return;

        Transform parent = damageFlashCanvasGroup.transform;

        CreateOrUpdateEdge(
            parent,
            "TopRed",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, 0f),
            new Vector2(0f, damageEdgeThickness)
        );

        CreateOrUpdateEdge(
            parent,
            "BottomRed",
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, damageEdgeThickness)
        );

        CreateOrUpdateEdge(
            parent,
            "LeftRed",
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0f),
            new Vector2(damageEdgeThickness, 0f)
        );

        CreateOrUpdateEdge(
            parent,
            "RightRed",
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0.5f),
            new Vector2(0f, 0f),
            new Vector2(damageEdgeThickness, 0f)
        );
    }

    private void CreateOrUpdateEdge(
        Transform parent,
        string edgeName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta
    )
    {
        Transform existing = parent.Find(edgeName);
        GameObject edgeObject;

        if (existing != null)
        {
            edgeObject = existing.gameObject;
        }
        else
        {
            edgeObject = new GameObject(edgeName);
            edgeObject.transform.SetParent(parent, false);
        }

        RectTransform rect = edgeObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = edgeObject.AddComponent<RectTransform>();
        }

        CanvasRenderer canvasRenderer = edgeObject.GetComponent<CanvasRenderer>();
        if (canvasRenderer == null)
        {
            canvasRenderer = edgeObject.AddComponent<CanvasRenderer>();
        }

        Image image = edgeObject.GetComponent<Image>();
        if (image == null)
        {
            image = edgeObject.AddComponent<Image>();
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        image.color = damageEdgeColor;
        image.raycastTarget = false;
        image.enabled = true;

        edgeObject.SetActive(true);
    }

    public void TakeDamage(float damage)
    {
        if (isGameOver || isGameClear || isInvincible) return;

        PlayDamageFlash(damage);

        if (_movement != null)
        {
            _movement.ForceStopTrail();
        }

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

    private void PlayDamageFlash(float damage)
    {
        if (damageFlashCanvasGroup == null)
        {
            Debug.LogWarning("Damage Flash Canvas Group が未設定なので赤ふちを表示できません。");
            return;
        }

        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
        }

        float targetAlpha = damage >= heavyDamageThreshold
            ? heavyDamageFlashMaxAlpha
            : damageFlashMaxAlpha;

        damageFlashCoroutine = StartCoroutine(DamageFlashRoutine(targetAlpha));
    }

    private IEnumerator DamageFlashRoutine(float targetAlpha)
    {
        damageFlashCanvasGroup.alpha = 0f;

        float timer = 0f;

        while (timer < damageFlashFadeInTime)
        {
            timer += Time.deltaTime;
            float t = damageFlashFadeInTime <= 0f ? 1f : timer / damageFlashFadeInTime;
            damageFlashCanvasGroup.alpha = Mathf.Lerp(0f, targetAlpha, t);
            yield return null;
        }

        damageFlashCanvasGroup.alpha = targetAlpha;

        yield return new WaitForSeconds(damageFlashHoldTime);

        timer = 0f;

        while (timer < damageFlashFadeOutTime)
        {
            timer += Time.deltaTime;
            float t = damageFlashFadeOutTime <= 0f ? 1f : timer / damageFlashFadeOutTime;
            damageFlashCanvasGroup.alpha = Mathf.Lerp(targetAlpha, 0f, t);
            yield return null;
        }

        damageFlashCanvasGroup.alpha = 0f;
        damageFlashCoroutine = null;
    }

    private IEnumerator LightDamageRoutine()
    {
        isInvincible = true;

        if (_movement != null)
        {
            _movement.ForceStopTrail();
            _movement.enabled = false;
        }

        if (_animator != null)
        {
            _animator.SetTrigger("TakingDamage");
        }

        yield return new WaitForSeconds(lightControlLockTime);

        if (!isGameOver && _movement != null)
        {
            _movement.ForceStopTrail();
            _movement.enabled = true;
        }

        yield return StartCoroutine(BlinkRoutine(lightInvincibleTime - lightControlLockTime));

        SetRenderersVisible(true);

        if (_movement != null)
        {
            _movement.ForceStopTrail();
        }

        if (!isGameOver)
        {
            isInvincible = false;
        }
    }

    private IEnumerator HeavyDamageRoutine()
    {
        isInvincible = true;

        if (_movement != null)
        {
            _movement.ForceStopTrail();
            _movement.enabled = false;
        }

        if (_animator != null)
        {
            _animator.SetTrigger("KnockDown");
        }

        yield return StartCoroutine(KnockbackRoutine());

        yield return new WaitForSeconds(heavyControlLockTime);

        if (!isGameOver && _movement != null)
        {
            _movement.ForceStopTrail();
            _movement.enabled = true;
        }

        yield return StartCoroutine(BlinkRoutine(heavyInvincibleTime - heavyControlLockTime));

        SetRenderersVisible(true);

        if (_movement != null)
        {
            _movement.ForceStopTrail();
        }

        if (!isGameOver)
        {
            isInvincible = false;
        }
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
        if (_renderers == null) return;

        foreach (Renderer r in _renderers)
        {
            if (r != null)
            {
                r.enabled = visible;
            }
        }
    }

    void UpdateHPUI()
    {
        if (hpText != null)
        {
            hpText.text = "HP: " + Mathf.CeilToInt(Mathf.Max(0, currentHp));
        }

        float hpRatio = Mathf.Clamp01(currentHp / maxHp);

        if (hpBarFill != null)
        {
            hpBarFill.fillAmount = hpRatio;
        }

        if (hpHighlightFill != null)
        {
            hpHighlightFill.fillAmount = hpRatio;
        }

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

        currentHp = maxHp;
        UpdateHPUI();

        if (hpDelayCoroutine != null)
        {
            StopCoroutine(hpDelayCoroutine);
            hpDelayCoroutine = null;
        }

        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
            damageFlashCoroutine = null;
        }

        if (damageFlashCanvasGroup != null)
        {
            damageFlashCanvasGroup.alpha = 0f;
        }

        if (hpDelayFill != null)
        {
            hpDelayFill.fillAmount = currentHp / maxHp;
        }

        SetRenderersVisible(true);

        SetGameOverScriptsEnabled(true);

        if (_movement != null)
        {
            _movement.ForceStopTrail();
        }

        BattleCursorManager.LockCursor();

        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0f;
            gameOverCanvasGroup.interactable = false;
            gameOverCanvasGroup.blocksRaycasts = false;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddDeathPenalty();
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
        SetGameOverScriptsEnabled(true);
        BattleCursorManager.UnlockCursor();

        SceneManager.LoadScene("TitleScene");
    }

    void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        isInvincible = true;

        if (_movement != null)
        {
            _movement.ForceStopTrail();
        }

        if (damageFlashCanvasGroup != null)
        {
            damageFlashCanvasGroup.alpha = 0f;
        }

        if (hpText != null)
        {
            hpText.text = "HP: 0";
        }

        SetRenderersVisible(true);

        SetGameOverScriptsEnabled(false);
        BattleCursorManager.UnlockCursor();

        if (_animator != null)
        {
            _animator.ResetTrigger("TakingDamage");
            _animator.ResetTrigger("KnockDown");
            _animator.SetTrigger("Die");
        }

        StartCoroutine(GameOverFadeRoutine());

        Debug.Log("GAME OVER.");
    }

    void RestartGame()
    {
        Time.timeScale = 1f;
        BattleCursorManager.LockCursor();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void FindScriptsForGameOver()
    {
        MonoBehaviour[] playerScripts = GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour script in playerScripts)
        {
            if (script == null) continue;

            string scriptName = script.GetType().Name;

            if (scriptName == "Combat")
            {
                _combat = script;
            }
            else if (scriptName == "Aiming")
            {
                _aiming = script;
            }
            else if (scriptName == "AimingController")
            {
                _aimingController = script;
            }
        }

        MonoBehaviour[] allScripts = Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Exclude
        );

        foreach (MonoBehaviour script in allScripts)
        {
            if (script == null) continue;

            if (script.GetType().Name == "SafePlayerCamera")
            {
                _safePlayerCamera = script;
                break;
            }
        }
    }

    private void SetGameOverScriptsEnabled(bool enabled)
    {
        if (_movement != null)
        {
            if (!enabled)
            {
                _movement.ForceStopTrail();
            }

            _movement.enabled = enabled;
        }

        if (_combat != null) _combat.enabled = enabled;
        if (_aiming != null) _aiming.enabled = enabled;
        if (_aimingController != null) _aimingController.enabled = enabled;
        if (_safePlayerCamera != null) _safePlayerCamera.enabled = enabled;

        if (disableOnGameOver != null)
        {
            foreach (MonoBehaviour script in disableOnGameOver)
            {
                if (script != null)
                {
                    script.enabled = enabled;
                }
            }
        }
    }

    private IEnumerator GameOverFadeRoutine()
    {
        yield return new WaitForSeconds(gameOverFadeDelay);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (gameOverCanvasGroup == null && gameOverPanel != null)
        {
            gameOverCanvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
        }

        if (gameOverCanvasGroup == null)
        {
            Debug.LogWarning("GameOverPanelにCanvasGroupがありません。フェードなしで表示します。");
            yield break;
        }

        gameOverCanvasGroup.alpha = 0f;
        gameOverCanvasGroup.interactable = false;
        gameOverCanvasGroup.blocksRaycasts = false;

        float timer = 0f;

        while (timer < gameOverFadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / gameOverFadeDuration;

            float eased = Mathf.SmoothStep(0f, 1f, t);
            gameOverCanvasGroup.alpha = eased;

            yield return null;
        }

        gameOverCanvasGroup.alpha = 1f;

        ResetGameOverButtons();

        gameOverCanvasGroup.interactable = true;
        gameOverCanvasGroup.blocksRaycasts = true;
    }

    private void ResetGameOverButtons()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (gameOverButtons == null) return;

        foreach (Button button in gameOverButtons)
        {
            if (button == null) continue;

            button.interactable = false;
            button.interactable = true;
        }
    }

    private IEnumerator DelayHPBarRoutine(float targetFillAmount)
    {
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