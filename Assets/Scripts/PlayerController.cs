using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public enum PlayerMode
{
    Water,
    Land
}

/// <summary>
/// Su altı (floaty) ve Kara (klasik platformer) modları arasında geçiş yapabilen gelişmiş karakter kontrol scripti.
/// F tuşu ile modlar arasında geçiş yapılır.
/// </summary>
public class PlayerController : MonoBehaviour
{
    // ==================== MOD SİSTEMİ ====================
    [Header("Mod Sistemi")]
    public PlayerMode currentMode = PlayerMode.Water;

    // ==================== HAREKET AYARLARI ====================
    [Header("Su ve Genel Hareket Ayarları")]
    [Tooltip("Maksimum hareket hızı")]
    public float moveSpeed = 5f;

    [Tooltip("Su altı: Hızlanma yumuşaklığı (düşük = daha floaty)")]
    [Range(0.01f, 1f)]
    public float accelerationSmoothing = 0.08f;

    [Tooltip("Su altı: Yavaşlama yumuşaklığı (düşük = daha fazla inertia)")]
    [Range(0.01f, 1f)]
    public float decelerationSmoothing = 0.04f;

    [Tooltip("Su altı: Su direnci / sürtünme katsayısı (velocity'yi yavaşça azaltır)")]
    [Range(0f, 5f)]
    public float waterDrag = 1.5f;

    // ==================== LAND (KARA) MODU AYARLARI ====================
    [Header("Kara Modu Ayarları")]
    [Tooltip("Zıplama kuvveti (AddForce ile uygulanacak)")]
    public float jumpForce = 12f;
    public Transform groundCheck;
    public float groundRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;
    private bool facingRight = true;

    [Header("Kara Dash Ayarları")]
    public float landDashForce = 20f;
    public float landDashDuration = 0.15f;
    public float landDashCooldown = 1f;

    private bool isLandDashing;
    private float landDashTimeRemaining;
    private float landDashCooldownTimer;

    // ==================== DASH AYARLARI (SADECE WATER) ====================
    [Header("Dash Ayarları (Water Mode)")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    private bool isDashing;
    public bool IsDashing => isDashing;
    private float dashTimeRemaining;
    private float dashCooldownTimer;
    private Vector2 dashDirection;

    // ==================== JUICE (EKLENTİ) AYARLARI ====================
    [Header("Juice (Eklentiler) Ayarları")]
    public CinemachineCamera vcam;
    private CinemachineBasicMultiChannelPerlin noise;

    public TrailRenderer dashTrail;
    public GameObject hitParticlePrefab;
    public float hitStopDuration = 0.05f;

    // ==================== REFERANSLAR ====================
    [Header("Referanslar")]
    public Rigidbody2D rb;

    // ==================== ÖZEL DEĞİŞKENLER ====================
    
    // Knockback (Hit-Stun)
    private bool isKnockback;
    private float knockbackTimer = 0f;

    // Hareket
    private Vector2 inputDirection;
    private Vector2 currentVelocity;
    private Vector2 velocityRef; 

    // ==================== UNITY YAŞAM DÖNGÜSÜ ====================

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        
        if (dashTrail != null) dashTrail.emitting = false;
    }

    private void Start()
    {
        if (vcam != null)
        {
            noise = vcam.GetComponent<CinemachineBasicMultiChannelPerlin>();
        }

        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        // Başlangıç modunu kur
        ApplyModeProperties(currentMode);
    }

    private void Update()
    {
        ReadInput();
        HandleModeSwitchInput();
        HandleActionInput();
        UpdateTimers();
        CheckGround();
    }

    private void FixedUpdate()
    {
        // 1. Knockback (Geri tepme) durumundayken input kabul etme
        if (isKnockback)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            
            if (knockbackTimer <= 0f)
            {
                isKnockback = false;
            }
            // Sadece return yapiyoruz (Movement kodu override etmesin diye engelleniyor)
            return;
        }

        // 2. Mod kontrolü
        if (currentMode == PlayerMode.Water)
        {
            if (isDashing)
            {
                PerformWaterDash();
            }
            else
            {
                HandleWaterMovement();
                ApplyWaterDrag();
            }
        }
        else if (currentMode == PlayerMode.Land)
        {
            if (isLandDashing)
            {
                // Land dash süresince normal kontrol iptal, hizi sabit tut
                landDashTimeRemaining -= Time.fixedDeltaTime;
                if (landDashTimeRemaining <= 0f)
                {
                    EndLandDash();
                }
            }
            else
            {
                HandleLandMovement();
            }
        }
    }

    // ==================== INPUT & MOD ====================

    private void ReadInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        inputDirection = new Vector2(horizontal, vertical).normalized;

        if (horizontal > 0f) facingRight = true;
        else if (horizontal < 0f) facingRight = false;
    }

    private void HandleModeSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            PlayerMode newMode = (currentMode == PlayerMode.Water) ? PlayerMode.Land : PlayerMode.Water;
            ApplyModeProperties(newMode);
        }
    }

    private void ApplyModeProperties(PlayerMode mode)
    {
        currentMode = mode;

        if (currentMode == PlayerMode.Water)
        {
            if (isLandDashing) EndLandDash();
            rb.gravityScale = 0f;
        }
        else if (currentMode == PlayerMode.Land)
        {
            if (isDashing) EndWaterDash();
            rb.gravityScale = 3f;
        }
    }

    private void HandleActionInput()
    {
        // Zıplama / Su Dash Tuşu (Space)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentMode == PlayerMode.Water)
            {
                if (CanWaterDash()) StartWaterDash();
            }
            else if (currentMode == PlayerMode.Land)
            {
                if (isGrounded) Jump();
            }
        }

        // Kara Dash Tuşu (Left Shift)
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (currentMode == PlayerMode.Land && CanLandDash())
            {
                StartLandDash();
            }
        }
    }

    // ==================== ZEMIN KONTROLU ====================

    private void CheckGround()
    {
        if (currentMode == PlayerMode.Land && groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
    }

    // ==================== HAREKET YÖNETİMİ ====================

    /// <summary>
    /// SmoothDamp destekli 360 derece uçuş/yüzüş hareketi
    /// </summary>
    private void HandleWaterMovement()
    {
        Vector2 targetVelocity = inputDirection * moveSpeed;

        float smoothTime = (inputDirection.magnitude > 0.1f)
            ? accelerationSmoothing
            : decelerationSmoothing;

        currentVelocity = Vector2.SmoothDamp(
            currentVelocity,
            targetVelocity,
            ref velocityRef,
            smoothTime
        );

        rb.linearVelocity = currentVelocity;
    }

    private void ApplyWaterDrag()
    {
        if (inputDirection.magnitude < 0.1f)
        {
            currentVelocity *= (1f - waterDrag * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// Katı ve net (velocity) bazlı yatay platformer hareketi
    /// </summary>
    private void HandleLandMovement()
    {
        // Karada sadece x ekseni velocity üzerinden etkilenir
        float targetVelocityX = inputDirection.x * moveSpeed;
        
        rb.linearVelocity = new Vector2(targetVelocityX, rb.linearVelocity.y);
    }

    private void Jump()
    {
        // Zıplamada daha keskin bir his için y eksenini sıfırlayıp kuvveti uygula
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    // ==================== DASH (LAND) ====================

    private bool CanLandDash()
    {
        return !isLandDashing && landDashCooldownTimer <= 0f;
    }

    private void StartLandDash()
    {
        isLandDashing = true;
        landDashTimeRemaining = landDashDuration;
        landDashCooldownTimer = landDashCooldown;

        if (dashTrail != null) dashTrail.emitting = true;

        // Input yoksa baktığı yön, input varsa input yönü (sadece yatay)
        float dashDir = 0f;
        if (inputDirection.x != 0) dashDir = Mathf.Sign(inputDirection.x);
        else dashDir = facingRight ? 1f : -1f;

        rb.linearVelocity = new Vector2(dashDir * landDashForce, rb.linearVelocity.y);
    }

    private void EndLandDash()
    {
        isLandDashing = false;
        if (dashTrail != null) dashTrail.emitting = false;
        
        // Hızı yumuşat
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, rb.linearVelocity.y);
    }

    // ==================== DASH (WATER) ====================

    private bool CanWaterDash()
    {
        return !isDashing && dashCooldownTimer <= 0f;
    }

    private void StartWaterDash()
    {
        isDashing = true;
        dashTimeRemaining = dashDuration;

        if (dashTrail != null) dashTrail.emitting = true;

        if (inputDirection.magnitude > 0.1f)
            dashDirection = GetEightDirectionVector(inputDirection);
        else
            dashDirection = Vector2.right; // varsayılan
    }

    private void PerformWaterDash()
    {
        rb.linearVelocity = dashDirection * dashSpeed;
        dashTimeRemaining -= Time.fixedDeltaTime;

        if (dashTimeRemaining <= 0f)
        {
            EndWaterDash();
        }
    }

    private void EndWaterDash()
    {
        isDashing = false;
        dashCooldownTimer = dashCooldown;

        if (dashTrail != null) dashTrail.emitting = false;

        currentVelocity = rb.linearVelocity * 0.5f;
    }

    // ==================== YARDIMCI METOTLAR ====================

    private void UpdateTimers()
    {
        if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;
        
        if (landDashCooldownTimer > 0f) landDashCooldownTimer -= Time.deltaTime;
    }

    private Vector2 GetEightDirectionVector(Vector2 input)
    {
        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;
        float rad = snappedAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    // ==================== KNOCKBACK VE GERİ BİLDİRİMLER ====================

    public void ApplyBounce(Vector2 direction, float force, Vector2 hitPosition)
    {
        if (currentMode == PlayerMode.Water && isDashing) EndWaterDash();
        if (currentMode == PlayerMode.Land && isLandDashing) EndLandDash();

        if (noise != null) StartCoroutine(ShakeRoutine(1.5f, 0.15f));

        if (hitParticlePrefab != null)
        {
            GameObject particleInstance = Instantiate(hitParticlePrefab, hitPosition, Quaternion.identity);
            ParticleSystem ps = particleInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                float destroyDelay = ps.main.duration + ps.main.startLifetime.constantMax;
                Destroy(particleInstance, destroyDelay);
            }
            else Destroy(particleInstance, 1f);
        }

        StartCoroutine(HitStopCoroutine(hitStopDuration));
        
        currentVelocity = direction.normalized * force;
        rb.linearVelocity = currentVelocity;
        
        isKnockback = true;
        knockbackTimer = 0.2f; 
    }

    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        if (currentMode == PlayerMode.Water && isDashing) EndWaterDash();
        if (currentMode == PlayerMode.Land && isLandDashing) EndLandDash();

        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * force;
            currentVelocity = rb.linearVelocity;
        }

        isKnockback = true;
        knockbackTimer = duration;

        if (noise != null) StartCoroutine(ShakeRoutine(3f, 0.2f)); 
    }

    private IEnumerator HitStopCoroutine(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    private IEnumerator ShakeRoutine(float intensity, float time)
    {
        if (noise != null)
        {
            noise.AmplitudeGain = intensity;
            yield return new WaitForSecondsRealtime(time);
            noise.AmplitudeGain = 0f;
        }
    }
}
