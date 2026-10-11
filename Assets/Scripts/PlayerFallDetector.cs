using UnityEngine;

/// <summary>
/// Monitors the player's world-space Y position and triggers the Game Over state
/// when falling below the configurable threshold.
/// Prevents false triggers through a restart grace period and guarantees Game Over is triggered strictly once.
/// </summary>
public class PlayerFallDetector : MonoBehaviour
{
    [Header("Fall Detection Settings")]
    [Tooltip("The world Y position threshold below which Game Over is triggered.")]
    [SerializeField] private float fallThreshold = -10f;

    [Tooltip("Grace period in seconds after spawning/restarting during which fall detection is ignored to prevent false triggers during physics initialization.")]
    [SerializeField] private float restartGracePeriod = 0.5f;

    [Header("Manager Reference")]
    [Tooltip("Reference to the GameOverManager. If unassigned, it will automatically find one in the scene.")]
    [SerializeField] private GameOverManager gameOverManager;

    [Header("Initial State Storage")]
    [Tooltip("Initial spawn position recorded on Start.")]
    [SerializeField] private Vector3 initialPosition;

    [Tooltip("Initial spawn rotation recorded on Start.")]
    [SerializeField] private Quaternion initialRotation;

    private bool isGameOverTriggered = false;
    private float gracePeriodTimer = 0f;

    public float FallThreshold
    {
        get => fallThreshold;
        set => fallThreshold = value;
    }

    public bool IsGameOverTriggered => isGameOverTriggered;
    public Vector3 InitialPosition => initialPosition;
    public Quaternion InitialRotation => initialRotation;

    private void Awake()
    {
        // Store player's initial position and rotation
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // Auto-find GameOverManager if not linked in Inspector
        if (gameOverManager == null)
        {
            gameOverManager = Object.FindAnyObjectByType<GameOverManager>();
        }

        // Apply initial grace period
        gracePeriodTimer = restartGracePeriod;
    }

    private void Start()
    {
        // Confirm initial position was not overwritten with zero by serialization
        if (initialPosition == Vector3.zero && transform.position != Vector3.zero)
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }
    }

    private void Update()
    {
        if (gracePeriodTimer > 0f)
        {
            gracePeriodTimer -= Time.deltaTime;
            return;
        }

        CheckFallCondition();
    }

    private void FixedUpdate()
    {
        if (gracePeriodTimer > 0f) return;

        // Also check during physics ticks for high-speed Rigidbody falls
        CheckFallCondition();
    }

    /// <summary>
    /// Checks if the player's Y position has fallen below the threshold.
    /// </summary>
    private void CheckFallCondition()
    {
        // Never trigger if already triggered or if GameOverManager is already in Game Over state
        if (isGameOverTriggered) return;
        if (gameOverManager != null && gameOverManager.IsGameOver) return;

        // Measure strictly in world space Y
        if (transform.position.y < fallThreshold)
        {
            isGameOverTriggered = true;
            OnPlayerFell();
        }
    }

    /// <summary>
    /// Invokes Game Over on the manager.
    /// </summary>
    private void OnPlayerFell()
    {
        if (gameOverManager != null)
        {
            gameOverManager.TriggerGameOver();
        }
        else
        {
            Debug.LogWarning($"[PlayerFallDetector] Player fell below {fallThreshold}, but GameOverManager was not found!");
        }
    }

    /// <summary>
    /// Resets the fall detector state and restarts the grace period so falling can be detected again safely after restart.
    /// </summary>
    public void ResetFallDetector()
    {
        isGameOverTriggered = false;
        gracePeriodTimer = restartGracePeriod;
    }
}
