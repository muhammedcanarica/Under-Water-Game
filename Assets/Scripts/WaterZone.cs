using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class WaterZone : MonoBehaviour
{
    [Header("Splash Ayarlari")]
    [SerializeField] private bool spawnSplashOnTransition = true;
    [SerializeField] private int splashBurstMin = 10;
    [SerializeField] private int splashBurstMax = 16;
    [SerializeField] private float splashLifetimeMin = 0.2f;
    [SerializeField] private float splashLifetimeMax = 0.45f;
    [SerializeField] private float splashSpeedMin = 1.5f;
    [SerializeField] private float splashSpeedMax = 3.5f;
    [SerializeField] private float splashSizeMin = 0.08f;
    [SerializeField] private float splashSizeMax = 0.16f;
    [SerializeField] private float splashGravity = 0.35f;
    [SerializeField] private Color splashColor = new Color(0.42f, 0.74f, 1f, 0.92f);

    private BoxCollider2D waterCollider;
    private static Material runtimeSplashMaterial;
    private static Sprite runtimeSplashSprite;

    private void Awake()
    {
        waterCollider = GetComponent<BoxCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleWaterTransition(collision, PlayerMode.Water);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        HandleWaterTransition(collision, PlayerMode.Land);
    }

    private void HandleWaterTransition(Collider2D collision, PlayerMode targetMode)
    {
        if (!collision.CompareTag("Player"))
            return;

        PlayerController player = collision.GetComponent<PlayerController>();
        if (player == null || player.currentMode == targetMode)
            return;

        if (spawnSplashOnTransition)
        {
            Vector2 velocity = collision.attachedRigidbody != null
                ? collision.attachedRigidbody.linearVelocity
                : Vector2.zero;

            SpawnSplashEffect(GetSplashPosition(collision.bounds), velocity);
        }

        player.ApplyModeProperties(targetMode);
    }

    private Vector3 GetSplashPosition(Bounds actorBounds)
    {
        if (waterCollider == null)
            return actorBounds.center;

        Bounds waterBounds = waterCollider.bounds;

        float topDistance = Mathf.Abs(actorBounds.min.y - waterBounds.max.y);
        float bottomDistance = Mathf.Abs(actorBounds.max.y - waterBounds.min.y);
        float leftDistance = Mathf.Abs(actorBounds.max.x - waterBounds.min.x);
        float rightDistance = Mathf.Abs(actorBounds.min.x - waterBounds.max.x);

        float nearestDistance = topDistance;
        Vector3 splashPosition = new Vector3(
            Mathf.Clamp(actorBounds.center.x, waterBounds.min.x, waterBounds.max.x),
            waterBounds.max.y,
            actorBounds.center.z);

        if (bottomDistance < nearestDistance)
        {
            nearestDistance = bottomDistance;
            splashPosition = new Vector3(
                Mathf.Clamp(actorBounds.center.x, waterBounds.min.x, waterBounds.max.x),
                waterBounds.min.y,
                actorBounds.center.z);
        }

        if (leftDistance < nearestDistance)
        {
            nearestDistance = leftDistance;
            splashPosition = new Vector3(
                waterBounds.min.x,
                Mathf.Clamp(actorBounds.center.y, waterBounds.min.y, waterBounds.max.y),
                actorBounds.center.z);
        }

        if (rightDistance < nearestDistance)
        {
            splashPosition = new Vector3(
                waterBounds.max.x,
                Mathf.Clamp(actorBounds.center.y, waterBounds.min.y, waterBounds.max.y),
                actorBounds.center.z);
        }

        return splashPosition;
    }

    private void SpawnSplashEffect(Vector3 position, Vector2 playerVelocity)
    {
        GameObject splashFx = new GameObject("WaterSplashFx");
        splashFx.transform.position = position;

        ParticleSystem particleSystem = splashFx.AddComponent<ParticleSystem>();
        ParticleSystemRenderer particleRenderer = splashFx.GetComponent<ParticleSystemRenderer>();
        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        float velocityBoost = Mathf.Clamp(playerVelocity.magnitude * 0.15f, 0f, 1.25f);

        var main = particleSystem.main;
        main.duration = 0.45f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(splashLifetimeMin, splashLifetimeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(splashSpeedMin + velocityBoost, splashSpeedMax + velocityBoost);
        main.startSize = new ParticleSystem.MinMaxCurve(splashSizeMin, splashSizeMax);
        main.startColor = splashColor;
        main.gravityModifier = splashGravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = splashBurstMax;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)splashBurstMin, (short)splashBurstMax)
        });

        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.12f;
        shape.radiusThickness = 1f;

        var textureSheetAnimation = particleSystem.textureSheetAnimation;
        textureSheetAnimation.enabled = true;
        textureSheetAnimation.mode = ParticleSystemAnimationMode.Sprites;

        Sprite splashSprite = GetSplashSprite();
        if (splashSprite != null)
        {
            textureSheetAnimation.AddSprite(splashSprite);
        }

        var velocityOverLifetime = particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        ParticleSystem.MinMaxCurve velocityX = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
        ParticleSystem.MinMaxCurve velocityY = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
        ParticleSystem.MinMaxCurve velocityZ = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocityX.mode = ParticleSystemCurveMode.TwoConstants;
        velocityY.mode = ParticleSystemCurveMode.TwoConstants;
        velocityZ.mode = ParticleSystemCurveMode.TwoConstants;
        velocityOverLifetime.x = velocityX;
        velocityOverLifetime.y = velocityY;
        velocityOverLifetime.z = velocityZ;

        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.6f);
        sizeCurve.AddKey(0.35f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.64f, 0.86f, 1f), 0f),
                new GradientColorKey(new Color(0.28f, 0.56f, 0.92f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.92f, 0f),
                new GradientAlphaKey(0.4f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        if (particleRenderer != null)
        {
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sortingOrder = 6;
            particleRenderer.material = GetSplashMaterial();
        }

        particleSystem.Play();
        Destroy(splashFx, main.duration + splashLifetimeMax + 0.2f);
    }

    private static Material GetSplashMaterial()
    {
        if (runtimeSplashMaterial != null)
            return runtimeSplashMaterial;

        ParticleSystemRenderer sourceRenderer = FindExistingSplashRenderer();
        if (sourceRenderer != null && sourceRenderer.sharedMaterial != null)
        {
            runtimeSplashMaterial = sourceRenderer.sharedMaterial;
            return runtimeSplashMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");

        if (shader == null)
            return null;

        runtimeSplashMaterial = new Material(shader)
        {
            name = "RuntimeWaterSplashMaterial"
        };

        return runtimeSplashMaterial;
    }

    private static Sprite GetSplashSprite()
    {
        if (runtimeSplashSprite != null)
            return runtimeSplashSprite;

        ParticleSystem sourceParticleSystem = Object.FindFirstObjectByType<MovementParticleController>()?.GetComponent<ParticleSystem>();
        if (sourceParticleSystem == null)
            sourceParticleSystem = GameObject.Find("Particle System")?.GetComponent<ParticleSystem>();

        if (sourceParticleSystem == null)
            return null;

        var textureSheetAnimation = sourceParticleSystem.textureSheetAnimation;
        if (!textureSheetAnimation.enabled || textureSheetAnimation.spriteCount == 0)
            return null;

        runtimeSplashSprite = textureSheetAnimation.GetSprite(0);
        return runtimeSplashSprite;
    }

    private static ParticleSystemRenderer FindExistingSplashRenderer()
    {
        ParticleSystemRenderer sourceRenderer = Object.FindFirstObjectByType<MovementParticleController>()?.GetComponent<ParticleSystemRenderer>();
        if (sourceRenderer != null)
            return sourceRenderer;

        GameObject particleObject = GameObject.Find("Particle System");
        if (particleObject == null)
            return null;

        return particleObject.GetComponent<ParticleSystemRenderer>();
    }
}
