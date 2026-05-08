using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class RewardCollectible : MonoBehaviour
{
    [Header("Reward")]
    [SerializeField] private int rewardAmount = 1;
    [SerializeField] private RewardManager rewardManager;

    [Header("Feedback")]
    [SerializeField] private ParticleSystem collectParticlePrefab;
    [SerializeField] private AudioClip collectSound;
    [SerializeField] [Range(0f, 1f)] private float collectSoundVolume = 1f;

    private Collider2D triggerCollider;
    private bool isCollected;

    private void Reset()
    {
        triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        if (rewardManager == null)
        {
            rewardManager = RewardManager.Instance != null
                ? RewardManager.Instance
                : FindFirstObjectByType<RewardManager>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected || !other.CompareTag("Player"))
        {
            return;
        }

        Collect();
    }

    public void Collect()
    {
        if (isCollected)
        {
            return;
        }

        isCollected = true;

        if (rewardManager == null)
        {
            rewardManager = RewardManager.Instance != null
                ? RewardManager.Instance
                : FindFirstObjectByType<RewardManager>();
        }

        rewardManager?.AddReward(rewardAmount);
        SpawnCollectParticle();
        PlayCollectSound();
        Destroy(gameObject);
    }

    private void SpawnCollectParticle()
    {
        if (collectParticlePrefab == null)
        {
            return;
        }

        ParticleSystem spawnedParticle = Instantiate(collectParticlePrefab, transform.position, Quaternion.identity);
        ParticleSystem.MainModule main = spawnedParticle.main;
        float lifetime = main.duration + main.startLifetime.constantMax;
        Destroy(spawnedParticle.gameObject, lifetime);
    }

    private void PlayCollectSound()
    {
        if (collectSound == null)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(collectSound, transform.position, collectSoundVolume);
    }
}
