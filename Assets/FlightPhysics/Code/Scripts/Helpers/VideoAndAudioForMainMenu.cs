using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(VideoPlayer))]
public class VideoAndAudioForMainMenu : MonoBehaviour
{
    private bool _isFinished = false;
    private AudioSource _audio;
    private VideoPlayer _player;

    private UnityEvent OnVideoFinished = new UnityEvent();

    private void Awake()
    {
        _player = GetComponent<VideoPlayer>();
        _audio = GetComponent<AudioSource>();
        _player.waitForFirstFrame = true;
        OnVideoFinished.AddListener(
            delegate
            {
                VideoFinished();
            }
            );
    }

    private void Update()
    {
        if (_player.isPlaying)
        {
            if (_player.frame > 5)
            {
                _isFinished = false;
                if ((long)_player.frame > (long)_player.frameCount - 5 && !_isFinished)
                {
                    OnVideoFinished.Invoke();

                    _isFinished = true;
                }
            }

        }
    }

    private void VideoFinished()
    {
        _player.Stop(); _audio.Stop();
        _player.waitForFirstFrame = false;
        _player.playbackSpeed = 1;
        _player.Play(); _audio.PlayOneShot(_audio.clip);
        _isFinished = false;

    }
}
