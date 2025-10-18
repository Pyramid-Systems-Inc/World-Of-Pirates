using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Manages dynamic ocean mesh tiles with LOD system
/// Creates infinite ocean effect by recycling tiles as camera moves
/// </summary>
[RequireComponent(typeof(OceanManager))]
public class OceanTileSystem : MonoBehaviour
{
    #region Tile Data Structures

    private struct TileKey : System.IEquatable<TileKey>
    {
        public int x;
        public int z;
        public int lod;

        public bool Equals(TileKey other)
        {
            return x == other.x && z == other.z && lod == other.lod;
        }

        public override int GetHashCode()
        {
            return (x * 397) ^ (z * 397) ^ (lod * 397);
        }
    }

    #endregion

    #region Inspector Fields

    [Header("Tile Settings")]
    [Tooltip("Vertices per tile edge (higher = smoother waves, but more expensive)")]
    public int baseTileResolution = 100;

    [Tooltip("World space size of each base tile")]
    public float baseTileSize = 100f;

    [Tooltip("Number of tiles in each direction from camera")]
    [Range(1, 5)]
    public int tileRadius = 2;

    [Header("LOD Settings")]
    [Tooltip("Number of LOD levels (0 = highest detail)")]
    [Range(1, 5)]
    public int lodLevels = 3;

    [Tooltip("Distance from camera for each LOD level")]
    public float[] lodDistances = new float[] { 150f, 300f, 600f };

    [Header("Optimization")]
    [Tooltip("Update tiles every N frames (1 = every frame)")]
    [Range(1, 10)]
    public int updateFrequency = 5;

    [Tooltip("Enable frustum culling for tiles")]
    public bool frustumCulling = true;

    [Header("Debug")]
    public bool showTileBounds = false;
    public bool logTileCreation = false;

    #endregion

    #region Private Fields

    private Dictionary<TileKey, GameObject> activeTiles = new Dictionary<TileKey, GameObject>();
    private Transform followTarget;
    private OceanManager oceanManager;

    private int frameCounter = 0;
    private Vector2Int lastCameraGridPos;

    // Mesh cache to avoid regenerating identical meshes
    private Dictionary<int, Mesh> meshCache = new Dictionary<int, Mesh>();

    #endregion

    #region Initialization

    void Start()
    {
        oceanManager = GetComponent<OceanManager>();

        // Follow main camera
        if (Camera.main != null)
        {
            followTarget = Camera.main.transform;
        }
        else
        {
            Debug.LogError("❌ No main camera found! Ocean tiles won't update.");
            enabled = false;
            return;
        }

        // Validate LOD distances
        if (lodDistances.Length != lodLevels)
        {
            System.Array.Resize(ref lodDistances, lodLevels);
            for (int i = 0; i < lodLevels; i++)
            {
                lodDistances[i] = 150f * (i + 1);
            }
        }

        // Generate initial tiles
        GenerateInitialTiles();

        Debug.Log($"🌊 Ocean Tile System initialized - {activeTiles.Count} tiles created");
    }

    #endregion

    #region Update Loop

    void Update()
    {
        if (followTarget == null) return;

        frameCounter++;
        if (frameCounter % updateFrequency != 0) return;

        UpdateTileSystem();
    }

    void UpdateTileSystem()
    {
        Vector2Int currentGridPos = WorldToGridPosition(followTarget.position, baseTileSize);

        // Only update if camera moved to a new tile
        if (currentGridPos != lastCameraGridPos)
        {
            RefreshTiles();
            lastCameraGridPos = currentGridPos;
        }
    }

    #endregion

    #region Tile Generation

    void GenerateInitialTiles()
    {
        Vector2Int centerGrid = WorldToGridPosition(followTarget.position, baseTileSize);

        for (int lod = 0; lod < lodLevels; lod++)
        {
            int lodTileSize = 1 << lod; // 1, 2, 4, 8...
            int lodRadius = Mathf.Max(1, tileRadius >> lod);

            for (int x = -lodRadius; x <= lodRadius; x++)
            {
                for (int z = -lodRadius; z <= lodRadius; z++)
                {
                    // Skip if this area is covered by higher detail LOD
                    if (lod > 0 && IsInnerLODRegion(x, z, lod))
                        continue;

                    int gridX = centerGrid.x + x * lodTileSize;
                    int gridZ = centerGrid.y + z * lodTileSize;

                    CreateTile(gridX, gridZ, lod);
                }
            }
        }
    }

    void RefreshTiles()
    {
        Vector2Int centerGrid = WorldToGridPosition(followTarget.position, baseTileSize);

        // Mark all tiles for potential removal
        HashSet<TileKey> tilesToKeep = new HashSet<TileKey>();

        // Determine which tiles should exist
        for (int lod = 0; lod < lodLevels; lod++)
        {
            int lodTileSize = 1 << lod;
            int lodRadius = Mathf.Max(1, tileRadius >> lod);

            for (int x = -lodRadius; x <= lodRadius; x++)
            {
                for (int z = -lodRadius; z <= lodRadius; z++)
                {
                    if (lod > 0 && IsInnerLODRegion(x, z, lod))
                        continue;

                    int gridX = centerGrid.x + x * lodTileSize;
                    int gridZ = centerGrid.y + z * lodTileSize;

                    TileKey key = new TileKey { x = gridX, z = gridZ, lod = lod };
                    tilesToKeep.Add(key);

                    // Create tile if it doesn't exist
                    if (!activeTiles.ContainsKey(key))
                    {
                        CreateTile(gridX, gridZ, lod);
                    }
                }
            }
        }

        // Remove tiles that are no longer needed
        List<TileKey> tilesToRemove = new List<TileKey>();
        foreach (var kvp in activeTiles)
        {
            if (!tilesToKeep.Contains(kvp.Key))
            {
                tilesToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in tilesToRemove)
        {
            DestroyTile(key);
        }
    }

    bool IsInnerLODRegion(int x, int z, int currentLOD)
    {
        // Check if this position is within the higher-detail LOD radius
        int innerRadius = tileRadius >> (currentLOD - 1);
        return Mathf.Abs(x) <= innerRadius && Mathf.Abs(z) <= innerRadius;
    }

    void CreateTile(int gridX, int gridZ, int lod)
    {
        TileKey key = new TileKey { x = gridX, z = gridZ, lod = lod };

        if (activeTiles.ContainsKey(key)) return;

        // Calculate tile properties
        int lodTileSize = 1 << lod; // 1, 2, 4, 8...
        float worldTileSize = baseTileSize * lodTileSize;
        int resolution = Mathf.Max(2, baseTileResolution >> lod);

        // Create GameObject
        GameObject tile = new GameObject($"OceanTile_L{lod}_{gridX}_{gridZ}");
        tile.transform.parent = transform;
        tile.transform.position = new Vector3(
            gridX * baseTileSize,
            0,
            gridZ * baseTileSize
        );

        // Get or create mesh
        Mesh mesh = GetOrCreateMesh(resolution, worldTileSize);

        // Add components
        MeshFilter mf = tile.AddComponent<MeshFilter>();
        MeshRenderer mr = tile.AddComponent<MeshRenderer>();

        mf.sharedMesh = mesh;
        mr.sharedMaterial = oceanManager.oceanMaterial;

        // Optimization: Set shadow casting
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;

        // Layer (set to Water layer if it exists)
        tile.layer = LayerMask.NameToLayer("Water") != -1 ? LayerMask.NameToLayer("Water") : 0;

        activeTiles[key] = tile;

        if (logTileCreation)
        {
            Debug.Log($"Created tile at grid ({gridX}, {gridZ}), LOD {lod}, Resolution {resolution}");
        }
    }

    void DestroyTile(TileKey key)
    {
        if (!activeTiles.ContainsKey(key)) return;

        GameObject tile = activeTiles[key];
        activeTiles.Remove(key);

        if (Application.isPlaying)
        {
            Destroy(tile);
        }
        else
        {
            DestroyImmediate(tile);
        }
    }

    #endregion

    #region Mesh Generation

    Mesh GetOrCreateMesh(int resolution, float size)
    {
        int cacheKey = (resolution << 16) | Mathf.RoundToInt(size);

        if (meshCache.ContainsKey(cacheKey))
        {
            return meshCache[cacheKey];
        }

        Mesh mesh = GeneratePlaneMesh(resolution, size);
        meshCache[cacheKey] = mesh;

        return mesh;
    }

    Mesh GeneratePlaneMesh(int resolution, float size)
    {
        Mesh mesh = new Mesh();
        mesh.name = $"OceanTile_{resolution}x{resolution}";

        // Use 32-bit indices for high-resolution meshes
        mesh.indexFormat = IndexFormat.UInt32;

        int vertexCount = (resolution + 1) * (resolution + 1);
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];

        float step = size / resolution;
        float halfSize = size * 0.5f;

        // Generate vertices
        int vertIndex = 0;
        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                float xPos = x * step - halfSize;
                float zPos = z * step - halfSize;

                vertices[vertIndex] = new Vector3(xPos, 0, zPos);
                uvs[vertIndex] = new Vector2((float)x / resolution, (float)z / resolution);
                normals[vertIndex] = Vector3.up;

                vertIndex++;
            }
        }

        // Generate triangles
        int[] triangles = new int[resolution * resolution * 6];
        int triIndex = 0;

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int bottomLeft = z * (resolution + 1) + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + (resolution + 1);
                int topRight = topLeft + 1;

                // First triangle
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = bottomRight;

                // Second triangle
                triangles[triIndex++] = bottomRight;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = topRight;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();

        return mesh;
    }

    #endregion

    #region Utilities

    Vector2Int WorldToGridPosition(Vector3 worldPos, float gridSize)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / gridSize),
            Mathf.FloorToInt(worldPos.z / gridSize)
        );
    }

    #endregion

    #region Debug

    void OnDrawGizmos()
    {
        if (!showTileBounds || activeTiles == null) return;

        foreach (var kvp in activeTiles)
        {
            Color lodColor = Color.Lerp(Color.green, Color.red, (float)kvp.Key.lod / lodLevels);
            Gizmos.color = lodColor;

            GameObject tile = kvp.Value;
            if (tile != null)
            {
                MeshFilter mf = tile.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    Gizmos.DrawWireCube(tile.transform.position, mf.sharedMesh.bounds.size);
                }
            }
        }
    }

    #endregion

    #region Cleanup

    void OnDestroy()
    {
        // Clear mesh cache
        foreach (var mesh in meshCache.Values)
        {
            if (mesh != null)
            {
                Destroy(mesh);
            }
        }
        meshCache.Clear();

        // Clear active tiles
        foreach (var tile in activeTiles.Values)
        {
            if (tile != null)
            {
                Destroy(tile);
            }
        }
        activeTiles.Clear();
    }

    #endregion
}