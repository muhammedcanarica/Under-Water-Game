using UnityEngine;
using System.Collections;
using Unity.Cinemachine; 

/// <summary>
/// Su altı 2D karakter kontrol scripti.
/// Floaty, yüzme hissi veren hareket ve 8 yönlü dash sistemi içerir.
/// Tüm değişkenler Inspector'dan ayarlanabilir.
/// </summary>
public class PlayerController : MonoBehaviour
{
    // ==================== HAREKET AYARLARI ====================

    [Header("Hareket Ayarları")]
    [Tooltip("Maksimum hareket hızı")]
    public float moveSpeed = 5f;

    [Tooltip("Hızlanma yumuşaklığı (düşük = daha floaty)")]
    [Range(0.01f, 1f)]
    public float accelerationSmoothing = 0.08f;

    [Tooltip("Yavaşlama yumuşaklığı (düşük = daha fazla inertia)")]
    [Range(0.01f, 1f)]
    public float decelerationSmoothing = 0.04f;

    [Tooltip("Su direnci / sürtünme katsayısı (velocity'yi yavaşça azaltır)")]
    [Range(0f, 5f)]
    public float waterDrag = 1.5f;

    // ==================== DASH AYARLARI ====================

    [Header("Dash Ayarları")]
    [Tooltip("Dash hız çarpanı")]
    public float dashSpeed = 15f;

    [Tooltip("Dash süresi (saniye)")]
    public float dashDuration = 0.2f;

    [Tooltip("Dash bekleme süresi (saniye)")]
    public float dashCooldown = 1f;

    // ==================== JUICE (EKLENTİ) AYARLARI ====================

    [Header("Juice (Eklentiler) Ayarları")]
    [Tooltip("Sahnemizdeki Cinemachine Kamerası (Shake için)")]
    public CinemachineCamera vcam;
    private CinemachineBasicMultiChannelPerlin noise;

    [Tooltip("Dash sırasında gözükecek Trail")]
    public TrailRenderer dashTrail;

    [Tooltip("Bounce anında doğacak (spawn) Particle Prefab")]
    public GameObject hitParticlePrefab;

    [Tooltip("Çarpma anındaki tatmin edici kısa duraksama (Hit Stop) süresi")]
    public float hitStopDuration = 0.05f;

    // ==================== REFERANSLAR ====================

    [Header("Referanslar")]
    [Tooltip("Rigidbody2D bileşeni (otomatik atanır)")]
    public Rigidbody2D rb;

    // ==================== ÖZEL DEĞİŞKENLER ====================

    // Hareket
    private Vector2 inputDirection;
    private Vector2 currentVelocity;
    private Vector2 velocityRef; // SmoothDamp referansı

    // Dash
    private bool isDashing;
    public bool IsDashing => isDashing; // Dışarıdan okunabilmesi için eklendi

    private float dashTimeRemaining;
    private float dashCooldownTimer;
    private Vector2 dashDirection;

    // ==================== UNITY YAŞAM DÖNGÜSÜ ====================

    private void Awake()
    {
        // Rigidbody2D referansını al
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        
        // Başlangıçta Trail kapalı olsun
        if (dashTrail != null)
            dashTrail.emitting = false;
    }

    private void Start()
    {
        // Kamera titremesi için Noise bileşenini al (Unity 6'da GetComponent ile alınır)
        if (vcam != null)
        {
            noise = vcam.GetComponent<CinemachineBasicMultiChannelPerlin>();
        }

        // Yerçekimini kapat — su altında yüzüyoruz
        rb.gravityScale = 0f;

        // Rigidbody'nin kendi drag'ini sıfırla, biz kendimiz yönetiyoruz
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
    }

    /// <summary>
    /// Input okuma her zaman Update'te yapılır.
    /// </summary>
    private void Update()
    {
        ReadInput();
        HandleDashInput();
        UpdateTimers();
    }

    /// <summary>
    /// Fizik hesaplamaları FixedUpdate'te yapılır.
    /// </summary>
    private void FixedUpdate()
    {
        if (isDashing)
        {
            PerformDash();
        }
        else
        {
            ApplyMovement();
            ApplyWaterDrag();
        }
    }

    // ==================== INPUT ====================

    /// <summary>
    /// Horizontal ve Vertical input değerlerini okur (WASD / Arrow Keys).
    /// </summary>
    private void ReadInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        inputDirection = new Vector2(horizontal, vertical).normalized;
    }

    /// <summary>
    /// Space tuşu ile dash başlatır.
    /// </summary>
    private void HandleDashInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && CanDash())
        {
            StartDash();
        }
    }

    // ==================== HAREKET ====================

    /// <summary>
    /// Yumuşak geçişli (SmoothDamp) hareket uygular.
    /// Input varsa hızlanır, yoksa yavaşça durur — floaty his verir.
    /// </summary>
    private void ApplyMovement()
    {
        Vector2 targetVelocity = inputDirection * moveSpeed;

        // Input var mı yok mu buna göre farklı smoothing uygula
        float smoothTime = (inputDirection.magnitude > 0.1f)
            ? accelerationSmoothing
            : decelerationSmoothing;

        // Velocity'yi hedefe doğru yumuşak şekilde yaklaştır
        currentVelocity = Vector2.SmoothDamp(
            currentVelocity,
            targetVelocity,
            ref velocityRef,
            smoothTime
        );

        rb.linearVelocity = currentVelocity;
    }

    /// <summary>
    /// Su direnci simülasyonu — hızı yavaşça azaltır.
    /// Input yokken daha belirgin bir yavaşlama sağlar.
    /// </summary>
    private void ApplyWaterDrag()
    {
        if (inputDirection.magnitude < 0.1f)
        {
            currentVelocity *= (1f - waterDrag * Time.fixedDeltaTime);
        }
    }

    // ==================== DASH ====================

    /// <summary>
    /// Dash yapılabilir mi kontrol eder.
    /// </summary>
    private bool CanDash()
    {
        return !isDashing && dashCooldownTimer <= 0f;
    }

    /// <summary>
    /// Dash'i başlatır. Yön, mevcut input'a göre belirlenir.
    /// Input yoksa varsayılan olarak sağa dash atılır.
    /// </summary>
    private void StartDash()
    {
        isDashing = true;
        dashTimeRemaining = dashDuration;

        // Trail efektini aç
        if (dashTrail != null) dashTrail.emitting = true;

        // Dash yönünü belirle
        if (inputDirection.magnitude > 0.1f)
        {
            // 8 yönlü dash: input yönüne göre
            dashDirection = GetEightDirectionVector(inputDirection);
        }
        else
        {
            // Input yoksa sağa dash at
            dashDirection = Vector2.right;
        }
    }

    /// <summary>
    /// Dash sırasında sabit hızda hareket uygular.
    /// </summary>
    private void PerformDash()
    {
        rb.linearVelocity = dashDirection * dashSpeed;

        dashTimeRemaining -= Time.fixedDeltaTime;

        if (dashTimeRemaining <= 0f)
        {
            EndDash();
        }
    }

    /// <summary>
    /// Dash'i bitirir ve cooldown'u başlatır.
    /// Dash sonrası mevcut hızı korur (momentum devam eder).
    /// </summary>
    private void EndDash()
    {
        isDashing = false;
        dashCooldownTimer = dashCooldown;

        // Trail efektini kapat
        if (dashTrail != null) dashTrail.emitting = false;

        // Dash sonrası velocity'yi currentVelocity'ye aktar
        // böylece yumuşak geçiş sağlanır
        currentVelocity = rb.linearVelocity * 0.5f;
    }

    /// <summary>
    /// Zamanlayıcıları günceller (cooldown vb.)
    /// </summary>
    private void UpdateTimers()
    {
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
    }

    // ==================== YARDIMCI METODLAR ====================

    /// <summary>
    /// Verilen input vektörünü en yakın 8 yönden birine çevirir.
    /// (Yukarı, Aşağı, Sol, Sağ ve 4 çapraz yön)
    /// </summary>
    private Vector2 GetEightDirectionVector(Vector2 input)
    {
        // Açıyı hesapla (derece cinsinden)
        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;

        // En yakın 45 derecelik açıya yuvarla
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;

        // Tekrar vektöre çevir
        float rad = snappedAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    /// <summary>
    /// Player bir düşmana dash ile çarptığında geri sekmesini sağlar (Bounce).
    /// </summary>
    public void ApplyBounce(Vector2 direction, float force)
    {
        if (isDashing)
        {
            EndDash(); // Dash işlemini anında iptal et
        }

        // --- JUICE (HİSSİYAT) EKLENTİLERİ ---
        
        // 1. Camera Shake (Noise kullanılarak)
        if (noise != null)
        {
            StartCoroutine(ShakeRoutine(1.5f, 0.15f));
        }

        // 2. Parçacık Efekti (Hit Particle)
        if (hitParticlePrefab != null)
        {
            Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);
        }

        // 3. Hit Stop (Zaman Dondurma - Freeze)
        StartCoroutine(HitStopCoroutine(hitStopDuration));
        
        // --- FiZİK ETKİSİ ---
        // Geri sekme kuvvetini uygula
        currentVelocity = direction.normalized * force;
        rb.linearVelocity = currentVelocity;
    }

    /// <summary>
    /// Vuruş hissiyatı için zamanı çok kısa bir süreliğine dondurup çözer.
    /// </summary>
    private IEnumerator HitStopCoroutine(float duration)
    {
        // Zamanı dondur (fizikler, update'ler bekler)
        Time.timeScale = 0f;
        
        // timeScale = 0 olduğunda beklemenin devam etmesi için Realtime kullanılır:
        yield return new WaitForSecondsRealtime(duration);
        
        // Zamanı eski haline getir
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Kameranın BasicMultiChannelPerlin Noise değerini değiştirerek sallanmasını sağlar.
    /// </summary>
    private IEnumerator ShakeRoutine(float intensity, float time)
    {
        if (noise != null)
        {
            noise.AmplitudeGain = intensity;
            
            // HitStop yüzünden zaman donuk olursa diye Realtime bekleme
            yield return new WaitForSecondsRealtime(time);
            
            noise.AmplitudeGain = 0f;
        }
    }
}
