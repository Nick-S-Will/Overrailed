using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine;

using Overrailed.Managers;
using Overrailed.Terrain.Tiles;
using Overrailed.Train;

namespace Overrailed.Terrain.Generation
{
    [SelectionBase]
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

        [Header("Biomes")]
        [SerializeField] private Biome currentBiome;
        [SerializeField] private Color highlightColor = Color.white;
        [SerializeField] private bool highlightEnabled = true;

        [Header("Stations")]
        [SerializeField] private GameObject stationPrefab;
        [SerializeField] private MonoBehaviour defaultPlayerPrefab;
        [SerializeField] private Tile stationBridge;
        [SerializeField] private Vector2Int stationSize = 5 * Vector2Int.one;
        [SerializeField] [Min(5)] private float trainInitialDelay = 10;
        [SerializeField] private bool startBonus = false;
        [Space]
        [SerializeField] private GameObject checkpointPrefab;
        [SerializeField] private Vector2Int checkpointSize = 3 * Vector2Int.one;

        [Header("Animation")]
        [SerializeField] private Vector3 spawnOffset = 10 * Vector3.down;
        [SerializeField] [Min(0)] private float rowSpawnInterval = 0.2f, bounceHeight = 1;
        [SerializeField] [Min(1)] private float slideSpeed = 100;
        [Space]
        [SerializeField] private GameObject[] numbersPrefabs;
        [SerializeField] private float numberFadeSpeed = 0.5f, numberFadeDuration = 1.25f;
        [SerializeField] private AudioClip numberSpawnSound;

        [Header("Visualizer Tools")]
        [SerializeField] private Material[] visualizerMaterials;
        [SerializeField] private bool generateHeightVisualizer, visualizeBounds;
        #endregion

        [SerializeField] [HideInInspector] private List<Transform> chunks = new List<Transform>();
        [SerializeField] [HideInInspector] private Transform groundParent, groundCollider, obstacleParent;
        [SerializeField] [HideInInspector] private Vector3Int stationPos;
        [SerializeField] [HideInInspector] private System.Random rng;
        private List<Transform> newHighlights = new List<Transform>(), highlights = new List<Transform>();
        private List<Vector2> randomNoiseOffsets;
        private static List<Locomotive> locomotives = new List<Locomotive>();

        public static Locomotive[] Locomotives => locomotives.ToArray();
        public Vector3Int IntPos => Vector3Int.RoundToInt(transform.position);
        public LayerMask InteractMask => interactMask;
        public bool HighlightEnabled => highlightEnabled;
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
        }

        private void Start()
        {
            if (Manager.instance is GameManager gm)
            {
                gm.OnCheckpoint += DisableObstacles;
                gm.OnEndCheckpoint += EnableObstacles;
                gm.OnEndCheckpoint += AddChunk;
                gm.OnEndCheckpoint += AnimateNewChunk;
                gm.OnGameEnd += ShowAllChunks;
                OnFinishAnimateChunk += SpawnPlayer;
                OnFinishAnimateChunk += StartTrain;

                GenerateMap();
            }
            else if (Manager.instance is MainMenuManager mm)
            {
                OnFinishAnimateChunk += mm.SlideInMainElements;
                OnFinishAnimateChunk += mm.MoveSelectedSkinToGame;
            }
            else if (Manager.instance is TutorialManager)
            {
                var station = GameObject.Find(stationPrefab.name).transform;
                stationPos = Vector3Int.RoundToInt(station.position);
                OnFinishAnimateChunk += SpawnPlayer;
            }

            AnimateNewChunk();

            if (highlightEnabled) _ = StartCoroutine(TileHighlighting());
        }

        public void SpawnPlayer()
        {
            MonoBehaviour playerPrefab = Manager.GetSkin();
            if (playerPrefab == null) playerPrefab = defaultPlayerPrefab;

            var player = Instantiate(playerPrefab).transform;
            player.parent = null;
            player.position = stationPos;
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

            if (Application.isPlaying) seed = int.Parse(PlayerPrefs.GetString(Manager.SeedKey, "0"));
            rng = new System.Random(seed);
            randomNoiseOffsets = new List<Vector2>();
            for (int i = 0; i < currentBiome.Regions.Length + 2; i++) randomNoiseOffsets.Add(new Vector2(rng.Next(-100000, 100000), rng.Next(-100000, 100000)));
            OnSeedChange?.Invoke(seed.ToString());

            groundCollider = new GameObject("Ground Collider").transform;
            groundCollider.parent = transform;
            groundCollider.gameObject.layer = LayerMask.NameToLayer("Ground");
            groundCollider.gameObject.AddComponent<BoxCollider>();

            AddChunk();

            await Task.Yield();
            if (Manager.Exists && Manager.instance is GameManager gm)
            {
                var locomotive = GetComponentInChildren<Locomotive>();
                locomotive.OnDeath += gm.EndGame;
                locomotives.Add(GetComponentInChildren<Locomotive>());
            }
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
            stationPos = IntPos + new Vector3Int(halfX, 0, rng.Next(halfZ, mapWidth - halfZ));
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
        /// <param name="tiles"></param>
        /// <param name="parent">Object the rows of the chunk are parented to</param>
        /// <param name="station">Bounds where the station resides if there is one in the chunk</param>
        /// <param name="checkpoint">Bounds where the checkpoint of the chunk resides</param>
        /// <param name="isGround">True if the desired section is ground tiles</param>
        private void GenerateSection(Tile[,] tiles, Transform parent, BoundsInt[] obstructions, bool isGround)
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
        /// <returns>The ground and obstacle tiles of the map</returns>
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
        #endregion

        #region Train Starting
        public void StartTrain() => StartTrain(trainInitialDelay);
        private async void StartTrain(float delay)
        {
            await Task.Delay(Mathf.RoundToInt(1000 * (delay - 5)));

            var locomotive = GetComponentInChildren<Locomotive>();
            if (locomotive == null) Debug.LogError("Map has no Locomotive");

            for (int countDown = 5; countDown > 0; countDown--)
            {
                Utils.FadeObject(numbersPrefabs[countDown], locomotive.transform.position + Vector3.up, numberFadeSpeed, numberFadeDuration);

                await Manager.Delay(1);
            }

            locomotive.StartTrain();
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
            if (tile == null) yield break;

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

        private IEnumerator TileHighlighting()
        {
            while (this)
            {
                RemoveOldHighlights();
                AddHighlights();

                newHighlights.Clear();

                yield return new WaitForEndOfFrame();
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
            if (Manager.Exists) return (Manager.IsPlaying() && PointIsInPlayBounds(point)) || (Manager.IsEditing() && PointIsInEditBounds(point));
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
            await Task.Delay(1000);
            OnFinishAnimateChunk?.Invoke();
            return;

            var chunk = transform.GetChild(chunkIndex + 1);
            Transform ground = chunk.GetChild(0), obstacles = chunk.GetChild(1);
            print(1);

            // Hiding and moving station and checkpoint
            var extras = new List<Transform>();
            var extraStartPos = new List<Vector3>();
            // Adds station to extras
            if (obstacles.childCount - ground.childCount == 2) extras.Add(obstacles.GetChild(obstacles.childCount - 2));
            // Adds checkpoint to extras
            if (obstacles.childCount - ground.childCount >= 1) extras.Add(obstacles.GetChild(obstacles.childCount - 1));
            foreach (var obj in extras)
            {
                extraStartPos.Add(obj.localPosition);
                obj.localPosition += spawnOffset;

                obj.gameObject.SetActive(false);
                foreach (var tile in obj.GetComponentsInChildren<Tile>()) tile.SetVisible(false);
            }
            print(ground.childCount);
            // Moving and hiding each row
            for (int i = 0; i < ground.childCount; i++)
            {
                ground.GetChild(i).localPosition += spawnOffset;
                obstacles.GetChild(i).localPosition += spawnOffset;

                ground.GetChild(i).gameObject.SetActive(false);
                // Hides obstacle graphics without affecting colliders
                foreach (var tile in obstacles.GetChild(i).GetComponentsInChildren<Tile>()) tile.SetVisible(false);
            }
            print(3);
            // Turning on and sliding each ground row into place
            foreach (Transform groundRow in ground)
            {
                groundRow.gameObject.SetActive(true);

                print("bruh");
                _ = AnimateSlideAndBounce(groundRow, Vector3.zero);
                print("bruh2");
                await Task.Delay(Mathf.RoundToInt(1000 * rowSpawnInterval));
                print("bruh3");
            }
            print(4);
            // Turning on and sliding each obstacle row into place
            for (int i = 0; i < ground.childCount; i++)
            {
                var obstacleRow = obstacles.GetChild(i);
                foreach (var tile in obstacleRow.GetComponentsInChildren<Tile>()) tile.SetVisible(true);

                _ = AnimateSlideAndBounce(obstacleRow, Vector3.zero);

                if (obstacleRow.childCount == 0) continue;
                await Task.Delay(Mathf.RoundToInt(500 * rowSpawnInterval));
            }
            print(5);
            // Turning on and sliding station and checkpoint into place
            foreach (var transform in extras)
            {
                transform.gameObject.SetActive(true);
                foreach (var tile in transform.GetComponentsInChildren<Tile>()) tile.SetVisible(true);
            }
            if (extras.Count > 0)
            {
                await AnimateSlide(extras[0], extraStartPos[0]);
                if (extras.Count > 1) await AnimateSlide(extras[1], extraStartPos[1]);
            }
            print(6);

            OnFinishAnimateChunk?.Invoke();
        }

        private async Task AnimateSlide(Transform t, Vector3 destination)
        {
            while (t.localPosition != destination)
            {
                t.localPosition = Vector3.MoveTowards(t.localPosition, destination, slideSpeed * Time.deltaTime);

                await Task.Yield();
            }
        }

        private async Task AnimateSlideAndBounce(Transform t, Vector3 destination)
        {
            await AnimateSlide(t, destination);

            var startTime = Time.time;
            while (Time.time - startTime <= 1)
            {
                t.localPosition = destination + bounceHeight * new Vector3(0, Mathf.Sin(2 * Mathf.PI * (Time.time - startTime)), 0);

                await Task.Yield();
            }

            t.localPosition = destination;
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
            if (visualizeBounds)
            {
                var bounds = GetPlayBounds();
                Gizmos.DrawCube(bounds.center, bounds.size);
            }
        }

        void OnDestroy()
        {
            if (!Manager.Exists) return;

            if (Manager.instance is GameManager gm)
            {
                gm.OnCheckpoint -= AddChunk;
                gm.OnCheckpoint -= DisableObstacles;
                gm.OnEndCheckpoint -= EnableObstacles;
                gm.OnEndCheckpoint -= AnimateNewChunk;
                gm.OnGameEnd -= ShowAllChunks;
                OnFinishAnimateChunk -= SpawnPlayer;
                OnFinishAnimateChunk -= StartTrain;
            }
            else if (Manager.instance is MainMenuManager mm)
            {
                OnFinishAnimateChunk -= mm.SlideInMainElements;
                OnFinishAnimateChunk -= mm.MoveSelectedSkinToGame;
            }
            else if (Manager.instance is TutorialManager)
            {
                OnFinishAnimateChunk -= SpawnPlayer;
            }

            var locomotive = GetComponentInChildren<Locomotive>();
            if (locomotive) locomotives.Remove(locomotive);
        }
    }
}