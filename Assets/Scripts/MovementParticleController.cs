using UnityEngine;

public class MovementParticleController : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Player Rigidbody2D")]
    public Rigidbody2D playerRb;

    [Tooltip("Kontrol edilecek ParticleSystem")]
    public ParticleSystem movementParticles;

    [Tooltip("PlayerController referansi")]
    public PlayerController playerController;

    [Header("Ayarlar")]
    [Tooltip("Bu hizin altinda particle uretilmez")]
    public float minSpeedThreshold = 0.3f;

    private ParticleSystem.EmissionModule emissionModule;
    private bool isEmitting;

    private void Awake()
    {
        if (movementParticles == null)
            movementParticles = GetComponent<ParticleSystem>();

        if (playerRb == null)
            playerRb = GetComponentInParent<Rigidbody2D>();

        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();

        if (movementParticles != null)
        {
            emissionModule = movementParticles.emission;
            emissionModule.enabled = false;
            isEmitting = false;
        }
    }

    private void Update()
    {
        if (playerRb == null || movementParticles == null || playerController == null)
            return;

        float speed = playerRb.linearVelocity.magnitude;
        bool isInWater = playerController.currentMode == PlayerMode.Water;
        bool shouldEmit = isInWater && speed > minSpeedThreshold;

        if (shouldEmit && !isEmitting)
        {
            emissionModule.enabled = true;
            isEmitting = true;
        }
        else if (!shouldEmit && isEmitting)
        {
            emissionModule.enabled = false;
            isEmitting = false;
        }
    }
}
