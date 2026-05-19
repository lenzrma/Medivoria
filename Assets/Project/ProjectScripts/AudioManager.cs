using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Музыка")]
    public AudioClip menuMusic;
    public AudioClip gameMusic;
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    [Header("Звуки")]
    public AudioClip matchSound;
    public AudioClip wrongSound;
    public AudioClip hintSound;
    public AudioClip shuffleSound;
    public AudioClip pauseSound;
    public AudioClip winSound;
    public AudioClip loseSound;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private bool isMuted = false;
    private AudioClip _runtimeWrongClip;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (GetComponent<GameAudioFeedback>() == null)
            gameObject.AddComponent<GameAudioFeedback>();

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start() => PlayMusicForScene(SceneManager.GetActiveScene().name);
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => PlayMusicForScene(scene.name);

    void PlayMusicForScene(string sceneName)
    {
        AudioClip clip = (sceneName == "MainMenu" && menuMusic != null) ? menuMusic : gameMusic;
        if (clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.volume = isMuted ? 0f : musicVolume;
        musicSource.Play();
    }

    public void PauseMusic(bool pause)
    {
        if (musicSource == null) return;
        if (pause) musicSource.Pause();
        else musicSource.UnPause();
    }

    public void SetMuted(bool muted)
    {
        isMuted = muted;
        AudioListener.volume = muted ? 0f : 1f;
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayMatch() => PlaySfx(matchSound);

    public void PlayWrong() => PlaySfx(GetWrongClip());

    AudioClip GetWrongClip()
    {
        if (wrongSound != null) return wrongSound;
        if (_runtimeWrongClip != null) return _runtimeWrongClip;
        _runtimeWrongClip = CreateWrongMatchClip();
        return _runtimeWrongClip;
    }

    /// <summary>Короткий «бзз», если в инспекторе не назначен wrongSound.</summary>
    static AudioClip CreateWrongMatchClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.18f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] data = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float env = 1f - t / duration;
            float tone = Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.55f
                       + Mathf.Sin(2f * Mathf.PI * 248f * t) * 0.25f;
            data[i] = tone * env * env;
        }

        var clip = AudioClip.Create("WrongMatchProcedural", sampleCount, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    public void PlayHint()    => PlaySfx(hintSound);
    public void PlayShuffle() => PlaySfx(shuffleSound);
    public void PlayPause()   => PlaySfx(pauseSound);
    public void PlayWin()     => PlaySfx(winSound);
    public void PlayLose()    => PlaySfx(loseSound);
}