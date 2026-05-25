using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSE : MonoBehaviour
{
    [Header("ñ¬ÇÁÇµÇΩÇ¢å¯â âπ")]
    public AudioClip clickSound;

    [Header("âπó ê›íË")]
    [Range(0f, 1f)]
    public float volume = 1.0f;

    [Header("ÉtÉçÉÄÉQÅ[ïó í≤êÆ")]
    public bool isHeavyDarkFantasyStyle = true;

    private static AudioSource _sharedAudioSource;

    void Start()
    {
        if (_sharedAudioSource == null)
        {
            GameObject sePlayer = new GameObject("UI_SE_Player");
            _sharedAudioSource = sePlayer.AddComponent<AudioSource>();

            _sharedAudioSource.ignoreListenerPause = true;
            _sharedAudioSource.ignoreListenerVolume = true;

            DontDestroyOnLoad(sePlayer);
        }

        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(PlaySound);
    }

    void PlaySound()
    {
        if (clickSound != null && _sharedAudioSource != null)
        {
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
}