using UnityEngine;

/// <summary>
/// Hareket sırasında Particle System'ı kontrol eder.
/// Player hareket ediyorsa emit açılır, durunca kapanır.
/// PlayerController'ın child'ına eklenmiş Particle System'da kullanılır.
/// </summary>
public class MovementParticleController : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Player'ın Rigidbody2D'si (otomatik bulunur)")]
    public Rigidbody2D playerRb;

    [Tooltip("Kontrol edilecek ParticleSystem (otomatik bulunur)")]
    public ParticleSystem movementParticles;

    [Header("Ayarlar")]
    [Tooltip("Bu hızın altında particle üretilmez")]
    public float minSpeedThreshold = 0.3f;

    // Önbellek
    private ParticleSystem.EmissionModule emissionModule;
    private bool isEmitting;

    private void Awake()
    {
        // Particle System referansını al
        if (movementParticles == null)
            movementParticles = GetComponent<ParticleSystem>();

        // Player'ın Rigidbody'sini bul (parent'tan)
        if (playerRb == null)
            playerRb = GetComponentInParent<Rigidbody2D>();

        // Emission modülünü önbelleğe al
        if (movementParticles != null)
        {
            emissionModule = movementParticles.emission;
            emissionModule.enabled = false; // Başlangıçta kapalı
            isEmitting = false;
        }
    }

    private void Update()
    {
        if (playerRb == null || movementParticles == null)
            return;

        float speed = playerRb.linearVelocity.magnitude;
        bool shouldEmit = speed > minSpeedThreshold;

        // Sadece durum değiştiğinde çağır (performans)
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
