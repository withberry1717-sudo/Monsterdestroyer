using UnityEngine;
using System.Collections;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI設定")]
    public GameObject clearPanel;     // クリア画面のパネル
    public TextMeshProUGUI timeText;  // クリア画面のタイム文字
    public TextMeshProUGUI rankText;  // クリア画面のランク文字

    [Header("プレイ中のUI設定")]
    public TextMeshProUGUI inGameTimeText; // ★ここを追加！プレイ中に表示するタイム文字

    [Header("ランクのタイム設定（秒）")]
    public float sRankTime = 90f;
    public float aRankTime = 120f;
    public float bRankTime = 240f;
    public float cRankTime = 300f;

    private float currentTime = 0f;
    private bool isGameActive = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        if (clearPanel != null) clearPanel.SetActive(false);
    }

    void Update()
    {
        if (isGameActive)
        {
            currentTime += Time.deltaTime;

            // ★追加：プレイ中も常に時間を計算して表示する
            if (inGameTimeText != null)
            {
                int currentMinutes = Mathf.FloorToInt(currentTime / 60F);
                int currentSeconds = Mathf.FloorToInt(currentTime - currentMinutes * 60);
                inGameTimeText.text = string.Format("{0:0}:{1:00}", currentMinutes, currentSeconds);
            }
        }
    }

    // ==========================================
    // ゲームクリア管理システム
    // ==========================================
    public void GameClear()
    {
        if (!isGameActive) return;

        isGameActive = false;

        string rank = "D";
        if (currentTime <= sRankTime) rank = "S";
        else if (currentTime <= aRankTime) rank = "A";
        else if (currentTime <= bRankTime) rank = "B";
        else if (currentTime <= cRankTime) rank = "C";

        int minutes = Mathf.FloorToInt(currentTime / 60F);
        int seconds = Mathf.FloorToInt(currentTime - minutes * 60);
        string niceTime = string.Format("{0:0}:{1:00}", minutes, seconds);

        if (timeText != null) timeText.text = "Clear Time : " + niceTime;
        if (rankText != null) rankText.text = "Rank : " + rank;

        if (clearPanel != null) clearPanel.SetActive(true);

        // クリアしたらプレイ中のタイマーは見えなくする（お好みで）
        if (inGameTimeText != null) inGameTimeText.gameObject.SetActive(false);
    }

    // ==========================================
    // ヒットストップ管理システム
    // ==========================================
    public void Stop(float duration)
    {
        if (Time.timeScale < 1f) return;
        StartCoroutine(DoHitStop(duration));
    }

    private IEnumerator DoHitStop(float duration)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
}