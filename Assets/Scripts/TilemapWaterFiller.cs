using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapWaterFiller : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap waterTilemap;
    [SerializeField] private Tilemap fillAreaTilemap;
    [SerializeField] private TileBase waterTile;

    [Header("Timing")]
    [SerializeField] private float fillDelayPerRow = 0.15f;

    [Header("Behavior")]
    [SerializeField] private bool randomizeTilesInRow;
    [SerializeField] private bool clearFillAreaVisualOnStart = true;

    private readonly List<List<Vector3Int>> cachedRows = new();

    private Coroutine fillRoutine;
    private TilemapCollider2D waterCollider;

    public bool IsFilling { get; private set; }
    public bool IsFilled { get; private set; }

    private void Awake()
    {
        CacheFillAreaCells();
        ClearWaterTilemap();
        HideFillAreaVisualIfNeeded();
    }

    public void StartFilling()
    {
        if (IsFilling || IsFilled)
            return;

        if (!ValidateReferences())
            return;

        if (cachedRows.Count == 0)
            CacheFillAreaCells();

        if (cachedRows.Count == 0)
            return;

        fillRoutine = StartCoroutine(FillRoutine());
    }

    private IEnumerator FillRoutine()
    {
        IsFilling = true;
        IsFilled = false;

        foreach (List<Vector3Int> sourceRow in cachedRows)
        {
            List<Vector3Int> row = randomizeTilesInRow ? GetShuffledCopy(sourceRow) : sourceRow;

            foreach (Vector3Int cellPosition in row)
            {
                waterTilemap.SetTile(cellPosition, waterTile);
            }

            RefreshWaterCollider();

            if (fillDelayPerRow > 0f)
                yield return new WaitForSeconds(fillDelayPerRow);
        }

        IsFilling = false;
        IsFilled = true;
        fillRoutine = null;
    }

    private void CacheFillAreaCells()
    {
        cachedRows.Clear();

        if (fillAreaTilemap == null)
        {
            Debug.LogWarning($"[{nameof(TilemapWaterFiller)}] Fill area tilemap is not assigned on '{name}'.", this);
            return;
        }

        Dictionary<int, List<Vector3Int>> rowLookup = new();
        BoundsInt bounds = fillAreaTilemap.cellBounds;

        foreach (Vector3Int cellPosition in bounds.allPositionsWithin)
        {
            if (!fillAreaTilemap.HasTile(cellPosition))
                continue;

            if (!rowLookup.TryGetValue(cellPosition.y, out List<Vector3Int> row))
            {
                row = new List<Vector3Int>();
                rowLookup.Add(cellPosition.y, row);
            }

            row.Add(cellPosition);
        }

        List<int> rowKeys = new(rowLookup.Keys);
        rowKeys.Sort();

        foreach (int rowKey in rowKeys)
        {
            List<Vector3Int> row = rowLookup[rowKey];
            row.Sort((left, right) => left.x.CompareTo(right.x));
            cachedRows.Add(row);
        }
    }

    private void ClearWaterTilemap()
    {
        if (waterTilemap == null)
        {
            Debug.LogWarning($"[{nameof(TilemapWaterFiller)}] Water tilemap is not assigned on '{name}'.", this);
            return;
        }

        waterTilemap.ClearAllTiles();
        RefreshWaterCollider();
        IsFilling = false;
        IsFilled = false;
    }

    private void HideFillAreaVisualIfNeeded()
    {
        if (!clearFillAreaVisualOnStart || fillAreaTilemap == null)
            return;

        TilemapRenderer fillAreaRenderer = fillAreaTilemap.GetComponent<TilemapRenderer>();
        if (fillAreaRenderer != null)
            fillAreaRenderer.enabled = false;
    }

    private void RefreshWaterCollider()
    {
        if (waterTilemap == null)
            return;

        if (waterCollider == null)
            waterCollider = waterTilemap.GetComponent<TilemapCollider2D>();

        if (waterCollider != null)
            waterCollider.ProcessTilemapChanges();
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (fillAreaTilemap == null)
        {
            Debug.LogWarning($"[{nameof(TilemapWaterFiller)}] Fill area tilemap is not assigned on '{name}'.", this);
            isValid = false;
        }

        if (waterTilemap == null)
        {
            Debug.LogWarning($"[{nameof(TilemapWaterFiller)}] Water tilemap is not assigned on '{name}'.", this);
            isValid = false;
        }

        if (waterTile == null)
        {
            Debug.LogWarning($"[{nameof(TilemapWaterFiller)}] Water tile is not assigned on '{name}'.", this);
            isValid = false;
        }

        return isValid;
    }

    private List<Vector3Int> GetShuffledCopy(List<Vector3Int> sourceRow)
    {
        List<Vector3Int> shuffledRow = new(sourceRow);

        for (int i = shuffledRow.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (shuffledRow[i], shuffledRow[randomIndex]) = (shuffledRow[randomIndex], shuffledRow[i]);
        }

        return shuffledRow;
    }

    private void OnValidate()
    {
        fillDelayPerRow = Mathf.Max(0f, fillDelayPerRow);
    }
}
