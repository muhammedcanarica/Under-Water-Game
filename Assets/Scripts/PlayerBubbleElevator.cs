using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerBubbleElevator : MonoBehaviour
{
    [Header("Bubble Movement")]
    [Tooltip("Yukarı doğru sabit yükselme hızı")]
    public float ascendSpeed = 3f;
    [Tooltip("Sağ/sol hareketin maksimum hızı")]
    public float maxHorizontalSpeed = 2f;
    [Tooltip("Sağ/sol hareketteki ivmelenme (yumuşaklık)")]
    public float horizontalAcceleration = 4f;

    [Header("Exit Conditions")]
    [Tooltip("Balonun içinde kalınabilecek maksimum süre (saniye)")]
    public float maxDuration = 3f;
    [Tooltip("Tavanı kontrol etmek için başın üstünden atılacak ışının uzunluğu")]
    public float ceilingCheckDistance = 0.6f;
    [Tooltip("Tavan kabul edilecek layer'lar")]
    public LayerMask ceilingLayer = 1 << 6; // Default to Ground layer

    [Header("Visuals")]
    [Tooltip("İsteğe bağlı: Bubble görseli için kullanılacak Sprite")]
    public Sprite bubbleSprite;
    public Color bubbleColor = new Color(0.7f, 0.9f, 1f, 0.6f);
    public Vector3 bubbleScale = new Vector3(1.4f, 1.8f, 1f);
    [Tooltip("Karakterin etrafında ne kadar boşluk bırakılacağı")]
    public Vector2 bubblePadding = new Vector2(0.25f, 0.35f);
    [Tooltip("Balonun karaktere göre küçük konum düzeltmesi için")]
    public Vector3 bubbleLocalOffset = new Vector3(0f, -0.2f, 0f);

    public bool IsInBubble { get; private set; }

    private Rigidbody2D rb;
    private PlayerInputReader inputReader;
    private PlayerController playerController;
    private PlayerMovement playerMovement;
    private PlayerDash playerDash;

    private float originalGravity;
    private float originalDrag;
    private float bubbleTimer;

    private GameObject bubbleVisualObj;
    private GameObject bubbleSpriteObj;
    private SpriteRenderer bubbleRenderer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputReader = GetComponent<PlayerInputReader>();
        
        // Disable edilecek ana komponentler
        playerController = GetComponent<PlayerController>();
        playerMovement = GetComponent<PlayerMovement>();
        playerDash = GetComponent<PlayerDash>();

        CreateBubbleVisual();
    }

    private void CreateBubbleVisual()
    {
        bubbleVisualObj = new GameObject("BubbleVisual");
        bubbleVisualObj.transform.SetParent(transform);
        bubbleVisualObj.transform.localPosition = bubbleLocalOffset;
        bubbleVisualObj.transform.localScale = bubbleScale;

        bubbleSpriteObj = new GameObject("BubbleSprite");
        bubbleSpriteObj.transform.SetParent(bubbleVisualObj.transform, false);

        bubbleRenderer = bubbleSpriteObj.AddComponent<SpriteRenderer>();
        bubbleRenderer.color = bubbleColor;
        bubbleRenderer.sortingOrder = 15; // Oyuncunun önünde çizilsin
        bubbleVisualObj.SetActive(false);

        // Eğer inspector'dan atanmamışsa geçici bir daire sprite'ı yarat
        if (bubbleSprite != null)
        {
            bubbleRenderer.sprite = bubbleSprite;
        }
        else
        {
            bubbleRenderer.sprite = CreateCircleSprite(128);
        }

        NormalizeBubbleSpritePivot();
        RefreshBubbleVisualScale();
    }

    private Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);
        float radius = size / 2f;
        float center = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Yumuşak balon kenarı çizimi
                if (dist < radius - 4f)
                {
                    // Balonun içi saydam
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, 0.1f)); 
                }
                else if (dist < radius)
                {
                    // Kenar çizgisi daha opak ve yumuşak geçişli
                    float alpha = (radius - dist) / 4f; 
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    // Dışarısı tamamen boş
                    texture.SetPixel(x, y, transparent);
                }
            }
        }
        texture.Apply();
        
        // Pivot tam ortada, 1 birim genişlik için pixelsPerUnit = size
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    public void EnterBubble()
    {
        if (IsInBubble) return;

        IsInBubble = true;
        bubbleTimer = 0f;

        // 1. Oyuncu kontrollerini dondur
        if (playerController != null) playerController.enabled = false;
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerDash != null) playerDash.enabled = false;

        // 2. Fizik ayarlarını devral
        originalGravity = rb.gravityScale;
        originalDrag = rb.linearDamping;

        rb.gravityScale = 0f;
        rb.linearDamping = 1f; // Hafif bir sürtünme ekleyerek floaty his yaratır

        // Dikey hızı sıfırla ki yumuşak başlasın
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        // 3. Görseli aç
        if (bubbleSprite == null && bubbleRenderer.sprite == null)
        {
            // Eğer sprite atanmamışsa, geçici bir şey göstermemek yerine
            // uyarı verin. UnityEditor kütüphanesini build'de kullanamayız.
            Debug.LogWarning("Bubble Elevator: Bubble Sprite atanmamış! Lütfen Inspector'dan bir sprite atayın.");
        }

        RefreshBubbleVisualScale();
        bubbleVisualObj.SetActive(true);
    }

    public void ExitBubble()
    {
        if (!IsInBubble) return;

        IsInBubble = false;

        // 1. Fizik ayarlarını geri ver
        rb.gravityScale = originalGravity;
        rb.linearDamping = originalDrag;

        // Çıkışta ivmeyi koru veya istersen hafifçe zıplat:
        // rb.linearVelocity = new Vector2(rb.linearVelocity.x, ascendSpeed * 0.5f);

        // 2. Oyuncu kontrollerini aç
        if (playerController != null) playerController.enabled = true;
        if (playerMovement != null) playerMovement.enabled = true;
        if (playerDash != null) playerDash.enabled = true;

        // 3. Görseli kapat
        bubbleVisualObj.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (!IsInBubble) return;

        bubbleTimer += Time.fixedDeltaTime;

        // --- Çıkış Koşulları Kontrolü ---
        
        // 1. Süre doldu mu?
        if (bubbleTimer >= maxDuration)
        {
            ExitBubble();
            return;
        }

        // 2. Tavana çarptı mı?
        Vector2 checkOrigin = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(checkOrigin, Vector2.up, ceilingCheckDistance, ceilingLayer);
        if (hit.collider != null)
        {
            ExitBubble();
            return;
        }

        // --- Hareket Mantığı ---

        Vector2 currentVel = rb.linearVelocity;

        // Y ekseni: Sabit hızla yukarı
        float targetVelocityY = ascendSpeed;

        // X ekseni: Sınırlı input kontrolü
        float inputX = inputReader != null ? inputReader.MoveInput.x : 0f;
        float targetVelocityX = inputX * maxHorizontalSpeed;

        // Yumuşak geçiş (Drag hissi)
        currentVel.x = Mathf.Lerp(currentVel.x, targetVelocityX, horizontalAcceleration * Time.fixedDeltaTime);
        
        // Yukarı hızda aniden fırlamamak için ivmelenerek çık
        currentVel.y = Mathf.Lerp(currentVel.y, targetVelocityY, horizontalAcceleration * Time.fixedDeltaTime);

        rb.linearVelocity = currentVel;
    }

    private void Update()
    {
        if (!IsInBubble) return;

        // 3. Jump tuşuna basıldı mı? (Update içinde input okumak daha güvenlidir)
        if (inputReader != null && inputReader.ConsumeJumpPressed())
        {
            ExitBubble();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Tavan kontrol ışınını çiz
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * ceilingCheckDistance);
    }

    private void RefreshBubbleVisualScale()
    {
        if (bubbleVisualObj == null)
            return;

        Bounds sizingBounds;
        if (!TryGetCharacterBounds(out sizingBounds))
        {
            bubbleVisualObj.transform.localPosition = bubbleLocalOffset;
            bubbleVisualObj.transform.localScale = bubbleScale;
            return;
        }

        Vector3 anchorWorldCenter = GetBubbleAnchorWorldCenter(sizingBounds);

        Vector3 localCenter = transform.InverseTransformPoint(anchorWorldCenter);
        bubbleVisualObj.transform.localPosition = localCenter + bubbleLocalOffset;

        float desiredWorldWidth = Mathf.Max(bubbleScale.x, sizingBounds.size.x + bubblePadding.x);
        float desiredWorldHeight = Mathf.Max(bubbleScale.y, sizingBounds.size.y + bubblePadding.y);

        Vector3 parentLossyScale = transform.lossyScale;
        float safeScaleX = Mathf.Abs(parentLossyScale.x) > 0.0001f ? Mathf.Abs(parentLossyScale.x) : 1f;
        float safeScaleY = Mathf.Abs(parentLossyScale.y) > 0.0001f ? Mathf.Abs(parentLossyScale.y) : 1f;

        Vector2 spriteWorldSize = GetBubbleSpriteWorldSize();
        float safeSpriteWidth = spriteWorldSize.x > 0.0001f ? spriteWorldSize.x : 1f;
        float safeSpriteHeight = spriteWorldSize.y > 0.0001f ? spriteWorldSize.y : 1f;

        float localWidth = desiredWorldWidth / (safeScaleX * safeSpriteWidth);
        float localHeight = desiredWorldHeight / (safeScaleY * safeSpriteHeight);

        bubbleVisualObj.transform.localScale = new Vector3(localWidth, localHeight, 1f);
        NormalizeBubbleSpritePivot();
    }

    private Vector3 GetBubbleAnchorWorldCenter(Bounds sizingBounds)
    {
        bool hasColliderBounds = TryGetColliderBounds(out Bounds colliderBounds);
        bool hasSpriteBounds = TryGetSpriteBounds(out Bounds spriteBounds);

        if (hasColliderBounds && hasSpriteBounds)
        {
            Vector3 combinedCenter = (colliderBounds.center + spriteBounds.center) * 0.5f;
            float loweredY = Mathf.Lerp(combinedCenter.y, sizingBounds.min.y + (sizingBounds.size.y * 0.42f), 0.65f);
            return new Vector3(combinedCenter.x, loweredY, combinedCenter.z);
        }

        if (hasColliderBounds)
        {
            float loweredY = colliderBounds.min.y + (colliderBounds.size.y * 0.45f);
            return new Vector3(colliderBounds.center.x, loweredY, colliderBounds.center.z);
        }

        if (hasSpriteBounds)
        {
            float loweredY = spriteBounds.min.y + (spriteBounds.size.y * 0.42f);
            return new Vector3(spriteBounds.center.x, loweredY, spriteBounds.center.z);
        }

        return sizingBounds.center;
    }

    private Vector2 GetBubbleSpriteWorldSize()
    {
        if (bubbleRenderer == null || bubbleRenderer.sprite == null)
            return Vector2.one;

        Bounds spriteBounds = bubbleRenderer.sprite.bounds;
        return spriteBounds.size;
    }

    private void NormalizeBubbleSpritePivot()
    {
        if (bubbleSpriteObj == null || bubbleRenderer == null || bubbleRenderer.sprite == null)
            return;

        bubbleSpriteObj.transform.localPosition = -bubbleRenderer.sprite.bounds.center;
    }

    private bool TryGetCharacterBounds(out Bounds bounds)
    {
        bool hasColliderBounds = TryGetColliderBounds(out Bounds colliderBounds);
        bool hasSpriteBounds = TryGetSpriteBounds(out Bounds spriteBounds);

        if (hasColliderBounds && hasSpriteBounds)
        {
            bounds = colliderBounds;
            bounds.Encapsulate(spriteBounds);
            return true;
        }

        if (hasColliderBounds)
        {
            bounds = colliderBounds;
            return true;
        }

        if (hasSpriteBounds)
        {
            bounds = spriteBounds;
            return true;
        }

        bounds = new Bounds(transform.position, Vector3.zero);
        return false;
    }

    private bool TryGetColliderBounds(out Bounds bounds)
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        bool hasBounds = false;
        bounds = new Bounds(transform.position, Vector3.zero);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D currentCollider = colliders[i];
            if (currentCollider == null || currentCollider.isTrigger)
                continue;

            if (!hasBounds)
            {
                bounds = currentCollider.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(currentCollider.bounds);
        }

        return hasBounds;
    }

    private bool TryGetSpriteBounds(out Bounds bounds)
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        bool hasBounds = false;
        bounds = new Bounds(transform.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer currentRenderer = renderers[i];
            if (currentRenderer == null || currentRenderer == bubbleRenderer)
                continue;

            if (!hasBounds)
            {
                bounds = currentRenderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(currentRenderer.bounds);
        }

        return hasBounds;
    }
}
