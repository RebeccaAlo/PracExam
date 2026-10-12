using UnityEngine;

/// <summary>
/// Categorizes designated environmental obstacles on the map.
/// </summary>
public enum ObstacleCategory
{
    Door,
    Flower,
    Tree,
    Fence,
    Wall,
    EnvironmentObstacle
}

/// <summary>
/// Marks a GameObject (such as a Door, Flower, Tree, Fence, or Wall) as a designated ground obstacle.
/// Plays "Debuff Downgrade 10" via AudioManager when the player collides with or enters the obstacle,
/// with built-in cooldown protection to prevent per-frame repeated sound playback during continuous contact.
/// </summary>
[DisallowMultipleComponent]
public class GroundObstacle : MonoBehaviour
{
    [Header("Obstacle Settings")]
    [Tooltip("Category of environmental obstacle (Door, Flower, Tree, Fence, Wall, EnvironmentObstacle).")]
    [SerializeField] private ObstacleCategory obstacleCategory = ObstacleCategory.EnvironmentObstacle;

    [Tooltip("Minimum cooldown (in seconds) before this obstacle can trigger the collision SFX again.")]
    [SerializeField] private float collisionCooldown = 0.65f;

    private float lastTriggeredTime = -999f;

    public ObstacleCategory Category
    {
        get => obstacleCategory;
        set => obstacleCategory = value;
    }

    public float CollisionCooldown
    {
        get => collisionCooldown;
        set => collisionCooldown = Mathf.Max(0.05f, value);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null) return;
        if (IsPlayerCollider(collision.collider))
        {
            TryTriggerObstacleSfx();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (IsPlayerCollider(other))
        {
            TryTriggerObstacleSfx();
        }
    }

    /// <summary>
    /// Attempts to trigger the obstacle collision sound effect ("Debuff Downgrade 10").
    /// Enforces a cooldown across the obstacle hierarchy so continuous contact does not spam audio.
    /// Returns true if the SFX was played.
    /// </summary>
    public bool TryTriggerObstacleSfx()
    {
        // Do not play obstacle sounds after the round has ended
        var levelMgr = GameOverManager.Instance ?? Object.FindAnyObjectByType<GameOverManager>();
        if (levelMgr != null && levelMgr.CurrentState != LevelState.Playing)
        {
            return false;
        }

        // Resolve the root-most GroundObstacle in this obstacle's hierarchy (e.g., Tree_FrontLeft or FrontDoor)
        // so hitting multiple sub-colliders of the same obstacle shares one cooldown.
        GroundObstacle groupObstacle = GetRootObstacleGroup();

        float now = Application.isPlaying ? Time.unscaledTime : Time.realtimeSinceStartup;
        if (groupObstacle.lastTriggeredTime >= 0f && (now - groupObstacle.lastTriggeredTime) < groupObstacle.collisionCooldown)
        {
            return false;
        }

        groupObstacle.lastTriggeredTime = now;
        lastTriggeredTime = now;

        var audioMgr = AudioManager.Instance ?? Object.FindAnyObjectByType<AudioManager>();
        if (audioMgr != null)
        {
            audioMgr.PlayObstacleCollisionSfx();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Resets the cooldown timer so the obstacle can immediately trigger again.
    /// </summary>
    public void ResetCooldown()
    {
        GroundObstacle groupObstacle = GetRootObstacleGroup();
        groupObstacle.lastTriggeredTime = -999f;
        lastTriggeredTime = -999f;
    }

    private GroundObstacle GetRootObstacleGroup()
    {
        GroundObstacle rootGroup = this;
        Transform current = transform.parent;
        while (current != null)
        {
            var parentObs = current.GetComponent<GroundObstacle>();
            if (parentObs != null)
            {
                rootGroup = parentObs;
            }
            current = current.parent;
        }
        return rootGroup;
    }

    /// <summary>
    /// Checks whether the colliding collider belongs to the Player.
    /// </summary>
    public static bool IsPlayerCollider(Collider other)
    {
        if (other == null) return false;

        // Ignore CollectibleItems so coins never count as player or obstacle
        if (other.GetComponent<CollectibleItem>() != null || other.GetComponentInParent<CollectibleItem>() != null)
        {
            return false;
        }

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
}
