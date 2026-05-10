using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class ElectricSwitch : MonoBehaviour
{
    [Header("Current Target")]
    public CurrentZone targetCurrent;
    public Vector2 activatedDirection = Vector2.right;
    public bool oneTimeUse = false;
    public bool toggleDirectionOnEachUse = true;

    [Header("Visual Feedback")]
    public GameObject inactiveVisual;
    public GameObject activeVisual;
    public ParticleSystem activateParticles;
    public AudioSource activateSound;

    private bool isActivated;
    private Collider2D triggerCollider;

    private void Reset()
    {
        triggerCollider = GetComponent<Collider2D>();
        EnsureTriggerCollider();
        UpdateVisualState();
    }

    private void Awake()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider2D>();

        EnsureTriggerCollider();
        UpdateVisualState();
    }

    private void OnValidate()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider2D>();

        EnsureTriggerCollider();
        UpdateVisualState();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (HasTag(other, "ElectricAttack"))
            ActivateSwitch();
    }

    public void ActivateSwitch()
    {
        if (oneTimeUse && isActivated)
            return;

        isActivated = true;

        if (targetCurrent != null)
        {
            if (toggleDirectionOnEachUse)
                targetCurrent.ReverseDirection();
            else
                targetCurrent.SetDirection(activatedDirection);
        }
        else
        {
            Debug.LogWarning("ElectricSwitch activated, but targetCurrent is not assigned.", this);
        }

        UpdateVisualState();
        PlayFeedback();
        Debug.Log("ElectricSwitch activated, current direction changed.", this);
    }

    private void UpdateVisualState()
    {
        if (inactiveVisual != null)
            inactiveVisual.SetActive(!isActivated);

        if (activeVisual != null)
            activeVisual.SetActive(isActivated);
    }

    private void PlayFeedback()
    {
        if (activateParticles != null)
            activateParticles.Play();

        if (activateSound != null)
            activateSound.Play();
    }

    private void EnsureTriggerCollider()
    {
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private static bool HasTag(Collider2D other, string tagName)
    {
        if (other == null || string.IsNullOrWhiteSpace(tagName))
            return false;

        try
        {
            return other.CompareTag(tagName);
        }
        catch (UnityException)
        {
            return false;
        }
    }
}
