using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BubbleLiftZone : MonoBehaviour
{
    [SerializeField] private bool isActive = true;
    [SerializeField] private float liftForce = 6f;

    private Collider2D triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;
    }

    private void Reset()
    {
        triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isActive || !other.CompareTag("Player"))
            return;

        Rigidbody2D playerBody = other.attachedRigidbody;
        if (playerBody == null)
            return;

        Vector2 velocity = playerBody.linearVelocity;
        velocity.y = liftForce;
        playerBody.linearVelocity = velocity;
    }

    public void SetActive(bool active)
    {
        isActive = active;
    }
}
