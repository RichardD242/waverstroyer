using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainDecorator : MonoBehaviour
{
    [Header("Terrain Textures")]
    [Tooltip("Base grass texture for lower areas")]
    public Texture2D grassTexture;
    [Tooltip("Rocky texture for steep areas")]
    public Texture2D rockTexture;
    [Tooltip("Snow texture for high areas")]
    public Texture2D snowTexture;
    public float textureScale = 20f;

    [Header("Vegetation")]
    [Tooltip("Tree prefabs to scatter")]
    public GameObject[] treePrefabs;
    [Tooltip("Number of trees to place")]
    public int treeCount = 500;
    [Tooltip("Maximum slope angle for tree placement")]
    public float maxTreeSlope = 30f;
    
    [Header("Height-Based Settings")]
    [Range(0f, 1f)]
    public float rockStartHeight = 0.3f;
    [Range(0f, 1f)]
    public float snowStartHeight = 0.7f;
    [Range(0f, 1f)]
    public float treeMinHeight = 0.1f;
    [Range(0f, 1f)]
    public float treeMaxHeight = 0.7f;

    [Header("Detail Settings")]
    public DetailPrototype[] grassDetails;
    public int grassDensity = 10;
    
    private Terrain terrain;
    private TerrainData terrainData;

    void Start()
    {
        // Get or create Terrain component
        terrain = GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("TerrainDecorator: No Terrain component found!");
            return;
        }

        // Get or create TerrainData
        terrainData = terrain.terrainData;
        if (terrainData == null)
        {
            #if UNITY_EDITOR
            // Create new TerrainData asset
            terrainData = new TerrainData();
            string terrainDataPath = "Assets/TerrainData.asset";
            UnityEditor.AssetDatabase.CreateAsset(terrainData, terrainDataPath);
            terrain.terrainData = terrainData;
            #else
            Debug.LogError("TerrainDecorator: No TerrainData found!");
            return;
            #endif
        }

        // Check required textures
        if (grassTexture == null || rockTexture == null || snowTexture == null)
        {
            Debug.LogError("TerrainDecorator: Please assign all required textures (grass, rock, snow)!");
            return;
        }

        ApplyTextures();
        PlaceTrees();
        AddGrassDetails();
    }

    void ApplyTextures()
    {
        try
        {
            // Create TerrainLayers directory if it doesn't exist
            #if UNITY_EDITOR
            string layerPath = "Assets/TerrainLayers";
            if (!System.IO.Directory.Exists(layerPath))
            {
                System.IO.Directory.CreateDirectory(Application.dataPath + "/TerrainLayers");
                UnityEditor.AssetDatabase.Refresh();
            }
            #endif

            TerrainLayer[] layers = new TerrainLayer[3];
            
            // Setup layers with more detailed texture blending
            for (int i = 0; i < 3; i++)
            {
                layers[i] = new TerrainLayer();
                string layerName = "";
                
                switch(i)
                {
                    case 0:
                        layers[i].diffuseTexture = grassTexture;
                        layerName = "GrassLayer";
                        break;
                    case 1:
                        layers[i].diffuseTexture = rockTexture;
                        layerName = "RockLayer";
                        break;
                    case 2:
                        layers[i].diffuseTexture = snowTexture;
                        layerName = "SnowLayer";
                        break;
                }
                
                layers[i].tileSize = new Vector2(textureScale, textureScale);
                layers[i].name = layerName;

                #if UNITY_EDITOR
                string assetPath = $"Assets/TerrainLayers/Layer_{layerName}.terrainlayer";
                UnityEditor.AssetDatabase.CreateAsset(layers[i], assetPath);
                #endif
            }

            terrainData.terrainLayers = layers;

            // Generate splatmap with smooth transitions
            float[,,] splatmapData = new float[terrainData.alphamapWidth, 
                                         terrainData.alphamapHeight, 3];

            for (int y = 0; y < terrainData.alphamapHeight; y++)
            {
                for (int x = 0; x < terrainData.alphamapWidth; x++)
                {
                    float height = terrainData.GetHeight(
                        (int)((float)x / terrainData.alphamapWidth * terrainData.heightmapResolution),
                        (int)((float)y / terrainData.alphamapHeight * terrainData.heightmapResolution)
                    );

                    // Normalize height (0-1 range)
                    float normalizedHeight = height / terrainData.size.y;
                    
                    // Get slope for rock placement
                    float slope = terrainData.GetSteepness(
                        (float)x / terrainData.alphamapWidth,
                        (float)y / terrainData.alphamapHeight
                    );

                    // Initialize weights
                    float grass = 1f;
                    float rock = 0f;
                    float snow = 0f;

                    // Add rock on steep slopes (smooth transition)
                    if (slope > 25)
                    {
                        float slopeBlend = Mathf.Clamp01((slope - 25) / 20);
                        rock = slopeBlend;
                        grass *= (1 - slopeBlend);
                    }

                    // Height-based blending
                    if (normalizedHeight > 0.3f) // Start transitioning to rock
                    {
                        float rockBlend = Mathf.Clamp01((normalizedHeight - 0.3f) / 0.2f);
                        rock = Mathf.Lerp(rock, 0.8f, rockBlend);
                        grass *= (1 - rockBlend * 0.8f);
                    }

                    if (normalizedHeight > 0.6f) // Start transitioning to snow
                    {
                        float snowBlend = Mathf.Clamp01((normalizedHeight - 0.6f) / 0.2f);
                        snow = snowBlend;
                        rock *= (1 - snowBlend);
                        grass *= (1 - snowBlend);
                    }

                    // Ensure weights sum to 1
                    float total = grass + rock + snow;
                    grass /= total;
                    rock /= total;
                    snow /= total;

                    splatmapData[x, y, 0] = grass;
                    splatmapData[x, y, 1] = rock;
                    splatmapData[x, y, 2] = snow;
                }
            }

            terrainData.SetAlphamaps(0, 0, splatmapData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TerrainDecorator: Error creating terrain layers: {e.Message}");
        }
    }

    void PlaceTrees()
    {
        if (treePrefabs == null || treePrefabs.Length == 0) return;

        for (int i = 0; i < treeCount; i++)
        {
            float x = Random.Range(0f, 1f);
            float z = Random.Range(0f, 1f);
            
            float height = terrainData.GetHeight(
                (int)(x * terrainData.heightmapResolution),
                (int)(z * terrainData.heightmapResolution)
            ) / terrainData.size.y;

            float slope = terrainData.GetSteepness(x, z);

            if (height >= treeMinHeight && 
                height <= treeMaxHeight && 
                slope <= maxTreeSlope)
            {
                Vector3 position = new Vector3(
                    x * terrainData.size.x,
                    height * terrainData.size.y,
                    z * terrainData.size.z
                );

                GameObject treePrefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
                GameObject tree = Instantiate(
                    treePrefab,
                    position,
                    Quaternion.Euler(0, Random.Range(0f, 360f), 0),
                    transform
                );

                // Add slight random scale variation
                float scale = Random.Range(0.8f, 1.2f);
                tree.transform.localScale *= scale;
            }
        }
    }

    void AddGrassDetails()
    {
        if (grassDetails == null || grassDetails.Length == 0) return;

        terrainData.detailPrototypes = grassDetails;
        int[,] detailMap = new int[terrainData.detailWidth, terrainData.detailHeight];

        for (int y = 0; y < terrainData.detailHeight; y++)
        {
            for (int x = 0; x < terrainData.detailWidth; x++)
            {
                float height = terrainData.GetHeight(
                    (int)((float)x / terrainData.detailWidth * terrainData.heightmapResolution),
                    (int)((float)y / terrainData.detailHeight * terrainData.heightmapResolution)
                ) / terrainData.size.y;

                if (height >= treeMinHeight && height <= treeMaxHeight)
                {
                    detailMap[x, y] = Random.Range(1, grassDensity);
                }
            }
        }

        terrainData.SetDetailLayer(0, 0, 0, detailMap);
    }
}