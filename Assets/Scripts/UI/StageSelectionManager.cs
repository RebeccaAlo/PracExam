using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Handles navigation for the Stage Selection scene.
    /// Provides methods to load Stage 1 (Playground), Stage 2 (Playground 2),
    /// and return to the Main Menu.
    /// </summary>
    public class StageSelectionManager : MonoBehaviour
    {
        [Header("Scene Navigation Names")]
        [Tooltip("Exact scene name for Stage 1 (Capsule Player).")]
        [SerializeField] private string stage1SceneName = "Playground";

        [Tooltip("Exact scene name for Stage 2 (Humanoid Player).")]
        [SerializeField] private string stage2SceneName = "Playground 2";

        [Tooltip("Exact scene name for Main Menu.")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Stage Selection Buttons (Optional Auto-Wiring)")]
        [Tooltip("Button to launch Stage 1 (Capsule Player).")]
        [SerializeField] private Button stage1Button;

        [Tooltip("Button to launch Stage 2 (Humanoid Player).")]
        [SerializeField] private Button stage2Button;

        [Tooltip("Button to return to the Main Menu.")]
        [SerializeField] private Button backButton;

        private void Awake()
        {
            // Ensure cursor is visible and unlocked when entering menu scenes
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Ensure Time.timeScale is normal
            if (Time.timeScale != 1f)
            {
                Time.timeScale = 1f;
            }
        }

        private void Start()
        {
            // Wire buttons if references are assigned
            if (stage1Button != null)
            {
                stage1Button.onClick.RemoveListener(LoadStage1);
                stage1Button.onClick.AddListener(LoadStage1);
            }

            if (stage2Button != null)
            {
                stage2Button.onClick.RemoveListener(LoadStage2);
                stage2Button.onClick.AddListener(LoadStage2);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(LoadMainMenu);
                backButton.onClick.AddListener(LoadMainMenu);
            }
        }

        /// <summary>
        /// Loads Stage 1 (Capsule Player) - Playground.unity.
        /// </summary>
        public void LoadStage1()
        {
            Debug.Log($"[StageSelectionManager] Loading Stage 1 scene: '{stage1SceneName}'");
            SceneManager.LoadScene(stage1SceneName);
        }

        /// <summary>
        /// Loads Stage 2 (Humanoid Player) - Playground 2.unity.
        /// </summary>
        public void LoadStage2()
        {
            Debug.Log($"[StageSelectionManager] Loading Stage 2 scene: '{stage2SceneName}'");
            SceneManager.LoadScene(stage2SceneName);
        }

        /// <summary>
        /// Returns to the Main Menu scene - MainMenu.unity.
        /// </summary>
        public void LoadMainMenu()
        {
            Debug.Log($"[StageSelectionManager] Returning to Main Menu: '{mainMenuSceneName}'");
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
