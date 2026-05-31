using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

        // ゲーム全体の音量はここで一括管理する。
        // BGM側で ignoreListenerVolume = true にしていると、この値が効かなくなる。
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
            blinkKeyText.text = "Press any key...";
        }

        StartCoroutine(WaitForBlinkKey());
    }

    private IEnumerator WaitForBlinkKey()
    {
        yield return null;

        while (isWaitingForBlinkKey)
        {
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    PlayerPrefs.SetString(BlinkKey, key.ToString());
                    PlayerPrefs.Save();

                    if (blinkKeyText != null)
                    {
                        blinkKeyText.text = "Blink Key : " + key.ToString();
                    }

                    Debug.Log("Blinkキー変更: " + key);

                    isWaitingForBlinkKey = false;
                    yield break;
                }
            }

            yield return null;
        }
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
            blinkKeyText.text = "Blink Key : " + blinkKey;
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
