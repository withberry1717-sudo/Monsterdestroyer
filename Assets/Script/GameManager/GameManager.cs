using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Timer")]
    [Tooltip("現在の経過時間です。ScoreManagerが参照します。")]
    public float CurrentTime { get; private set; }

    [Header("Clear UI")]
    [Tooltip("クリア時に表示するパネルです。Canvas内のClearPanelを入れてください。")]
    [SerializeField] private GameObject clearPanel;

    [Tooltip("ゲーム開始時にClearPanelを自動で非表示にします。")]
    [SerializeField] private bool hideClearPanelOnStart = true;

    [Tooltip("クリア時にTime.timeScaleを0にします。スコア演出を止めたくない場合はオフ推奨です。")]
    [SerializeField] private bool pauseGameOnClear = false;

    [Header("Player Control")]
    [Tooltip("クリア時に止めたいスクリプトがあれば入れてください。空でもOKです。")]
    [SerializeField] private MonoBehaviour[] disableOnClear;

    private bool isGameClear = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        CurrentTime = 0f;
        isGameClear = false;

        if (hideClearPanelOnStart && clearPanel != null)
        {
            clearPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (isGameClear) return;

        CurrentTime += Time.deltaTime;
    }

    public void GameClear()
    {
        if (isGameClear) return;

        isGameClear = true;
        Debug.Log("Game Clear");

        if (clearPanel != null)
        {
            clearPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("GameManager: ClearPanelが設定されていません。");
        }

        // ★命令でスクリプトをオフにする処理
        if (disableOnClear != null)
        {
            foreach (MonoBehaviour script in disableOnClear)
            {
                if (script != null)
                {
                    script.enabled = false;
                }
            }
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ShowFinalScore(CurrentTime);
        }

        BattleCursorManager.UnlockCursor();

        // オフ推奨なので、Inspectorでチェックを外しておけば時間は止まりません
        if (pauseGameOnClear)
        {
            Time.timeScale = 0f;
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    public void BackToTitle()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
    }
}