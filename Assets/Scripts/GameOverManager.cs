using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Possible states for the level lifecycle.
/// </summary>
public enum LevelState
{
    Playing,
    Won,
    Lost
}

/// <summary>
/// Controls level state (Playing, Won, Lost), result screens (WIN & GAME OVER),
/// input toggling, player teleportation/restart, and Rigidbody physics restoration.
/// Acts as the single source of truth for level ending conditions.
/// </summary>
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("Level State & Rules")]
    [Tooltip("Current lifecycle state of the level.")]
    [SerializeField] private LevelState currentState = LevelState.Playing;

    [Tooltip("Target score required to trigger victory.")]
    [SerializeField] private int targetWinScore = 10000;

    [Header("UI Result Panels")]
    [Tooltip("Root GameObject of the Game Over panel.")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("Root GameObject of the Win panel.")]
    [SerializeField] private GameObject winPanel;

    [Tooltip("Final score text displayed on the Game Over panel.")]
    [SerializeField] private TMP_Text gameOverScoreText;

    [Tooltip("Final score text displayed on the Win panel.")]
    [SerializeField] private TMP_Text winScoreText;

    [Tooltip("Restart button on the Game Over panel.")]
    [SerializeField] private Button gameOverRestartButton;

    [Tooltip("Restart button on the Win panel.")]
    [SerializeField] private Button winRestartButton;

    [Header("Player Target")]
    [Tooltip("The player GameObject. If unassigned, automatically finds object tagged 'Player'.")]
    [SerializeField] private GameObject player;

    [Tooltip("The player's Rigidbody. If unassigned, automatically retrieved from the player object.")]
    [SerializeField] private Rigidbody playerRigidbody;

    [Tooltip("The PlayerFallDetector on the player.")]
    [SerializeField] private PlayerFallDetector fallDetector;

    [Header("Movement & Input Handling")]
    [Tooltip("Specific movement or input components to disable when the level ends.")]
    [SerializeField] private Behaviour[] movementComponentsToDisable;

    [Tooltip("Automatically detect and disable movement/input components on the player when level ends.")]
    [SerializeField] private bool autoDetectMovementComponents = true;

    [Header("Cursor Settings")]
    [Tooltip("If true, unlocks cursor when level ends and re-locks upon restart.")]
    [SerializeField] private bool manageCursor = true;

    [Tooltip("If true, locks cursor back to center when game restarts.")]
    [SerializeField] private bool lockCursorOnRestart = true;

    [Header("Spawn Position")]
    [Tooltip("Initial spawn position.")]
    [SerializeField] private Vector3 initialPosition = Vector3.zero;

    [Tooltip("Initial spawn rotation.")]
    [SerializeField] private Quaternion initialRotation = Quaternion.identity;

    private RigidbodyInterpolation originalInterpolation = RigidbodyInterpolation.None;
    private RigidbodyConstraints originalConstraints = RigidbodyConstraints.None;
    private bool originalIsKinematic = false;

    private System.Collections.Generic.List<Behaviour> detectedMovementComponents = new System.Collections.Generic.List<Behaviour>();

    public LevelState CurrentState => currentState;
    public int TargetWinScore => targetWinScore;
    public bool IsGameOver => currentState != LevelState.Playing;
    public Vector3 InitialPosition => initialPosition;
    public Quaternion InitialRotation => initialRotation;
    public GameObject GameOverPanel => gameOverPanel;
    public GameObject WinPanel => winPanel;
    public TMP_Text GameOverScoreText => gameOverScoreText;
    public TMP_Text WinScoreText => winScoreText;
    public Button GameOverRestartButton => gameOverRestartButton;
    public Button WinRestartButton => winRestartButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializePlayerReferences();
        CacheInitialState();
        SetupUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Loads the Main Menu scene safely, restoring cursor and timeScale.
    /// </summary>
    public void ReturnToMainMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Loads the Stage Selection scene safely, restoring cursor and timeScale.
    /// </summary>
    public void ReturnToStageSelection()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("StageSelection");
    }

    private void Start()
    {
        if (player != null && initialPosition == Vector3.zero && player.transform.position != Vector3.zero)
        {
            initialPosition = player.transform.position;
            initialRotation = player.transform.rotation;
        }
    }

    private void InitializePlayerReferences()
    {
        if (player == null)
        {
            if (fallDetector != null)
            {
                player = fallDetector.gameObject;
            }
            else
            {
                player = GameObject.FindGameObjectWithTag("Player");
            }
        }

        if (player != null)
        {
            if (playerRigidbody == null)
            {
                playerRigidbody = player.GetComponent<Rigidbody>();
            }

            if (fallDetector == null)
            {
                fallDetector = player.GetComponent<PlayerFallDetector>();
            }

            CacheMovementComponents();
        }
    }

    private void CacheMovementComponents()
    {
        if (!autoDetectMovementComponents || player == null) return;

        detectedMovementComponents.Clear();

        var allBehaviours = player.GetComponentsInChildren<Behaviour>(true);
        foreach (var comp in allBehaviours)
        {
            if (comp == null || comp == this || comp == fallDetector) continue;

            string typeName = comp.GetType().Name.ToLower();

            if (typeName.Contains("movement") ||
                typeName.Contains("controller") ||
                typeName.Contains("motor") ||
                typeName.Contains("input") ||
                typeName.Contains("locomotion"))
            {
                if (!detectedMovementComponents.Contains(comp))
                {
                    detectedMovementComponents.Add(comp);
                }
            }
        }
    }

    private void CacheInitialState()
    {
        if (player != null)
        {
            initialPosition = player.transform.position;
            initialRotation = player.transform.rotation;
        }

        if (playerRigidbody != null)
        {
            originalInterpolation = playerRigidbody.interpolation;
            originalConstraints = playerRigidbody.constraints;
            originalIsKinematic = playerRigidbody.isKinematic;
        }
    }

    private void SetupUI()
    {
        // Hide both result panels at start
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);

        // Hook up restart buttons
        if (gameOverRestartButton != null)
        {
            gameOverRestartButton.onClick.RemoveListener(RestartGame);
            gameOverRestartButton.onClick.AddListener(RestartGame);
        }

        if (winRestartButton != null)
        {
            winRestartButton.onClick.RemoveListener(RestartGame);
            winRestartButton.onClick.AddListener(RestartGame);
        }
    }

    /// <summary>
    /// Evaluates outcome when the countdown reaches zero.
    /// Wins if score >= 10,000; loses if score < 10,000.
    /// </summary>
    public void OnTimerExpired()
    {
        if (currentState != LevelState.Playing) return;

        int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        if (finalScore >= targetWinScore)
        {
            TriggerWin();
        }
        else
        {
            TriggerGameOver();
        }
    }

    /// <summary>
    /// Triggers the Win state: displays WIN screen, updates final score, halts timer, and disables input.
    /// Can only be activated once while in the Playing state.
    /// </summary>
    public void TriggerWin()
    {
        if (currentState != LevelState.Playing) return;
        currentState = LevelState.Won;

        Debug.Log("[GameOverManager] WIN state triggered!");

        // 1. Stop timer
        var timer = GameTimer.Instance ?? Object.FindAnyObjectByType<GameTimer>();
        if (timer != null)
        {
            timer.StopTimer();
        }

        // 2. Hide Game Over panel, show WIN panel
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(true);

        // 3. Display final score
        int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        if (winScoreText != null)
        {
            winScoreText.text = $"Final Score: {finalScore}";
        }

        // 4. Unlock cursor for UI
        if (manageCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 5. Freeze Rigidbody
        if (playerRigidbody != null)
        {
            ZeroRigidbodyVelocity(playerRigidbody);
            playerRigidbody.isKinematic = true;
        }

        // 6. Disable movement and input
        SetPlayerMovementEnabled(false);
    }

    /// <summary>
    /// Triggers the Loss state: displays GAME OVER screen, halts timer, freezes player, and disables input.
    /// Can only be activated once while in the Playing state.
    /// </summary>
    public void TriggerGameOver()
    {
        if (currentState != LevelState.Playing) return;
        currentState = LevelState.Lost;

        Debug.Log("[GameOverManager] GAME OVER state triggered!");

        // 1. Stop timer
        var timer = GameTimer.Instance ?? Object.FindAnyObjectByType<GameTimer>();
        if (timer != null)
        {
            timer.StopTimer();
        }

        // 2. Hide WIN panel, show Game Over panel
        if (winPanel != null) winPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        // 3. Display final score if supported
        int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        if (gameOverScoreText != null)
        {
            gameOverScoreText.text = $"Final Score: {finalScore}";
        }

        // 4. Unlock cursor for UI
        if (manageCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 5. Freeze Rigidbody
        if (playerRigidbody != null)
        {
            ZeroRigidbodyVelocity(playerRigidbody);
            playerRigidbody.isKinematic = true;
        }

        // 6. Disable movement and input
        SetPlayerMovementEnabled(false);
    }

    /// <summary>
    /// Restarts the level: resets physics, teleports player, resets timer and score, and returns to Playing state.
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("[GameOverManager] Restarting level...");

        // 1. Reset Rigidbody physics before restoring movement
        ResetRigidbodyPhysics();

        // 2. Teleport player back to initial position and rotation
        ResetPlayerTransform();

        // 3. Clear accumulated momentum or jump timers
        ClearAccumulatedMovementState();

        // 4. Re-enable player movement and input
        SetPlayerMovementEnabled(true);

        // 5. Restore cursor locking if configured
        if (manageCursor && lockCursorOnRestart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // 6. Reset fall detector state with fresh grace period
        if (fallDetector != null)
        {
            fallDetector.ResetFallDetector();
        }

        // 7. Reset and restart timer (30s)
        var timer = GameTimer.Instance ?? Object.FindAnyObjectByType<GameTimer>();
        if (timer != null)
        {
            timer.ResetTimer();
        }

        // 8. Reset score to initial
        var score = ScoreManager.Instance ?? Object.FindAnyObjectByType<ScoreManager>();
        if (score != null)
        {
            score.ResetScore();
        }

        // 9. Reset fallback bridge stats
        var statsManager = Object.FindAnyObjectByType<GameStatsManager>();
        if (statsManager != null)
        {
            statsManager.ResetStats();
        }

        // 10. Hide both result panels
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);

        // 11. Restore Playing state
        currentState = LevelState.Playing;
    }

    private void ResetRigidbodyPhysics()
    {
        if (playerRigidbody == null) return;

        playerRigidbody.interpolation = RigidbodyInterpolation.None;
        playerRigidbody.isKinematic = originalIsKinematic;
        playerRigidbody.constraints = originalConstraints;

        Quaternion targetRot = GetValidRotation(initialRotation);
        playerRigidbody.position = initialPosition;
        playerRigidbody.rotation = targetRot;

        ZeroRigidbodyVelocity(playerRigidbody);

        if (originalInterpolation != RigidbodyInterpolation.None)
        {
            StartCoroutine(RestoreInterpolationRoutine(playerRigidbody, originalInterpolation));
        }
    }

    private void ResetPlayerTransform()
    {
        if (player == null) return;

        bool ccWasEnabled = false;
        if (player.TryGetComponent<CharacterController>(out var cc))
        {
            ccWasEnabled = cc.enabled;
            cc.enabled = false;
        }

        Quaternion targetRot = GetValidRotation(initialRotation);
        player.transform.position = initialPosition;
        player.transform.rotation = targetRot;

        Physics.SyncTransforms();

        if (cc != null && ccWasEnabled)
        {
            cc.enabled = true;
        }

        if (playerRigidbody != null)
        {
            ZeroRigidbodyVelocity(playerRigidbody);
        }
    }

    private IEnumerator RestoreInterpolationRoutine(Rigidbody rb, RigidbodyInterpolation targetInterpolation)
    {
        yield return new WaitForFixedUpdate();
        if (rb != null)
        {
            rb.interpolation = targetInterpolation;
        }
    }

    private void ZeroRigidbodyVelocity(Rigidbody rb)
    {
        if (rb == null || rb.isKinematic) return;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif
        rb.angularVelocity = Vector3.zero;
    }

    private Quaternion GetValidRotation(Quaternion rot)
    {
        float sqr = rot.x * rot.x + rot.y * rot.y + rot.z * rot.z + rot.w * rot.w;
        if (sqr < 0.0001f) return Quaternion.identity;
        return rot.normalized;
    }

    private void SetPlayerMovementEnabled(bool enable)
    {
        if (movementComponentsToDisable != null)
        {
            foreach (var comp in movementComponentsToDisable)
            {
                if (comp != null) comp.enabled = enable;
            }
        }

        foreach (var comp in detectedMovementComponents)
        {
            if (comp != null) comp.enabled = enable;
        }

        if (player != null && player.TryGetComponent<CharacterController>(out var cc))
        {
            cc.enabled = enable;
        }

#if ENABLE_INPUT_SYSTEM
        if (player != null && player.TryGetComponent<PlayerInput>(out var playerInput))
        {
            playerInput.enabled = enable;
        }
#endif
    }

    private void ClearAccumulatedMovementState()
    {
        if (player == null) return;

        var inputsComp = player.GetComponent("StarterAssetsInputs");
        if (inputsComp != null)
        {
            SetReflectedField(inputsComp, "move", Vector2.zero);
            SetReflectedField(inputsComp, "jump", false);
            SetReflectedField(inputsComp, "sprint", false);
        }

        var controllerComp = player.GetComponent("ThirdPersonController");
        if (controllerComp != null)
        {
            SetReflectedField(controllerComp, "_verticalVelocity", 0f);
            SetReflectedField(controllerComp, "_speed", 0f);
            SetReflectedField(controllerComp, "_fallTimeoutDelta", 0.15f);
            SetReflectedField(controllerComp, "_jumpTimeoutDelta", 0.5f);
        }
    }

    private void SetReflectedField(object target, string fieldName, object value)
    {
        if (target == null) return;
        var field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null) field.SetValue(target, value);
    }
}

/// <summary>
/// Alias for GameOverManager to support LevelManager references cleanly.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static GameOverManager Instance => GameOverManager.Instance;
}
