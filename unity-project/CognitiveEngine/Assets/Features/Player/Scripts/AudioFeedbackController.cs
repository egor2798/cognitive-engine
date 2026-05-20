using UnityEngine;

public class AudioFeedbackController : MonoBehaviour
{
    [Header("Enable")]
    [SerializeField] private bool soundEnabled = true;

    [Header("Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Clips")]
    [SerializeField] private AudioClip sessionStartedClip;
    [SerializeField] private AudioClip pacerStartedClip;
    [SerializeField] private AudioClip corridorExitClip;
    [SerializeField] private AudioClip sessionCompletedClip;
    [SerializeField] private AudioClip metronomeTickClip;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.75f;

    [Range(0f, 1f)]
    [SerializeField] private float metronomeVolume = 0.5f;

    [Header("Metronome")]
    [SerializeField] private bool metronomeEnabled = false;
    [SerializeField] private float metronomeBpm = 60f;

    [Header("Anti-spam")]
    [SerializeField] private float corridorExitCooldownSec = 0.7f;

    private float lastCorridorExitSoundTime = -999f;
    private bool metronomeRunning;
    private float metronomeTimer;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
    }

    private void Update()
    {
        UpdateMetronome();
    }

    public void ApplySettings(ExerciseSettingsData settings)
    {
        if (settings == null)
            return;

        soundEnabled = settings.audioEnabled;
        volume = Mathf.Clamp01(settings.audioVolume01);

        metronomeEnabled = settings.metronomeEnabled;
        metronomeBpm = Mathf.Clamp(settings.metronomeBpm, 20f, 240f);
        metronomeVolume = Mathf.Clamp01(settings.metronomeVolume01);

        Debug.Log($"Audio settings applied: enabled={soundEnabled}, volume={volume}, metronome={metronomeEnabled}, bpm={metronomeBpm}, metronomeVolume={metronomeVolume}");
    }

    public void BeginSession()
    {
        metronomeTimer = 0f;
        metronomeRunning = metronomeEnabled && soundEnabled;
    }

    public void EndSession()
    {
        metronomeRunning = false;
        metronomeTimer = 0f;
    }

    public void SetSoundEnabled(bool enabled)
    {
        soundEnabled = enabled;
    }

    public void PlaySessionStarted()
    {
        PlayOneShot(sessionStartedClip, volume);
    }

    public void PlayPacerStarted()
    {
        PlayOneShot(pacerStartedClip, volume);
    }

    public void PlayCorridorExit()
    {
        if (Time.time - lastCorridorExitSoundTime < corridorExitCooldownSec)
            return;

        lastCorridorExitSoundTime = Time.time;
        PlayOneShot(corridorExitClip, volume);
    }

    public void PlaySessionCompleted()
    {
        PlayOneShot(sessionCompletedClip, volume);
    }

    private void UpdateMetronome()
    {
        if (!metronomeRunning || !soundEnabled)
            return;

        float interval = 60f / Mathf.Max(20f, metronomeBpm);

        metronomeTimer += Time.deltaTime;

        while (metronomeTimer >= interval)
        {
            metronomeTimer -= interval;
            PlayOneShot(metronomeTickClip, metronomeVolume);
        }
    }

    private void PlayOneShot(AudioClip clip, float clipVolume)
    {
        if (!soundEnabled)
            return;

        if (audioSource == null)
            return;

        if (clip == null)
        {
#if UNITY_EDITOR
            Debug.Log("[AudioFeedbackController] Clip not assigned.");
#endif
            return;
        }

        audioSource.PlayOneShot(clip, Mathf.Clamp01(clipVolume));
    }
}
