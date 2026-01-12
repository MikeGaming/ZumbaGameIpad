using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class CustomVideoPlayer : MonoBehaviour
{
    VideoPlayer player;
    AudioSource audioSource;
    [SerializeField] float startTimeSeconds = 0f;

    bool seekCompleted;

    private void Awake()
    {
        player = GetComponentInChildren<VideoPlayer>();
        audioSource = GetComponentInChildren<AudioSource>();
        audioSource.playOnAwake = false;
        player.playOnAwake = false;
    }

    private void Start()
    {
        StartCoroutine(PlayAtTime(startTimeSeconds));
    }

    IEnumerator PlayAtTime(float startAtSeconds)
    {
        if (player == null)
        {
            Debug.LogWarning("VideoPlayer component not found.");
            yield break;
        }

        if (player.isPlaying)
        {
            player.Stop();
        }
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        player.Prepare();
        Debug.Log("Preparing video...");
        yield return new WaitUntil(() => player.isPrepared);
        Debug.Log("Video prepared.");

        // Subscribe to seekCompleted, set the time, then wait for the seek to finish
        seekCompleted = false;
        player.seekCompleted += OnSeekCompleted;

        Debug.Log($"Seeking to {startAtSeconds} seconds...");
        player.time = startAtSeconds;

        // Wait for the engine to finish the seek operation before playing
        yield return new WaitUntil(() => seekCompleted);

        player.seekCompleted -= OnSeekCompleted;

        Debug.Log("Seek completed. Playing video and audio.");
        player.Play();
        audioSource.Play();
    }

    void OnSeekCompleted(VideoPlayer source)
    {
        seekCompleted = true;
    }
}
