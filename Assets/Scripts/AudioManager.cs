using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Centralized, persistent audio manager handling scene-specific Background Music (BGM),
/// gameplay Sound Effects (SFX), and persistent volume settings via PlayerPrefs.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public const string BGM_VOLUME_KEY = "BGMVolume";
    public const string SFX_VOLUME_KEY = "SFXVolume";

    public const float DEFAULT_BGM_VOLUME = 0.8f;
    public const float DEFAULT_SFX_VOLUME = 1.0f;

    [Header("Audio Sources (Channels)")]
    [Tooltip("Dedicated AudioSource for looping Background Music.")]
    [SerializeField] private AudioSource bgmSource;

    [Tooltip("Dedicated AudioSource for one-shot Sound Effects.")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Background Music Clips")]
    [Tooltip("Background music for MainMenu (and StageSelection): Chunky_Monkey.")]
    [SerializeField] private AudioClip mainMenuMusic;

    [Tooltip("Background music for Stage 1 (Playground): JumpyGame.")]
    [SerializeField] private AudioClip stage1Music;

    [Tooltip("Background music for Stage 2 (Playground 2): Tiny_Blocks.")]
    [SerializeField] private AudioClip stage2Music;

    [Header("Sound Effect (SFX) Clips")]
    [Tooltip("SFX played when an item/coin is collected: Collect star 5.")]
    [SerializeField] private AudioClip collectItemSfx;

    [Tooltip("SFX played when colliding with a designated obstacle: Debuff Downgrade 10.")]
    [SerializeField] private AudioClip obstacleCollisionSfx;

    [Tooltip("SFX played when transitioning to Game Over state: Game Over 2.")]
    [SerializeField] private AudioClip gameOverSfx;

    [Tooltip("SFX played when transitioning to Win state: Win sound 5.")]
    [SerializeField] private AudioClip winSfx;

    [Header("Volume Settings (0.0 to 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = DEFAULT_BGM_VOLUME;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = DEFAULT_SFX_VOLUME;

    // Diagnostic & verification properties
    public AudioSource BgmSource => bgmSource;
    public AudioSource SfxSource => sfxSource;
    public AudioClip MainMenuMusic => mainMenuMusic;
    public AudioClip Stage1Music => stage1Music;
    public AudioClip Stage2Music => stage2Music;
    public AudioClip CollectItemSfx => collectItemSfx;
    public AudioClip ObstacleCollisionSfx => obstacleCollisionSfx;
    public AudioClip GameOverSfx => gameOverSfx;
    public AudioClip WinSfx => winSfx;

    public float BgmVolume => bgmVolume;
    public float SfxVolume => sfxVolume;

    public AudioClip LastPlayedSfxClip { get; private set; }
    public int TotalSfxPlayedCount { get; private set; }
    public int CollectSfxPlayedCount { get; private set; }
    public int ObstacleSfxPlayedCount { get; private set; }
    public int GameOverSfxPlayedCount { get; private set; }
    public int WinSfxPlayedCount { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Transfer any clip references that might be missing on the existing instance
            Instance.AdoptMissingReferencesFrom(this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (Application.isPlaying && transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }

        EnsureAudioSources();
        LoadVolumeSettings();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureAudioSources();
        ApplyVolumesToSources();
        PlayMusicForScene(scene.name);
    }

    private void AdoptMissingReferencesFrom(AudioManager other)
    {
        if (other == null) return;
        if (mainMenuMusic == null) mainMenuMusic = other.mainMenuMusic;
        if (stage1Music == null) stage1Music = other.stage1Music;
        if (stage2Music == null) stage2Music = other.stage2Music;
        if (collectItemSfx == null) collectItemSfx = other.collectItemSfx;
        if (obstacleCollisionSfx == null) obstacleCollisionSfx = other.obstacleCollisionSfx;
        if (gameOverSfx == null) gameOverSfx = other.gameOverSfx;
        if (winSfx == null) winSfx = other.winSfx;
    }

    /// <summary>
    /// Ensures separate BGM and SFX AudioSource components exist and are configured properly.
    /// </summary>
    public void EnsureAudioSources()
    {
        if (bgmSource == null || sfxSource == null)
        {
            var sources = GetComponents<AudioSource>();
            if (bgmSource == null && sources.Length >= 1)
            {
                bgmSource = sources[0];
            }
            if (sfxSource == null && sources.Length >= 2)
            {
                sfxSource = sources[1];
            }
        }

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxSource == null || sfxSource == bgmSource)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    /// <summary>
    /// Loads saved BGM and SFX volume levels from PlayerPrefs, or uses defaults if not yet saved.
    /// </summary>
    public void LoadVolumeSettings()
    {
        bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BGM_VOLUME_KEY, DEFAULT_BGM_VOLUME));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SFX_VOLUME_KEY, DEFAULT_SFX_VOLUME));
        ApplyVolumesToSources();
    }

    /// <summary>
    /// Sets and persists the Background Music volume immediately.
    /// Accepts either normalized [0..1] or percentage [0..100] values.
    /// </summary>
    public void SetBgmVolume(float volume)
    {
        if (volume > 1.0001f)
        {
            volume /= 100f;
        }

        bgmVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmVolume);
        PlayerPrefs.Save();
        ApplyVolumesToSources();
    }

    /// <summary>
    /// Sets and persists the Sound Effects (SFX) volume immediately.
    /// Accepts either normalized [0..1] or percentage [0..100] values.
    /// </summary>
    public void SetSfxVolume(float volume)
    {
        if (volume > 1.0001f)
        {
            volume /= 100f;
        }

        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
        PlayerPrefs.Save();
        ApplyVolumesToSources();
    }

    private void ApplyVolumesToSources()
    {
        EnsureAudioSources();

        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
            bgmSource.mute = bgmVolume <= 0.0001f;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
            sfxSource.mute = sfxVolume <= 0.0001f;
        }
    }

    /// <summary>
    /// Selects and plays the designated Background Music for the specified scene name.
    /// Avoids restarting the same music if it is already playing.
    /// </summary>
    public void PlayMusicForScene(string sceneName)
    {
        EnsureAudioSources();
        ApplyVolumesToSources();

        AudioClip targetClip = GetMusicClipForScene(sceneName);
        if (targetClip == null)
        {
            Debug.LogWarning($"[AudioManager] No background music clip assigned for scene '{sceneName}'.");
            return;
        }

        // Do not restart the same background music unnecessarily if already playing
        if (bgmSource.clip == targetClip && bgmSource.isPlaying)
        {
            bgmSource.loop = true;
            return;
        }

        bgmSource.Stop();
        bgmSource.clip = targetClip;
        bgmSource.loop = true;
        bgmSource.Play();

        Debug.Log($"[AudioManager] Playing BGM '{targetClip.name}' for scene '{sceneName}' (Volume: {Mathf.RoundToInt(bgmVolume * 100f)}%).");
    }

    /// <summary>
    /// Returns the AudioClip designated for the given scene name.
    /// </summary>
    public AudioClip GetMusicClipForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return mainMenuMusic;

        switch (sceneName)
        {
            case "MainMenu":
            case "StageSelection":
                return mainMenuMusic;
            case "Playground":
                return stage1Music;
            case "Playground 2":
                return stage2Music;
            default:
                if (sceneName.Contains("Playground 2")) return stage2Music;
                if (sceneName.Contains("Playground")) return stage1Music;
                return mainMenuMusic;
        }
    }

    /// <summary>
    /// Plays the item collection SFX ("Collect star 5") once through the SFX channel.
    /// </summary>
    public void PlayCollectItemSfx()
    {
        CollectSfxPlayedCount++;
        PlaySfx(collectItemSfx);
    }

    /// <summary>
    /// Plays the obstacle collision SFX ("Debuff Downgrade 10") through the SFX channel.
    /// </summary>
    public void PlayObstacleCollisionSfx()
    {
        ObstacleSfxPlayedCount++;
        PlaySfx(obstacleCollisionSfx);
    }

    /// <summary>
    /// Plays the Game Over SFX ("Game Over 2") once through the SFX channel.
    /// </summary>
    public void PlayGameOverSfx()
    {
        GameOverSfxPlayedCount++;
        PlaySfx(gameOverSfx);
    }

    /// <summary>
    /// Plays the Win SFX ("Win sound 5") once through the SFX channel.
    /// </summary>
    public void PlayWinSfx()
    {
        WinSfxPlayedCount++;
        PlaySfx(winSfx);
    }

    /// <summary>
    /// Plays an arbitrary AudioClip one-shot through the SFX channel respecting SFX volume and mute state.
    /// </summary>
    public void PlaySfx(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] Attempted to play a null SFX clip.");
            return;
        }

        EnsureAudioSources();
        ApplyVolumesToSources();

        LastPlayedSfxClip = clip;
        TotalSfxPlayedCount++;

        if (!sfxSource.mute && sfxVolume > 0.0001f)
        {
            sfxSource.PlayOneShot(clip, 1f);
        }

        Debug.Log($"[AudioManager] Played SFX '{clip.name}' (SFX Volume: {Mathf.RoundToInt(sfxVolume * 100f)}%).");
    }

    /// <summary>
    /// Resets diagnostic play counters (used for automated testing).
    /// </summary>
    public void ResetDiagnostics()
    {
        LastPlayedSfxClip = null;
        TotalSfxPlayedCount = 0;
        CollectSfxPlayedCount = 0;
        ObstacleSfxPlayedCount = 0;
        GameOverSfxPlayedCount = 0;
        WinSfxPlayedCount = 0;
    }
}
