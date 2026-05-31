using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ForcePlayBattleBGM : MonoBehaviour
{
    private const string VolumeKey = "MasterVolume";

    [Header("BGM Settings")]
    [Tooltip("ONならシーン開始時に保存済みの音量設定をAudioListener.volumeへ反映します。")]
    [SerializeField] private bool applySavedMasterVolumeOnStart = true;

    [Tooltip("ONならTime.timeScale停止やAudioListener.pause中でもBGMだけは流します。音量設定は無視しません。")]
    [SerializeField] private bool ignoreListenerPause = true;

    private AudioSource bgmSource;

    private void Awake()
    {
        bgmSource = GetComponent<AudioSource>();

        // 音量設定を無視しない。これが重要。
        bgmSource.ignoreListenerVolume = false;

        // pauseだけ無視したい場合はここで管理。
        bgmSource.ignoreListenerPause = ignoreListenerPause;
    }

    private void Start()
    {
        if (applySavedMasterVolumeOnStart)
        {
            float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
            AudioListener.volume = Mathf.Clamp01(savedVolume);
        }

        // ここで AudioListener.volume = 1f; は絶対にしない。
        // SettingsManagerの音量設定を上書きしてしまうため。

        if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }
}
