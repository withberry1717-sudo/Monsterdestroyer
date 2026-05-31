using UnityEngine;
using UnityEngine.Rendering;

public class DifficultyWorldEnvironmentApplier : MonoBehaviour
{
    [System.Serializable]
    public class DifficultyEnvironmentData
    {
        [Header("Difficulty")]
        public QuestDifficultyImageSelector.Difficulty difficulty;

        [Header("Skybox / Background")]
        [Tooltip("この難易度で使うSkybox Material。空なら変更しません。")]
        public Material skyboxMaterial;

        [Tooltip("背景用オブジェクト群。Easy/Normal/Hardで別背景モデルを使う場合に入れます。")]
        public GameObject[] enableObjects;

        [Header("Lighting")]
        [Tooltip("この難易度で使うDirectional Light。空なら下のCommon Directional Lightを使います。")]
        public Light directionalLightOverride;

        [Tooltip("Directional Lightの色を変更するか。")]
        public bool overrideLightColor = true;

        public Color lightColor = Color.white;

        [Tooltip("Directional Lightの強さを変更するか。")]
        public bool overrideLightIntensity = true;

        public float lightIntensity = 1f;

        [Header("Ambient")]
        [Tooltip("Ambient Lightを変更するか。")]
        public bool overrideAmbient = true;

        public Color ambientColor = Color.gray;

        [Header("Fog")]
        [Tooltip("Fog設定を変更するか。")]
        public bool overrideFog = false;

        public bool fogEnabled = false;
        public Color fogColor = Color.gray;
        public float fogDensity = 0.01f;

        [Header("Rain")]
        [Tooltip("この難易度で雨Particleを表示するか。HardだけON推奨。")]
        public bool rainEnabled = false;
    }

    [Header("Common References")]
    [Tooltip("共通のDirectional Light。各難易度側のDirectional Light Overrideが空ならこれを使います。")]
    [SerializeField] private Light commonDirectionalLight;

    [Tooltip("雨Particleの親オブジェクト。HardだけONにしたい雨Prefab/Particleをここに入れます。")]
    [SerializeField] private GameObject rainObject;

    [Tooltip("Rain Object内のParticleを難易度切替時にStop/Playします。")]
    [SerializeField] private bool controlRainParticles = true;

    [Tooltip("ONなら未使用難易度のEnable Objectsを全部OFFにします。")]
    [SerializeField] private bool disableOtherDifficultyObjects = true;

    [Header("Difficulty Environments")]
    [SerializeField] private DifficultyEnvironmentData easy;
    [SerializeField] private DifficultyEnvironmentData normal;
    [SerializeField] private DifficultyEnvironmentData hard;

    [Header("Debug")]
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool logAppliedEnvironment = true;

    private void Reset()
    {
        easy = new DifficultyEnvironmentData
        {
            difficulty = QuestDifficultyImageSelector.Difficulty.Easy,
            lightColor = new Color(1f, 0.95f, 0.85f),
            lightIntensity = 1.0f,
            ambientColor = new Color(0.55f, 0.58f, 0.62f),
            fogEnabled = false,
            rainEnabled = false
        };

        normal = new DifficultyEnvironmentData
        {
            difficulty = QuestDifficultyImageSelector.Difficulty.Normal,
            lightColor = Color.white,
            lightIntensity = 1.0f,
            ambientColor = new Color(0.45f, 0.48f, 0.52f),
            fogEnabled = false,
            rainEnabled = false
        };

        hard = new DifficultyEnvironmentData
        {
            difficulty = QuestDifficultyImageSelector.Difficulty.Hard,
            lightColor = new Color(0.55f, 0.65f, 1f),
            lightIntensity = 0.65f,
            ambientColor = new Color(0.18f, 0.20f, 0.28f),
            overrideFog = true,
            fogEnabled = true,
            fogColor = new Color(0.12f, 0.14f, 0.18f),
            fogDensity = 0.018f,
            rainEnabled = true
        };
    }

    private void Start()
    {
        if (applyOnStart)
        {
            ApplySavedDifficultyEnvironment();
        }
    }

    public void ApplySavedDifficultyEnvironment()
    {
        QuestDifficultyImageSelector.Difficulty difficulty =
            QuestDifficultyImageSelector.LoadSavedDifficulty();

        ApplyEnvironment(difficulty);
    }

    public void ApplyEnvironment(QuestDifficultyImageSelector.Difficulty difficulty)
    {
        DifficultyEnvironmentData data = GetData(difficulty);

        if (data == null)
        {
            Debug.LogWarning("[DifficultyWorldEnvironmentApplier] 難易度データがありません: " + difficulty, this);
            return;
        }

        if (disableOtherDifficultyObjects)
        {
            DisableAllDifficultyObjects();
        }

        ApplySkybox(data);
        ApplyLighting(data);
        ApplyFog(data);
        ApplyDifficultyObjects(data);
        ApplyRain(data);

        if (logAppliedEnvironment)
        {
            Debug.Log("[DifficultyWorldEnvironmentApplier] Environment Applied: " + difficulty, this);
        }
    }

    private DifficultyEnvironmentData GetData(QuestDifficultyImageSelector.Difficulty difficulty)
    {
        if (easy != null && easy.difficulty == difficulty) return easy;
        if (normal != null && normal.difficulty == difficulty) return normal;
        if (hard != null && hard.difficulty == difficulty) return hard;

        return null;
    }

    private void ApplySkybox(DifficultyEnvironmentData data)
    {
        if (data.skyboxMaterial == null) return;

        RenderSettings.skybox = data.skyboxMaterial;

        // Skybox変更をすぐ反映
        DynamicGI.UpdateEnvironment();
    }

    private void ApplyLighting(DifficultyEnvironmentData data)
    {
        Light targetLight = data.directionalLightOverride != null
            ? data.directionalLightOverride
            : commonDirectionalLight;

        if (targetLight != null)
        {
            if (data.overrideLightColor)
            {
                targetLight.color = data.lightColor;
            }

            if (data.overrideLightIntensity)
            {
                targetLight.intensity = data.lightIntensity;
            }
        }

        if (data.overrideAmbient)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = data.ambientColor;
        }
    }

    private void ApplyFog(DifficultyEnvironmentData data)
    {
        if (!data.overrideFog) return;

        RenderSettings.fog = data.fogEnabled;
        RenderSettings.fogColor = data.fogColor;
        RenderSettings.fogDensity = Mathf.Max(0f, data.fogDensity);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
    }

    private void ApplyDifficultyObjects(DifficultyEnvironmentData data)
    {
        if (data.enableObjects == null) return;

        for (int i = 0; i < data.enableObjects.Length; i++)
        {
            if (data.enableObjects[i] != null)
            {
                data.enableObjects[i].SetActive(true);
            }
        }
    }

    private void DisableAllDifficultyObjects()
    {
        DisableObjects(easy);
        DisableObjects(normal);
        DisableObjects(hard);
    }

    private void DisableObjects(DifficultyEnvironmentData data)
    {
        if (data == null || data.enableObjects == null) return;

        for (int i = 0; i < data.enableObjects.Length; i++)
        {
            if (data.enableObjects[i] != null)
            {
                data.enableObjects[i].SetActive(false);
            }
        }
    }

    private void ApplyRain(DifficultyEnvironmentData data)
    {
        if (rainObject == null) return;

        rainObject.SetActive(data.rainEnabled);

        if (!controlRainParticles) return;

        ParticleSystem[] rainParticles = rainObject.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < rainParticles.Length; i++)
        {
            if (rainParticles[i] == null) continue;

            if (data.rainEnabled)
            {
                rainParticles[i].Play(true);
            }
            else
            {
                rainParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
