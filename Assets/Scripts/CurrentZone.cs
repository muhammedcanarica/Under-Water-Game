using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class CurrentZone : MonoBehaviour
{
    private enum DirectionSource
    {
        FlowDirection = 0,
        TransformRight = 1,
        CardinalDirection = 2
    }

    private enum CardinalDirection
    {
        Right = 0,
        Left = 1,
        Up = 2,
        Down = 3
    }

    [Header("Flow Settings")]
    [SerializeField] private DirectionSource directionSource = DirectionSource.FlowDirection;
    [FormerlySerializedAs("flowDirection")]
    public Vector2 direction = Vector2.right;
    [SerializeField] private CardinalDirection inspectorDirection = CardinalDirection.Right;
    [FormerlySerializedAs("flowForce")]
    public float force = 3f;
    public bool affectOnlyInWater;
    [Tooltip("How quickly the push direction eases into a newly set direction. Use 0 for instant force reversal.")]
    public float directionBlendTime = 0.08f;

    [Header("Particle Visual")]
    public Transform visualArrowParent;
    [SerializeField] private bool showParticles = true;
    [SerializeField] private Color particleColor = new Color(0.3f, 0.75f, 1f, 0.7f);
    [SerializeField] private float particleSpeed = 3f;
    [SerializeField] private int emissionRate = 25;
    [SerializeField] private float particleSize = 0.28f;
    [SerializeField] private float particleLifetime = 1.2f; // legacy; unused by BuildParticleSystem

    private Collider2D triggerCollider;
    private ParticleSystem flowParticles;
    private Vector2 lastParticleDirection;
    private Vector3 lastParticleBoundsCenter;
    private Vector3 lastParticleBoundsSize;
    private bool particleStateCached;
    private bool lastShowParticles;
    private float lastParticleSpeed;
    private int lastEmissionRate;
    private float lastParticleSize;
    private Color lastParticleColor;
    private Vector2 appliedDirection;
    private Vector2 appliedDirectionVelocity;
    private bool appliedDirectionInitialized;

    private void Reset()
    {
        CacheCollider();
        NormalizeFlowDirection();
        SnapAppliedDirection();
        EnsureTriggerCollider();
        UpdateVisualDirection();
    }

    private void Awake()
    {
        CacheCollider();
        NormalizeFlowDirection();
        SnapAppliedDirection();
        EnsureTriggerCollider();
        RefreshParticleSystem(true);
        UpdateVisualDirection();
    }

    private void OnValidate()
    {
        CacheCollider();
        NormalizeFlowDirection();
        EnsureTriggerCollider();
        force = Mathf.Max(0f, force);
        directionBlendTime = Mathf.Max(0f, directionBlendTime);
        particleStateCached = false;
        UpdateVisualDirection();
    }

    private void LateUpdate()
    {
        UpdateAppliedDirection(Time.deltaTime);
        RefreshParticleSystem();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        UpdateAppliedDirection(Time.fixedDeltaTime);

        Vector2 normalizedDirection = GetAppliedFlowDirection();
        if (normalizedDirection == Vector2.zero || force <= 0f)
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (affectOnlyInWater && (player == null || player.currentMode != PlayerMode.Water))
            return;

        PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();
        if (movement != null)
        {
            movement.AddExternalVelocity(normalizedDirection * force);
            return;
        }

        Rigidbody2D targetBody = other.attachedRigidbody;
        if (targetBody != null)
            targetBody.linearVelocity += normalizedDirection * force * Time.fixedDeltaTime;
    }

    public void SetDirection(Vector2 newDirection)
    {
        directionSource = DirectionSource.FlowDirection;
        direction = newDirection.sqrMagnitude > 0f ? newDirection.normalized : Vector2.zero;
        InitializeAppliedDirectionIfNeeded();
        if (!Application.isPlaying || directionBlendTime <= 0f)
            SnapAppliedDirection();

        particleStateCached = false;
        RefreshParticleSystem(true);
        UpdateVisualDirection();
    }

    public void ReverseDirection()
    {
        Vector2 normalizedDirection = GetNormalizedFlowDirection();
        if (normalizedDirection == Vector2.zero)
            normalizedDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;

        SetDirection(-normalizedDirection);
    }

    public void SetForce(float newForce)
    {
        force = Mathf.Max(0f, newForce);
    }

    private void BuildParticleSystem(Bounds zoneBounds, Vector2 dir)
    {
        DestroyFlowParticles();

        Vector2 worldSize = zoneBounds.size;
        Vector2 worldCenter = zoneBounds.center;

        bool horizontal = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y);
        float zoneAlong = horizontal ? worldSize.x : worldSize.y;
        float zoneCross = horizontal ? worldSize.y : worldSize.x;
        float travelTime = Mathf.Max(0.1f, zoneAlong / Mathf.Max(particleSpeed, 0.01f));
        float stripThick = Mathf.Max(zoneAlong * 0.05f, 0.1f);

        Vector3 stripScale = horizontal
            ? new Vector3(stripThick, zoneCross, 0.05f)
            : new Vector3(zoneCross, stripThick, 0.05f);

        Vector3 spawnPos = new Vector3(
            worldCenter.x - dir.x * (zoneAlong * 0.5f - stripThick * 0.5f),
            worldCenter.y - dir.y * (zoneAlong * 0.5f - stripThick * 0.5f),
            0f);

        GameObject psGO = new GameObject("CurrentFlowFX");
        psGO.transform.SetParent(transform, false);
        psGO.transform.position = spawnPos;
        psGO.transform.rotation = Quaternion.identity;

        flowParticles = psGO.AddComponent<ParticleSystem>();
        var rend = psGO.GetComponent<ParticleSystemRenderer>();

        var main = flowParticles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(travelTime * 0.9f, travelTime * 1.15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f);
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.6f, particleSize);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(particleColor.r, particleColor.g, particleColor.b, 0.2f),
            particleColor);
        main.gravityModifier = 0f;
        main.maxParticles = 400;

        var emission = flowParticles.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        var shape = flowParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = stripScale;
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;

        var velocity = flowParticles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(dir.x * particleSpeed);
        velocity.y = new ParticleSystem.MinMaxCurve(dir.y * particleSpeed);
        velocity.z = new ParticleSystem.MinMaxCurve(0f);

        var force = flowParticles.forceOverLifetime;
        force.enabled = false;

        var sol = flowParticles.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 6f),
            new Keyframe(0.12f, 1f),
            new Keyframe(0.82f, 1f),
            new Keyframe(1f, 0f, -6f, 0f)));

        var colt = flowParticles.colorOverLifetime;
        colt.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(particleColor.r, particleColor.g, particleColor.b), 0f),
                new GradientColorKey(new Color(particleColor.r, particleColor.g, particleColor.b), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(particleColor.a, 0.18f),
                new GradientAlphaKey(particleColor.a, 0.78f),
                new GradientAlphaKey(0f, 1f)
            });
        colt.color = new ParticleSystem.MinMaxGradient(gradient);

        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sortingLayerName = "Default";
        rend.sortingOrder = 20;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                     ?? Shader.Find("Sprites/Default")
                     ?? Shader.Find("Particles/Standard Unlit");

        if (shader != null)
        {
            var mat = new Material(shader) { name = "CurrentFlowFX_Mat" };
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            mat.color = particleColor;
            rend.material = mat;
        }

        flowParticles.Play();
    }

    private void RefreshParticleSystem(bool forceRebuild = false)
    {
        if (!showParticles)
        {
            DestroyFlowParticles();
            CacheParticleState(Vector2.zero, default, false);
            return;
        }

        Collider2D col = triggerCollider != null ? triggerCollider : GetComponent<Collider2D>();
        Vector2 dir = GetNormalizedFlowDirection();
        if (col == null || dir == Vector2.zero)
        {
            DestroyFlowParticles();
            CacheParticleState(dir, default, false);
            return;
        }

        Bounds bounds = col.bounds;
        if (!forceRebuild && !ParticleSystemNeedsRefresh(dir, bounds))
        {
            return;
        }

        BuildParticleSystem(bounds, dir);
        CacheParticleState(dir, bounds, true);
        transform.hasChanged = false;
    }

    private bool ParticleSystemNeedsRefresh(Vector2 dir, Bounds bounds)
    {
        if (flowParticles == null || !particleStateCached)
            return true;

        if (transform.hasChanged || lastShowParticles != showParticles)
            return true;

        if ((lastParticleDirection - dir).sqrMagnitude > 0.0001f)
            return true;

        if ((lastParticleBoundsCenter - bounds.center).sqrMagnitude > 0.0001f)
            return true;

        if ((lastParticleBoundsSize - bounds.size).sqrMagnitude > 0.0001f)
            return true;

        if (!Mathf.Approximately(lastParticleSpeed, particleSpeed))
            return true;

        if (lastEmissionRate != emissionRate)
            return true;

        if (!Mathf.Approximately(lastParticleSize, particleSize))
            return true;

        return lastParticleColor != particleColor;
    }

    private void CacheParticleState(Vector2 dir, Bounds bounds, bool particlesVisible)
    {
        particleStateCached = true;
        lastParticleDirection = dir;
        lastParticleBoundsCenter = bounds.center;
        lastParticleBoundsSize = bounds.size;
        lastShowParticles = particlesVisible && showParticles;
        lastParticleSpeed = particleSpeed;
        lastEmissionRate = emissionRate;
        lastParticleSize = particleSize;
        lastParticleColor = particleColor;
    }

    private void DestroyFlowParticles()
    {
        if (flowParticles == null)
            return;

        if (Application.isPlaying)
            Destroy(flowParticles.gameObject);
        else
            DestroyImmediate(flowParticles.gameObject);

        flowParticles = null;
    }

    private Vector2 GetNormalizedFlowDirection()
    {
        Vector2 sourceDirection = directionSource switch
        {
            DirectionSource.TransformRight => (Vector2)transform.right,
            DirectionSource.CardinalDirection => GetCardinalDirectionVector(),
            _ => direction
        };

        return sourceDirection.sqrMagnitude > 0f ? sourceDirection.normalized : Vector2.zero;
    }

    private Vector2 GetAppliedFlowDirection()
    {
        InitializeAppliedDirectionIfNeeded();
        return appliedDirection.sqrMagnitude > 0f ? appliedDirection.normalized : Vector2.zero;
    }

    private Vector2 GetCardinalDirectionVector()
    {
        return inspectorDirection switch
        {
            CardinalDirection.Left => Vector2.left,
            CardinalDirection.Up => Vector2.up,
            CardinalDirection.Down => Vector2.down,
            _ => Vector2.right
        };
    }

    private void CacheCollider()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider2D>();
    }

    private void EnsureTriggerCollider()
    {
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void NormalizeFlowDirection()
    {
        if (direction.sqrMagnitude > 0f)
            direction = direction.normalized;
    }

    private void InitializeAppliedDirectionIfNeeded()
    {
        if (appliedDirectionInitialized)
            return;

        SnapAppliedDirection();
    }

    private void SnapAppliedDirection()
    {
        appliedDirection = GetNormalizedFlowDirection();
        appliedDirectionVelocity = Vector2.zero;
        appliedDirectionInitialized = true;
    }

    private void UpdateAppliedDirection(float deltaTime)
    {
        InitializeAppliedDirectionIfNeeded();

        Vector2 targetDirection = GetNormalizedFlowDirection();
        if (directionBlendTime <= 0f || deltaTime <= 0f)
        {
            appliedDirection = targetDirection;
            appliedDirectionVelocity = Vector2.zero;
            return;
        }

        appliedDirection = Vector2.SmoothDamp(
            appliedDirection,
            targetDirection,
            ref appliedDirectionVelocity,
            directionBlendTime,
            Mathf.Infinity,
            deltaTime);

        if ((appliedDirection - targetDirection).sqrMagnitude <= 0.0001f)
        {
            appliedDirection = targetDirection;
            appliedDirectionVelocity = Vector2.zero;
        }
    }

    private void UpdateVisualDirection()
    {
        if (visualArrowParent == null)
            return;

        Vector2 normalizedDirection = GetNormalizedFlowDirection();
        if (Mathf.Abs(normalizedDirection.x) <= 0.0001f)
            return;

        Vector3 scale = visualArrowParent.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(normalizedDirection.x);
        visualArrowParent.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D zoneCollider = triggerCollider != null ? triggerCollider : GetComponent<Collider2D>();
        if (zoneCollider == null)
            return;

        Bounds bounds = zoneCollider.bounds;
        Vector3 center = bounds.center;
        Vector3 direction = GetNormalizedFlowDirection();

        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.2f);
        Gizmos.DrawCube(center, bounds.size);

        if (direction == Vector3.zero)
            return;

        Gizmos.color = Color.cyan;
        Vector3 arrowEnd = center + (direction.normalized * Mathf.Max(bounds.extents.x, bounds.extents.y, 0.5f));
        Gizmos.DrawLine(center, arrowEnd);

        Vector3 headR = Quaternion.Euler(0f, 0f, 25f) * -direction.normalized * 0.35f;
        Vector3 headL = Quaternion.Euler(0f, 0f, -25f) * -direction.normalized * 0.35f;
        Gizmos.DrawLine(arrowEnd, arrowEnd + headR);
        Gizmos.DrawLine(arrowEnd, arrowEnd + headL);
    }
}
