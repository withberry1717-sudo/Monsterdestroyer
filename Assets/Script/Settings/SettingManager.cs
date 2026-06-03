using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Retro.ThirdPersonCharacter;
using System.Collections;
using System.Collections.Generic;

public class SettingManager : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Quality")]
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Header("Resolution")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("Volume")]
    [SerializeField] private Slider volumeSlider;

    [Header("Blink Key")]
    [SerializeField] private TextMeshProUGUI blinkKeyText;

    private bool isWaitingForBlinkKey = false;
    private string pendingGamepadBlinkBinding = null;
    private bool waitingGamepadConfirm = false;

    private const string QualityKey = "QualityLevel";
    private const string ResolutionKey = "ResolutionIndex";
    private const string VolumeKey = "MasterVolume";
    private const string BlinkKey = "BlinkKey";

    private readonly Vector2Int[] resolutions =
    {
        new Vector2Int(1920, 1080),
        new Vector2Int(1600, 900),
        new Vector2Int(1280, 720)
    };

    private void Start()
    {
        SetupQualityDropdown();
        SetupResolutionDropdown();
        SetupVolumeSlider();

        LoadSettings();

        if (qualityDropdown != null)
        {
            qualityDropdown.onValueChanged.RemoveAllListeners();
            qualityDropdown.onValueChanged.AddListener(ChangeQuality);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.RemoveAllListeners();
            resolutionDropdown.onValueChanged.AddListener(ChangeResolution);
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.onValueChanged.AddListener(ChangeVolume);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        isWaitingForBlinkKey = false;
    }

    public void Retry()
    {
        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    private void SetupQualityDropdown()
    {
        if (qualityDropdown == null)
        {
            Debug.LogWarning("QualityDropdownが設定されていません。");
            return;
        }

        qualityDropdown.ClearOptions();

        qualityDropdown.AddOptions(new List<string>
        {
            "Laptop",
            "Normal",
            "High"
        });
    }

    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null)
        {
            Debug.LogWarning("ResolutionDropdownが設定されていません。");
            return;
        }

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();

        for (int i = 0; i < resolutions.Length; i++)
        {
            options.Add(resolutions[i].x + " x " + resolutions[i].y);
        }

        resolutionDropdown.AddOptions(options);
    }

    private void SetupVolumeSlider()
    {
        if (volumeSlider == null)
        {
            Debug.LogWarning("VolumeSliderが設定されていません。");
            return;
        }

        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
    }

    public void ChangeQuality(int qualityIndex)
    {
        qualityIndex = Mathf.Clamp(qualityIndex, 0, 2);

        if (qualityIndex == 0)
        {
            QualitySettings.SetQualityLevel(0, true);
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.antiAliasing = 0;
            QualitySettings.lodBias = 0.5f;
            QualitySettings.maximumLODLevel = 1;
            QualitySettings.globalTextureMipmapLimit = 1;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
        else if (qualityIndex == 1)
        {
            QualitySettings.SetQualityLevel(0, true);
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowDistance = 45f;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.antiAliasing = 2;
            QualitySettings.lodBias = 1.0f;
            QualitySettings.maximumLODLevel = 0;
            QualitySettings.globalTextureMipmapLimit = 0;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
        else
        {
            QualitySettings.SetQualityLevel(0, true);
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 100f;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.antiAliasing = 4;
            QualitySettings.lodBias = 1.5f;
            QualitySettings.maximumLODLevel = 0;
            QualitySettings.globalTextureMipmapLimit = 0;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        PlayerPrefs.SetInt(QualityKey, qualityIndex);
        PlayerPrefs.Save();

        Debug.Log("画質変更: " + qualityIndex);
    }

    public void ChangeResolution(int resolutionIndex)
    {
        resolutionIndex = Mathf.Clamp(resolutionIndex, 0, resolutions.Length - 1);

        Vector2Int selectedResolution = resolutions[resolutionIndex];

        Screen.SetResolution(
            selectedResolution.x,
            selectedResolution.y,
            Screen.fullScreenMode
        );

        PlayerPrefs.SetInt(ResolutionKey, resolutionIndex);
        PlayerPrefs.Save();

        Debug.Log("解像度変更: " + selectedResolution.x + " x " + selectedResolution.y);
    }

    public void ChangeVolume(float volume)
    {
        ApplyMasterVolume(volume, true);
    }

    private void ApplyMasterVolume(float volume, bool save)
    {
        volume = Mathf.Clamp01(volume);

        AudioListener.volume = volume;

        if (save)
        {
            PlayerPrefs.SetFloat(VolumeKey, volume);
            PlayerPrefs.Save();
        }

        Debug.Log("音量変更: " + volume);
    }

    public void StartChangeBlinkKey()
    {
        if (isWaitingForBlinkKey) return;

        isWaitingForBlinkKey = true;

        if (blinkKeyText != null)
        {
            blinkKeyText.text = "Press new blink button, then ×";
        }

        StartCoroutine(WaitForBlinkKey());
    }

    private IEnumerator WaitForBlinkKey()
    {
        yield return null;

        pendingGamepadBlinkBinding = null;
        waitingGamepadConfirm = false;

        while (isWaitingForBlinkKey)
        {
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    SaveBlinkBinding(key.ToString());
                    yield break;
                }
            }

            var gamepad = UnityEngine.InputSystem.Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.buttonEast.wasPressedThisFrame)
                {
                    CancelBlinkBindingChange();
                    yield break;
                }

                if (waitingGamepadConfirm && gamepad.buttonSouth.wasPressedThisFrame)
                {
                    SaveBlinkBinding(pendingGamepadBlinkBinding);
                    yield break;
                }

                string pressedBinding = ReadPressedGamepadBlinkBinding(gamepad);

                if (!string.IsNullOrEmpty(pressedBinding))
                {
                    if (pressedBinding == PlayerInput.ToGamepadBlinkPrefsValue(PlayerInput.GamepadBlinkButton.South))
                    {
                        if (blinkKeyText != null)
                        {
                            blinkKeyText.text = "Press new blink button, then ×";
                        }
                    }
                    else
                    {
                        pendingGamepadBlinkBinding = pressedBinding;
                        waitingGamepadConfirm = true;

                        if (blinkKeyText != null)
                        {
                            blinkKeyText.text = "Blink Key : " + PlayerInput.FormatBlinkPrefsValue(pressedBinding) + "  → Press × to Confirm / 〇 to Cancel";
                        }
                    }
                }
            }

            yield return null;
        }
    }

    private string ReadPressedGamepadBlinkBinding(UnityEngine.InputSystem.Gamepad gamepad)
    {
        if (gamepad == null) return null;

        if (gamepad.leftShoulder.wasPressedThisFrame) return PlayerInput.ToGamepadBlinkPrefsValue(PlayerInput.GamepadBlinkButton.LeftShoulder);
        if (gamepad.rightShoulder.wasPressedThisFrame) return PlayerInput.ToGamepadBlinkPrefsValue(PlayerInput.GamepadBlinkButton.RightShoulder);
        if (gamepad.leftTrigger.wasPressedThisFrame) return PlayerInput.ToGamepadBlinkPrefsValue(PlayerInput.GamepadBlinkButton.LeftTrigger);
        if (gamepad.rightTrigger.wasPressedThisFrame) return PlayerInput.ToGamepadBlinkPrefsValue(PlayerInput.GamepadBlinkButton.RightTrigger);
        if (gamepad.buttonSouth.wasPressedThisFrame) return PlayerInput.ToGamepadBlinkPrefsValue(PlayerInput.GamepadBlinkButton.South);
        if (gamepad.buttonEast.wasPressedThisFrame) return PlayerInput.ToGamepadBlinkPrefsValue(PlayerInput.GamepadBlinkButton.East);
        if (gamepad.buttonWest.wasPressedThisFrame) return PlayerInput.ToGamepadBlinkPrefsValue(PlayerInput.GamepadBlinkButton.West);
        if (gamepad.buttonNorth.wasPressedThisFrame) return PlayerInput.ToGamepadBlinkPrefsValue(PlayerInput.GamepadBlinkButton.North);
        if (gamepad.selectButton.wasPressedThisFrame) return PlayerInput.ToGamepadBlinkPrefsValue(PlayerInput.GamepadBlinkButton.Select);
        if (gamepad.startButton.wasPressedThisFrame) return PlayerInput.ToGamepadBlinkPrefsValue(PlayerInput.GamepadBlinkButton.Start);
        if (gamepad.leftStickButton.wasPressedThisFrame) return PlayerInput.ToGamepadBlinkPrefsValue(PlayerInput.GamepadBlinkButton.LeftStick);
        if (gamepad.rightStickButton.wasPressedThisFrame) return PlayerInput.ToGamepadBlinkPrefsValue(PlayerInput.GamepadBlinkButton.RightStick);
        if (gamepad.dpad.up.wasPressedThisFrame) return PlayerInput.ToGamepadBlinkPrefsValue(PlayerInput.GamepadBlinkButton.DpadUp);
        if (gamepad.dpad.down.wasPressedThisFrame) return PlayerInput.ToGamepadBlinkPrefsValue(PlayerInput.GamepadBlinkButton.DpadDown);
        if (gamepad.dpad.left.wasPressedThisFrame) return PlayerInput.ToGamepadBlinkPrefsValue(PlayerInput.GamepadBlinkButton.DpadLeft);
        if (gamepad.dpad.right.wasPressedThisFrame) return PlayerInput.ToGamepadBlinkPrefsValue(PlayerInput.GamepadBlinkButton.DpadRight);

        return null;
    }

    private void CancelBlinkBindingChange()
    {
        isWaitingForBlinkKey = false;
        pendingGamepadBlinkBinding = null;
        waitingGamepadConfirm = false;

        string blinkKey = PlayerPrefs.GetString(BlinkKey, KeyCode.LeftShift.ToString());

        if (blinkKeyText != null)
        {
            blinkKeyText.text = "Blink Key : " + PlayerInput.FormatBlinkPrefsValue(blinkKey);
        }
    }

    private void SaveBlinkBinding(string value)
    {
        PlayerPrefs.SetString(BlinkKey, value);
        PlayerPrefs.Save();

        if (blinkKeyText != null)
        {
            blinkKeyText.text = "Blink Key : " + PlayerInput.FormatBlinkPrefsValue(value);
        }

        Debug.Log("Blinkキー変更: " + value);

        isWaitingForBlinkKey = false;
    }

    private void LoadSettings()
    {
        int quality = PlayerPrefs.GetInt(QualityKey, 0);
        quality = Mathf.Clamp(quality, 0, 2);

        if (qualityDropdown != null)
        {
            qualityDropdown.SetValueWithoutNotify(quality);
            qualityDropdown.RefreshShownValue();
        }

        ApplyQualityWithoutSave(quality);

        int resolutionIndex = PlayerPrefs.GetInt(ResolutionKey, 2);
        resolutionIndex = Mathf.Clamp(resolutionIndex, 0, resolutions.Length - 1);

        if (resolutionDropdown != null)
        {
            resolutionDropdown.SetValueWithoutNotify(resolutionIndex);
            resolutionDropdown.RefreshShownValue();
        }

        ApplyResolutionWithoutSave(resolutionIndex);

        float volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        volume = Mathf.Clamp01(volume);

        ApplyMasterVolume(volume, false);

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(volume);
        }

        string blinkKey = PlayerPrefs.GetString(BlinkKey, KeyCode.LeftShift.ToString());

        if (blinkKeyText != null)
        {
            blinkKeyText.text = "Blink Key : " + PlayerInput.FormatBlinkPrefsValue(blinkKey);
        }
    }

    private void ApplyQualityWithoutSave(int qualityIndex)
    {
        if (qualityIndex == 0)
        {
            QualitySettings.SetQualityLevel(0, true);
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.antiAliasing = 0;
            QualitySettings.lodBias = 0.5f;
            QualitySettings.maximumLODLevel = 1;
            QualitySettings.globalTextureMipmapLimit = 1;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
        else if (qualityIndex == 1)
        {
            QualitySettings.SetQualityLevel(0, true);
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowDistance = 45f;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.antiAliasing = 2;
            QualitySettings.lodBias = 1.0f;
            QualitySettings.maximumLODLevel = 0;
            QualitySettings.globalTextureMipmapLimit = 0;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
        else
        {
            QualitySettings.SetQualityLevel(0, true);
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 100f;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.antiAliasing = 4;
            QualitySettings.lodBias = 1.5f;
            QualitySettings.maximumLODLevel = 0;
            QualitySettings.globalTextureMipmapLimit = 0;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
    }

    private void ApplyResolutionWithoutSave(int resolutionIndex)
    {
        resolutionIndex = Mathf.Clamp(resolutionIndex, 0, resolutions.Length - 1);

        Vector2Int selectedResolution = resolutions[resolutionIndex];

        Screen.SetResolution(
            selectedResolution.x,
            selectedResolution.y,
            Screen.fullScreenMode
        );
    }
}