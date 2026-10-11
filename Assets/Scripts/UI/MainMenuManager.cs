using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Handles navigation and interactions for the Main Menu scene.
    /// Manages loading the Stage Selection scene, toggling the Options panel,
    /// and safely quitting the game.
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
