using UnityEngine;

/// <summary>
/// Basit düşman scripti.
/// Belirlenen minX ve maxX arasında devriye gezer (Patrol).
/// Oyuncu dash atarak çarptığında hasar alır ve oyuncuyu geri sektirir.
/// </summary>
public class EnemyController : MonoBehaviour
{
    [Header("Sağlık Sistemi")]
    [Tooltip("Düşmanın canı")]
    public int health = 3;

    [Header("Patrol (Devriye) Ayarları")]
    [Tooltip("Düşmanın devriye atma hızı")]
    public float moveSpeed = 2f;
    
    [Tooltip("Hızlanma/Yavaşlama yumuşaklığı (düşük = daha floaty)")]
    [Range(0.01f, 1f)]
    public float accelerationSmoothing = 0.2f;

    [Tooltip("Sol devriye sınırı (Dünya X koordinatı)")]
    public float minX = -5f;
    
    [Tooltip("Sağ devriye sınırı (Dünya X koordinatı)")]
    public float maxX = 5f;

    [Header("Çarpışma (Bounce) Ayarları")]
    [Tooltip("Oyuncu bu düşmana dash atarak çarptığında ne kadar geri sekecek?")]
    public float bounceForce = 15f;

    // Referans
    private Rigidbody2D rb;

    // Durum
    private bool movingRight = true;
    private Vector2 currentVelocity;
    private Vector2 velocityRef;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Su altı hissi için yerçekimini ve varsayılan sürtünmeyi kapat (Player ile aynı)
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        // Başlangıçta X pozisyonunu sınırların içine al
        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        transform.position = new Vector2(clampedX, transform.position.y);
    }

    private void FixedUpdate()
    {
        Patrol();
    }

    /// <summary>
    /// Düşmanı minX ve maxX sınırları arasında yumuşak bir şekilde hareket ettirir.
    /// </summary>
    private void Patrol()
    {
        // Sağ sınıra ulaştıysa yön değiştir
        if (movingRight && transform.position.x >= maxX)
        {
            FlipDirection();
        }
        // Sol sınıra ulaştıysa yön değiştir
        else if (!movingRight && transform.position.x <= minX)
        {
            FlipDirection();
        }

        // Hedefe göre hızı belirle
        float targetVelocityX = movingRight ? moveSpeed : -moveSpeed;
        Vector2 targetVelocity = new Vector2(targetVelocityX, 0f);
        
        // Velocity'yi hedefe doğru yumuşak şekilde yaklaştır (floaty hissi)
        currentVelocity = Vector2.SmoothDamp(
            currentVelocity,
            targetVelocity,
            ref velocityRef,
            accelerationSmoothing
        );

        rb.linearVelocity = currentVelocity;
    }

    /// <summary>
    /// Hareket yönünü ve görseli (sprite) tersine çevirir.
    /// </summary>
    private void FlipDirection()
    {
        movingRight = !movingRight;

        // Karakterin baktığı yönü döndür (Scale X'i ters çevirerek)
        Vector3 newScale = transform.localScale;
        newScale.x *= -1;
        transform.localScale = newScale;
    }

    /// <summary>
    /// Hasar alma fonksiyonu. Can sıfırlandığında düşmanı yok eder.
    /// </summary>
    public void TakeDamage(int amount)
    {
        health -= amount;

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Ölüm efektleri (Particle, ses vs.) eklenebilir.
        Destroy(gameObject);
    }

    /// <summary>
    /// Çarpışmaları (Collision) kontrol eder. 
    /// Player 'dash' ile çarparsa hasar alır ve player'ı geri sektirir.
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();

            // Eğer PlayerController varsa ve karakter Dash yapıyorsa
            if (player != null && player.IsDashing)
            {
                // Düşman hasar alır
                TakeDamage(1);

                // Geri sekme yönünü hesapla
                // Yön: Player'ın pozisyonu - Düşmanın pozisyonu (yani player'ı düşmandan uzaklaşacak yöne doğru itiyoruz)
                Vector2 bounceDirection = (collision.transform.position - transform.position).normalized;
                
                // Oyuncuyu belirtilen kuvvet ile geri sektir (bounce)
                player.ApplyBounce(bounceDirection, bounceForce);
            }
        }
    }
}
