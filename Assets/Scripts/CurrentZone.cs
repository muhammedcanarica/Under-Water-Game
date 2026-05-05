using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class CurrentZone : MonoBehaviour
{
    private enum DirectionSource
    {
        FlowDirection = 0,
        TransformRight = 1
    }

    [Header("Flow Settings")]
    [SerializeField] private DirectionSource directionSource = DirectionSource.FlowDirection;
    [SerializeField] private Vector2 flowDirection = Vector2.right;
    [SerializeField] private float flowForce = 3f;

    [Header("Particle Visual")]
    [SerializeField] private bool showParticles = true;
    [SerializeField] private Color particleColor = new Color(0.3f, 0.75f, 1f, 0.7f);
    [SerializeField] private float particleSpeed = 3f;
    [SerializeField] private int emissionRate = 25;
    [SerializeField] private float particleSize = 0.28f;
    [SerializeField] private float particleLifetime = 1.2f; // legacy; unused by BuildParticleSystem

    private Collider2D triggerCollider;
    private ParticleSystem flowParticles;

    // ──────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────

    private void Reset()
    {
        CacheCollider();
        NormalizeFlowDirection();
        EnsureTriggerCollider();
    }

    private void Awake()
    {
        CacheCollider();
        NormalizeFlowDirection();
        EnsureTriggerCollider();

        if (showParticles)
            BuildParticleSystem();
    }

    private void OnValidate()
    {
        CacheCollider();
        NormalizeFlowDirection();
        EnsureTriggerCollider();
        flowForce = Mathf.Max(0f, flowForce);
    }

    // ──────────────────────────────────────────────
    // Trigger — force
    // ──────────────────────────────────────────────

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Vector2 normalizedDirection = GetNormalizedFlowDirection();
        if (normalizedDirection == Vector2.zero || flowForce <= 0f)
            return;

        // Route through PlayerMovement so the force isn't overwritten by velocity smoothing
        PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();
        if (movement != null)
        {
            movement.AddExternalVelocity(normalizedDirection * flowForce);
            return;
        }

        // Fallback: direct velocity push (no PlayerMovement found)
        Rigidbody2D targetBody = other.attachedRigidbody;
        if (targetBody != null)
            targetBody.linearVelocity += normalizedDirection * flowForce * Time.fixedDeltaTime;
    }

    // ──────────────────────────────────────────────
    // Particle system — directional streaming
    // ──────────────────────────────────────────────

    private void BuildParticleSystem()
    {
        if (flowParticles != null)
        {
            Destroy(flowParticles.gameObject);
            flowParticles = null;
        }

        Collider2D col = triggerCollider != null ? triggerCollider : GetComponent<Collider2D>();
        Vector2 dir    = GetNormalizedFlowDirection();
        if (dir == Vector2.zero) return;

        // ── World-space zone size (accounts for transform scale) ──
        Vector2 worldSize;
        Vector2 worldCenter;
        if (col is BoxCollider2D box)
        {
            // size is in local units; multiply by lossy scale for world size
            worldSize   = new Vector2(box.size.x * Mathf.Abs(transform.lossyScale.x),
                                      box.size.y * Mathf.Abs(transform.lossyScale.y));
            // world-space center = transform pos + rotated offset (rotation ignored for simplicity)
            worldCenter = (Vector2)transform.position
                        + new Vector2(box.offset.x * transform.lossyScale.x,
                                      box.offset.y * transform.lossyScale.y);
        }
        else
        {
            Bounds wb   = col.bounds;
            worldSize   = wb.size;
            worldCenter = wb.center;
        }

        // "Along" = axis parallel to flow.  "Cross" = perpendicular.
        bool horizontal = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y);
        float zoneAlong = horizontal ? worldSize.x : worldSize.y;
        float zoneCross = horizontal ? worldSize.y : worldSize.x;

        // Lifetime so a particle crosses the full zone at particleSpeed
        float travelTime = zoneAlong / Mathf.Max(particleSpeed, 0.01f);

        // ── Spawn strip on the UPWIND edge ──
        float stripThick = Mathf.Max(zoneAlong * 0.05f, 0.1f);

        // Shape scale in WORLD units (SimulationSpace.World)
        Vector3 stripScale = horizontal
            ? new Vector3(stripThick, zoneCross, 0.05f)
            : new Vector3(zoneCross,  stripThick, 0.05f);

        // World-space position of the strip centre (upwind edge of zone)
        Vector3 spawnPos = new Vector3(
            worldCenter.x - dir.x * (zoneAlong * 0.5f - stripThick * 0.5f),
            worldCenter.y - dir.y * (zoneAlong * 0.5f - stripThick * 0.5f),
            0f);

        // ── Build GO ──
        // Place the GO at the spawn strip world position and ROTATE it to face flow direction
        // This lets startSpeed drive particles in the right direction — no velocityOverLifetime needed.
        GameObject psGO = new GameObject("CurrentFlowFX");
        psGO.transform.SetParent(transform, false);
        psGO.transform.position = spawnPos; // world-space upwind edge

        // Keep GO at world position, no rotation needed
        psGO.transform.rotation = Quaternion.identity;

        flowParticles = psGO.AddComponent<ParticleSystem>();
        var rend = psGO.GetComponent<ParticleSystemRenderer>();

        // ── Main ──
        var main = flowParticles.main;
        main.loop            = true;
        main.playOnAwake     = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(travelTime * 0.9f, travelTime * 1.15f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0f); // ForceOverLifetime drives movement
        main.startSize       = new ParticleSystem.MinMaxCurve(particleSize * 0.6f, particleSize);
        main.startColor      = new ParticleSystem.MinMaxGradient(
                                 new Color(particleColor.r, particleColor.g, particleColor.b, 0.2f),
                                 particleColor);
        main.gravityModifier = 0f;
        main.maxParticles    = 400;

        // ── Emission ──
        var emission = flowParticles.emission;
        emission.enabled      = true;
        emission.rateOverTime = emissionRate;

        // ── Shape: thin strip on the upwind edge (world-aligned, no rotation) ──
        var shape = flowParticles.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = stripScale;
        shape.position  = Vector3.zero;
        shape.rotation  = Vector3.zero;

        // ── ForceOverLifetime: constant world-space push in flow direction ──
        // Force = velocity / time so particles reach ~particleSpeed by mid-lifetime.
        // Using travelTime as the time reference gives a natural acceleration.
        float forceMagnitude = particleSpeed / (travelTime * 0.35f);
        var force = flowParticles.forceOverLifetime;
        force.enabled = true;
        force.space   = ParticleSystemSimulationSpace.World;
        force.x       = new ParticleSystem.MinMaxCurve(dir.x * forceMagnitude);
        force.y       = new ParticleSystem.MinMaxCurve(dir.y * forceMagnitude);
        force.z       = new ParticleSystem.MinMaxCurve(0f);

        // ── Size over lifetime: fade in → full → fade out ──
        var sol = flowParticles.sizeOverLifetime;
        sol.enabled = true;
        sol.size    = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f,    0f,  0f, 6f),
            new Keyframe(0.12f, 1f),
            new Keyframe(0.82f, 1f),
            new Keyframe(1f,    0f,  -6f, 0f)));

        // ── Color over lifetime: alpha fade ──
        var colt = flowParticles.colorOverLifetime;
        colt.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(particleColor.r, particleColor.g, particleColor.b), 0f),
                new GradientColorKey(new Color(particleColor.r, particleColor.g, particleColor.b), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f,              0f),
                new GradientAlphaKey(particleColor.a, 0.18f),
                new GradientAlphaKey(particleColor.a, 0.78f),
                new GradientAlphaKey(0f,              1f)
            });
        colt.color = new ParticleSystem.MinMaxGradient(g);

        // ── Renderer ──
        rend.renderMode       = ParticleSystemRenderMode.Billboard;
        rend.sortingLayerName = "Default";
        rend.sortingOrder     = 20; // above water tilemaps (order 0)

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                     ?? Shader.Find("Sprites/Default")
                     ?? Shader.Find("Particles/Standard Unlit");

        if (shader != null)
        {
            var mat = new Material(shader) { name = "CurrentFlowFX_Mat" };
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend",   0f);
            mat.SetFloat("_ZWrite",  0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            mat.color       = particleColor;
            rend.material   = mat;
        }

        flowParticles.Play();
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private Vector2 GetNormalizedFlowDirection()
    {
        Vector2 sourceDirection = directionSource == DirectionSource.TransformRight
            ? (Vector2)transform.right
            : flowDirection;

        return sourceDirection.sqrMagnitude > 0f ? sourceDirection.normalized : Vector2.zero;
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
        if (flowDirection.sqrMagnitude > 0f)
            flowDirection = flowDirection.normalized;
    }

    // ──────────────────────────────────────────────
    // Gizmos
    // ──────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Collider2D zoneCollider = triggerCollider != null ? triggerCollider : GetComponent<Collider2D>();
        if (zoneCollider == null) return;

        Bounds bounds = zoneCollider.bounds;
        Vector3 center = bounds.center;
        Vector3 direction = GetNormalizedFlowDirection();

        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.2f);
        Gizmos.DrawCube(center, bounds.size);

        if (direction == Vector3.zero) return;

        Gizmos.color = Color.cyan;
        Vector3 arrowEnd = center + (direction.normalized * Mathf.Max(bounds.extents.x, bounds.extents.y, 0.5f));
        Gizmos.DrawLine(center, arrowEnd);

        Vector3 headR = Quaternion.Euler(0f, 0f, 25f)  * -direction.normalized * 0.35f;
        Vector3 headL = Quaternion.Euler(0f, 0f, -25f) * -direction.normalized * 0.35f;
        Gizmos.DrawLine(arrowEnd, arrowEnd + headR);
        Gizmos.DrawLine(arrowEnd, arrowEnd + headL);
    }
}
