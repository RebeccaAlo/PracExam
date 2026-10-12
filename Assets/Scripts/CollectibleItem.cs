using UnityEngine;

/// <summary>
/// Defines the type of bonus or penalty the collectible item grants upon pickup.
/// </summary>
public enum CollectibleType
{
    TimeBonus,
    TimePenalty,
    ScoreBonus,
    ScorePenalty
}

/// <summary>
/// A reusable script shared by all collectible coin prefabs.
/// Configurable via the Inspector to behave as TimeBonus, TimePenalty, ScoreBonus, or ScorePenalty.
/// </summary>
[SelectionBase]
[RequireComponent(typeof(Collider))]
public class CollectibleItem : MonoBehaviour
{
    [Header("Collectible Configuration")]
    [Tooltip("The type of collectible: TimeBonus, TimePenalty, ScoreBonus, or ScorePenalty.")]
    [SerializeField] private CollectibleType itemType = CollectibleType.ScoreBonus;

    [Tooltip("Amount of time (seconds) or score points gained or lost upon collection.")]
    [SerializeField] private int itemValue = 500;

    [Tooltip("Color tint applied to the coin's visual mesh.")]
    [SerializeField] private Color itemColor = Color.yellow;

    [Header("Visual & Effects")]
    [Tooltip("Reference to the coin's Renderer or visual GameObject.")]
    [SerializeField] private Renderer itemVisual;

    [Tooltip("Optional prefab/particle effect instantiated when the item is collected.")]
    [SerializeField] private GameObject collectedEffect;

    [Header("Optional Animation")]
    [Tooltip("If true, rotates the visual continuously.")]
    [SerializeField] private bool autoRotate = true;

    [Tooltip("Speed of visual rotation in degrees per second.")]
    [SerializeField] private float rotationSpeed = 120f;

    [Tooltip("If true, bobs the visual up and down gently.")]
    [SerializeField] private bool autoBob = true;

    [Tooltip("Bobbing frequency (cycles per second).")]
    [SerializeField] private float bobFrequency = 2.5f;

    [Tooltip("Bobbing amplitude (height in meters).")]
    [SerializeField] private float bobAmplitude = 0.08f;

    [Header("Interaction Settings")]
    [Tooltip("If true, requires pressing the interaction key (E) within range instead of automatic collection on touch.")]
    [SerializeField] private bool requireInteraction = false;

    // Internal state
    private bool isCollected = false;
    private Collider itemCollider;
    private Vector3 initialVisualLocalPos;
    private static MaterialPropertyBlock propertyBlock;

    // Public properties for external inspection or script configuration
    public CollectibleType ItemType
    {
        get => itemType;
        set { itemType = value; UpdateVisualColor(); }
    }

    public int ItemValue
    {
        get => itemValue;
        set => itemValue = value;
    }

    public Color ItemColor
    {
        get => itemColor;
        set { itemColor = value; UpdateVisualColor(); }
    }

    public Renderer ItemVisual
    {
        get => itemVisual;
        set { itemVisual = value; UpdateVisualColor(); }
    }

    public GameObject CollectedEffect
    {
        get => collectedEffect;
        set => collectedEffect = value;
    }

    public bool RequireInteraction
    {
        get => requireInteraction;
        set => requireInteraction = value;
    }

    public bool IsCollected => isCollected;

    private void Awake()
    {
        itemCollider = GetComponent<Collider>();
        if (itemCollider != null)
        {
            itemCollider.isTrigger = true;
        }

        // Ensure coin has a Kinematic Rigidbody so trigger detection against CharacterController and kinematic bodies is 100% reliable in Unity PhysX
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;

        if (itemVisual == null)
        {
            itemVisual = GetComponentInChildren<Renderer>();
        }

        if (itemVisual != null)
        {
            initialVisualLocalPos = itemVisual.transform.localPosition;
        }
    }

    private void Start()
    {
        UpdateVisualColor();
    }

    private void Update()
    {
        if (isCollected) return;

        AnimateVisual();
    }

    private void OnValidate()
    {
        // Immediately update visual color in the Scene View when editing in Inspector
        UpdateVisualColor();
    }

    /// <summary>
    /// Applies rotation and vertical bobbing to the visual GameObject.
    /// </summary>
    private void AnimateVisual()
    {
        if (itemVisual == null) return;

        if (autoRotate)
        {
            itemVisual.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        if (autoBob)
        {
            float yOffset = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            itemVisual.transform.localPosition = initialVisualLocalPos + new Vector3(0f, yOffset, 0f);
        }
    }

    /// <summary>
    /// Updates the visual renderer's color using MaterialPropertyBlock to avoid creating duplicate material assets.
    /// Supports both URP (_BaseColor) and Built-in (_Color) shaders.
    /// </summary>
    public void UpdateVisualColor()
    {
        if (itemVisual == null)
        {
            itemVisual = GetComponentInChildren<Renderer>();
        }

        if (itemVisual != null)
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            itemVisual.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", itemColor);
            propertyBlock.SetColor("_Color", itemColor);
            itemVisual.SetPropertyBlock(propertyBlock);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Guard against duplicate trigger events
        if (isCollected) return;

        // If manual interaction is required (e.g., Stage 2 E-key collection), do not auto-collect on touch
        if (requireInteraction || PlayerItemInteractor.IsManualInteractionActive)
        {
            return;
        }

        // Prevent collection if the level has already ended (Won or Lost)
        var levelMgr = GameOverManager.Instance ?? Object.FindAnyObjectByType<GameOverManager>();
        if (levelMgr != null && levelMgr.CurrentState != LevelState.Playing)
        {
            return;
        }

        if (IsPlayer(other))
        {
            isCollected = true;
            Collect();
        }
    }

    /// <summary>
    /// Attempts to collect this item via explicit player interaction (pressing E in Stage 2).
    /// Returns true if the item was eligible and successfully collected.
    /// </summary>
    public bool TryCollectByInteraction()
    {
        if (isCollected) return false;

        var levelMgr = GameOverManager.Instance ?? Object.FindAnyObjectByType<GameOverManager>();
        if (levelMgr != null && levelMgr.CurrentState != LevelState.Playing)
        {
            return false;
        }

        isCollected = true;
        Collect();
        return true;
    }

    /// <summary>
    /// Checks whether the colliding object is the player.
    /// Supports CharacterController, Rigidbody, tagged parents, and child colliders.
    /// </summary>
    private bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player")) return true;
        if (other.transform.root.CompareTag("Player")) return true;
        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player")) return true;
        if (other.GetComponentInParent<PlayerFallDetector>() != null) return true;
        if (other.GetComponentInParent<CharacterController>() != null) return true;

        Transform current = other.transform.parent;
        while (current != null)
        {
            if (current.CompareTag("Player")) return true;
            current = current.parent;
        }

        return false;
    }

    /// <summary>
    /// Executes the collection logic: disables collider, applies effect, spawns feedback, and destroys the coin.
    /// </summary>
    private void Collect()
    {
        // Disable collider immediately to prevent any re-triggering from multiple colliders
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }

        // Apply effect based on type and value
        ApplyEffect();

        // Play item collection SFX ("Collect star 5") once per successful collection
        var audioMgr = AudioManager.Instance ?? Object.FindAnyObjectByType<AudioManager>();
        if (audioMgr != null)
        {
            audioMgr.PlayCollectItemSfx();
        }

        // Spawn optional collection feedback effect
        if (collectedEffect != null)
        {
            Instantiate(collectedEffect, transform.position, Quaternion.identity);
        }

        // Destroy the collected coin object
        if (Application.isPlaying)
        {
            Destroy(gameObject);
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }

    /// <summary>
    /// Applies the effect to the game's timer or score systems.
    /// Directly updates ScoreManager and GameTimer, with fallback to GameStatsManager.
    /// </summary>
    private void ApplyEffect()
    {
        switch (itemType)
        {
            case CollectibleType.TimeBonus:
            {
                var timer = GameTimer.Instance ?? Object.FindAnyObjectByType<GameTimer>();
                if (timer != null)
                {
                    timer.AddTime(itemValue);
                }
                else
                {
                    var stats = Object.FindAnyObjectByType<GameStatsManager>();
                    if (stats != null) stats.AddTime(itemValue);
                }
                Debug.Log($"[CollectibleItem] Collected {gameObject.name} ({itemType}): Added {itemValue}s to timer.");
                break;
            }

            case CollectibleType.TimePenalty:
            {
                var timer = GameTimer.Instance ?? Object.FindAnyObjectByType<GameTimer>();
                if (timer != null)
                {
                    timer.SubtractTime(itemValue);
                }
                else
                {
                    var stats = Object.FindAnyObjectByType<GameStatsManager>();
                    if (stats != null) stats.SubtractTime(itemValue);
                }
                Debug.Log($"[CollectibleItem] Collected {gameObject.name} ({itemType}): Subtracted {itemValue}s from timer.");
                break;
            }

            case CollectibleType.ScoreBonus:
            {
                var score = ScoreManager.Instance ?? Object.FindAnyObjectByType<ScoreManager>();
                if (score != null)
                {
                    score.AddScore(itemValue);
                }
                else
                {
                    var stats = Object.FindAnyObjectByType<GameStatsManager>();
                    if (stats != null) stats.AddScore(itemValue);
                }
                Debug.Log($"[CollectibleItem] Collected {gameObject.name} ({itemType}): Added {itemValue} points to score.");
                break;
            }

            case CollectibleType.ScorePenalty:
            {
                var score = ScoreManager.Instance ?? Object.FindAnyObjectByType<ScoreManager>();
                if (score != null)
                {
                    score.SubtractScore(itemValue);
                }
                else
                {
                    var stats = Object.FindAnyObjectByType<GameStatsManager>();
                    if (stats != null) stats.SubtractScore(itemValue);
                }
                Debug.Log($"[CollectibleItem] Collected {gameObject.name} ({itemType}): Subtracted {itemValue} points from score.");
                break;
            }
        }
    }
}
