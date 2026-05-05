using Unity.Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class Room : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCameraBase virtualCamera;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool fitCameraToRoom = true;
    [SerializeField] private Vector2 cameraPadding = Vector2.zero;
    [SerializeField] private float minimumOrthographicSize = 0.1f;
    [SerializeField] private Color activeGizmoFillColor = new Color(1f, 0.9f, 0.1f, 0.16f);
    [SerializeField] private Color activeGizmoOutlineColor = new Color(1f, 0.9f, 0.1f, 1f);
    [SerializeField] private Color inactiveGizmoFillColor = new Color(1f, 0.15f, 0.15f, 0.12f);
    [SerializeField] private Color inactiveGizmoOutlineColor = new Color(1f, 0.2f, 0.2f, 0.95f);
    [SerializeField] private Color cameraFrameColor = new Color(0.3f, 1f, 0.3f, 0.95f);
    [SerializeField] private Color cameraCenterColor = new Color(0.3f, 1f, 0.3f, 0.75f);

    public CinemachineVirtualCameraBase VirtualCamera => virtualCamera;

    private void Awake()
    {
        if (virtualCamera == null)
        {
            Debug.LogWarning($"[{nameof(Room)}] No virtual camera assigned on '{name}'.", this);
        }
    }

    private void Reset()
    {
        EnsureTriggerCollider();
    }

    private void OnValidate()
    {
        EnsureTriggerCollider();
        FitCameraToRoom(Camera.main, GetCurrentScreenAspect());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        CameraManager.Instance?.ActivateRoom(this);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        CameraManager.Instance?.ActivateRoom(this);
    }

    public void SetPriority(int priority)
    {
        if (virtualCamera == null)
        {
            return;
        }

        PrioritySettings prioritySettings = virtualCamera.Priority;
        prioritySettings.Enabled = true;
        prioritySettings.Value = priority;
        virtualCamera.Priority = prioritySettings;
    }

    public void FitCameraToRoom(Camera outputCamera, float screenAspect)
    {
        if (!fitCameraToRoom || virtualCamera == null)
        {
            ResetOutputCameraViewport(outputCamera);
            return;
        }

        CinemachineCamera cinemachineCamera = virtualCamera as CinemachineCamera;
        if (cinemachineCamera == null)
        {
            ResetOutputCameraViewport(outputCamera);
            return;
        }

        Bounds bounds = GetRoomBounds();
        float paddedWidth = bounds.size.x + (cameraPadding.x * 2f);
        float paddedHeight = bounds.size.y + (cameraPadding.y * 2f);
        float requiredHeight = Mathf.Max(minimumOrthographicSize * 2f, paddedHeight);
        float targetAspect = requiredHeight > 0.0001f ? paddedWidth / requiredHeight : (16f / 9f);

        LensSettings lens = cinemachineCamera.Lens;
        lens.OrthographicSize = requiredHeight * 0.5f;
        cinemachineCamera.Lens = lens;

        AlignCameraToRoomCenter(bounds.center);

        if (outputCamera != null)
            outputCamera.rect = CalculateViewportRect(screenAspect, targetAspect);
    }

    private void EnsureTriggerCollider()
    {
        BoxCollider2D roomBounds = GetComponent<BoxCollider2D>();
        roomBounds.isTrigger = true;
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D roomBounds = GetComponent<BoxCollider2D>();
        if (roomBounds == null)
        {
            return;
        }

        bool isActiveRoom = CameraManager.Instance != null && CameraManager.Instance.CurrentRoom == this;
        Color fillColor = isActiveRoom ? activeGizmoFillColor : inactiveGizmoFillColor;
        Color outlineColor = isActiveRoom ? activeGizmoOutlineColor : inactiveGizmoOutlineColor;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        Vector3 colliderCenter = roomBounds.offset;
        Vector3 colliderSize = roomBounds.size;
        float cornerSize = Mathf.Clamp(Mathf.Min(colliderSize.x, colliderSize.y) * 0.12f, 0.35f, 2.5f);
        float halfWidth = colliderSize.x * 0.5f;
        float halfHeight = colliderSize.y * 0.5f;

        Gizmos.color = fillColor;
        Gizmos.DrawCube(colliderCenter, colliderSize);

        Gizmos.color = outlineColor;
        Gizmos.DrawWireCube(colliderCenter, colliderSize);

        DrawCorner(colliderCenter, new Vector3(-halfWidth, halfHeight, 0f), Vector3.right, Vector3.down, cornerSize);
        DrawCorner(colliderCenter, new Vector3(halfWidth, halfHeight, 0f), Vector3.left, Vector3.down, cornerSize);
        DrawCorner(colliderCenter, new Vector3(-halfWidth, -halfHeight, 0f), Vector3.right, Vector3.up, cornerSize);
        DrawCorner(colliderCenter, new Vector3(halfWidth, -halfHeight, 0f), Vector3.left, Vector3.up, cornerSize);

        Gizmos.matrix = previousMatrix;

        DrawCameraGizmo();
    }

    private void DrawCorner(Vector3 center, Vector3 cornerOffset, Vector3 horizontalDirection, Vector3 verticalDirection, float length)
    {
        Vector3 corner = center + cornerOffset;
        Gizmos.DrawLine(corner, corner + horizontalDirection * length);
        Gizmos.DrawLine(corner, corner + verticalDirection * length);
    }

    private void DrawCameraGizmo()
    {
        if (virtualCamera == null)
        {
            return;
        }

        CinemachineCamera cinemachineCamera = virtualCamera as CinemachineCamera;
        Camera mainCamera = Camera.main;
        if (cinemachineCamera == null || mainCamera == null || !mainCamera.orthographic)
        {
            return;
        }

        Transform cameraTransform = virtualCamera.transform;
        float halfHeight = cinemachineCamera.Lens.OrthographicSize;
        float halfWidth = halfHeight * GetViewportAspect(mainCamera);
        Vector3 center = cameraTransform.position;
        Vector3 size = new Vector3(halfWidth * 2f, halfHeight * 2f, 0f);
        float crossSize = Mathf.Clamp(Mathf.Min(size.x, size.y) * 0.08f, 0.2f, 1.25f);

        Gizmos.color = cameraFrameColor;
        Gizmos.DrawWireCube(center, size);

        Gizmos.color = cameraCenterColor;
        Gizmos.DrawLine(center + Vector3.left * crossSize, center + Vector3.right * crossSize);
        Gizmos.DrawLine(center + Vector3.up * crossSize, center + Vector3.down * crossSize);
        Gizmos.DrawLine(transform.position, center);
    }

    private Bounds GetRoomBounds()
    {
        BoxCollider2D roomBounds = GetComponent<BoxCollider2D>();
        if (roomBounds == null)
        {
            return new Bounds(transform.position, Vector3.zero);
        }

        return roomBounds.bounds;
    }

    private void AlignCameraToRoomCenter(Vector3 roomCenter)
    {
        Transform cameraTransform = virtualCamera.transform;
        Vector3 currentPosition = cameraTransform.position;
        cameraTransform.position = new Vector3(roomCenter.x, roomCenter.y, currentPosition.z);
    }

    private static void ResetOutputCameraViewport(Camera outputCamera)
    {
        if (outputCamera != null)
            outputCamera.rect = new Rect(0f, 0f, 1f, 1f);
    }

    private static Rect CalculateViewportRect(float screenAspect, float targetAspect)
    {
        float safeScreenAspect = screenAspect > 0.0001f ? screenAspect : (16f / 9f);
        float safeTargetAspect = targetAspect > 0.0001f ? targetAspect : safeScreenAspect;

        if (safeScreenAspect > safeTargetAspect)
        {
            float normalizedWidth = safeTargetAspect / safeScreenAspect;
            float xOffset = (1f - normalizedWidth) * 0.5f;
            return new Rect(xOffset, 0f, normalizedWidth, 1f);
        }

        if (safeScreenAspect < safeTargetAspect)
        {
            float normalizedHeight = safeScreenAspect / safeTargetAspect;
            float yOffset = (1f - normalizedHeight) * 0.5f;
            return new Rect(0f, yOffset, 1f, normalizedHeight);
        }

        return new Rect(0f, 0f, 1f, 1f);
    }

    private static float GetViewportAspect(Camera camera)
    {
        if (camera == null)
            return 16f / 9f;

        Rect rect = camera.rect;
        if (rect.height <= 0.0001f || Screen.height <= 0)
            return 16f / 9f;

        float screenAspect = (float)Screen.width / Screen.height;
        return screenAspect * (rect.width / rect.height);
    }

    private static float GetCurrentScreenAspect()
    {
        if (Screen.height <= 0)
            return 16f / 9f;

        return (float)Screen.width / Screen.height;
    }
}
