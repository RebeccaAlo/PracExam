using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Handles navigation, Options panel toggling, and persistent Audio Settings (Background Music & SFX sliders)
    /// for the Main Menu scene.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Scene Navigation")]
        [Tooltip("Name of the Stage Selection scene to load.")]
        [SerializeField] private string stageSelectionSceneName = "StageSelection";

        [Header("UI Panels")]
        [Tooltip("Root GameObject for the Options popup panel.")]
        [SerializeField] private GameObject optionsPanel;

        [Header("Menu Buttons (Optional Auto-Wiring)")]
        [Tooltip("Button to transition to Stage Selection.")]
        [SerializeField] private Button startButton;

        [Tooltip("Button to open the Options panel.")]
        [SerializeField] private Button optionButton;

        [Tooltip("Button to quit the game.")]
        [SerializeField] private Button exitButton;

        [Tooltip("Back button inside the Options panel to return to Main Menu.")]
        [SerializeField] private Button optionsBackButton;

        [Header("Audio Settings UI")]
        [Tooltip("Slider controlling Background Music volume (0.0 to 1.0).")]
        [SerializeField] private Slider bgmVolumeSlider;

        [Tooltip("Slider controlling Sound Effects (SFX) volume (0.0 to 1.0).")]
        [SerializeField] private Slider sfxVolumeSlider;

        [Tooltip("Text displaying the current Background Music volume percentage.")]
        [SerializeField] private TMP_Text bgmVolumeValueText;

        [Tooltip("Text displaying the current SFX volume percentage.")]
        [SerializeField] private TMP_Text sfxVolumeValueText;

        public GameObject OptionsPanel => optionsPanel;
        public Slider BgmVolumeSlider => bgmVolumeSlider;
        public Slider SfxVolumeSlider => sfxVolumeSlider;
        public TMP_Text BgmVolumeValueText => bgmVolumeValueText;
        public TMP_Text SfxVolumeValueText => sfxVolumeValueText;

        private void Awake()
        {
            // Ensure cursor is visible and unlocked when entering menu scenes
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Ensure Time.timeScale is normal in case previous gameplay paused it
            if (Time.timeScale != 1f)
            {
                Time.timeScale = 1f;
            }
        }

        private void Start()
        {
            // Initialize audio sliders from saved settings before hiding options panel
            InitializeAudioSliders();

            // By default, ensure options panel starts hidden
            if (optionsPanel != null)
            {
                optionsPanel.SetActive(false);
            }

            // Wire buttons if references are assigned
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartClicked);
                startButton.onClick.AddListener(OnStartClicked);
            }

            if (optionButton != null)
            {
                optionButton.onClick.RemoveListener(OnOptionClicked);
                optionButton.onClick.AddListener(OnOptionClicked);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(OnExitClicked);
                exitButton.onClick.AddListener(OnExitClicked);
            }

            if (optionsBackButton != null)
            {
                optionsBackButton.onClick.RemoveListener(OnCloseOptionsClicked);
                optionsBackButton.onClick.AddListener(OnCloseOptionsClicked);
            }
        }

        /// <summary>
        /// Initializes the Background Music and SFX sliders with saved values and binds their change listeners.
        /// </summary>
        public void InitializeAudioSliders()
        {
            var audioMgr = AudioManager.Instance ?? Object.FindAnyObjectByType<AudioManager>();
            if (audioMgr != null)
            {
                audioMgr.LoadVolumeSettings();
            }

            float savedBgm = audioMgr != null
                ? audioMgr.BgmVolume
                : Mathf.Clamp01(PlayerPrefs.GetFloat(AudioManager.BGM_VOLUME_KEY, AudioManager.DEFAULT_BGM_VOLUME));

            float savedSfx = audioMgr != null
                ? audioMgr.SfxVolume
                : Mathf.Clamp01(PlayerPrefs.GetFloat(AudioManager.SFX_VOLUME_KEY, AudioManager.DEFAULT_SFX_VOLUME));

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.minValue = 0f;
                bgmVolumeSlider.maxValue = 1f;
                bgmVolumeSlider.wholeNumbers = false;
                bgmVolumeSlider.SetValueWithoutNotify(savedBgm);
                bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
                bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.minValue = 0f;
                sfxVolumeSlider.maxValue = 1f;
                sfxVolumeSlider.wholeNumbers = false;
                sfxVolumeSlider.SetValueWithoutNotify(savedSfx);
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }

            UpdateVolumeLabels(savedBgm, savedSfx);
        }

        /// <summary>
        /// Called immediately when the Background Music volume slider moves.
        /// </summary>
        public void OnBgmVolumeChanged(float value)
        {
            float normalized = value > 1.0001f ? Mathf.Clamp01(value / 100f) : Mathf.Clamp01(value);

            var audioMgr = AudioManager.Instance ?? Object.FindAnyObjectByType<AudioManager>();
            if (audioMgr != null)
            {
                audioMgr.SetBgmVolume(normalized);
            }
            else
            {
                PlayerPrefs.SetFloat(AudioManager.BGM_VOLUME_KEY, normalized);
                PlayerPrefs.Save();
            }

            if (bgmVolumeValueText != null)
            {
                bgmVolumeValueText.text = $"{Mathf.RoundToInt(normalized * 100f)}%";
            }
        }

        /// <summary>
        /// Called immediately when the SFX volume slider moves.
        /// </summary>
        public void OnSfxVolumeChanged(float value)
        {
            float normalized = value > 1.0001f ? Mathf.Clamp01(value / 100f) : Mathf.Clamp01(value);

            var audioMgr = AudioManager.Instance ?? Object.FindAnyObjectByType<AudioManager>();
            if (audioMgr != null)
            {
                audioMgr.SetSfxVolume(normalized);
            }
            else
            {
                PlayerPrefs.SetFloat(AudioManager.SFX_VOLUME_KEY, normalized);
                PlayerPrefs.Save();
            }

            if (sfxVolumeValueText != null)
            {
                sfxVolumeValueText.text = $"{Mathf.RoundToInt(normalized * 100f)}%";
            }
        }

        private void UpdateVolumeLabels(float bgmVol, float sfxVol)
        {
            if (bgmVolumeValueText != null)
            {
                bgmVolumeValueText.text = $"{Mathf.RoundToInt(Mathf.Clamp01(bgmVol) * 100f)}%";
            }

            if (sfxVolumeValueText != null)
            {
                sfxVolumeValueText.text = $"{Mathf.RoundToInt(Mathf.Clamp01(sfxVol) * 100f)}%";
            }
        }

        /// <summary>
        /// Transitions to the Stage Selection scene.
        /// </summary>
        public void OnStartClicked()
        {
            Debug.Log($"[MainMenuManager] Loading scene: '{stageSelectionSceneName}'");
            SceneManager.LoadScene(stageSelectionSceneName);
        }

        /// <summary>
        /// Opens the Options panel without leaving the Main Menu scene.
        /// </summary>
        public void OnOptionClicked()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            InitializeAudioSliders();

            if (optionsPanel != null)
            {
                optionsPanel.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[MainMenuManager] Options panel is not assigned!");
            }
        }

        /// <summary>
        /// Closes the Options panel and returns to the Main Menu.
        /// </summary>
        public void OnCloseOptionsClicked()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (optionsPanel != null)
            {
                optionsPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Quits the standalone game, or prints a debug message in the Unity Editor.
        /// </summary>
        public void OnExitClicked()
        {
#if UNITY_EDITOR
            Debug.Log("[MainMenuManager] Exit button clicked: Application.Quit() called (simulated in Editor).");
#else
            Application.Quit();
#endif
        }
    }
}
