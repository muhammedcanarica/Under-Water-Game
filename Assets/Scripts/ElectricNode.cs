using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ElectricNode : MonoBehaviour
{
    [Header("Door Links")]
    [SerializeField] private Door[] connectedDoors;

    [Header("Activation")]
    [SerializeField] private bool stayOpen = true;
    [SerializeField] private float autoCloseDelay = 2f;

    [Header("References")]
    [SerializeField] private Collider2D triggerCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Animation")]
    [SerializeField] private string activeParameterName = "IsActive";

    [Header("Hit Flash")]
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color hitColor = Color.yellow;
    [SerializeField] private int flashCount = 3;
    [SerializeField] private float flashInterval = 0.08f;

    public bool IsActive { get; private set; }

    private Coroutine autoCloseRoutine;
    private Coroutine flashRoutine;

    private void Reset()
    {
        triggerCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        UpdateVisualState();
    }

    public void ReactToElectricHit()
    {
        PlayHitFlash();
        Activate();
    }

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdateVisualState();
        OpenConnectedDoors();

        if (!stayOpen && autoCloseDelay > 0f)
        {
            if (autoCloseRoutine != null)
                StopCoroutine(autoCloseRoutine);

            autoCloseRoutine = StartCoroutine(AutoCloseRoutine());
        }
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        if (autoCloseRoutine != null)
        {
            StopCoroutine(autoCloseRoutine);
            autoCloseRoutine = null;
        }

        IsActive = false;
        UpdateVisualState();
        CloseConnectedDoors();
    }

    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        autoCloseRoutine = null;
        Deactivate();
    }

    private void OpenConnectedDoors()
    {
        for (int i = 0; i < connectedDoors.Length; i++)
        {
            Door door = connectedDoors[i];
            if (door != null)
                door.Open();
        }
    }

    private void CloseConnectedDoors()
    {
        for (int i = 0; i < connectedDoors.Length; i++)
        {
            Door door = connectedDoors[i];
            if (door != null)
                door.Close();
        }
    }

    private void UpdateVisualState()
    {
        if (animator != null && !string.IsNullOrWhiteSpace(activeParameterName))
            animator.SetBool(activeParameterName, IsActive);

        if (spriteRenderer != null && flashRoutine == null)
            spriteRenderer.color = idleColor;
    }

    private void PlayHitFlash()
    {
        if (spriteRenderer == null)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = hitColor;
            yield return new WaitForSeconds(flashInterval);
            spriteRenderer.color = idleColor;
            yield return new WaitForSeconds(flashInterval);
        }

        flashRoutine = null;
        UpdateVisualState();
    }
}
