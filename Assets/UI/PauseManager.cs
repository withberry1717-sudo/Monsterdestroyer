using UnityEngine;
using UnityEngine.SceneManagement;
using Retro.ThirdPersonCharacter;

public class PauseManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Player")]
    [SerializeField] private GameObject playerObject;

    [Header("Camera")]
    [SerializeField] private MonoBehaviour[] cameraScripts;

    [Header("Enemy")]
    [SerializeField] private MonoBehaviour[] enemyScripts;

    private bool isPaused = false;

    private Movement playerMovement;
    private Combat playerCombat;
    private MonoBehaviour playerAiming;
    private MonoBehaviour playerAimingController;

    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (playerObject != null)
        {
            playerMovement = playerObject.GetComponent<Movement>();
            playerCombat = playerObject.GetComponent<Combat>();

            // ñºëOÇ≈íTÇ∑ÅBAimingånÇÃå^ñºÇ™à·Ç¡ÇƒÇ‡èEÇ¶ÇÈÇÊÇ§Ç…Ç∑ÇÈ
            MonoBehaviour[] scripts = playerObject.GetComponents<MonoBehaviour>();

            foreach (MonoBehaviour script in scripts)
            {
                if (script == null) continue;

                string scriptName = script.GetType().Name;

                if (scriptName == "Aiming")
                {
                    playerAiming = script;
                }
                else if (scriptName == "AimingController")
                {
                    playerAimingController = script;
                }
            }
        }

        Time.timeScale = 1f;
        SetGameplayEnabled(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                OpenPauseMenu();
            }
        }
    }

    public void OpenPauseMenu()
    {
        isPaused = true;
        Time.timeScale = 0f;

        SetGameplayEnabled(false);

        if (pausePanel != null) pausePanel.SetActive(true);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        SetGameplayEnabled(true);

        if (pausePanel != null) pausePanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void BackToTitle()
    {
        Time.timeScale = 1f;
        SetGameplayEnabled(true);
        SceneManager.LoadScene("TitleScene");
    }

    public void OpenControls()
    {
        if (controlsPanel != null) controlsPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void CloseControls()
    {
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void SetGameplayEnabled(bool enabled)
    {
        if (playerMovement != null) playerMovement.enabled = enabled;
        if (playerCombat != null) playerCombat.enabled = enabled;
        if (playerAiming != null) playerAiming.enabled = enabled;
        if (playerAimingController != null) playerAimingController.enabled = enabled;

        if (cameraScripts != null)
        {
            foreach (MonoBehaviour script in cameraScripts)
            {
                if (script != null) script.enabled = enabled;
            }
        }

        if (enemyScripts != null)
        {
            foreach (MonoBehaviour script in enemyScripts)
            {
                if (script != null) script.enabled = enabled;
            }
        }
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}