using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ForcePlayBattleBGM : MonoBehaviour
{
    private void Start()
    {
        AudioListener.pause = false;
        AudioListener.volume = 1f;

        AudioSource bgmSource = GetComponent<AudioSource>();
        bgmSource.ignoreListenerPause = true;
        bgmSource.ignoreListenerVolume = true;

        if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }
}