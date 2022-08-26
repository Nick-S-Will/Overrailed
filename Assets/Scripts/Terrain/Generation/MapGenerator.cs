using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Overrailed.Managers;
using Overrailed.Terrain.Tiles;
using Overrailed.Train;

namespace Overrailed.Terrain.Generation
{
    [RequireComponent(typeof(MapManager))]
    public class MapGenerator : MonoBehaviour
    {
        [SerializeField] private Biome currentBiome;
        [SerializeField] private Coin coinPrefab;
        [SerializeField] private int seed = 0;
        [SerializeField] [Min(1)] private float noiseScale = 10;
        [SerializeField] private Vector2 noiseOffset;
        [Range(10, 1000)] [SerializeField] private int chunkLength = 25, mapWidth = 19;
        [Space]
        [SerializeField] private GameObject stationPrefab;
        [SerializeField] private Tile stationBridge;
        [SerializeField] private Vector2Int stationSize = 5 * Vector2Int.one;
        [SerializeField] private bool startBonus = false;
        [Space]
        [SerializeField] private GameObject checkpointPrefab;
        [SerializeField] private Vector2Int checkpointSize = 3 * Vector2Int.one;

        [Header("Visualizer Tools")]
        [SerializeField] private Material[] visualizerMaterials;
        [SerializeField] private bool generateHeightVisualizer;

        [SerializeField] [HideInInspector] private List<Transform> chunks = new List<Transform>();
        [SerializeField] [HideInInspector] private Transform groundCollider, groundParent, obstacleParent;
        [SerializeField] [HideInInspector] private System.Random rng;
        private List<Vector2> randomNoiseOffsets;

        public Vector3Int IntPos => Vector3Int.RoundToInt(transform.position);
        public int Seed => seed;

        private void Start()
        {
            if (Manager.instance is GameManager gm)
            {
                gm.OnCheckpoint += AddChunk;
                // Done in this class to lift new chunk *after* it's added
                gm.OnCheckpoint += GetComponent<MapManager>().PreventCollisions;

                GenerateMap();
            }
        }

        #region Generation
        /// <summary>
        /// Clears current map and generates the first chunk of a new one
        /// </summary>
        public async void GenerateMap()
        {
            transform.position = IntPos;

            chunks.Clear();
            groundCollider = null;
            System.Action<Object> DestroyType = DestroyImmediate;
            if (Application.isPlaying) DestroyType = Destroy;
            foreach (Transform t in transform.Cast<Transform>().ToList()) DestroyType(t.gameObject);

            await Task.Yield(); // For destroy cleanup

            if (Application.isPlaying) seed = int.Parse(PlayerPrefs.GetString(Manager.SeedKey, "0"));
            rng = new System.Random(seed);
            randomNoiseOffsets = new List<Vector2>();
            for (int i = 0; i < currentBiome.Regions.Length + 2; i++) randomNoiseOffsets.Add(new Vector2(rng.Next(-100000, 100000), rng.Next(-100000, 100000)));

            groundCollider = new GameObject("Ground Collider").transform;
            groundCollider.parent = transform;
            groundCollider.gameObject.layer = LayerMask.NameToLayer("Ground");
            groundCollider.gameObject.AddComponent<BoxCollider>();

            AddChunk();

            var locomotive = GetComponentInChildren<Locomotive>();
            if (locomotive) MapManager.AddLocomotive(GetComponentInChildren<Locomotive>());
            GetComponent<MapManager>().AnimateFirstChunk();
        }

        /// <summary>
        /// Generates map floor and obstacles based on based on Generation variables
        /// </summary>
        public void AddChunk()
        {
            if (Application.isPlaying && chunks.Count > 1) chunks[chunks.Count - 2].gameObject.SetActive(false);
            if (rng == null)
            {
                Debug.LogWarning("Script was reloaded. Generate map first");
                return;
            }

            // Ground collider
            var mapLength = (chunks.Count + 1) * chunkLength;
            groundCollider.GetComponent<BoxCollider>().size = new Vector3(mapLength, 1, mapWidth);
            groundCollider.position = IntPos + new Vector3(mapLength / 2f - 0.5f, -0.5f, mapWidth / 2f - 0.5f);

            var newChunk = new GameObject("Chunk " + chunks.Count).transform;
            newChunk.parent = transform;
            chunks.Add(newChunk);

            // Station info
            int halfX = stationSize.x / 2, halfZ = stationSize.y / 2;
            var stationPos = IntPos + new Vector3Int(halfX, 0, rng.Next(halfZ, mapWidth - halfZ));
            BoundsInt stationBounds = new BoundsInt(stationPos - new Vector3Int(halfX, 0, halfZ), new Vector3Int(stationSize.x, 3, stationSize.y));
            // Checkpoint info
            halfX = checkpointSize.x / 2; halfZ = checkpointSize.y / 2;
            Vector3Int checkpointPos = IntPos + new Vector3Int(mapLength - halfX - 1, 0, rng.Next(halfZ, mapWidth - halfZ));
            BoundsInt checkpointBounds = new BoundsInt(checkpointPos - new Vector3Int(halfX, 0, halfZ), new Vector3Int(checkpointSize.x, 3, checkpointSize.y));

            // Tiles
            var sectionTiles = GenerateMapTiles();
            // Ground tiles
            groundParent = new GameObject("Ground").transform;
            groundParent.parent = newChunk;
            GenerateSection(sectionTiles.Item1, groundParent, new BoundsInt[] { stationBounds, checkpointBounds }, true);
            // Obstacle tiles
            obstacleParent = new GameObject("Obstacles").transform;
            obstacleParent.parent = newChunk;
            GenerateSection(sectionTiles.Item2, obstacleParent, new BoundsInt[] { stationBounds, checkpointBounds }, false);

            // Spawn station and checkpoint
            if (chunks.Count == 1)
            {
                var station = Instantiate(stationPrefab, stationPos, Quaternion.identity, obstacleParent).transform;
                station.Find("Bonus").gameObject.SetActive(startBonus);
            }
            _ = Instantiate(checkpointPrefab, checkpointPos, Quaternion.identity, obstacleParent);
        }

        /// <summary>
        /// Generates the ground or obstacles of a chunk
        /// </summary>
        /// <param name="tiles"><see cref="Tile"/> objects to be instantiated</param>
        /// <param name="parent">Object the rows of the chunk are parented to</param>
        /// <param name="station">Bounds where the station resides if there is one in the chunk</param>
        /// <param name="checkpoint">Bounds where the checkpoint of the chunk resides</param>
        /// <param name="isGround">True if the desired section is ground tiles</param>
        private void GenerateSection(Tile[,] tiles, Transform parent, BoundsInt[] obstructions, bool isGround)
        {
            int mapLength = chunks.Count * chunkLength;

            if (!isGround)
            {
                var coinPos = Vector3Int.left;
                while (coinPos == Vector3Int.left)
                {
                    coinPos = new Vector3Int(rng.Next(mapLength - chunkLength, mapLength), 0, rng.Next(0, mapWidth));
                    if (obstructions.Any(bounds => bounds.Contains(coinPos))) coinPos = Vector3Int.left;
                }
                tiles[coinPos.x, coinPos.z] = coinPrefab;
            }

            System.Action<Object> destroyType = DestroyImmediate;
            if (Application.isPlaying) destroyType = Destroy;
            for (int x = mapLength - chunkLength; x < mapLength; x++)
            {
                var rowParent = new GameObject("Row " + x).transform;
                rowParent.parent = parent;

                for (int z = 0; z < mapWidth; z++)
                {
                    // Get tile
                    var tilePrefab = tiles[x, z];
                    if (tilePrefab == null) continue;

                    // Get position
                    var newPos = IntPos + new Vector3Int(x, 0, z);
                    if (!isGround) newPos += Vector3Int.up;

                    // Modifies station and checkpoint areas
                    Tile bridge = null;
                    if (obstructions.Any(bounds => bounds.Contains(newPos)))
                    {
                        if (isGround)
                        {
                            // Adds bridges to liquid
                            if (tilePrefab is LiquidTile) bridge = Instantiate(stationBridge, newPos, Quaternion.identity, rowParent);
                        }
                        // Won't add obstacles
                        else continue;
                    }

                    // Spawn tile
                    var tileObj = Instantiate(tilePrefab, newPos, Quaternion.identity, rowParent);
                    tileObj.name = string.Format("{0} {1}", tileObj.name.Substring(0, tileObj.name.Length - 7), z);
                    if (tileObj.RotateOnSpawn) tileObj.MeshParent.localRotation = Quaternion.Euler(0, Random.Range(-45, 45), 0);

                    if (bridge)
                    {
                        bridge.transform.parent = tileObj.transform;
                        destroyType(tileObj.GetComponent<BoxCollider>());
                    }
                }
            }
        }

        /// <summary>
        /// Generates 2D perlin noise based on generation variables
        /// </summary>
        private float[,] GenerateHeightMap(Vector2 offset)
        {
            int mapLength = chunks.Count * chunkLength;
            float[,] heightMap = new float[mapLength, mapWidth];

            for (int y = 0; y < mapWidth; y++)
            {
                for (int x = 0; x < mapLength; x++)
                {
                    heightMap[x, y] = Mathf.Clamp01(Mathf.PerlinNoise(
                        x / noiseScale + offset.x + noiseOffset.x,
                        y / noiseScale + offset.y + noiseOffset.y)
                    );
                    heightMap[x, y] = Mathf.Pow(heightMap[x, y], Mathf.Lerp(1.5f, 0.5f, heightMap[x, y]));
                }
            }

            return heightMap;
        }

        /// <summary>
        /// Generates ground and obstacle tiles from biome data
        /// </summary>
        /// <returns>The ground and obstacle tiles of the maps as a tuple</returns>
        private (Tile[,], Tile[,]) GenerateMapTiles()
        {
            float[,] liquidTerrainMap = GenerateHeightMap(randomNoiseOffsets[randomNoiseOffsets.Count - 1]);
            float[,] regionMaps = GenerateHeightMap(randomNoiseOffsets[randomNoiseOffsets.Count - 2]);
            var heightMaps = new List<float[,]>();
            for (int i = 0; i < currentBiome.Regions.Length; i++) heightMaps.Add(GenerateHeightMap(randomNoiseOffsets[i]));

            var groundTiles = new Tile[liquidTerrainMap.GetLength(0), mapWidth];
            var obstacleTiles = new Tile[liquidTerrainMap.GetLength(0), mapWidth];
            for (int x = 0; x < liquidTerrainMap.GetLength(0); x++)
            {
                for (int z = 0; z < mapWidth; z++)
                {
                    if (liquidTerrainMap[x, z] >= currentBiome.MinObstaclePercentage)
                    {
                        int regionIndex = Mathf.FloorToInt(currentBiome.Regions.Length * regionMaps[x, z]);
                        float tileHeight = heightMaps[regionIndex][x, z];

                        (groundTiles[x, z], obstacleTiles[x, z]) = currentBiome.Regions[regionIndex].GetTiles(tileHeight);
                    }
                    else groundTiles[x, z] = currentBiome.LiquidTile;
                }
            }

            if (!Application.isPlaying && generateHeightVisualizer) VisualizeHeightMaps(liquidTerrainMap, heightMaps);

            return (groundTiles, obstacleTiles);
        }
        #endregion

        private void VisualizeHeightMaps(float[,] liquidMap, List<float[,]> heightMaps)
        {
            var visualizer = new List<(Vector3, Material)>();
            int mapLength = chunks.Count * chunkLength;
            for (int x = mapLength - chunkLength; x < mapLength; x++)
            {
                for (int z = 0; z < mapWidth; z++)
                {
                    if (liquidMap[x, z] < currentBiome.MinObstaclePercentage) visualizer.Add((new Vector3(x, 0, z), visualizerMaterials[visualizerMaterials.Length - 1]));
                    else for (int i = 0; i < heightMaps.Count; i++) visualizer.Add((new Vector3(x, 10 * heightMaps[i][x, z], z), visualizerMaterials[i]));
                }
            }
            visualizer.Add((10 * Vector3.up, visualizerMaterials[visualizerMaterials.Length - 1]));

            var visualizerParent = new GameObject("Visualizer " + (chunks.Count - 1)).transform;
            visualizerParent.parent = transform;
            foreach (var tile in visualizer)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                cube.name = tile.Item2.name + " at " + tile.Item1.ToString();
                cube.gameObject.layer = LayerMask.NameToLayer("TransparentFX");

                cube.parent = visualizerParent;
                cube.position = tile.Item1 + 5 * Vector3.up;

                cube.GetComponent<MeshRenderer>().material = tile.Item2;
            }
        }

        private void OnValidate()
        {
            if (stationSize[0] < 5) stationSize[0] = 5;
            if (stationSize[1] < 5) stationSize[1] = 5;

            if (checkpointSize[0] < 3) checkpointSize[0] = 3;
            if (checkpointSize[1] < 3) checkpointSize[1] = 3;

            int minChunkLength = stationSize[0] + checkpointSize[0] + 1;
            if (chunkLength < minChunkLength) chunkLength = minChunkLength;

            int minMapWidth = Mathf.Max(stationSize[1], checkpointSize[1]) + 2;
            if (mapWidth < minMapWidth) mapWidth = minMapWidth;
        }

        private void OnDestroy()
        {
            if (Manager.instance is GameManager gm)
            {
                gm.OnCheckpoint -= AddChunk;
            }
        }
    }
}