using UnityEngine;

/// <summary>
/// Attached to the Player GameObject (PlayerArmature) in Stage 1 and Stage 2.
/// Bridges CharacterController (OnControllerColliderHit), Rigidbody (OnCollisionEnter),
/// and Trigger (OnTriggerEnter) contacts with designated GroundObstacle objects.
/// Ignores ordinary floor/ground contact, coins, and self-collisions.
/// </summary>
[DisallowMultipleComponent]
public class PlayerObstacleCollisionHandler : MonoBehaviour
{
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit == null || hit.collider == null) return;
        HandleColliderContact(hit.collider);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null) return;
        HandleColliderContact(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        HandleColliderContact(other);
    }

    /// <summary>
    /// Evaluates whether the contacted collider is a designated GroundObstacle
    /// and triggers the obstacle collision SFX if eligible.
    /// </summary>
    public bool HandleColliderContact(Collider col)
    {
        if (col == null) return false;

        // Never trigger obstacle SFX for the player itself or its children
        if (col.transform == transform || col.transform.IsChildOf(transform))
        {
            return false;
        }

        // Never trigger obstacle SFX for collectible coins
        if (col.GetComponent<CollectibleItem>() != null || col.GetComponentInParent<CollectibleItem>() != null)
        {
            return false;
        }

        // Only trigger if the object (or its obstacle parent) is explicitly designated with GroundObstacle
        GroundObstacle obstacle = col.GetComponent<GroundObstacle>() ?? col.GetComponentInParent<GroundObstacle>();
        if (obstacle == null)
        {
            return false;
        }

        return obstacle.TryTriggerObstacleSfx();
    }
}
