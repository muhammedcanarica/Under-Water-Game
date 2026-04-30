using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BubblePickup : MonoBehaviour
{
    [Tooltip("Balon alindiktan sonra belli bir sure sonra tekrar olussun mu?")]
    public bool respawnable = true;

    [Tooltip("Eger tekrar olusacaksa bekleme suresi")]
    public float respawnTime = 5f;

    [Header("Movement")]
    [Tooltip("Balonun yukari dogru hareket hizi")]
    [SerializeField] private float riseSpeed = 2f;

    [Tooltip("Baslangic noktasindan ne kadar yukari cikacagi")]
    [SerializeField] private float riseDistance = 3f;

    [Tooltip("En uste ulasinca tekrar alttan baslasin mi?")]
    [SerializeField] private bool loopFromBottom = true;

    [Header("Visual")]
    [SerializeField] private Sprite customSprite;
    [SerializeField] private Color visualColor = new Color(0.7f, 0.9f, 1f, 0.85f);
    [SerializeField] private Vector3 visualScale = new Vector3(0.9f, 0.9f, 1f);
    [SerializeField] private int sortingOrder = 14;

    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private Vector3 startPosition;
    private bool isCollected;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        startPosition = transform.position;

        // Pickup sadece trigger olarak calismali.
        col.isTrigger = true;

        EnsureVisual();
    }

    private void Update()
    {
        if (isCollected)
        {
            return;
        }

        transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

        if (!loopFromBottom)
        {
            return;
        }

        if (transform.position.y >= startPosition.y + riseDistance)
        {
            transform.position = startPosition;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
        {
            return;
        }

        PlayerBubbleElevator elevator = collision.GetComponent<PlayerBubbleElevator>();
        if (elevator == null || elevator.IsInBubble)
        {
            return;
        }

        isCollected = true;
        elevator.EnterBubble();

        if (respawnable)
        {
            StartCoroutine(RespawnRoutine());
            return;
        }

        Destroy(gameObject);
    }

    private IEnumerator RespawnRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        col.enabled = false;

        yield return new WaitForSeconds(respawnTime);

        transform.position = startPosition;
        isCollected = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        col.enabled = true;
    }

    private void EnsureVisual()
    {
        if (spriteRenderer == null)
        {
            GameObject visualObject = new GameObject("BubblePickupVisual");
            visualObject.transform.SetParent(transform, false);
            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localScale = visualScale;
            spriteRenderer = visualObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.color = visualColor;
        spriteRenderer.sortingOrder = sortingOrder;
        spriteRenderer.transform.localScale = visualScale;

        if (customSprite != null)
        {
            spriteRenderer.sprite = customSprite;
            return;
        }

        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = CreateBubbleSprite(128);
        }
    }

    private Sprite CreateBubbleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color transparent = new Color(0f, 0f, 0f, 0f);
        float radius = size * 0.5f;
        float center = radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                if (distance < radius - 6f)
                {
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, 0.12f));
                }
                else if (distance < radius)
                {
                    float alpha = (radius - distance) / 6f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, transparent);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
