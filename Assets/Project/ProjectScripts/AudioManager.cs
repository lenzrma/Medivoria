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

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

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

    public void PlayMatch()   => PlaySfx(matchSound);
    public void PlayWrong()   => PlaySfx(wrongSound);
    public void PlayHint()    => PlaySfx(hintSound);
    public void PlayShuffle() => PlaySfx(shuffleSound);
    public void PlayPause()   => PlaySfx(pauseSound);
    public void PlayWin()     => PlaySfx(winSound);
    public void PlayLose()    => PlaySfx(loseSound);
}