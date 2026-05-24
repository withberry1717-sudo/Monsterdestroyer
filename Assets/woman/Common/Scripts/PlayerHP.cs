using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerHP : MonoBehaviour
{
    // ★ここを int から float にして、初期値を100にします
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameClearPanel;

    private float currentHp; // ★ここも float に変更
    private bool isGameOver = false;
    private bool isGameClear = false;

    private Animator _animator;
    private Retro.ThirdPersonCharacter.Movement _movement;

    void Start()
    {
        currentHp = maxHp;
        _animator = GetComponent<Animator>();
        _movement = GetComponent<Retro.ThirdPersonCharacter.Movement>();

        UpdateHPUI();
        Debug.Log("Game Start! Player HP: " + currentHp);
    }

    // ★int だったダメージ値を float に変更
    public void TakeDamage(float damage)
    {
        if (isGameOver || isGameClear) return;

        currentHp -= damage; // 1ずつ減らすのではなく、ダメージ分をそのまま減らす！
        UpdateHPUI();
        Debug.Log("Hit! Damage: " + damage + " | Remaining HP: " + currentHp);

        if (currentHp <= 0)
        {
            GameOver();
        }
        else
        {
            if (_animator != null)
            {
                _animator.SetTrigger("Stun");
            }
        }
    }

    void UpdateHPUI()
    {
        if (hpText != null)
        {
            // ★表示も「HP: 整数」になるように調整
            hpText.text = "HP: " + Mathf.CeilToInt(currentHp);
        }
    }

    // （以下、GameOverやGameClearなどの処理はそのまま）
    void GameOver()
    {
        isGameOver = true;
        if (hpText != null) hpText.text = "HP: 0";
        if (_animator != null) _animator.SetTrigger("Die");
        if (_movement != null) _movement.enabled = false;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        Debug.Log("GAME OVER. Restarting...");
        Invoke("RestartGame", 2.0f);
    }

    // ... (RestartGameなどのメソッド)
    void RestartGame() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
}