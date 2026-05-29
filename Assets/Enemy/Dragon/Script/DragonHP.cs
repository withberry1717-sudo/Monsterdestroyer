using UnityEngine;
using System;
using System.Collections;

public class DragonHP : MonoBehaviour
{
    [Header("ドラゴンの本体HP")]
    [Tooltip("ドラゴン本体の最大HP")]
    public float maxHP = 3000f;

    [Tooltip("現在の本体HP")]
    public float currentHP;

    [Header("尻尾切断部位HP")]
    [Tooltip("尻尾切断部位の最大HP。これが0になると尻尾切断が発生し、尻尾攻撃を封印します。")]
    public float maxTailCrystalHP = 300f;

    [Tooltip("現在の尻尾切断部位HP")]
    public float currentTailCrystalHP;

    [Tooltip("尻尾が切断済みか。昔のクリスタル破壊判定も兼ねます。")]
    public bool isTailCrystalBroken = false;

    [Header("尻尾部位ダメージ設定")]
    [Tooltip("ONにすると、尻尾切断部位への攻撃でも本体HPにダメージが入ります。")]
    [SerializeField] private bool tailDamageAlsoDamagesBody = true;

    [Tooltip("尻尾切断部位に攻撃した時、本体へ入るダメージ倍率。1なら同じダメージが本体にも入ります。")]
    [SerializeField] private float tailToBodyDamageMultiplier = 1.0f;

    [Header("互換用：昔のCrystal変数")]
    [Tooltip("昔のコードとの互換用。isTailCrystalBrokenと同じ意味")]
    public bool isCrystalBroken = false;

    [Header("状態")]
    [Tooltip("ドラゴンが死亡しているか")]
    public bool isDead = false;

    [Tooltip("HP50%イベントをすでに発動したか")]
    public bool halfHpTriggered = false;

    [Header("尻尾切断演出管理")]
    [Tooltip("DragonCoreの子オブジェクトにあるTailSeverControllerを入れてください。尻尾切断、吹っ飛び、当たり判定OFF、パーティクル、SEを行います。")]
    [SerializeField] private TailSeverController tailSeverController;

    [Tooltip("未設定なら子オブジェクトからTailSeverControllerを自動で探します。基本オン推奨です。")]
    [SerializeField] private bool autoFindTailSeverController = true;

    [Header("ゲームクリア演出")]
    [Tooltip("ドラゴンのHPが0になってから、クリアパネルを出すまでの待ち時間です。")]
    public float gameClearDelay = 5f;

    [Tooltip("オンにすると、Time.timeScaleが0でも5秒後にクリアパネルを出します。基本オン推奨です。")]
    public bool useRealtimeGameClearDelay = true;

    public event Action OnHalfHP;
    public event Action OnDeath;

    [Tooltip("尻尾切断時に呼ばれます。昔のOnTailCrystalBrokenと互換目的で残しています。")]
    public event Action OnTailCrystalBroken;

    [Tooltip("尻尾切断時に呼ばれます。昔のOnCrystalBrokenと互換目的で残しています。")]
    public event Action OnCrystalBroken;

    private bool gameClearStarted = false;

    private void Awake()
    {
        if (tailSeverController == null && autoFindTailSeverController)
        {
            tailSeverController = GetComponentInChildren<TailSeverController>(true);
        }
    }

    private void Start()
    {
        currentHP = maxHP;
        currentTailCrystalHP = maxTailCrystalHP;

        isDead = false;
        halfHpTriggered = false;
        gameClearStarted = false;

        isTailCrystalBroken = false;
        isCrystalBroken = false;
    }

    public void TakeDamage(float damage)
    {
        ApplyBodyDamage(damage, "本体");
    }

    private void ApplyBodyDamage(float damage, string sourceLabel)
    {
        if (isDead) return;

        float finalDamage = Mathf.Max(0f, damage);

        currentHP -= finalDamage;
        currentHP = Mathf.Max(currentHP, 0f);

        Debug.Log($"{sourceLabel}に {finalDamage} ダメージ。残り本体HP: {currentHP}");

        if (!halfHpTriggered && currentHP <= maxHP * 0.5f)
        {
            halfHpTriggered = true;
            Debug.Log("ドラゴンHP50%以下。強化フェーズへ移行");
            OnHalfHP?.Invoke();
        }

        if (currentHP <= 0f)
        {
            Die();
        }
    }

    public void TakeTailCrystalDamage(float damage)
    {
        TakeTailSeverPartDamage(damage);
    }

    public void TakeCrystalDamage(float damage)
    {
        TakeTailSeverPartDamage(damage);
    }

    public void TakeTailSeverPartDamage(float damage)
    {
        if (isDead) return;

        float finalDamage = Mathf.Max(0f, damage);

        // 尻尾への攻撃でも本体にダメージを入れる
        if (tailDamageAlsoDamagesBody)
        {
            float bodyDamage = finalDamage * Mathf.Max(0f, tailToBodyDamageMultiplier);
            ApplyBodyDamage(bodyDamage, "尻尾部位経由で本体");
        }

        if (isDead) return;
        if (IsTailSevered()) return;

        currentTailCrystalHP -= finalDamage;
        currentTailCrystalHP = Mathf.Max(currentTailCrystalHP, 0f);

        Debug.Log($"尻尾切断部位に {finalDamage} ダメージ。残り耐久: {currentTailCrystalHP}");

        if (currentTailCrystalHP <= 0f)
        {
            SeverTail();
        }
    }

    private void SeverTail()
    {
        if (IsTailSevered()) return;

        isTailCrystalBroken = true;
        isCrystalBroken = true;
        currentTailCrystalHP = 0f;

        Debug.Log("尻尾切断。尻尾攻撃を封印して、切断演出を実行");

        if (tailSeverController != null)
        {
            tailSeverController.SeverTail();
        }
        else
        {
            Debug.LogWarning("TailSeverController が見つかりません。尻尾切断・吹っ飛び・当たり判定OFF・パーティクル・SEは行われません。");
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddCrystalBreakBonus();
        }

        OnTailCrystalBroken?.Invoke();
        OnCrystalBroken?.Invoke();
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        currentHP = 0f;

        Debug.Log("ドラゴン討伐完了");

        OnDeath?.Invoke();

        if (!gameClearStarted)
        {
            gameClearStarted = true;
            StartCoroutine(GameClearDelayRoutine());
        }
    }

    private IEnumerator GameClearDelayRoutine()
    {
        float delay = Mathf.Max(0f, gameClearDelay);

        if (useRealtimeGameClearDelay)
        {
            yield return new WaitForSecondsRealtime(delay);
        }
        else
        {
            yield return new WaitForSeconds(delay);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameClear();
        }
        else
        {
            Debug.LogWarning("GameManager.Instance が見つからないため、ゲームクリア処理を呼べませんでした。");
        }
    }

    public bool IsCrystalBroken()
    {
        return isTailCrystalBroken || isCrystalBroken;
    }

    public bool IsTailSevered()
    {
        return isTailCrystalBroken || isCrystalBroken;
    }
}