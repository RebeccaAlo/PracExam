using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Handles E-key interaction-based item collection for Stage 2 (Playground 2).
/// Detects the nearest eligible CollectibleItem within range, displays "Press E to collect",
/// and collects one item per key press when within range.
/// </summary>
public class PlayerItemInteractor : MonoBehaviour
{
    public static PlayerItemInteractor Instance { get; private set; }

    /// <summary>
    /// Returns true when an active PlayerItemInteractor is present in the scene,
    /// indicating that items require pressing E rather than automatic trigger collection.
    /// </summary>
    public static bool IsManualInteractionActive =>
        Instance != null && Instance.enabled && Instance.gameObject.activeInHierarchy;

    [Header("Interaction Configuration")]
    [Tooltip("Maximum distance (in meters) within which the player can collect an item.")]
    [SerializeField] private float interactionRange = 2.5f;

    [Tooltip("Interaction key on the keyboard.")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Interaction Prompt UI")]
    [Tooltip("Root UI container for the interaction prompt (shown when an item is in range).")]
    [SerializeField] private GameObject promptUIContainer;

    [Tooltip("TextMeshPro UI element displaying 'Press E to collect'.")]
    [SerializeField] private TMP_Text promptText;

    [Tooltip("Prompt text displayed when approaching an eligible item.")]
    [SerializeField] private string promptMessage = "Press E to collect";

    private CollectibleItem currentTarget;

    public float InteractionRange
    {
        get => interactionRange;
        set => interactionRange = Mathf.Max(0.1f, value);
    }

    public CollectibleItem CurrentTarget => currentTarget;
    public KeyCode InteractKey => interactKey;
    public GameObject PromptUIContainer => promptUIContainer;
    public TMP_Text PromptText => promptText;
    public bool IsPromptVisible =>
        promptUIContainer != null ? promptUIContainer.activeSelf : (promptText != null && promptText.gameObject.activeSelf);

    private void Awake()
    {
        Instance = this;
        SetPromptVisible(false);
    }

    private void Start()
    {
        if (promptText != null)
        {
            promptText.text = promptMessage;
        }
        SetPromptVisible(false);
    }

    private void OnEnable()
    {
        Instance = this;
    }

    private void OnDisable()
    {
        SetPromptVisible(false);
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        RefreshTargetAndPrompt();

        if (WasInteractKeyPressedThisFrame())
        {
            TryInteract();
        }
    }

    /// <summary>
    /// Scans for the nearest eligible CollectibleItem within interactionRange
    /// and updates the visibility of the "Press E to collect" UI prompt.
    /// </summary>
    public void RefreshTargetAndPrompt()
    {
        // Do not allow interaction or show prompt once the level has ended
        var levelMgr = GameOverManager.Instance ?? Object.FindAnyObjectByType<GameOverManager>();
        if (levelMgr != null && levelMgr.CurrentState != LevelState.Playing)
        {
            currentTarget = null;
            SetPromptVisible(false);
            return;
        }

        currentTarget = FindNearestEligibleItem();

        if (currentTarget != null)
        {
            if (promptText != null && promptText.text != promptMessage)
            {
                promptText.text = promptMessage;
            }
            SetPromptVisible(true);
        }
        else
        {
            SetPromptVisible(false);
        }
    }

    /// <summary>
    /// Attempts to collect the nearest eligible item within interaction range.
    /// Collects at most one item per call. Returns true if an item was collected.
    /// </summary>
    public bool TryInteract()
    {
        var levelMgr = GameOverManager.Instance ?? Object.FindAnyObjectByType<GameOverManager>();
        if (levelMgr != null && levelMgr.CurrentState != LevelState.Playing)
        {
            currentTarget = null;
            SetPromptVisible(false);
            return false;
        }

        // Always re-evaluate nearest item right before collecting
        currentTarget = FindNearestEligibleItem();
        if (currentTarget == null)
        {
            SetPromptVisible(false);
            return false;
        }

        bool collected = currentTarget.TryCollectByInteraction();
        currentTarget = null;

        // Immediately refresh target and prompt so UI updates on the same frame
        RefreshTargetAndPrompt();
        return collected;
    }

    /// <summary>
    /// Finds the closest uncollected CollectibleItem within interactionRange.
    /// </summary>
    public CollectibleItem FindNearestEligibleItem()
    {
        var allItems = Object.FindObjectsByType<CollectibleItem>();
        CollectibleItem nearest = null;
        float minDistance = interactionRange;

        foreach (var item in allItems)
        {
            if (item == null || item.IsCollected || !item.gameObject.activeInHierarchy)
            {
                continue;
            }

            float dist = GetDistanceToItem(item);
            if (dist <= minDistance)
            {
                minDistance = dist;
                nearest = item;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Calculates the distance between the player and the target item,
    /// checking both the player's torso center and feet position.
    /// </summary>
    public float GetDistanceToItem(CollectibleItem item)
    {
        if (item == null) return float.MaxValue;

        Vector3 itemPos = item.transform.position;
        Vector3 playerFeet = transform.position;
        Vector3 playerCenter = playerFeet + Vector3.up * 0.9f;

        float distCenter = Vector3.Distance(playerCenter, itemPos);
        float distFeet = Vector3.Distance(playerFeet, itemPos);
        return Mathf.Min(distCenter, distFeet);
    }

    private bool WasInteractKeyPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && interactKey != KeyCode.None)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                return true;
            }
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(interactKey))
        {
            return true;
        }
#endif
        return false;
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptUIContainer != null)
        {
            if (promptUIContainer.activeSelf != visible)
            {
                promptUIContainer.SetActive(visible);
            }
        }
        else if (promptText != null)
        {
            if (promptText.gameObject.activeSelf != visible)
            {
                promptText.gameObject.SetActive(visible);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, interactionRange);
    }
}
