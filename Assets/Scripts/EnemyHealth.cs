using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Sağlık Ayarları")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Knockback Ayarları")]
    [SerializeField] private float knockbackForce = 3f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float stunnedKnockbackMultiplier = 1.8f;

    [Header("Efekt Ayarları")]
    [SerializeField] private GameObject hitParticle;
    [SerializeField] private GameObject deathParticle;
    [SerializeField] private float stunnedHitParticleScaleMultiplier = 1.6f;
    
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
        bool hitWhileStunned = enemyController != null && enemyController.IsStunned;
        currentHealth -= damage;

        if (hitParticle != null)
        {
            GameObject p = Instantiate(hitParticle, transform.position, Quaternion.identity);
            if (hitWhileStunned)
            {
                p.transform.localScale *= stunnedHitParticleScaleMultiplier;
            }

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
            float finalKnockbackForce = hitWhileStunned ? knockbackForce * stunnedKnockbackMultiplier : knockbackForce;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(direction * finalKnockbackForce, ForceMode2D.Impulse);
            
            if (enemyController != null)
            {
                enemyController.ApplyHitStun(knockbackDuration);
            }
        }
    }

    public bool ApplyStun(float duration)
    {
        if (enemyController != null)
        {
            return enemyController.TryApplyStun(duration);
        }

        return false;
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
