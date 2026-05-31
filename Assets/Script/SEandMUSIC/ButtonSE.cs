using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSE : MonoBehaviour
{
    [Header("鳴らしたい効果音")]
    public AudioClip clickSound;

    [Header("音量設定")]
    [Range(0f, 1f)]
    public float volume = 1.0f;

    [Header("フロムゲー風 調整")]
    public bool isHeavyDarkFantasyStyle = true;

    [Header("設定音量を反映")]
    [Tooltip("ONならSettingManagerのAudioListener.volumeに従います。基本ON推奨。")]
    [SerializeField] private bool respectMasterVolume = true;

    [Tooltip("ONならポーズ中でもUI音を鳴らします。")]
    [SerializeField] private bool playWhilePaused = true;

    private static AudioSource _sharedAudioSource;
    private static GameObject _sharedAudioObject;

    private Button button;

    private void Awake()
    {
        PrepareSharedAudioSource();

        button = GetComponent<Button>();
        button.onClick.RemoveListener(PlaySound);
        button.onClick.AddListener(PlaySound);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlaySound);
        }
    }

    private void PrepareSharedAudioSource()
    {
        if (_sharedAudioSource != null) return;

        _sharedAudioObject = GameObject.Find("UI_SE_Player");

        if (_sharedAudioObject == null)
        {
            _sharedAudioObject = new GameObject("UI_SE_Player");
            DontDestroyOnLoad(_sharedAudioObject);
        }

        _sharedAudioSource = _sharedAudioObject.GetComponent<AudioSource>();

        if (_sharedAudioSource == null)
        {
            _sharedAudioSource = _sharedAudioObject.AddComponent<AudioSource>();
        }

        _sharedAudioSource.playOnAwake = false;

        // ポーズ中に鳴らすかどうかだけ制御
        _sharedAudioSource.ignoreListenerPause = playWhilePaused;

        // ここが重要：音量バーを反映する
        _sharedAudioSource.ignoreListenerVolume = !respectMasterVolume;
    }

    private void PlaySound()
    {
        if (clickSound == null) return;

        PrepareSharedAudioSource();

        if (_sharedAudioSource == null) return;

        _sharedAudioSource.ignoreListenerPause = playWhilePaused;
        _sharedAudioSource.ignoreListenerVolume = !respectMasterVolume;

        if (isHeavyDarkFantasyStyle)
        {
            _sharedAudioSource.pitch = Random.Range(0.80f, 0.95f);
        }
        else
        {
            _sharedAudioSource.pitch = 1.0f;
        }

        _sharedAudioSource.PlayOneShot(clickSound, volume);
    }
}