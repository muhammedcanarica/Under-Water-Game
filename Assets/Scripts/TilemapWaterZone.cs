using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(TilemapCollider2D))]
[RequireComponent(typeof(CompositeCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class TilemapWaterZone : MonoBehaviour
{
    [Header("Flow Settings")]
    [SerializeField] private Vector2 flowDirection = Vector2.zero;
    [SerializeField] private float flowForce = 0f;

    private readonly HashSet<Rigidbody2D> trackedPlayers = new();

    private TilemapCollider2D tilemapCollider;
    private CompositeCollider2D compositeCollider;
    private Rigidbody2D zoneBody;

    private void Reset()
    {
        CacheComponents();
        NormalizeFlowDirection();
        ConfigurePhysics();
    }

    private void Awake()
    {
        CacheComponents();
        NormalizeFlowDirection();
        ConfigurePhysics();
    }

    private void OnValidate()
    {
        CacheComponents();
        NormalizeFlowDirection();
        flowForce = Mathf.Max(0f, flowForce);
        ConfigurePhysics();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Rigidbody2D playerBody = other.attachedRigidbody;
        if (playerBody == null)
        {
            return;
        }

        trackedPlayers.Add(playerBody);
        SetPlayerMode(playerBody, PlayerMode.Water, forceInitialize: true);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Rigidbody2D playerBody = other.attachedRigidbody;
        if (playerBody == null)
        {
            return;
        }

        trackedPlayers.Add(playerBody);
        SetPlayerMode(playerBody, PlayerMode.Water, forceInitialize: true);
        ApplyFlow(playerBody);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Rigidbody2D playerBody = other.attachedRigidbody;
        if (playerBody == null)
        {
            return;
        }

        trackedPlayers.Remove(playerBody);
        SetPlayerMode(playerBody, PlayerMode.Land, forceInitialize: false);
    }

    private void OnDisable()
    {
        foreach (Rigidbody2D playerBody in trackedPlayers)
        {
            if (playerBody == null)
            {
                continue;
            }

            SetPlayerMode(playerBody, PlayerMode.Land, forceInitialize: true);
        }

        trackedPlayers.Clear();
    }

    private void ApplyFlow(Rigidbody2D playerBody)
    {
        if (flowDirection == Vector2.zero || flowForce <= 0f)
        {
            return;
        }

        Vector2 nextVelocity = playerBody.linearVelocity + (flowDirection * flowForce * Time.fixedDeltaTime);
        playerBody.linearVelocity = nextVelocity;
    }

    private void SetPlayerMode(Rigidbody2D playerBody, PlayerMode targetMode, bool forceInitialize)
    {
        PlayerController player = playerBody.GetComponent<PlayerController>();
        if (player == null)
        {
            return;
        }

        if (player.currentMode == targetMode)
        {
            return;
        }

        player.ApplyModeProperties(targetMode, forceInitialize);
    }

    private void CacheComponents()
    {
        if (tilemapCollider == null)
        {
            tilemapCollider = GetComponent<TilemapCollider2D>();
        }

        if (compositeCollider == null)
        {
            compositeCollider = GetComponent<CompositeCollider2D>();
        }

        if (zoneBody == null)
        {
            zoneBody = GetComponent<Rigidbody2D>();
        }
    }

    private void NormalizeFlowDirection()
    {
        if (flowDirection.sqrMagnitude > 0f)
        {
            flowDirection = flowDirection.normalized;
        }
    }

    private void ConfigurePhysics()
    {
        if (tilemapCollider != null)
        {
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            tilemapCollider.isTrigger = true;
        }

        if (compositeCollider != null)
        {
            compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;
            compositeCollider.isTrigger = true;
        }

        if (zoneBody != null)
        {
            zoneBody.bodyType = RigidbodyType2D.Static;
            zoneBody.simulated = true;
        }
    }
}
