using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerHP : MonoBehaviour
{
    [SerializeField] private int maxHp = 3;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameClearPanel;

    private int currentHp;
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

    public void TakeDamage(int damage)
    {
        if (isGameOver || isGameClear) return;

        currentHp -= damage;
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
            hpText.text = "HP: " + currentHp;
        }
    }

    void GameOver()
    {
        isGameOver = true;

        if (hpText != null)
        {
            hpText.text = "HP: 0";
        }

        if (_animator != null)
        {
            _animator.SetTrigger("Die");
        }

        if (_movement != null)
        {
            _movement.enabled = false;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        Debug.Log("GAME OVER. Restarting in 1.5 seconds...");
        Invoke("RestartGame", 1.5f);
    }

    public void GameClear()
    {
        if (isGameOver || isGameClear) return;

        isGameClear = true;

        if (_movement != null)
        {
            _movement.enabled = false;
        }

        if (gameClearPanel != null)
        {
            gameClearPanel.SetActive(true);
        }

        Debug.Log("GAME CLEAR");
    }

    void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}