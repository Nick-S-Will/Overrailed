using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;

using Overrailed.Managers;
using Overrailed.Terrain.Tiles;

namespace Overrailed.Terrain.Generation
{
    public class MapManager : MonoBehaviour
    {
        public event System.Action<string> OnSeedChange;
        public event System.Action OnFinishAnimateChunk;

        #region Inspector Variables
        [SerializeField] private LayerMask interactMask;

        [Header("Generation")] [SerializeField] private int seed = 0;
        [SerializeField] [Min(1)] private float noiseScale = 10;
        [SerializeField] private Vector2 noiseOffset;
        [Range(10, 1000)] [SerializeField] private int chunkLength = 25, mapWidth = 19;
        [SerializeField] private bool showBounds;

        [Header("Biomes")]
        [SerializeField] private Biome currentBiome;
        [SerializeField] private Color highlightColor = Color.white;

        [Header("Stations")]
        [SerializeField] private GameObject stationPrefab;
        [SerializeField] private Tile stationBridge;
        [SerializeField] private Vector2Int stationSize = 5 * Vector2Int.one;
        [SerializeField] private bool startBonus = false;
        [Space]
        [SerializeField] private GameObject checkpointPrefab;
        [SerializeField] private Vector2Int checkpointSize = 3 * Vector2Int.one;

        [Header("Animation")]
        [SerializeField] private Vector3 spawnOffset = 10 * Vector3.down;
        [SerializeField] [Min(1)] private float slideSpeed = 100;
        [SerializeField] [Min(0)] private float rowSpawnInterval = 0.2f;
        #endregion

        [SerializeField] [HideInInspector] private List<Transform> chunks = new List<Transform>();
        [SerializeField] [HideInInspector] private Transform groundParent, groundCollider, obstacleParent;
        [SerializeField] [HideInInspector] private Vector3Int stationPos;
        [SerializeField] [HideInInspector] private System.Random rng;

        private List<Transform> newHighlights = new List<Transform>(), highlights = new List<Transform>();
        private Vector3Int IntPos => Vector3Int.RoundToInt(transform.position);

        public LayerMask InteractMask => interactMask;
        public int Seed
        {
            get { return seed; }
            private set
            {
                seed = value;
                OnSeedChange?.Invoke(seed.ToString());
            }
        }

        private void Awake()
        {
            transform.position = IntPos;
            rng = new System.Random(seed);
            OnSeedChange?.Invoke(seed.ToString());
        }

        private void Start()
        {
            if (GameManager.instance)
            {
                GameManager.instance.OnCheckpoint += DisableObstacles;
                GameManager.instance.OnEndCheckpoint += EnableObstacles;
                GameManager.instance.OnEndCheckpoint += AddChunk;
                GameManager.instance.OnEndCheckpoint += AnimateNewChunk;
                GameManager.instance.OnGameEnd += ShowAllChunks;
            }

            if (transform.childCount == 0)  GenerateMap();
            AnimateNewChunk();
        }

        void LateUpdate()
        {
            RemoveOldHighlights();
            AddHighlights();

            newHighlights.Clear();
        }

        #region Generation
        /// <summary>
        /// Clears current map and generates the first chunk of a new one
        /// </summary>
        public void GenerateMap()
        {
            transform.position = IntPos;

            chunks.Clear();
            groundCollider = null;
            System.Action<Object> DestroyType = DestroyImmediate;
            if (Application.isPlaying) DestroyType = Destroy;
            foreach (Transform t in transform.Cast<Transform>().ToList()) DestroyType(t.gameObject);
            rng = new System.Random(seed);

            AddChunk();
        }

        /// <summary>
        /// Generates map floor and obstacles based on based on Generation variables
        /// </summary>
        public void AddChunk()
        {
            if (Application.isPlaying)
            {
                OnSeedChange?.Invoke(seed.ToString());

                if (chunks.Count > 1) chunks[chunks.Count - 2].gameObject.SetActive(false);
            }

            var newChunk = new GameObject("Chunk " + chunks.Count).transform;
            chunks.Add(newChunk);
            var mapLength = chunks.Count * chunkLength;

            // Station info
            int halfX = stationSize.x / 2, halfZ = stationSize.y / 2;
            stationPos = IntPos + new Vector3Int(halfX, 0, rng.Next(halfZ, mapWidth - halfZ));
            BoundsInt stationBounds = new BoundsInt(stationPos - new Vector3Int(halfX, 0, halfZ), new Vector3Int(stationSize.x, 3, stationSize.y));
            // Checkpoint info
            halfX = checkpointSize.x / 2; halfZ = checkpointSize.y / 2;
            Vector3Int checkpointPos = IntPos + new Vector3Int(mapLength - halfX - 1, 0, rng.Next(halfZ, mapWidth - halfZ));
            BoundsInt checkpointBounds = new BoundsInt(checkpointPos - new Vector3Int(halfX, 0, halfZ), new Vector3Int(checkpointSize.x, 3, checkpointSize.y));

            // Ground collider
            if (groundCollider == null)
            {
                groundCollider = new GameObject("Ground Collider").transform;
                groundCollider.parent = transform;
                groundCollider.gameObject.layer = LayerMask.NameToLayer("Ground");
                groundCollider.gameObject.AddComponent<BoxCollider>();
            }
            groundCollider.GetComponent<BoxCollider>().size = new Vector3(mapLength, 1, mapWidth);
            groundCollider.position = IntPos + new Vector3(mapLength / 2f - 0.5f, -0.5f, mapWidth / 2f - 0.5f);

            // Tiles
            var sectionTiles = GenerateMapTiles(GenerateHeightMap());
            // Ground tiles
            groundParent = new GameObject("Ground").transform;
            groundParent.parent = newChunk;
            GenerateSection(sectionTiles.Item1, groundParent, stationBounds, checkpointBounds, true);
            // Obstacle tiles
            obstacleParent = new GameObject("Obstacles").transform;
            obstacleParent.parent = newChunk;
            GenerateSection(sectionTiles.Item2, obstacleParent, stationBounds, checkpointBounds, false);

            // Spawn station and checkpoint
            if (chunks.Count == 1)
            {
                var station = Instantiate(stationPrefab, stationPos, Quaternion.identity, obstacleParent).transform;
                station.Find("Bonus").gameObject.SetActive(startBonus);
            }
            _ = Instantiate(checkpointPrefab, checkpointPos, Quaternion.identity, obstacleParent);

            newChunk.parent = transform;
        }

        /// <summary>
        /// Generates the ground or obstacles of a chunk
        /// </summary>
        /// <param name="tiles"></param>
        /// <param name="parent">Object the rows of the chunk are parented to</param>
        /// <param name="station">Bounds where the station resides if there is one in the chunk</param>
        /// <param name="checkpoint">Bounds where the checkpoint of the chunk resides</param>
        /// <param name="isGround">True if the desired section is ground tiles</param>
        private void GenerateSection(Tile[,] tiles, Transform parent, BoundsInt station, BoundsInt checkpoint, bool isGround)
        {
            int mapLength = chunks.Count * chunkLength;

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
                    if (station.Contains(newPos) || checkpoint.Contains(newPos))
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
                    if (tileObj.RotateOnSpawn) tileObj.MeshParent.localRotation = Quaternion.Euler(0, Random.Range(-15, 15), 0);

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
        /// <param name="noiseScale"></param>
        /// <returns></returns>
        private float[,] GenerateHeightMap()
        {
            int mapLength = (chunks.Count + 1) * chunkLength;
            float[,] heightMap = new float[mapLength, mapWidth];
            Vector2 offset = new Vector2(rng.Next(-100000, 100000), rng.Next(-100000, 100000));

            for (int y = 0; y < mapWidth; y++)
            {
                for (int x = 0; x < mapLength; x++)
                {
                    heightMap[x, y] = Mathf.Clamp01(Mathf.PerlinNoise(x / noiseScale + offset.x + noiseOffset.x, y / noiseScale + offset.y + noiseOffset.y));
                }
            }

            return heightMap;
        }

        private int MaxHeightAt((int, int) coords, List<float[,]> obstacleHeightMaps)
        {
            var maxValue = float.MinValue;
            var maxIndex = 0;

            for (int i = 0; i < obstacleHeightMaps.Count; i++)
            {
                if (obstacleHeightMaps[i][coords.Item1, coords.Item2] > maxValue)
                {
                    maxValue = obstacleHeightMaps[i][coords.Item1, coords.Item2];
                    maxIndex = i;
                }
            }

            return maxIndex;
        }

        /// <summary>
        /// Generates ground and obstacle tiles from biome data
        /// </summary>
        /// <param name="heightMap">height map splits terrain into liquid and ground sections</param>
        /// <returns>The ground and obstacle tiles of the map</returns>
        private (Tile[,], Tile[,]) GenerateMapTiles(float[,] heightMap)
        {
            var obstacleSampleMaps = new List<float[,]>();
            var obstacleHeightMaps = new List<float[,]>();
            for (int i = 0; i < currentBiome.Regions.Length; i++)
            {
                obstacleSampleMaps.Add(GenerateHeightMap());
                obstacleHeightMaps.Add(GenerateHeightMap());
            }

            var groundTiles = new Tile[heightMap.GetLength(0), mapWidth];
            var obstacleTiles = new Tile[heightMap.GetLength(0), mapWidth];

            for (int y = 0; y < mapWidth; y++)
            {
                for (int x = 0; x < heightMap.GetLength(0); x++)
                {
                    if (heightMap[x, y] >= currentBiome.MinObstaclePercentage)
                    {
                        int mapIndex = MaxHeightAt((x, y), obstacleHeightMaps);
                        var tiles = currentBiome.Regions[mapIndex].GetTiles(obstacleSampleMaps[mapIndex][x, y]);

                        groundTiles[x, y] = tiles.Item1;
                        obstacleTiles[x, y] = tiles.Item2;
                    }
                    else groundTiles[x, y] = currentBiome.LiquidTile;
                }
            }

            return (groundTiles, obstacleTiles);
        }
        #endregion

        #region Tile highlighting
        public void TryHighlightTile(Transform tile)
        {
            if (tile == null) return;

            if (newHighlights.Contains(tile)) return;
            else newHighlights.Add(tile);
        }

        /// <summary>
        /// Removes the highlight on tiles that are no longer selected
        /// </summary>
        private void RemoveOldHighlights()
        {
            foreach (var tile in highlights.ToArray())
                if (!newHighlights.Contains(tile))
                    highlights.Remove(tile);
        }

        /// <summary>
        /// Highlights the tiles in <see cref="newHighlights"/> that aren't already highlited
        /// </summary>
        private void AddHighlights()
        {
            foreach (var tile in newHighlights)
            {
                if (!highlights.Contains(tile))
                {
                    highlights.Add(tile);
                    _ = StartCoroutine(HightlightTile(tile));
                }
            }
        }

        /// <summary>
        /// Tints selected tile's children's meshes by highlightColor until tile is no longer selected
        /// </summary>
        private IEnumerator HightlightTile(Transform tile)
        {
            var renderers = tile.GetComponentsInChildren<MeshRenderer>();
            var originalColors = new Color[renderers.Length];

            // Tint mesh colors
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].material.color;
                renderers[i].material.color = 0.5f * (originalColors[i] + highlightColor);
            }

            yield return new WaitWhile(() => highlights.Contains(tile));

            // Reset colors
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) break;
                else renderers[i].material.color = originalColors[i];
            }
        }
        #endregion

        #region Getting Map Points/Tiles
        private Transform GetParentAt(Vector3Int position)
        {
            try
            {
                var chunk = transform.GetChild(position.x / chunkLength + 1);
                var section = chunk.GetChild(position.y); // Ground or Obstacles
                var row = section.GetChild(position.x % chunkLength);
                return row;
            }
            catch (UnityException) { return null; }
        }
        /// <summary>
        /// Gets the tile at the given coords
        /// </summary>
        /// <param name="point">Coords of the searched tile</param>
        /// <returns>The transform of the tile at <paramref name="point"/> if there is one, otherwise null</returns>
        public Transform GetTileAt(Vector3Int point)
        {
            if (!PointIsInPlayBounds(point)) return null;

            // Ground
            if (point.y == transform.position.y)
            {
                point -= IntPos;
                var row = GetParentAt(point);

                try { return row.GetChild(point.z); }
                catch (UnityException) 
                { 
                    Debug.LogError("Invalid point on map " + point);
                    return null;
                }
            }
            // Obstacles
            else
            {
                _ = Physics.Raycast(new Vector3(point.x, transform.position.y - 0.5f, point.z), Vector3.up, out RaycastHit hitData, 1, interactMask);

                return hitData.transform;
            }
        }

        private Bounds GetPlayBounds()
        {
            var groundCollider = transform.GetChild(0);
            var center = groundCollider.position + Vector3.up;
            var size = groundCollider.GetComponent<BoxCollider>().size;

            return new Bounds(center, new Vector3(size.x, 1, size.z));
        }
        /// <summary>
        /// Checks if <paramref name="point"/> is over the ground collider, up to 3 units above it
        /// </summary>
        public bool PointIsInPlayBounds(Vector3 point) => GetPlayBounds().Contains(point);
        /// <summary>
        /// Checks if <paramref name="point"/> is over the ground collider extended by 30 on the x axis and up to 3 units above it
        /// </summary>
        public bool PointIsInEditBounds(Vector3 point)
        {
            var bounds = GetPlayBounds();
            bounds.size += 30 * Vector3.right;

            return bounds.Contains(point);
        }
        /// <summary>
        /// Checks if <paramref name="point"/> is in bounds based on <see cref="GameManager.CurrentState"/>
        /// </summary>
        public bool PointIsInBounds(Vector3 point)
        {
            if (GameManager.instance) return (GameManager.IsPlaying() && PointIsInPlayBounds(point)) || (GameManager.IsEditing() && PointIsInEditBounds(point));
            else return PointIsInPlayBounds(point);
        }
        #endregion

        #region Pickup Placing
        /// <summary>
        /// Places <paramref name="pickup"/> at the nearest coords around <paramref name="pickup"/>'s position
        /// </summary>
        /// <returns>The coords at which the pickup is move to</returns>
        public Vector3Int MovePickup(IPickupable pickup) => PlacePickup(pickup, Vector3Int.RoundToInt((pickup as Tile).transform.position), false);
        /// <summary>
        /// Places given pickup at coords, sets obstacleParent as its parent, and enables its BoxCollider
        /// </summary>
        /// <returns>The coords at which the pickup is placed</returns>
        public Vector3Int PlacePickup(IPickupable pickup, Vector3Int startCoords) => PlacePickup(pickup, startCoords, true);
        private Vector3Int PlacePickup(IPickupable pickup, Vector3Int startCoords, bool includeStart)
        {
            if (!PointIsInPlayBounds(startCoords)) throw new System.Exception("Starting coord isn't in the bounds of the map");

            LayerMask mask = LayerMask.GetMask("Default", "Water", "Rail", "Entity");
            var flags = new HashSet<Vector3Int>();
            var toCheck = new Queue<Vector3Int>();

            if (includeStart) toCheck.Enqueue(startCoords);
            else
            {
                flags.Add(startCoords);
                foreach (var point in GetAdjacentCoords(startCoords)) if (PointIsInBounds(point)) toCheck.Enqueue(point);
            }

            while (toCheck.Count > 0)
            {
                var coords = toCheck.Dequeue();
                Collider[] colliders = Physics.OverlapBox(coords, 0.45f * Vector3.one, Quaternion.identity, mask);
                if (colliders.Length > 0)
                {
                    // Tries to stack pickup if possible
                    var stack = colliders[0].GetComponent<StackTile>();
                    if (pickup is StackTile toStack && stack != null && toStack.TryStackOn(stack)) return coords;

                    // Prevents from checking same coord twice
                    flags.Add(coords);

                    // Adds adjacent points to list of points to check
                    foreach (var point in GetAdjacentCoords(coords)) if (!flags.Contains(point) && PointIsInBounds(point)) toCheck.Enqueue(point);
                }
                else
                {
                    ForcePlacePickup(pickup, coords);
                    return coords;
                }
            }

            throw new System.Exception("No viable coord found on map");
        }
        /// <summary>
        /// Places <paramref name="pickup"/> at <paramref name="coords"/> without any collision checks
        /// </summary>
        public void ForcePlacePickup(IPickupable pickup, Vector3Int coords)
        {
            // Places pickup in empty space
            var t = (pickup as MonoBehaviour).transform;
            t.parent = GetParentAt(coords);
            t.position = coords;
            t.localRotation = Quaternion.identity;

            t.GetComponent<BoxCollider>().enabled = true;
            pickup.Drop(coords);
        }

        private Vector3Int[] GetAdjacentCoords(Vector3Int coord) => new Vector3Int[] { coord + Vector3Int.forward, coord + Vector3Int.right, coord + Vector3Int.back, coord + Vector3Int.left };
        #endregion

        #region Map Toggles
        private void DisableObstacles() => SetObstacleHitboxes(false);
        private void EnableObstacles() => SetObstacleHitboxes(true);
        private void SetObstacleHitboxes(bool enabled)
        {
            // Water
            for (int i = 1; i < transform.childCount; i++)
            {
                foreach (var collider in transform.GetChild(i).GetChild(0).GetComponentsInChildren<BoxCollider>())
                {
                    collider.enabled = enabled;
                }
            }

            // Obstacles
            for (int i = 1; i < transform.childCount; i++)
            {
                foreach (var collider in transform.GetChild(i).GetChild(1).GetComponentsInChildren<BoxCollider>())
                {
                    // Prevents tiles in a stack to all enable their hitboxes
                    var stack = collider.GetComponent<StackTile>();
                    if (stack && stack.PrevInStack) continue;

                    if (collider.gameObject.layer == LayerMask.NameToLayer("Default")) collider.enabled = enabled;
                }
            }
        }

        public void ShowAllChunks()
        {
            foreach (var c in chunks) c.gameObject.SetActive(true);
        }
        #endregion

        #region Spawning animation
        public void AnimateNewChunk() => _ = AnimateChunk(chunks.Count - 1);
        public async Task AnimateChunk(int chunkIndex)
        {
            var chunk = transform.GetChild(chunkIndex + 1);
            Transform ground = chunk.GetChild(0), obstacles = chunk.GetChild(1);

            // Hiding and moving station and checkpoint
            var extras = new List<Transform>();
            var extraStartPos = new List<Vector3>();
            // Adds station to extras
            if (obstacles.childCount - ground.childCount == 2) extras.Add(obstacles.GetChild(obstacles.childCount - 2));
            // Adds checkpoint to extras
            extras.Add(obstacles.GetChild(obstacles.childCount - 1));
            foreach (var obj in extras)
            {
                extraStartPos.Add(obj.localPosition);
                obj.localPosition += spawnOffset;

                obj.gameObject.SetActive(false);
                foreach (var tile in obj.GetComponentsInChildren<Tile>()) tile.SetVisible(false);
            }

            // Moving and hiding each row
            for (int i = 0; i < ground.childCount; i++)
            {
                ground.GetChild(i).localPosition += spawnOffset;
                obstacles.GetChild(i).localPosition += spawnOffset;

                ground.GetChild(i).gameObject.SetActive(false);
                // Hides obstacle graphics without affecting colliders
                foreach (var tile in obstacles.GetChild(i).GetComponentsInChildren<Tile>()) tile.SetVisible(false);
            }

            // Turning on and sliding each ground row into place
            foreach (Transform groundRow in ground)
            {
                groundRow.gameObject.SetActive(true);

                _ = AnimateSpawnSlide(groundRow, Vector3.zero);
                await Task.Delay(Mathf.RoundToInt(1000 * rowSpawnInterval));
            }

            // Turning on and sliding each obstacle row into place
            for (int i = 0; i < ground.childCount; i++)
            {
                var obstacleRow = obstacles.GetChild(i);
                foreach (var tile in obstacleRow.GetComponentsInChildren<Tile>()) tile.SetVisible(true);

                _ = AnimateSpawnSlide(obstacleRow, Vector3.zero);

                if (obstacleRow.childCount == 0) continue;
                await Task.Delay(Mathf.RoundToInt(1000 * rowSpawnInterval));
            }

            // Turning on and sliding station and checkpoint into place
            foreach (var transform in extras)
            {
                transform.gameObject.SetActive(true);
                foreach (var tile in transform.GetComponentsInChildren<Tile>()) tile.SetVisible(true);
            }
            await AnimateSpawnSlide(extras[0], extraStartPos[0]);
            if (extras.Count > 1) await AnimateSpawnSlide(extras[1], extraStartPos[1]);

            OnFinishAnimateChunk?.Invoke();
        }

        private async Task AnimateSpawnSlide(Transform t, Vector3 destination)
        {
            while (t.localPosition != destination)
            {
                t.localPosition = Vector3.MoveTowards(t.localPosition, destination, slideSpeed * Time.deltaTime);

                await Task.Yield();
            }
        }
        #endregion

        void OnValidate()
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

        private void OnDrawGizmos()
        {
            if (showBounds)
            {
                var bounds = GetPlayBounds();
                Gizmos.DrawCube(bounds.center, bounds.size);
            }
        }

        void OnDestroy()
        {
            if (GameManager.instance == null) return;

            GameManager.instance.OnCheckpoint -= AddChunk;
            GameManager.instance.OnCheckpoint -= DisableObstacles;
            GameManager.instance.OnEndCheckpoint -= EnableObstacles;
            GameManager.instance.OnEndCheckpoint -= AnimateNewChunk;
        }
    }
}