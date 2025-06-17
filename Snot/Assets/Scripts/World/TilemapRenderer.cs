using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

// ScriptableObject to define tile properties
[CreateAssetMenu(fileName = "New Tile Data", menuName = "Tilemap/Tile Data")]
public class TileData : ScriptableObject
{
    [Header("Tile Properties")]
    public int tileId;
    public bool hasSolidCollision = true;
    public bool isOneWayPlatform = false;
    public PhysicsMaterial2D physicsMaterial;

    [Header("Visual")]
    public Color tintColor = Color.white;
    public bool flipX = false;
    public bool flipY = false;
}

// Main tilemap generator that creates a single mesh for the entire map
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TilemapRenderer : MonoBehaviour
{
    [Header("Tilemap Settings")]
    public Texture2D tilesheet;
    public int tilesPerRow = 10;
    public int tilesPerColumn = 10;
    public float tileSize = 1f;

    [Header("Map Data")]
    public int mapWidth = 20;
    public int mapHeight = 15;
    public int[] tileIds;

    [Header("Tile Data")]
    public TileData[] tileDataArray;

    [Header("Collision")]
    public bool generateCollision = true;
    public LayerMask collisionLayer = 1;

    [Header("Tile Painting (Editor Only)")]
    public bool enablePainting = true;
    public int selectedTileId = 1;
    public bool showGrid = true;
    public bool showTilePreview = true;

    // Components
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private PolygonCollider2D polygonCollider;

    // Mesh data
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<Color> colors = new List<Color>();

    void Start()
    {
        InitializeComponents();
        GenerateTilemap();
    }

    void InitializeComponents()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (generateCollision)
        {
            polygonCollider = GetComponent<PolygonCollider2D>();
            if (polygonCollider == null)
                polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
        }

        // Initialize tile IDs array if empty
        if (tileIds == null || tileIds.Length != mapWidth * mapHeight)
        {
            tileIds = new int[mapWidth * mapHeight];
        }
    }

    public void GenerateTilemap()
    {
        ClearMeshData();

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                int index = y * mapWidth + x;
                int tileId = tileIds[index];

                if (tileId > 0)
                {
                    AddTileToMesh(x, y, tileId);
                }
            }
        }

        CreateMesh();

        if (generateCollision)
        {
            GenerateCollision();
        }
    }

    void ClearMeshData()
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        colors.Clear();
    }

    void AddTileToMesh(int gridX, int gridY, int tileId)
    {
        Vector3 position = new Vector3(gridX * tileSize, gridY * tileSize, 0);
        Vector2[] tileUVs = GetTileUVs(tileId);

        Color tileColor = Color.white;
        if (tileId < tileDataArray.Length && tileDataArray[tileId] != null)
        {
            tileColor = tileDataArray[tileId].tintColor;
        }

        int vertexIndex = vertices.Count;
        vertices.Add(position);
        vertices.Add(position + Vector3.right * tileSize);
        vertices.Add(position + Vector3.up * tileSize);
        vertices.Add(position + Vector3.right * tileSize + Vector3.up * tileSize);

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);

        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);

        uvs.AddRange(tileUVs);

        colors.Add(tileColor);
        colors.Add(tileColor);
        colors.Add(tileColor);
        colors.Add(tileColor);
    }

    Vector2[] GetTileUVs(int tileId)
    {
        int tileIndex = tileId - 1;
        int tileX = tileIndex % tilesPerRow;
        int tileY = tilesPerColumn - 1 - (tileIndex / tilesPerRow);

        float uvWidth = 1f / tilesPerRow;
        float uvHeight = 1f / tilesPerColumn;

        float u = tileX * uvWidth;
        float v = tileY * uvHeight;

        return new Vector2[]
        {
            new Vector2(u, v),
            new Vector2(u + uvWidth, v),
            new Vector2(u, v + uvHeight),
            new Vector2(u + uvWidth, v + uvHeight)
        };
    }

    void CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Generated Tilemap";

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors = colors.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;

        if (meshRenderer.material.mainTexture != tilesheet)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.mainTexture = tilesheet;
            meshRenderer.material = mat;
        }
    }

    void GenerateCollision()
    {
        List<List<Vector2>> collisionPaths = new List<List<Vector2>>();

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                int index = y * mapWidth + x;
                int tileId = tileIds[index];

                if (tileId > 0 && ShouldGenerateCollision(tileId))
                {
                    List<Vector2> tileCollision = new List<Vector2>();

                    Vector2 bottomLeft = new Vector2(x * tileSize, y * tileSize);
                    Vector2 bottomRight = new Vector2((x + 1) * tileSize, y * tileSize);
                    Vector2 topRight = new Vector2((x + 1) * tileSize, (y + 1) * tileSize);
                    Vector2 topLeft = new Vector2(x * tileSize, (y + 1) * tileSize);

                    tileCollision.Add(bottomLeft);
                    tileCollision.Add(bottomRight);
                    tileCollision.Add(topRight);
                    tileCollision.Add(topLeft);

                    collisionPaths.Add(tileCollision);
                }
            }
        }

        if (collisionPaths.Count > 0)
        {
            polygonCollider.pathCount = collisionPaths.Count;
            for (int i = 0; i < collisionPaths.Count; i++)
            {
                polygonCollider.SetPath(i, collisionPaths[i].ToArray());
            }
        }
    }

    bool ShouldGenerateCollision(int tileId)
    {
        if (tileId < tileDataArray.Length && tileDataArray[tileId] != null)
        {
            return tileDataArray[tileId].hasSolidCollision;
        }
        return true;
    }

    public void SetTile(int x, int y, int tileId)
    {
        if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
        {
            int index = y * mapWidth + x;
            tileIds[index] = tileId;
        }
    }

    public int GetTile(int x, int y)
    {
        if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
        {
            int index = y * mapWidth + x;
            return tileIds[index];
        }
        return 0;
    }

    public Vector2Int WorldToTilePosition(Vector3 worldPosition)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        int x = Mathf.FloorToInt(localPos.x / tileSize);
        int y = Mathf.FloorToInt(localPos.y / tileSize);
        return new Vector2Int(x, y);
    }

    public Vector3 TileToWorldPosition(int x, int y)
    {
        Vector3 localPos = new Vector3(x * tileSize, y * tileSize, 0);
        return transform.TransformPoint(localPos);
    }

    public Rect GetTileBounds(int x, int y)
    {
        Vector3 worldPos = TileToWorldPosition(x, y);
        return new Rect(worldPos.x, worldPos.y, tileSize, tileSize);
    }
}

// Tile painter tool for Unity Editor
#if UNITY_EDITOR

[EditorTool("Tile Painter", typeof(TilemapRenderer))]
public class TilePainterTool : EditorTool
{
    private TilemapRenderer tilemap;
    private bool isPainting = false;
    private Vector2Int lastPaintedTile = new Vector2Int(-1, -1);

    public override void OnToolGUI(EditorWindow window)
    {
        tilemap = target as TilemapRenderer;
        if (tilemap == null) return;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Event e = Event.current;
        Vector3 mousePos = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
        Vector2Int tilePos = tilemap.WorldToTilePosition(mousePos);

        // Draw grid
        if (tilemap.showGrid)
        {
            DrawGrid();
        }

        // Draw tile preview
        if (tilemap.showTilePreview && IsValidTilePosition(tilePos))
        {
            DrawTilePreview(tilePos);
        }

        // Handle painting
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            isPainting = true;
            PaintTile(tilePos);
        }
        else if (e.type == EventType.MouseDrag && isPainting)
        {
            if (tilePos != lastPaintedTile)
            {
                PaintTile(tilePos);
            }
        }
        else if (e.type == EventType.MouseUp)
        {
            isPainting = false;
            lastPaintedTile = new Vector2Int(-1, -1);
        }

        // Handle erasing with right click
        if (e.type == EventType.MouseDown && e.button == 1)
        {
            EraseTile(tilePos);
        }

        // Handle eyedropper with middle click
        if (e.type == EventType.MouseDown && e.button == 2)
        {
            PickTile(tilePos);
        }
    }

    void DrawGrid()
    {
        Handles.color = new Color(1, 1, 1, 0.3f);

        // Calculate visible area
        Camera sceneCamera = SceneView.currentDrawingSceneView.camera;
        Vector3 cameraPos = sceneCamera.transform.position;
        float cameraSize = sceneCamera.orthographicSize;

        int startX = Mathf.Max(0, Mathf.FloorToInt((cameraPos.x - cameraSize) / tilemap.tileSize));
        int endX = Mathf.Min(tilemap.mapWidth, Mathf.CeilToInt((cameraPos.x + cameraSize) / tilemap.tileSize));
        int startY = Mathf.Max(0, Mathf.FloorToInt((cameraPos.y - cameraSize) / tilemap.tileSize));
        int endY = Mathf.Min(tilemap.mapHeight, Mathf.CeilToInt((cameraPos.y + cameraSize) / tilemap.tileSize));

        // Draw vertical lines
        for (int x = startX; x <= endX; x++)
        {
            Vector3 start = tilemap.TileToWorldPosition(x, startY);
            Vector3 end = tilemap.TileToWorldPosition(x, endY);
            Handles.DrawLine(start, end);
        }

        // Draw horizontal lines
        for (int y = startY; y <= endY; y++)
        {
            Vector3 start = tilemap.TileToWorldPosition(startX, y);
            Vector3 end = tilemap.TileToWorldPosition(endX, y);
            Handles.DrawLine(start, end);
        }
    }

    void DrawTilePreview(Vector2Int tilePos)
    {
        Handles.color = new Color(0, 1, 0, 0.5f);
        Rect tileBounds = tilemap.GetTileBounds(tilePos.x, tilePos.y);

        Vector3[] corners = new Vector3[]
        {
            new Vector3(tileBounds.xMin, tileBounds.yMin, 0),
            new Vector3(tileBounds.xMax, tileBounds.yMin, 0),
            new Vector3(tileBounds.xMax, tileBounds.yMax, 0),
            new Vector3(tileBounds.xMin, tileBounds.yMax, 0)
        };

        Handles.DrawSolidRectangleWithOutline(corners, new Color(0, 1, 0, 0.2f), new Color(0, 1, 0, 0.8f));
    }

    void PaintTile(Vector2Int tilePos)
    {
        if (!IsValidTilePosition(tilePos)) return;

        Undo.RecordObject(tilemap, "Paint Tile");
        tilemap.SetTile(tilePos.x, tilePos.y, tilemap.selectedTileId);
        tilemap.GenerateTilemap();
        lastPaintedTile = tilePos;

        EditorUtility.SetDirty(tilemap);
    }

    void EraseTile(Vector2Int tilePos)
    {
        if (!IsValidTilePosition(tilePos)) return;

        Undo.RecordObject(tilemap, "Erase Tile");
        tilemap.SetTile(tilePos.x, tilePos.y, 0);
        tilemap.GenerateTilemap();

        EditorUtility.SetDirty(tilemap);
    }

    void PickTile(Vector2Int tilePos)
    {
        if (!IsValidTilePosition(tilePos)) return;

        int tileId = tilemap.GetTile(tilePos.x, tilePos.y);
        if (tileId > 0)
        {
            tilemap.selectedTileId = tileId;
        }
    }

    bool IsValidTilePosition(Vector2Int tilePos)
    {
        return tilePos.x >= 0 && tilePos.x < tilemap.mapWidth &&
               tilePos.y >= 0 && tilePos.y < tilemap.mapHeight;
    }
}

[CustomEditor(typeof(TilemapRenderer))]
public class TilemapRendererEditor : Editor
{
    private Vector2 scrollPosition;
    private int tilesPerRow = 8;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TilemapRenderer tilemap = (TilemapRenderer)target;

        EditorGUILayout.Space();

        // Tile Painter Section
        EditorGUILayout.LabelField("Tile Painter", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Activate Tile Painter Tool"))
        {
            ToolManager.SetActiveTool<TilePainterTool>();
        }

        if (GUILayout.Button("Generate Tilemap"))
        {
            tilemap.GenerateTilemap();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Tile Palette
        if (tilemap.tilesheet != null)
        {
            EditorGUILayout.LabelField("Tile Palette", EditorStyles.boldLabel);
            DrawTilePalette(tilemap);
        }

        EditorGUILayout.Space();

        // Quick Fill Tools
        EditorGUILayout.LabelField("Quick Fill Tools:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All"))
        {
            Undo.RecordObject(tilemap, "Clear Tilemap");
            for (int i = 0; i < tilemap.tileIds.Length; i++)
            {
                tilemap.tileIds[i] = 0;
            }
            tilemap.GenerateTilemap();
        }

        if (GUILayout.Button("Create Border"))
        {
            CreateBorder(tilemap);
        }
        EditorGUILayout.EndHorizontal();

        // Instructions
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Tile Painter Controls:\n" +
            "• Left Click: Paint selected tile\n" +
            "• Right Click: Erase tile\n" +
            "• Middle Click: Pick tile (eyedropper)\n" +
            "• Use the palette below to select tiles",
            MessageType.Info);
    }

    void DrawTilePalette(TilemapRenderer tilemap)
    {
        int totalTiles = tilemap.tilesPerRow * tilemap.tilesPerColumn;
        tilesPerRow = EditorGUILayout.IntSlider("Palette Tiles Per Row", tilesPerRow, 1, 16);

        float buttonSize = 32f;
        float spacing = 2f;
        float totalWidth = tilesPerRow * (buttonSize + spacing);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

        GUILayout.BeginVertical();

        for (int row = 0; row < Mathf.CeilToInt((float)totalTiles / tilesPerRow); row++)
        {
            GUILayout.BeginHorizontal();

            for (int col = 0; col < tilesPerRow; col++)
            {
                int tileIndex = row * tilesPerRow + col;
                int tileId = tileIndex + 1;

                if (tileIndex >= totalTiles) break;

                // Create tile preview
                Rect tileRect = GetTileRect(tilemap, tileIndex);
                Texture2D tileTexture = CreateTilePreview(tilemap.tilesheet, tileRect);

                // Highlight selected tile
                Color oldColor = GUI.backgroundColor;
                if (tilemap.selectedTileId == tileId)
                {
                    GUI.backgroundColor = Color.green;
                }

                if (GUILayout.Button(tileTexture, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                {
                    tilemap.selectedTileId = tileId;
                }

                GUI.backgroundColor = oldColor;
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    Rect GetTileRect(TilemapRenderer tilemap, int tileIndex)
    {
        int tileX = tileIndex % tilemap.tilesPerRow;
        int tileY = tileIndex / tilemap.tilesPerRow;

        float uvWidth = 1f / tilemap.tilesPerRow;
        float uvHeight = 1f / tilemap.tilesPerColumn;

        return new Rect(
            tileX * uvWidth,
            (tilemap.tilesPerColumn - 1 - tileY) * uvHeight,
            uvWidth,
            uvHeight
        );
    }

    Texture2D CreateTilePreview(Texture2D tilesheet, Rect uvRect)
    {
        int previewSize = 32;
        Texture2D preview = new Texture2D(previewSize, previewSize);

        int startX = Mathf.FloorToInt(uvRect.x * tilesheet.width);
        int startY = Mathf.FloorToInt(uvRect.y * tilesheet.height);
        int width = Mathf.FloorToInt(uvRect.width * tilesheet.width);
        int height = Mathf.FloorToInt(uvRect.height * tilesheet.height);

        Color[] pixels = new Color[previewSize * previewSize];

        for (int y = 0; y < previewSize; y++)
        {
            for (int x = 0; x < previewSize; x++)
            {
                int sourceX = startX + Mathf.FloorToInt((float)x / previewSize * width);
                int sourceY = startY + Mathf.FloorToInt((float)y / previewSize * height);

                sourceX = Mathf.Clamp(sourceX, 0, tilesheet.width - 1);
                sourceY = Mathf.Clamp(sourceY, 0, tilesheet.height - 1);

                pixels[y * previewSize + x] = tilesheet.GetPixel(sourceX, sourceY);
            }
        }

        preview.SetPixels(pixels);
        preview.Apply();

        return preview;
    }

    void CreateBorder(TilemapRenderer tilemap)
    {
        Undo.RecordObject(tilemap, "Create Border");

        for (int i = 0; i < tilemap.tileIds.Length; i++)
        {
            tilemap.tileIds[i] = 0;
        }

        for (int y = 0; y < tilemap.mapHeight; y++)
        {
            for (int x = 0; x < tilemap.mapWidth; x++)
            {
                if (x == 0 || x == tilemap.mapWidth - 1 || y == 0 || y == tilemap.mapHeight - 1)
                {
                    int index = y * tilemap.mapWidth + x;
                    tilemap.tileIds[index] = 1;
                }
            }
        }

        tilemap.GenerateTilemap();
    }
}
#endif