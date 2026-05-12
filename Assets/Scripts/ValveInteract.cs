using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class ValveInteract : MonoBehaviour
{
    [SerializeField] private Room5Controller room5Controller;
    [SerializeField] private bool isMainValve = true;

    private bool playerInside;
    private bool used;

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
        if (!playerInside || used)
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
        if (room5Controller == null)
            return;

        used = true;

        if (isMainValve)
            room5Controller.ActivateMainValve();
        else
            room5Controller.ActivateSecondValve();
    }
}
