using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Sağlık Ayarları")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Knockback Ayarları")]
    [SerializeField] private float knockbackForce = 3f;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("Efekt Ayarları")]
    [SerializeField] private GameObject hitParticle;
    [SerializeField] private GameObject deathParticle;
    
    [Header("Referanslar")]
    [SerializeField] private Rigidbody2D rb;

    private EnemyController enemyController;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        enemyController = GetComponent<EnemyController>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage, Vector2 damageSourcePosition)
    {
        currentHealth -= damage;

        if (hitParticle != null)
        {
            GameObject p = Instantiate(hitParticle, transform.position, Quaternion.identity);
            Destroy(p, 1f);
        }

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (rb != null)
        {
            Vector2 direction = ((Vector2)transform.position - damageSourcePosition).normalized;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
            
            if (enemyController != null)
            {
                enemyController.ApplyHitStun(knockbackDuration);
            }
        }
    }

    private void Die()
    {
        if (deathParticle != null)
        {
            GameObject p = Instantiate(deathParticle, transform.position, Quaternion.identity);
            Destroy(p, 1f);
        }
        Destroy(gameObject);
    }
}
