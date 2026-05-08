using UnityEngine;

[DisallowMultipleComponent]
public class FloatingReward : MonoBehaviour
{
    [Header("Floating")]
    [SerializeField] private bool useLocalPosition = true;
    [SerializeField] private float amplitude = 0.15f;
    [SerializeField] private float frequency = 1.5f;
    [SerializeField] private bool randomizeStartOffset = true;

    private Vector3 startPosition;
    private float phaseOffset;

    private void Awake()
    {
        startPosition = useLocalPosition ? transform.localPosition : transform.position;
        phaseOffset = randomizeStartOffset ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    private void Update()
    {
        float offsetY = Mathf.Sin(Time.time * frequency + phaseOffset) * amplitude;
        Vector3 nextPosition = startPosition + Vector3.up * offsetY;

        if (useLocalPosition)
        {
            transform.localPosition = nextPosition;
            return;
        }

        transform.position = nextPosition;
    }
}
