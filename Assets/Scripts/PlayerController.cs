using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public enum PlayerMode
{
    Water,
    Land
}

public class PlayerController : MonoBehaviour
{
    [Header("Mod Sistemi")]
    public PlayerMode currentMode = PlayerMode.Water;

    [Header("Gecis Ayarlari")]
    public float transitionDuration = 0.15f;
    private bool isTransitioning;
    private float transitionTimer;
    private float targetGravityScale;

    [Header("Su ve Genel Hareket Ayarlari")]
    [Tooltip("Maksimum hareket hizi")]
    public float moveSpeed = 5f;

    [Tooltip("Su alti hizlanma yumusakligi")]
    [Range(0.01f, 1f)]
    public float accelerationSmoothing = 0.08f;

    [Tooltip("Su alti yavaslama yumusakligi")]
    [Range(0.01f, 1f)]
    public float decelerationSmoothing = 0.04f;

    [Tooltip("Su direnci")]
    [Range(0f, 5f)]
    public float waterDrag = 1.5f;

    [Tooltip("Input birakildiginda karakterin suda ne kadar hizli duracagi")]
    public float waterStopSpeed = 28f;

    [Header("Kara Modu Ayarlari")]
    public float targetLandGravity = 3f;
    [Tooltip("Ziplama kuvveti")]
    public float jumpForce = 12f;
    public Transform groundCheck;
    public float groundRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;
    private bool facingRight = true;

    [Tooltip("Sudan ciktiktan sonra zemin algilama gecikmesi")]
    private float groundCheckGraceTimer = 0f;
    private const float GROUND_CHECK_GRACE_DURATION = 0.15f;

    [Header("Kara Dash Ayarlari")]
    public float landDashForce = 20f;
    public float landDashDuration = 0.15f;
    public float landDashCooldown = 1f;

    private bool isLandDashing;
    private float landDashTimeRemaining;
    private float landDashCooldownTimer;

    [Header("Dash Ayarlari (Water Mode)")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    private bool isDashing;
    public bool IsDashing => isDashing;
    public Vector2 FacingDirection => facingRight ? Vector2.right : Vector2.left;
    private float dashTimeRemaining;
    private float dashCooldownTimer;
    private Vector2 dashDirection;

    [Header("Su Cikis Ayarlari")]
    [Tooltip("Sudan karaya geciste yukari hizin asamayacagi maksimum deger")]
    public float maxWaterYSpeed = 12f;

    [Header("Juice Ayarlari")]
    public CinemachineCamera vcam;
    private CinemachineBasicMultiChannelPerlin noise;

    public TrailRenderer dashTrail;
    public GameObject hitParticlePrefab;
    public float hitStopDuration = 0.05f;

    [Header("Referanslar")]
    public Rigidbody2D rb;

    private bool isKnockback;
    private float knockbackTimer = 0f;

    private Vector2 inputDirection;
    private Vector2 currentVelocity;

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

        targetGravityScale = currentMode == PlayerMode.Water ? 0f : targetLandGravity;
        rb.gravityScale = targetGravityScale;

        ApplyModeProperties(currentMode, true);
    }

    private void Update()
    {
        ReadInput();
        HandleModeSwitchInput();
        HandleActionInput();
        UpdateTimers();
    }

    private void FixedUpdate()
    {
        CheckGround();

        if (isKnockback)
        {
            knockbackTimer -= Time.fixedDeltaTime;

            if (knockbackTimer <= 0f)
            {
                isKnockback = false;
            }

            return;
        }

        rb.gravityScale = Mathf.Lerp(rb.gravityScale, targetGravityScale, Time.fixedDeltaTime * 8f);

        if (isTransitioning)
        {
            transitionTimer -= Time.fixedDeltaTime;
            if (transitionTimer <= 0f)
            {
                isTransitioning = false;
            }

            return;
        }

        if (currentMode == PlayerMode.Water)
        {
            if (isDashing)
            {
                PerformWaterDash();
            }
            else
            {
                HandleWaterMovement();
            }
        }
        else if (currentMode == PlayerMode.Land)
        {
            if (isLandDashing)
            {
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
            PlayerMode newMode = currentMode == PlayerMode.Water ? PlayerMode.Land : PlayerMode.Water;
            ApplyModeProperties(newMode);
        }
    }

    public void ApplyModeProperties(PlayerMode mode, bool forceInitialize = false)
    {
        if (currentMode == mode && !forceInitialize) return;

        PlayerMode previousMode = currentMode;
        currentMode = mode;

        if (!forceInitialize)
        {
            isTransitioning = true;
            transitionTimer = transitionDuration;
        }

        float preservedYVelocity = 0f;
        if (!forceInitialize && previousMode == PlayerMode.Water && mode == PlayerMode.Land)
        {
            if (rb.linearVelocity.y > 0f)
            {
                preservedYVelocity = Mathf.Min(rb.linearVelocity.y, maxWaterYSpeed);
            }

            groundCheckGraceTimer = GROUND_CHECK_GRACE_DURATION;
        }

        rb.linearVelocity = new Vector2(0f, preservedYVelocity);
        currentVelocity = new Vector2(0f, preservedYVelocity);
        if (currentMode == PlayerMode.Water)
        {
            if (isLandDashing) EndLandDash();
            targetGravityScale = 0f;
            rb.linearDamping = 0.2f;
        }
        else if (currentMode == PlayerMode.Land)
        {
            if (isDashing) EndWaterDash();
            targetGravityScale = targetLandGravity;
            rb.linearDamping = 0f;
        }
    }

    private void HandleActionInput()
    {
        if (currentMode == PlayerMode.Water)
        {
            if (Input.GetKeyDown(KeyCode.Space) && CanWaterDash())
            {
                StartWaterDash();
            }
        }
        else if (currentMode == PlayerMode.Land)
        {
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                Jump();
            }

            if (Input.GetKeyDown(KeyCode.LeftShift) && CanLandDash())
            {
                StartLandDash();
            }
        }
    }

    private void CheckGround()
    {
        if (currentMode == PlayerMode.Water)
        {
            isGrounded = false;
            return;
        }

        if (groundCheckGraceTimer > 0f)
        {
            groundCheckGraceTimer -= Time.fixedDeltaTime;
            isGrounded = false;
            return;
        }

        if (isLandDashing || isDashing)
        {
            isGrounded = false;
            return;
        }

        if (rb.linearVelocity.y > 0.1f)
        {
            isGrounded = false;
            return;
        }

        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        }
        else
        {
            isGrounded = false;
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

    private void HandleWaterMovement()
    {
        if (inputDirection.magnitude > 0.1f)
        {
            currentVelocity = inputDirection * moveSpeed;
        }
        else
        {
            currentVelocity = Vector2.MoveTowards(
                currentVelocity,
                Vector2.zero,
                waterStopSpeed * Time.fixedDeltaTime);
        }

        rb.linearVelocity = currentVelocity;

        if (rb.linearVelocity.y > maxWaterYSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxWaterYSpeed);
            currentVelocity.y = maxWaterYSpeed;
        }
    }

    private void HandleLandMovement()
    {
        float targetVelocityX = inputDirection.x * moveSpeed;
        rb.linearVelocity = new Vector2(targetVelocityX, rb.linearVelocity.y);
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

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

        float dashDir = 0f;
        if (inputDirection.x != 0f) dashDir = Mathf.Sign(inputDirection.x);
        else dashDir = facingRight ? 1f : -1f;

        rb.linearVelocity = new Vector2(dashDir * landDashForce, rb.linearVelocity.y);
    }

    private void EndLandDash()
    {
        isLandDashing = false;
        if (dashTrail != null) dashTrail.emitting = false;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, rb.linearVelocity.y);
    }

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
            dashDirection = Vector2.right;
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
            else
            {
                Destroy(particleInstance, 1f);
            }
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

    public void TriggerHitStop(float duration)
    {
        if (duration <= 0f)
            return;

        StartCoroutine(HitStopCoroutine(duration));
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
