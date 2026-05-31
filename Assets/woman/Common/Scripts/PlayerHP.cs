using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerHP : MonoBehaviour
{
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public bool IsDead => isGameOver || currentHp <= 0f;
    public bool IsGameOver => isGameOver;
    public bool IsGameClear => isGameClear;

    [Header("HP")]
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameClearPanel;
    [SerializeField] private Image hpBarFill;
    [SerializeField] private Image hpHighlightFill;
    [SerializeField] private Image hpDelayFill;

    [Header("HP遅延バー")]
    [SerializeField] private float hpDelayWait = 0.35f;
    [SerializeField] private float hpDelaySpeed = 1.5f;

    [Header("被弾設定")]
    [SerializeField] private float heavyDamageThreshold = 25f;
    [SerializeField] private float minimumDamageInterval = 0.05f;

    [Header("小ダメージ")]
    [SerializeField] private float lightInvincibleTime = 0.5f;
    [SerializeField] private float lightControlLockTime = 0.25f;

    [Header("大ダメージ")]
    [SerializeField] private float heavyInvincibleTime = 1.2f;
    [SerializeField] private float heavyControlLockTime = 0.5f;
    [SerializeField] private float knockbackPower = 4f;
    [SerializeField] private float knockbackUpPower = 1.2f;
    [SerializeField] private float knockbackDuration = 0.25f;

    [Header("ドラゴン攻撃からの吹っ飛び")]
    [SerializeField] private float externalKnockbackMultiplier = 1.0f;
    [SerializeField] private bool applyGravityDuringKnockback = true;
    [SerializeField] private float gravityDuringKnockback = -20f;

    [Header("ノックバック暴発防止")]
    [Tooltip("ON推奨。DragonKnockbackでは方向だけ保存し、実際の吹っ飛びはTakeDamage後の被弾処理で1回だけ行います。ブリンク中の二重ノックバック防止用です。")]
    [SerializeField] private bool useDamageRoutineOnlyForKnockback = true;

    [Tooltip("1フレームで移動できるノックバック量の上限です。異常な値が来てもスポーン位置まで飛ばないようにする保険です。通常は0.8〜1.2でOK。")]
    [SerializeField] private float maxKnockbackMovePerFrame = 1.0f;

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

    [Header("Hard Mode Death Limit")]
    [Tooltip("ONならHardだけ死亡回数制限を使います。1回目は通常GameOver、2回目はYouDiedだけ表示してタイトルへ戻します。")]
    [SerializeField] private bool hardModeOneDeathRetry = true;

    [Tooltip("Hardで許可する死亡回数です。1なら、1回目は復帰可能、2回目で強制タイトルです。")]
    [SerializeField] private int hardModeAllowedDeaths = 1;

    [Tooltip("Hardで死亡制限を超えた時、タイトルへ戻るまでの秒数です。")]
    [SerializeField] private float hardModeReturnToTitleDelay = 5f;

    [Tooltip("戻るタイトルシーン名です。")]
    [SerializeField] private string titleSceneName = "TitleScene";

    [Tooltip("Hard最終死亡時に表示するYouDiedテキストです。空ならGameOverPanel配下から名前にYouDiedを含むオブジェクトを探します。")]
    [SerializeField] private GameObject hardModeYouDiedText;

    [Tooltip("ONならHard最終死亡時、GameOverPanel配下のYouDied以外を非表示にします。")]
    [SerializeField] private bool hideOtherGameOverChildrenOnHardFinalDeath = true;

    [Header("Game Over時に止めるスクリプト 手動追加用")]
    [SerializeField] private MonoBehaviour[] disableOnGameOver;

    [Header("World Safety / 裏世界落下・押し出し対策")]
    [Tooltip("ON推奨。壁際やドラゴンに押しつぶされた時、床下や異常位置へ飛んだら最後の安全位置へ戻します。")]
    [SerializeField] private bool useWorldSafety = true;

    [Tooltip("このY座標より下へ行ったら裏世界判定で復帰します。ステージ床より十分下にしてください。")]
    [SerializeField] private float worldMinY = -8f;

    [Tooltip("1フレームでこの距離以上動いたら異常移動として復帰します。ブリンク距離より少し大きめ推奨。")]
    [SerializeField] private float maxUnexpectedMovePerFrame = 6f;

    [Tooltip("最後の安全位置へ戻す時に少し上へ浮かせる量です。")]
    [SerializeField] private float safeReturnUpOffset = 0.35f;

    [Tooltip("安全位置を記録する間隔です。")]
    [SerializeField] private float safePositionRecordInterval = 0.08f;

    private float currentHp;
    private bool isGameOver;
    private bool isGameClear;
    private bool isInvincible;

    private int hardModeDeathCount = 0;
    private Coroutine hardModeFinalDeathCoroutine;

    private int controlLockCount;
    private float lastDamageTime = -999f;

    private Coroutine hpDelayCoroutine;
    private Coroutine damageFlashCoroutine;
    private Coroutine damageRoutineCoroutine;
    private Coroutine externalKnockbackCoroutine;
    private Coroutine staggerCoroutine;

    private Animator _animator;
    private Retro.ThirdPersonCharacter.Movement _movement;
    private Retro.ThirdPersonCharacter.PlayerInput _playerInput;
    private CharacterController _characterController;
    private Rigidbody _rigidbody;
    private Renderer[] _renderers;

    private MonoBehaviour _combat;
    private MonoBehaviour _aiming;
    private MonoBehaviour _aimingController;
    private MonoBehaviour _safePlayerCamera;

    private Vector3 lastKnockbackDirection = Vector3.zero;

    private Vector3 lastSafePosition;
    private Vector3 lastFramePosition;
    private float safePositionRecordTimer;
    private bool hasSafePosition;
    private bool safetyRecovering;

    private void Start()
    {
        currentHp = maxHp;

        _animator = GetComponent<Animator>();
        _movement = GetComponent<Retro.ThirdPersonCharacter.Movement>();
        _playerInput = GetComponent<Retro.ThirdPersonCharacter.PlayerInput>();
        _characterController = GetComponent<CharacterController>();
        _rigidbody = GetComponent<Rigidbody>();
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

        InitializeWorldSafety();

        Debug.Log("Game Start! Player HP: " + currentHp);
    }

    private void Update()
    {
        UpdateWorldSafety();
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

        CreateOrUpdateEdge(parent, "TopRed", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, damageEdgeThickness));
        CreateOrUpdateEdge(parent, "BottomRed", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, damageEdgeThickness));
        CreateOrUpdateEdge(parent, "LeftRed", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(damageEdgeThickness, 0f));
        CreateOrUpdateEdge(parent, "RightRed", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(damageEdgeThickness, 0f));
    }

    private void CreateOrUpdateEdge(Transform parent, string edgeName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
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

        if (edgeObject.GetComponent<CanvasRenderer>() == null)
        {
            edgeObject.AddComponent<CanvasRenderer>();
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
        if (!CanTakeDamage()) return;

        ApplyDamage(damage);

        if (currentHp <= 0f)
        {
            GameOver();
            return;
        }

        StartDamageReaction(damage);
    }

    public void DragonStagger(float duration)
    {
        if (isGameOver || isGameClear) return;
        if (duration <= 0f) return;

        if (damageRoutineCoroutine != null) return;

        if (staggerCoroutine != null)
        {
            StopCoroutine(staggerCoroutine);
            staggerCoroutine = null;
            ForceUnlockControl();
        }

        staggerCoroutine = StartCoroutine(StaggerRoutine(duration));
    }

    public void DragonKnockback(Vector3 knockbackDirection)
    {
        if (isGameOver || isGameClear) return;

        CancelBlinkBeforeDamageKnockback();

        if (!IsFiniteVector(knockbackDirection) || knockbackDirection.sqrMagnitude < 0.001f)
        {
            knockbackDirection = -transform.forward;
        }

        knockbackDirection.y = 0f;

        if (knockbackDirection.sqrMagnitude < 0.001f)
        {
            knockbackDirection = -transform.forward;
        }

        lastKnockbackDirection = knockbackDirection.normalized;

        // 重要：DragonAttackHitbox側が DragonKnockback → TakeDamage の順で呼ぶ場合、
        // ここで外部ノックバックを開始すると、TakeDamage後のHeavyDamageRoutineの
        // 内部ノックバックと二重に走る。ブリンク中はこれが特に大きな吹っ飛びになる。
        if (externalKnockbackCoroutine != null)
        {
            StopCoroutine(externalKnockbackCoroutine);
            externalKnockbackCoroutine = null;
        }

        if (useDamageRoutineOnlyForKnockback)
        {
            return;
        }

        externalKnockbackCoroutine = StartCoroutine(
            ExternalKnockbackRoutine(
                lastKnockbackDirection,
                knockbackPower * externalKnockbackMultiplier,
                knockbackDuration
            )
        );
    }

    private bool CanTakeDamage()
    {
        if (isGameOver || isGameClear || isInvincible) return false;

        if (minimumDamageInterval > 0f && Time.time < lastDamageTime + minimumDamageInterval)
        {
            return false;
        }

        lastDamageTime = Time.time;
        return true;
    }

    private void ApplyDamage(float damage)
    {
        PlayDamageFlash(damage);

        CancelBlinkBeforeDamageKnockback();

        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0f);

        UpdateHPUI();

        Debug.Log("Hit! Damage: " + damage + " | Remaining HP: " + currentHp);
    }

    private void CancelBlinkBeforeDamageKnockback()
    {
        if (_movement == null) return;

        _movement.CancelBlinkByDamage();
        _movement.ForceStopTrail();
    }

    private void StartDamageReaction(float damage)
    {
        CancelBlinkBeforeDamageKnockback();

        if (externalKnockbackCoroutine != null)
        {
            StopCoroutine(externalKnockbackCoroutine);
            externalKnockbackCoroutine = null;
        }

        if (damageRoutineCoroutine != null)
        {
            StopCoroutine(damageRoutineCoroutine);
            damageRoutineCoroutine = null;
            ForceUnlockControl();
        }

        if (staggerCoroutine != null)
        {
            StopCoroutine(staggerCoroutine);
            staggerCoroutine = null;
            ForceUnlockControl();
        }

        if (damage >= heavyDamageThreshold)
        {
            damageRoutineCoroutine = StartCoroutine(HeavyDamageRoutine());
        }
        else
        {
            damageRoutineCoroutine = StartCoroutine(LightDamageRoutine());
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

        float targetAlpha = damage >= heavyDamageThreshold ? heavyDamageFlashMaxAlpha : damageFlashMaxAlpha;

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

        LockControl();

        if (_animator != null)
        {
            _animator.ResetTrigger("KnockDown");
            _animator.SetTrigger("TakingDamage");
        }

        yield return new WaitForSeconds(lightControlLockTime);

        UnlockControl();

        float blinkTime = Mathf.Max(0f, lightInvincibleTime - lightControlLockTime);
        yield return StartCoroutine(BlinkRoutine(blinkTime));

        SetRenderersVisible(true);

        if (_movement != null)
        {
            _movement.ForceStopTrail();
        }

        if (!isGameOver)
        {
            isInvincible = false;
        }

        damageRoutineCoroutine = null;
    }

    private IEnumerator HeavyDamageRoutine()
    {
        isInvincible = true;

        LockControl();

        if (_animator != null)
        {
            _animator.ResetTrigger("TakingDamage");
            _animator.SetTrigger("KnockDown");
        }

        Vector3 knockDir = lastKnockbackDirection.sqrMagnitude > 0.001f ? lastKnockbackDirection : -transform.forward;

        yield return StartCoroutine(InternalKnockbackRoutine(knockDir));

        yield return new WaitForSeconds(heavyControlLockTime);

        UnlockControl();

        float blinkTime = Mathf.Max(0f, heavyInvincibleTime - heavyControlLockTime);
        yield return StartCoroutine(BlinkRoutine(blinkTime));

        SetRenderersVisible(true);

        if (_movement != null)
        {
            _movement.ForceStopTrail();
        }

        if (!isGameOver)
        {
            isInvincible = false;
        }

        damageRoutineCoroutine = null;
    }

    private IEnumerator InternalKnockbackRoutine(Vector3 direction)
    {
        float timer = 0f;

        if (!IsFiniteVector(direction))
        {
            direction = -transform.forward;
        }

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = -transform.forward;
        }

        direction.Normalize();

        Vector3 velocity = direction * knockbackPower;
        velocity.y = knockbackUpPower;

        while (timer < knockbackDuration)
        {
            timer += Time.deltaTime;
            MovePlayerByKnockback(velocity);
            yield return null;
        }
    }

    private IEnumerator ExternalKnockbackRoutine(Vector3 direction, float power, float duration)
    {
        float timer = 0f;

        if (!IsFiniteVector(direction))
        {
            direction = -transform.forward;
        }

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = -transform.forward;
        }

        direction.Normalize();

        Vector3 velocity = direction * Mathf.Max(0f, power);
        velocity.y = knockbackUpPower;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            MovePlayerByKnockback(velocity);
            yield return null;
        }

        externalKnockbackCoroutine = null;
    }

    private void MovePlayerByKnockback(Vector3 velocity)
    {
        if (!IsFiniteVector(velocity))
        {
            velocity = -transform.forward * knockbackPower;
        }

        if (_characterController != null && _characterController.enabled)
        {
            Vector3 move = velocity * Time.deltaTime;

            if (applyGravityDuringKnockback)
            {
                move.y += gravityDuringKnockback * Time.deltaTime * Time.deltaTime;
            }

            // 異常な方向・速度が来ても一瞬で初期スポーン付近まで飛ばないようにする保険。
            float maxMove = Mathf.Max(0.05f, maxKnockbackMovePerFrame);
            if (!IsFiniteVector(move))
            {
                move = Vector3.zero;
            }
            else if (move.magnitude > maxMove)
            {
                move = move.normalized * maxMove;
            }

            SafeCharacterMove(move);
        }
        else if (_rigidbody != null && !_rigidbody.isKinematic)
        {
            _rigidbody.AddForce(velocity, ForceMode.Impulse);
        }
    }

    private bool IsFiniteVector(Vector3 value)
    {
        return float.IsFinite(value.x)
            && float.IsFinite(value.y)
            && float.IsFinite(value.z);
    }

    private void InitializeWorldSafety()
    {
        lastSafePosition = transform.position;
        lastFramePosition = transform.position;
        safePositionRecordTimer = 0f;
        hasSafePosition = true;
        safetyRecovering = false;
    }

    private void SafeCharacterMove(Vector3 move)
    {
        if (_characterController == null || !_characterController.enabled)
        {
            transform.position += move;
            return;
        }

        if (!IsFiniteVector(move))
        {
            RecoverToLastSafePosition("Move vector is invalid");
            return;
        }

        _characterController.Move(move);

        if (useWorldSafety && !IsPositionSafeEnough(transform.position))
        {
            RecoverToLastSafePosition("Unsafe position after knockback move");
        }
    }

    private void UpdateWorldSafety()
    {
        if (!useWorldSafety) return;
        if (!hasSafePosition)
        {
            InitializeWorldSafety();
            return;
        }

        Vector3 currentPosition = transform.position;

        if (!IsPositionSafeEnough(currentPosition))
        {
            RecoverToLastSafePosition("Fell under world or invalid position");
            return;
        }

        float movedThisFrame = Vector3.Distance(currentPosition, lastFramePosition);
        if (!safetyRecovering && movedThisFrame > Mathf.Max(1f, maxUnexpectedMovePerFrame))
        {
            RecoverToLastSafePosition("Unexpected large displacement");
            return;
        }

        safetyRecovering = false;

        safePositionRecordTimer -= Time.deltaTime;
        if (safePositionRecordTimer <= 0f && CanRecordSafePosition())
        {
            lastSafePosition = currentPosition;
            safePositionRecordTimer = Mathf.Max(0.02f, safePositionRecordInterval);
        }

        lastFramePosition = transform.position;
    }

    private bool CanRecordSafePosition()
    {
        if (!IsPositionSafeEnough(transform.position)) return false;
        if (_characterController == null) return true;

        // 操作不能中・被弾ノックバック中は、押し込まれた位置を安全位置として保存しない。
        if (controlLockCount > 0) return false;
        if (damageRoutineCoroutine != null) return false;
        if (externalKnockbackCoroutine != null) return false;

        return _characterController.isGrounded;
    }

    private bool IsPositionSafeEnough(Vector3 position)
    {
        if (!IsFiniteVector(position)) return false;
        if (position.y < worldMinY) return false;
        return true;
    }

    private void RecoverToLastSafePosition(string reason)
    {
        if (!useWorldSafety) return;

        CancelBlinkBeforeDamageKnockback();

        if (externalKnockbackCoroutine != null)
        {
            StopCoroutine(externalKnockbackCoroutine);
            externalKnockbackCoroutine = null;
        }

        Vector3 recoverPosition = hasSafePosition ? lastSafePosition : transform.position;
        if (!IsFiniteVector(recoverPosition))
        {
            recoverPosition = Vector3.zero;
        }

        recoverPosition.y = Mathf.Max(recoverPosition.y + safeReturnUpOffset, worldMinY + safeReturnUpOffset);

        if (_characterController != null)
        {
            bool wasEnabled = _characterController.enabled;
            _characterController.enabled = false;
            transform.position = recoverPosition;
            _characterController.enabled = wasEnabled;
        }
        else
        {
            transform.position = recoverPosition;
        }

        lastFramePosition = transform.position;
        lastSafePosition = transform.position;
        hasSafePosition = true;
        safetyRecovering = true;
        lastKnockbackDirection = Vector3.zero;

        Debug.LogWarning("[PlayerHP] World safety recover: " + reason, this);
    }

    private IEnumerator StaggerRoutine(float duration)
    {
        LockControl();
        yield return new WaitForSeconds(duration);
        UnlockControl();
        staggerCoroutine = null;
    }

    private void LockControl()
    {
        controlLockCount++;
        SetActionScriptsEnabled(false);
    }

    private void UnlockControl()
    {
        controlLockCount = Mathf.Max(0, controlLockCount - 1);

        if (controlLockCount > 0) return;
        if (isGameOver) return;
        if (isGameClear) return;

        SetActionScriptsEnabled(true);
    }

    private void ForceUnlockControl()
    {
        controlLockCount = 0;

        if (isGameOver) return;
        if (isGameClear) return;

        SetActionScriptsEnabled(true);
    }

    private void SetActionScriptsEnabled(bool enabled)
    {
        if (!enabled)
        {
            CancelCombatChargeAndEffects();
        }

        if (_movement != null)
        {
            if (!enabled)
            {
                _movement.ForceStopTrail();
                _movement.StopAttackForwardMove();
                _movement.EndChargeAttackMove();
                _movement.SetAllowBlinkWhileAttacking(false);
                _movement.isAttacking = false;
                _movement.canMoveWhileAttacking = false;
            }

            _movement.enabled = enabled;
        }

        if (_playerInput != null)
        {
            _playerInput.ClearActionInputs();
            _playerInput.enabled = enabled;
        }

        if (_combat != null) _combat.enabled = enabled;
        if (_aiming != null) _aiming.enabled = enabled;
        if (_aimingController != null) _aimingController.enabled = enabled;

        if (!enabled)
        {
            DisablePlayerAttackHitboxes();
        }
    }

    private void CancelCombatChargeAndEffects()
    {
        if (_combat == null) return;

        _combat.gameObject.SendMessage(
            "CancelCurrentActionByExternalInterrupt",
            SendMessageOptions.DontRequireReceiver
        );
    }

    private void DisablePlayerAttackHitboxes()
    {
        MonoBehaviour[] scripts = GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour script in scripts)
        {
            if (script == null) continue;

            string scriptName = script.GetType().Name;

            if (scriptName == "WeaponHitbox")
            {
                script.gameObject.SendMessage("DisableHitbox", SendMessageOptions.DontRequireReceiver);
            }
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

    private void UpdateHPUI()
    {
        if (hpText != null)
        {
            hpText.text = "HP: " + Mathf.CeilToInt(Mathf.Max(0f, currentHp));
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
        if (hardModeFinalDeathCoroutine != null)
        {
            StopCoroutine(hardModeFinalDeathCoroutine);
            hardModeFinalDeathCoroutine = null;
        }

        ResetGameOverButtons();

        isGameOver = false;
        isGameClear = false;
        isInvincible = false;
        controlLockCount = 0;

        currentHp = maxHp;
        UpdateHPUI();

        StopPlayerCoroutinesForRevive();

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
            _animator.ResetTrigger("TakingDamage");
            _animator.ResetTrigger("KnockDown");
            _animator.ResetTrigger("Die");
            _animator.Play("RFA_Movement");
        }

        Debug.Log("Player Revived");
    }

    private void StopPlayerCoroutinesForRevive()
    {
        if (hpDelayCoroutine != null) StopCoroutine(hpDelayCoroutine);
        if (damageFlashCoroutine != null) StopCoroutine(damageFlashCoroutine);
        if (damageRoutineCoroutine != null) StopCoroutine(damageRoutineCoroutine);
        if (externalKnockbackCoroutine != null) StopCoroutine(externalKnockbackCoroutine);
        if (staggerCoroutine != null) StopCoroutine(staggerCoroutine);

        hpDelayCoroutine = null;
        damageFlashCoroutine = null;
        damageRoutineCoroutine = null;
        externalKnockbackCoroutine = null;
        staggerCoroutine = null;

        if (damageFlashCanvasGroup != null)
        {
            damageFlashCanvasGroup.alpha = 0f;
        }
    }

    public void BackToTitle()
    {
        Time.timeScale = 1f;
        SetGameOverScriptsEnabled(true);
        BattleCursorManager.UnlockCursor();
        SceneManager.LoadScene(titleSceneName);
    }

    private void GameOver()
    {
        if (isGameOver) return;

        if (ShouldUseHardModeFinalDeath())
        {
            StartHardModeFinalDeath();
            return;
        }

        isGameOver = true;
        isInvincible = true;
        controlLockCount = 999;

        StopPlayerCoroutinesOnGameOver();

        if (_movement != null)
        {
            _movement.ForceStopTrail();
        }

        CancelCombatChargeAndEffects();
        DisablePlayerAttackHitboxes();

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

    private bool ShouldUseHardModeFinalDeath()
    {
        if (!hardModeOneDeathRetry) return false;
        if (!IsHardMode()) return false;

        hardModeDeathCount++;
        return hardModeDeathCount > Mathf.Max(0, hardModeAllowedDeaths);
    }

    private bool IsHardMode()
    {
        return QuestDifficultyImageSelector.LoadSavedDifficulty() == QuestDifficultyImageSelector.Difficulty.Hard;
    }

    private void StartHardModeFinalDeath()
    {
        isGameOver = true;
        isInvincible = true;
        controlLockCount = 999;

        StopPlayerCoroutinesOnGameOver();

        if (_movement != null)
        {
            _movement.ForceStopTrail();
        }

        CancelCombatChargeAndEffects();
        DisablePlayerAttackHitboxes();

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

        if (hardModeFinalDeathCoroutine != null)
        {
            StopCoroutine(hardModeFinalDeathCoroutine);
        }

        hardModeFinalDeathCoroutine = StartCoroutine(HardModeFinalDeathRoutine());

        Debug.Log("HARD FINAL DEATH. Return to title.");
    }

    private IEnumerator HardModeFinalDeathRoutine()
    {
        ShowOnlyHardModeYouDiedText();

        yield return new WaitForSeconds(hardModeReturnToTitleDelay);

        Time.timeScale = 1f;
        BattleCursorManager.UnlockCursor();
        SceneManager.LoadScene(titleSceneName);
    }

    private void ShowOnlyHardModeYouDiedText()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (gameOverCanvasGroup == null && gameOverPanel != null)
        {
            gameOverCanvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
        }

        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 1f;
            gameOverCanvasGroup.interactable = false;
            gameOverCanvasGroup.blocksRaycasts = false;
        }

        GameObject youDied = ResolveHardModeYouDiedObject();

        if (youDied == null)
        {
            Debug.LogWarning("Hard最終死亡用のYouDiedテキストが見つかりません。PlayerHPのHard Mode You Died Textに入れてください。", this);
            return;
        }

        if (gameOverPanel != null && hideOtherGameOverChildrenOnHardFinalDeath)
        {
            SetOnlyYouDiedVisible(gameOverPanel.transform, youDied.transform);
        }

        // 親をOFFにしたままだとTextだけONにしても表示されないので、
        // GameOverPanelからYouDiedまでの親階層を必ずONに戻す。
        ActivatePathToYouDied(youDied.transform);
        youDied.SetActive(true);
    }

    private GameObject ResolveHardModeYouDiedObject()
    {
        if (hardModeYouDiedText != null)
        {
            // InspectorにGameOverPanelや親コンテナを間違って入れても、
            // その中のYouDiedテキストを優先して探す。
            GameObject foundInsideAssigned = FindYouDiedObject(hardModeYouDiedText.transform);
            if (foundInsideAssigned != null)
            {
                return foundInsideAssigned;
            }

            return hardModeYouDiedText;
        }

        if (gameOverPanel != null)
        {
            return FindYouDiedObject(gameOverPanel.transform);
        }

        return null;
    }

    private void SetOnlyYouDiedVisible(Transform root, Transform youDied)
    {
        if (root == null || youDied == null) return;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform t in all)
        {
            if (t == null) continue;
            if (t == root) continue;

            bool isYouDied = t == youDied;
            bool isChildOfYouDied = t.IsChildOf(youDied);
            bool isParentOfYouDied = youDied.IsChildOf(t);

            // YouDied本体、YouDiedの子、YouDiedまでの親階層だけ残す。
            // それ以外のボタン、背景、Score、Retryなどは全階層でOFF。
            t.gameObject.SetActive(isYouDied || isChildOfYouDied || isParentOfYouDied);
        }
    }

    private void ActivatePathToYouDied(Transform youDied)
    {
        if (youDied == null) return;

        Transform current = youDied;

        while (current != null)
        {
            current.gameObject.SetActive(true);

            if (gameOverPanel != null && current == gameOverPanel.transform)
            {
                break;
            }

            current = current.parent;
        }
    }

    private GameObject FindYouDiedObject(Transform root)
    {
        if (root == null) return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child == null) continue;

            string normalizedName = child.name.ToLower().Replace(" ", "").Replace("_", "").Replace("-", "");
            if (normalizedName.Contains("youdied"))
            {
                return child.gameObject;
            }
        }

        TMPro.TMP_Text[] texts = root.GetComponentsInChildren<TMPro.TMP_Text>(true);

        foreach (TMPro.TMP_Text text in texts)
        {
            if (text == null) continue;
            string normalizedText = text.text.ToLower().Replace(" ", "").Replace("_", "").Replace("-", "");
            if (normalizedText.Contains("youdied"))
            {
                return text.gameObject;
            }
        }

        return null;
    }

    private void StopPlayerCoroutinesOnGameOver()
    {
        if (damageRoutineCoroutine != null) StopCoroutine(damageRoutineCoroutine);
        if (externalKnockbackCoroutine != null) StopCoroutine(externalKnockbackCoroutine);
        if (staggerCoroutine != null) StopCoroutine(staggerCoroutine);

        damageRoutineCoroutine = null;
        externalKnockbackCoroutine = null;
        staggerCoroutine = null;
    }

    public void RestartGame()
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
            else if (scriptName == "PlayerInput")
            {
                _playerInput = script as Retro.ThirdPersonCharacter.PlayerInput;
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

        MonoBehaviour[] allScripts = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);

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

        if (!enabled)
        {
            CancelCombatChargeAndEffects();
        }

        if (_combat != null) _combat.enabled = enabled;
        if (_playerInput != null) _playerInput.enabled = enabled;
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
            float t = gameOverFadeDuration <= 0f ? 1f : timer / gameOverFadeDuration;
            gameOverCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
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