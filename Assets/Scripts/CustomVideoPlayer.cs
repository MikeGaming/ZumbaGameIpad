using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class CustomVideoPlayer : MonoBehaviour
{
    VideoPlayer[] players = new VideoPlayer[2];
    VideoPlayer player;
    VideoPlayer player2;
    AudioSource audioSource;
    [SerializeField] float startTimeSeconds = 0f;
    [SerializeField] float startTimeSeconds2 = 0f;
    bool multiplayer;

    bool seekCompleted;
    public bool songStarted = false;

    private void Awake()
    {
        players = GetComponentsInChildren<VideoPlayer>();
        if (players.Length > 1)
        {
            player = players[0];
            player2 = players[1];
            multiplayer = true;
        }
        else if (players.Length == 1)
        {
            player = players[0];
            multiplayer = false;
        }
        audioSource = GetComponentInChildren<AudioSource>();
        audioSource.playOnAwake = false;
        player.playOnAwake = false;

    }

    private void Start()
    {
        if(multiplayer)
        {
            StartCoroutine(PlayTwoAtTime(startTimeSeconds, startTimeSeconds2));
        }
        else
        {
            StartCoroutine(PlayOneAtTime(startTimeSeconds));
        }
    }

    IEnumerator PlayTwoAtTime(float startAtSeconds1, float startAtSeconds2)
    {
        if (player == null || player2 == null)
        {
            Debug.LogWarning("One or both VideoPlayer components not found.");
            yield break;
        }
        if (player.isPlaying)
        {
            player.Stop();
        }
        if (player2.isPlaying)
        {
            player2.Stop();
        }
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        player.Prepare();
        player2.Prepare();
        Debug.Log("Preparing videos...");
        yield return new WaitUntil(() => player.isPrepared && player2.isPrepared);
        Debug.Log("Videos prepared.");
        // Seek first video
        seekCompleted = false;
        player.seekCompleted += OnSeekCompleted;
        Debug.Log($"Seeking first video to {startAtSeconds1} seconds...");
        player.time = startAtSeconds1;
        yield return new WaitUntil(() => seekCompleted);
        player.seekCompleted -= OnSeekCompleted;
        // Seek second video
        seekCompleted = false;
        player2.seekCompleted += OnSeekCompleted;
        Debug.Log($"Seeking second video to {startAtSeconds2} seconds...");
        player2.time = startAtSeconds2;
        yield return new WaitUntil(() => seekCompleted);
        player2.seekCompleted -= OnSeekCompleted;
        Debug.Log("Both seeks completed. Playing videos and audio.");
        player.Play();
        player2.Play();
        audioSource.Play();
        songStarted = true;
    }

    IEnumerator PlayOneAtTime(float startAtSeconds)
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
        songStarted = true;
    }

    void OnSeekCompleted(VideoPlayer source)
    {
        seekCompleted = true;
    }
}
