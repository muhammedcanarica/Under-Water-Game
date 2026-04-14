using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 5;
    public int currentHealth;
    
    public float invincibleTime = 0.5f;

    [Header("Knockback Settings")]
    public float knockbackForce = 15f;
    public float knockbackStunTime = 0.25f;

    private bool isInvincible;
    private SpriteRenderer spriteRenderer;
    private PlayerController playerController;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerController = GetComponent<PlayerController>();
    }

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage, Transform damageSource = null)
    {
        if (isInvincible) return;

        currentHealth -= damage;
        Debug.Log("Player Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // --- FEEDBACK EFEKTLERI ---

        // Yanip sonme
        if (spriteRenderer != null)
        {
            StartCoroutine(FlickerRoutine());
        }

        // Enemy'e carpma veya hasar durumunda geriye firlatma islemi
        if (damageSource != null && playerController != null)
        {
            Vector2 knockbackDirection = (transform.position - damageSource.position).normalized;
            // Artik direkt kullanicinin belirledigi ApplyKnockback metodunu cagiriyoruz
            playerController.ApplyKnockback(knockbackDirection, knockbackForce, knockbackStunTime);
        }

        // Hasar yenilemezlik (I-frame)
        StartCoroutine(InvincibilityRoutine());
    }

    void Die()
    {
        Debug.Log("Player dead.");
    }

    private IEnumerator FlickerRoutine()
    {
        float timer = 0f;
        while (timer < invincibleTime)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.05f);
            timer += 0.05f;
        }
        spriteRenderer.enabled = true;
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibleTime);
        isInvincible = false;
    }
}
