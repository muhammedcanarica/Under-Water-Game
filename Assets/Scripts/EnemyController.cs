using UnityEngine;

/// <summary>
/// Basit, temiz ve modüler düşman hareket scripti.
/// Belirlenen minX ve maxX arasında devriye gezer (Patrol).
/// Oyuncu ile çarpışmalarını (bounce) yönetir. Sağlık sistemi EnemyHealth içerisindedir.
/// </summary>
public class EnemyController : MonoBehaviour
{
    // ==========================================
    // DEĞİŞKENLER
    // ==========================================

    [Header("Enemy Combat Ayarları")]
    [Tooltip("Oyuncuya değdiğinde vereceği hasar miktarı")]
    public int damageToPlayer = 1;

    [Tooltip("Oyuncuya hasar verdiğinde onu ne kadar uzağa geri fırlatacak?")]
    public float hitKnockbackForce = 6f;

    [Tooltip("Oyuncu bu düşmana dash atarak çarptığında ne kadar geri sekecek?")]
    public float bounceForce = 15f;

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

    // Referans ve Durum
    private Rigidbody2D rb;
    private bool movingRight = true;
    private Vector2 currentVelocity;
    private Vector2 velocityRef;
    private EnemyHealth enemyHealth;

    // Hit-Stun Durumu (Knockback esnasında Patrol durdurulur)
    private bool isKnockback;
    private float knockbackTimer = 0f;

    // ==========================================
    // UNITY YAŞAM DÖNGÜSÜ
    // ==========================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyHealth = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
        }

        // Başlangıçta X pozisyonunu sınırların içine al
        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        transform.position = new Vector2(clampedX, transform.position.y);
    }

    private void FixedUpdate()
    {
        if (isKnockback)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            if (knockbackTimer <= 0f) isKnockback = false;
            return;
        }

        if (rb != null)
        {
            Patrol();
        }
    }

    public void ApplyHitStun(float duration)
    {
        isKnockback = true;
        knockbackTimer = duration;
    }

    // ==========================================
    // PATROL (DEVRİYE HAREKETİ)
    // ==========================================

    private void Patrol()
    {
        // Sınırlara ulaşınca dön
        if (movingRight && transform.position.x >= maxX)
        {
            FlipDirection();
        }
        else if (!movingRight && transform.position.x <= minX)
        {
            FlipDirection();
        }

        // Rigidbody üzerinden Velocity ataması
        float targetVelocityX = movingRight ? moveSpeed : -moveSpeed;
        Vector2 targetVelocity = new Vector2(targetVelocityX, 0f);
        
        currentVelocity = Vector2.SmoothDamp(
            currentVelocity,
            targetVelocity,
            ref velocityRef,
            accelerationSmoothing
        );

        rb.linearVelocity = currentVelocity;
    }

    private void FlipDirection()
    {
        movingRight = !movingRight;
        Vector3 newScale = transform.localScale;
        newScale.x *= -1;
        transform.localScale = newScale;
    }

    // ==========================================
    // ÇARPIŞMA KONTROLÜ (ON COLLISION ENTER)
    // ==========================================

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Sadece Player etiketi olan objeler ile etkileşime gir
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
            if (playerController == null) return;

            // DURUM 1: Player Dash Atıyor (Enemy hasar alır, Player seker)
            if (playerController.IsDashing)
            {
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(1, collision.transform.position);
                }

                // Geri sekme yönünü hesapla (Player'ı, düştüğü çarpışma açısından ters itiyoruz)
                Vector2 bounceDirection = (collision.transform.position - transform.position).normalized;
                Vector2 contactPoint = collision.GetContact(0).point;

                playerController.ApplyBounce(bounceDirection, bounceForce, contactPoint);
            }
            // DURUM 2: Player Dash Atmıyor (Player hasar alır ve geriye seker)
            else
            {
                PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
                
                if (playerHealth != null)
                {
                    Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
                    playerController.ApplyKnockback(knockbackDir, hitKnockbackForce, 0.15f);
                    
                    playerHealth.TakeDamage(damageToPlayer, transform);
                }
            }
        }
    }
}
