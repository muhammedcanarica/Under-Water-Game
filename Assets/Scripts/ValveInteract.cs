using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(Collider2D))]
public class ValveInteract : MonoBehaviour
{
    [FormerlySerializedAs("room5Controller")]
    [FormerlySerializedAs("room4Controller")]
    [SerializeField] private RoomActivationController roomActivationController;

    private bool playerInside;

    private void Awake()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;
    }

    private void Reset()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;
    }

    private void Update()
    {
        if (!playerInside)
            return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            UseValve();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInside = false;
    }

    private void UseValve()
    {
        if (roomActivationController == null)
        {
            Debug.LogWarning($"[{nameof(ValveInteract)}] RoomActivationController is not assigned on '{name}'.", this);
            return;
        }

        roomActivationController.ActivateRoom();
    }
}
