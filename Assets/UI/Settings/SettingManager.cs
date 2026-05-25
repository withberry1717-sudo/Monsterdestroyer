using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
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
        LoadSettings();

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
        if (qualityDropdown == null) return;

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
        if (resolutionDropdown == null) return;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();

        for (int i = 0; i < resolutions.Length; i++)
        {
            options.Add(resolutions[i].x + " x " + resolutions[i].y);
        }

        resolutionDropdown.AddOptions(options);
    }

    public void ChangeQuality(int qualityIndex)
    {
        qualityIndex = Mathf.Clamp(qualityIndex, 0, 2);

        // Laptop：ノーパソ・低スペック向け
        if (qualityIndex == 0)
        {
            QualitySettings.SetQualityLevel(0);

            // 影をかなり軽くする
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;
            QualitySettings.shadowResolution = ShadowResolution.Low;

            // ギザギザ補正OFF
            QualitySettings.antiAliasing = 0;

            // 遠景やLODを軽くする
            QualitySettings.lodBias = 0.5f;
            QualitySettings.maximumLODLevel = 1;

            // テクスチャを少し軽くする
            QualitySettings.globalTextureMipmapLimit = 1;

            // フレームレートは60狙い
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
        // Normal：普通のPC向け
        else if (qualityIndex == 1)
        {
            QualitySettings.SetQualityLevel(0);

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
        // High：見栄え重視
        else if (qualityIndex == 2)
        {
            QualitySettings.SetQualityLevel(0);

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
    }

    public void ChangeResolution(int resolutionIndex)
    {
        resolutionIndex = Mathf.Clamp(resolutionIndex, 0, resolutions.Length - 1);

        Vector2Int selectedResolution = resolutions[resolutionIndex];

        Screen.SetResolution(
            selectedResolution.x,
            selectedResolution.y,
            Screen.fullScreen
        );

        PlayerPrefs.SetInt(ResolutionKey, resolutionIndex);
        PlayerPrefs.Save();
    }

    public void ChangeVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VolumeKey, volume);
        PlayerPrefs.Save();
    }

    public void StartChangeBlinkKey()
    {
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

                    isWaitingForBlinkKey = false;
                    yield break;
                }
            }

            yield return null;
        }
    }

    private void LoadSettings()
    {
        // デフォルトはLaptopにしておくと、ノーパソの人でも安心
        int quality = PlayerPrefs.GetInt(QualityKey, 0);
        quality = Mathf.Clamp(quality, 0, 2);

        ChangeQuality(quality);

        if (qualityDropdown != null)
        {
            qualityDropdown.value = quality;
            qualityDropdown.RefreshShownValue();
        }

        int resolutionIndex = PlayerPrefs.GetInt(ResolutionKey, 2);
        resolutionIndex = Mathf.Clamp(resolutionIndex, 0, resolutions.Length - 1);

        ChangeResolution(resolutionIndex);

        if (resolutionDropdown != null)
        {
            resolutionDropdown.value = resolutionIndex;
            resolutionDropdown.RefreshShownValue();
        }

        float volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        AudioListener.volume = volume;

        if (volumeSlider != null)
        {
            volumeSlider.value = volume;
        }

        string blinkKey = PlayerPrefs.GetString(BlinkKey, KeyCode.LeftShift.ToString());

        if (blinkKeyText != null)
        {
            blinkKeyText.text = "Blink Key : " + blinkKey;
        }
    }
}