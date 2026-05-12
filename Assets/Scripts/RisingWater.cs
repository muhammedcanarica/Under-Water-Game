using System.Collections;
using UnityEngine;

public class RisingWater : MonoBehaviour
{
    [SerializeField] private Transform waterVisual;
    [SerializeField] private BoxCollider2D waterCollider;
    [SerializeField] private float startHeight = 1f;
    [SerializeField] private float targetHeight = 6f;
    [SerializeField] private float bottomY;
    [SerializeField] private float riseSpeed = 1f;

    private Coroutine riseRoutine;

    private void Start()
    {
        SetHeight(startHeight);
    }

    public void StartRising()
    {
        if (riseRoutine != null)
            StopCoroutine(riseRoutine);

        riseRoutine = StartCoroutine(Rise());
    }

    private IEnumerator Rise()
    {
        float height = startHeight;

        while (height < targetHeight)
        {
            height = Mathf.MoveTowards(height, targetHeight, riseSpeed * Time.deltaTime);
            SetHeight(height);
            yield return null;
        }

        riseRoutine = null;
    }

    private void SetHeight(float height)
    {
        height = Mathf.Max(0f, height);
        float centerY = bottomY + height / 2f;

        if (waterCollider != null)
        {
            Vector2 size = waterCollider.size;
            size.y = height;
            waterCollider.size = size;

            Vector3 position = waterCollider.transform.position;
            position.y = centerY;
            waterCollider.transform.position = position;
        }

        if (waterVisual != null)
        {
            Vector3 scale = waterVisual.localScale;
            scale.y = height;
            waterVisual.localScale = scale;

            Vector3 position = waterVisual.position;
            position.y = centerY;
            waterVisual.position = position;
        }
    }
}
