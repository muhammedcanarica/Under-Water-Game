using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpikePlatform : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Damage dealt when the player touches the spikes.")]
    public int damage = 1;

    [Tooltip("Repeat damage interval in seconds. 0 means only the first touch deals damage.")]
    [Range(0f, 3f)]
    public float damageInterval = 0.5f;

    private float nextDamageTime;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (damageInterval <= 0f)
            return;

        if (Time.time >= nextDamageTime)
            TryDamagePlayer(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (damageInterval <= 0f)
            return;

        if (Time.time >= nextDamageTime)
            TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (other == null || !other.CompareTag("Player"))
            return;

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health == null && other.attachedRigidbody != null)
            health = other.attachedRigidbody.GetComponent<PlayerHealth>();
        if (health == null)
            health = other.GetComponentInParent<PlayerHealth>();
        if (health == null)
            return;

        health.TakeDamage(damage, transform);
        nextDamageTime = Time.time + damageInterval;
    }

    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
            return;

        Bounds bounds = col.bounds;
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.2f);
        Gizmos.DrawCube(bounds.center, bounds.size);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.7f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
