using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ForcePlayBGM : MonoBehaviour
{
    private void Start()
    {
        // 1. 他のスクリプトによる「音の一時停止」を強制解除
        AudioListener.pause = false;

        // 2. SettingsManagerなどが全体の音量を0にしている可能性を上書き
        AudioListener.volume = 1f;

        AudioSource bgmSource = GetComponent<AudioSource>();

        // 3. 万が一裏で音量0の命令が出続けても、このBGMだけは「完全に無視」して鳴るようにする
        bgmSource.ignoreListenerPause = true;
        bgmSource.ignoreListenerVolume = true;

        // 4. 重複再生を防ぎつつ確実に再生
        if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }
}