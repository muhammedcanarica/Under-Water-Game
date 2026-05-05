using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Door : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider2D blockingCollider;
    [SerializeField] private Animator animator;

    [Header("Animation")]
    [SerializeField] private string openParameterName = "IsOpen";

    [Header("State")]
    [SerializeField] private bool startOpen;

    public bool IsOpen { get; private set; }

    private void Reset()
    {
        blockingCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (blockingCollider == null)
            blockingCollider = GetComponent<Collider2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        SetOpenState(startOpen);
    }

    public void Open()
    {
        SetOpenState(true);
    }

    public void Close()
    {
        SetOpenState(false);
    }

    private void SetOpenState(bool open)
    {
        IsOpen = open;

        if (blockingCollider != null)
            blockingCollider.enabled = !open;

        if (animator != null && !string.IsNullOrWhiteSpace(openParameterName))
            animator.SetBool(openParameterName, open);
    }
}
