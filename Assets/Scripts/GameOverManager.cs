using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Controls the Game Over screen, input disabling, and seamless player restart.
/// Handles Rigidbody physics resets, position/rotation restoration, interpolation safety,
/// and synchronizes with GameTimer and ScoreManager without reloading the entire scene.
/// </summary>
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The root UI GameObject/Panel of the Game Over screen.")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("The UI Button that restarts gameplay when clicked.")]
    [SerializeField] private Button restartButton;

    [Header("Player Target")]
    [Tooltip("The player GameObject. If unassigned, will automatically find object tagged 'Player'.")]
    [SerializeField] private GameObject player;

    [Tooltip("The player's Rigidbody. If unassigned, will be retrieved from the player object.")]
    [SerializeField] private Rigidbody playerRigidbody;

    [Tooltip("The PlayerFallDetector on the player.")]
    [SerializeField] private PlayerFallDetector fallDetector;

    [Header("Movement & Input Handling")]
    [Tooltip("Specific movement or input components to disable during Game Over.")]
    [SerializeField] private Behaviour[] movementComponentsToDisable;

    [Tooltip("Automatically detect and disable movement/input components on the player during Game Over.")]
    [SerializeField] private bool autoDetectMovementComponents = true;

    [Header("Cursor Settings")]
    [Tooltip("If true, unlocks cursor during Game Over and re-locks upon restart.")]
    [SerializeField] private bool manageCursor = true;

    [Tooltip("If true, locks cursor back to center when game restarts.")]
    [SerializeField] private bool lockCursorOnRestart = true;

    [Header("Spawn Position")]
    [Tooltip("Initial spawn position.")]
    [SerializeField] private Vector3 initialPosition = Vector3.zero;

    [Tooltip("Initial spawn rotation.")]
    [SerializeField] private Quaternion initialRotation = Quaternion.identity;

    // Cached Rigidbody settings
    private RigidbodyInterpolation originalInterpolation = RigidbodyInterpolation.None;
    private RigidbodyConstraints originalConstraints = RigidbodyConstraints.None;
    private bool originalIsKinematic = false;

    // State
    private bool isGameOver = false;
    private System.Collections.Generic.List<Behaviour> detectedMovementComponents = new System.Collections.Generic.List<Behaviour>();

    public bool IsGameOver => isGameOver;
    public Vector3 InitialPosition => initialPosition;
    public Quaternion InitialRotation => initialRotation;

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

    private void Start()
    {
        // Re-confirm initial position on Start after all objects are positioned
        if (player != null && initialPosition == Vector3.zero && player.transform.position != Vector3.zero)
        {
            initialPosition = player.transform.position;
            initialRotation = player.transform.rotation;
        }
    }

    /// <summary>
    /// Finds and links player components if unassigned in Inspector.
    /// </summary>
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

    /// <summary>
    /// Detects movement and input components on the player to safely toggle on Game Over.
    /// </summary>
    private void CacheMovementComponents()
    {
        if (!autoDetectMovementComponents || player == null) return;

        detectedMovementComponents.Clear();

        var allBehaviours = player.GetComponentsInChildren<Behaviour>(true);
        foreach (var comp in allBehaviours)
        {
            if (comp == null || comp == this || comp == fallDetector) continue;

            string typeName = comp.GetType().Name.ToLower();

            // Match common movement and input component types
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

    /// <summary>
    /// Stores the player's initial position, rotation, and Rigidbody configuration.
    /// </summary>
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

    /// <summary>
    /// Initializes UI panel and hooks up the restart button.
    /// </summary>
    private void SetupUI()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
            restartButton.onClick.AddListener(RestartGame);
        }
    }

    /// <summary>
    /// Triggers the Game Over sequence: displays UI, freezes player physics, stops timer, and disables input.
    /// </summary>
    public void TriggerGameOver()
    {
        // Prevent duplicate Game Over executions
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("[GameOverManager] Game Over triggered!");

        // 1. Stop countdown timer immediately
        var timer = GameTimer.Instance ?? Object.FindAnyObjectByType<GameTimer>();
        if (timer != null)
        {
            timer.StopTimer();
        }

        // 2. Show Game Over UI
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // 3. Unlock mouse cursor so the player can interact with the Restart button
        if (manageCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 4. Freeze Rigidbody to prevent infinite downward fall while screen is open
        if (playerRigidbody != null)
        {
            ZeroRigidbodyVelocity(playerRigidbody);
            playerRigidbody.isKinematic = true;
        }

        // 5. Disable player movement and inputs
        SetPlayerMovementEnabled(false);
    }

    /// <summary>
    /// Restarts the player and resets the game state without reloading the scene.
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("[GameOverManager] Restarting game...");

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

        // 7. Reset and restart timer
        var timer = GameTimer.Instance ?? Object.FindAnyObjectByType<GameTimer>();
        if (timer != null)
        {
            timer.ResetTimer();
        }

        // 8. Reset score
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

        // 10. Hide Game Over screen and reset Game Over flag
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        isGameOver = false;
    }

    /// <summary>
    /// Safely resets Rigidbody physics: zeroes velocity, restores constraints,
    /// and handles interpolation to eliminate visual teleportation glitches.
    /// </summary>
    private void ResetRigidbodyPhysics()
    {
        if (playerRigidbody == null) return;

        // Temporarily turn off interpolation so physics doesn't interpolate across the teleport distance
        playerRigidbody.interpolation = RigidbodyInterpolation.None;

        // Restore original kinematic state and constraints
        playerRigidbody.isKinematic = originalIsKinematic;
        playerRigidbody.constraints = originalConstraints;

        // Reset position and rotation via Rigidbody API
        Quaternion targetRot = GetValidRotation(initialRotation);
        playerRigidbody.position = initialPosition;
        playerRigidbody.rotation = targetRot;

        // Set velocity and angular velocity to zero
        ZeroRigidbodyVelocity(playerRigidbody);

        // Safely restore interpolation on next fixed update to prevent any visual stretching
        if (originalInterpolation != RigidbodyInterpolation.None)
        {
            StartCoroutine(RestoreInterpolationRoutine(playerRigidbody, originalInterpolation));
        }
    }

    /// <summary>
    /// Restores transform position and rotation, handling CharacterController if present.
    /// </summary>
    private void ResetPlayerTransform()
    {
        if (player == null) return;

        // Temporarily disable CharacterController during teleport to prevent position conflicts
        bool ccWasEnabled = false;
        if (player.TryGetComponent<CharacterController>(out var cc))
        {
            ccWasEnabled = cc.enabled;
            cc.enabled = false;
        }

        Quaternion targetRot = GetValidRotation(initialRotation);
        player.transform.position = initialPosition;
        player.transform.rotation = targetRot;

        // Force immediate synchronization of physics transforms
        Physics.SyncTransforms();

        if (cc != null && ccWasEnabled)
        {
            cc.enabled = true;
        }

        // Confirm Rigidbody velocities remain zero after transform sync
        if (playerRigidbody != null)
        {
            ZeroRigidbodyVelocity(playerRigidbody);
        }
    }

    /// <summary>
    /// Waits until the next physics step before reenabling interpolation to guarantee no visual glitch.
    /// </summary>
    private IEnumerator RestoreInterpolationRoutine(Rigidbody rb, RigidbodyInterpolation targetInterpolation)
    {
        yield return new WaitForFixedUpdate();
        if (rb != null)
        {
            rb.interpolation = targetInterpolation;
        }
    }

    /// <summary>
    /// Zeroes both linear velocity and angular velocity on a Rigidbody across Unity versions.
    /// </summary>
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

    /// <summary>
    /// Validates that the quaternion is non-zero and properly normalized before setting physics rotation.
    /// </summary>
    private Quaternion GetValidRotation(Quaternion rot)
    {
        float sqr = rot.x * rot.x + rot.y * rot.y + rot.z * rot.z + rot.w * rot.w;
        if (sqr < 0.0001f) return Quaternion.identity;
        return rot.normalized;
    }

    /// <summary>
    /// Enables or disables movement and input components.
    /// </summary>
    private void SetPlayerMovementEnabled(bool enable)
    {
        // 1. User-assigned components in Inspector
        if (movementComponentsToDisable != null)
        {
            foreach (var comp in movementComponentsToDisable)
            {
                if (comp != null)
                {
                    comp.enabled = enable;
                }
            }
        }

        // 2. Auto-detected movement components
        foreach (var comp in detectedMovementComponents)
        {
            if (comp != null)
            {
                comp.enabled = enable;
            }
        }

        // 3. Built-in CharacterController toggle
        if (player != null && player.TryGetComponent<CharacterController>(out var cc))
        {
            cc.enabled = enable;
        }

#if ENABLE_INPUT_SYSTEM
        // 4. Unity Input System PlayerInput toggle
        if (player != null && player.TryGetComponent<PlayerInput>(out var playerInput))
        {
            playerInput.enabled = enable;
        }
#endif
    }

    /// <summary>
    /// Clears any accumulated velocity, jump cooldown, or input buffers.
    /// </summary>
    private void ClearAccumulatedMovementState()
    {
        if (player == null) return;

        // Reset StarterAssets inputs (move, jump, sprint) if present
        var inputsComp = player.GetComponent("StarterAssetsInputs");
        if (inputsComp != null)
        {
            SetReflectedField(inputsComp, "move", Vector2.zero);
            SetReflectedField(inputsComp, "jump", false);
            SetReflectedField(inputsComp, "sprint", false);
        }

        // Reset controller internal velocity fields if present
        var controllerComp = player.GetComponent("ThirdPersonController");
        if (controllerComp != null)
        {
            SetReflectedField(controllerComp, "_verticalVelocity", 0f);
            SetReflectedField(controllerComp, "_speed", 0f);
            SetReflectedField(controllerComp, "_fallTimeoutDelta", 0.15f);
            SetReflectedField(controllerComp, "_jumpTimeoutDelta", 0.5f);
        }
    }

    /// <summary>
    /// Helper to safely set private/public fields via reflection without hard assembly references.
    /// </summary>
    private void SetReflectedField(object target, string fieldName, object value)
    {
        if (target == null) return;
        var field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}
