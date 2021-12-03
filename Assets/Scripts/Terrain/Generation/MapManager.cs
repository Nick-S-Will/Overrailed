using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Uncooked.Player;
using Uncooked.Managers;
using Uncooked.Terrain.Tiles;

namespace Uncooked.Terrain.Generation
{
    public class MapManager : MonoBehaviour
    {
        [Header("Generation")] [SerializeField] private int seed = 0;
        [SerializeField] [Min(1)] private float noiseScale = 10;
        [SerializeField] private Vector2 noiseOffset;
        [Range(1, 1000)] [SerializeField] private int chunkLength = 25, mapWidth = 19;

        [Header("Regions")] [SerializeField] private Biome groundBiome = Biome.Base;
        [SerializeField] private Biome obstacleBiome = Biome.Base;
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
        [SerializeField] private Vector3 spawnDelta = 10 * Vector3.up;
        [SerializeField] [Min(1)] private float spawnSpeed = 100;
        [SerializeField] [Min(0)] private float spawnTimeOffset = 0.2f;

        [SerializeField] [HideInInspector] private List<Transform> chunks = new List<Transform>();
        [SerializeField] [HideInInspector] private Transform groundParent, groundCollider, obstacleParent;
        [SerializeField] [HideInInspector] private Vector3Int stationPos;
        [SerializeField] [HideInInspector] private System.Random rng;

        private PlayerController player;
        private Transform lastHighlightTile;

        public Vector3 StartPoint => stationPos;

        public void Start()
        {
            GameManager.instance.OnCheckpoint += AddChunk; // Must be before DisableObstacles
            GameManager.instance.OnCheckpoint += DisableObstacles; // Must be after AddChunk
            GameManager.instance.OnEndCheckpoint += EnableObstacles;
            GameManager.instance.OnEndCheckpoint += AnimateNewChunk;
            foreach (var p in FindObjectsOfType<PlayerController>()) p.OnPlacePickup += PlacePickup;

            HUDManager.instance.UpdateSeedText(seed.ToString());
            player = FindObjectOfType<PlayerController>();
            rng = new System.Random(seed);
        }

        void Update()
        {
            // Highlights tile player is looking at
            var tile = GetTileAt(player.LookPoint);
            if (tile == null) tile = GetTileAt(player.LookPoint + Vector3Int.down);
            TryHighlightTile(tile);
        }

        #region Tile highlighting
        private void TryHighlightTile(Transform tile)
        {
            if (tile == lastHighlightTile) return;
            else if (tile == null)
            {
                lastHighlightTile = null;
                return;
            }

            lastHighlightTile = tile;
            _ = StartCoroutine(HightlightTile(tile));
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

            yield return new WaitWhile(() => tile == lastHighlightTile);

            // Reset colors
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) break;
                else renderers[i].material.color = originalColors[i];
            }
        }
        #endregion

        #region Generation
        /// <summary>
        /// Clears current map and generates the first chunk of a new one
        /// </summary>
        public void GenerateMap()
        {
            chunks.Clear();
            System.Action<Object> destroyType = DestroyImmediate;
            if (Application.isPlaying) destroyType = Destroy;
            foreach (Transform t in transform.Cast<Transform>().ToList()) destroyType(t.gameObject);
            rng = new System.Random(seed);

            AddChunk();
        }

        /// <summary>
        /// Generates map floor and obstacles based on based on Generation variables
        /// </summary>
        public void AddChunk()
        {
            if (Application.isPlaying) HUDManager.instance.UpdateSeedText(seed.ToString());
            var heightMap = GenerateHeightMap();

            var newChunk = new GameObject("Chunk " + chunks.Count).transform;
            chunks.Add(newChunk);
            var mapLength = chunks.Count * chunkLength;

            // Station info
            int halfX = stationSize.x / 2, halfZ = stationSize.y / 2;
            stationPos = new Vector3Int(halfX, (int)transform.position.y, rng.Next(halfZ, mapWidth - halfZ));
            BoundsInt stationBounds = new BoundsInt(stationPos - new Vector3Int(halfX, 0, halfZ), new Vector3Int(stationSize.x, 3, stationSize.y));
            // Checkpoint info
            halfX = checkpointSize.x / 2; halfZ = checkpointSize.y / 2;
            Vector3Int checkpointPos = new Vector3Int(mapLength - halfX - 1, (int)transform.position.y, rng.Next(halfZ, mapWidth - halfZ));
            BoundsInt checkpointBounds = new BoundsInt(checkpointPos - new Vector3Int(halfX, 0, halfZ), new Vector3Int(checkpointSize.x, 3, checkpointSize.y));

            // Ground tiles
            groundParent = new GameObject("Ground").transform;
            groundParent.parent = newChunk;
            GenerateSection(heightMap, groundBiome, groundParent, stationBounds, checkpointBounds, true);

            // Ground collider
            if (groundCollider == null)
            {
                groundCollider = new GameObject("Ground Collider").transform;
                groundCollider.parent = transform;
                groundCollider.gameObject.layer = LayerMask.NameToLayer("Ground");
                groundCollider.gameObject.AddComponent<BoxCollider>();
            }
            groundCollider.gameObject.GetComponent<BoxCollider>().size = new Vector3(mapLength, 1, mapWidth);
            groundCollider.position = new Vector3(mapLength / 2f - 0.5f, transform.position.y - 0.5f, mapWidth / 2f - 0.5f);

            // Obstacle tiles
            obstacleParent = new GameObject("Obstacles").transform;
            obstacleParent.parent = newChunk;
            GenerateSection(heightMap, obstacleBiome, obstacleParent, stationBounds, checkpointBounds, false);

            // Spawn station and checkpoint
            if (chunks.Count == 1) Instantiate(stationPrefab, stationPos, Quaternion.identity, obstacleParent).transform.Find("Bonus").gameObject.SetActive(startBonus);
            _ = Instantiate(checkpointPrefab, checkpointPos, Quaternion.identity, obstacleParent);

            newChunk.parent = transform;
        }

        private void GenerateSection(float[,] heightMap, Biome biome, Transform parent, BoundsInt station, BoundsInt checkpoint, bool isGround)
        {
            int mapLength = chunks.Count * chunkLength;

            for (int x = mapLength - chunkLength; x < mapLength; x++)
            {
                var rowParent = new GameObject("Row " + x).transform;
                rowParent.parent = parent;

                for (int z = 0; z < mapWidth; z++)
                {
                    Tile bridge = null;

                    // Gets tile type based on height map and biome
                    var tilePrefab = biome.GetTile(heightMap[x, z]);
                    if (tilePrefab == null) continue;

                    var newPos = new Vector3Int(x, (int)transform.position.y, z);
                    if (!isGround) newPos += Vector3Int.up;

                    // Modifies station and checkpoint areas
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

                    var tileObj = Instantiate(tilePrefab, newPos, Quaternion.identity, rowParent);
                    tileObj.name = tileObj.name.Substring(0, tileObj.name.Length - 7) + " " + z;
                    if (bridge)
                    {
                        bridge.transform.parent = tileObj.transform;
                        tileObj.GetComponent<BoxCollider>().enabled = false;
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
        #endregion

        /// <summary>
        /// Gets the tile at the given coords
        /// </summary>
        /// <param name="pos">Coords of the searched tile</param>
        /// <returns>The transform of the tile at <paramref name="pos"/> if there is one, otherwise null</returns>
        public Transform GetTileAt(Vector3Int pos)
        {
            if (!PlayerController.PointIsInBounds(pos)) return null;

            if (pos.y == transform.position.y)
            {
                var row = GetParentAt(pos);
                return row.GetChild(pos.z);
            }
            else
            {
                _ = Physics.Raycast(new Vector3(pos.x, transform.position.y - 0.5f, pos.z), Vector3.up, out RaycastHit hitData, 1, player.InteractMask);

                return hitData.transform;
            }
        }

        private Transform GetParentAt(Vector3Int position)
        {
            try
            {
                var chunk = transform.GetChild(position.x / chunkLength + 1);
                var section = chunk.GetChild(position.y == transform.position.y ? 0 : 1); // Ground or Obstacles
                var row = section.GetChild(position.x % chunkLength);
                return row;
            }
            catch (UnityException) { return null; }
        }

        /// <summary>
        /// Places given Tile at coords, sets obstacleParent as its parent, and enables its BoxCollider
        /// </summary>
        public void PlacePickup(IPickupable pickup, Vector3Int coords)
        {
            Transform obj = (pickup as Tile).transform;

            obj.parent = GetParentAt(coords);
            obj.position = coords;
            obj.localRotation = Quaternion.identity;

            obj.GetComponent<BoxCollider>().enabled = true;
            pickup.Drop(coords);
        }

        private void DisableObstacles() => SetObstacleHitboxes(false);
        private void EnableObstacles() => SetObstacleHitboxes(true);

        private void SetObstacleHitboxes(bool enabled)
        {
            for (int i = 1; i < transform.childCount; i++)
            {
                foreach (var collider in transform.GetChild(i).GetChild(1).GetComponentsInChildren<BoxCollider>())
                {
                    if (collider.gameObject.layer == LayerMask.NameToLayer("Default")) collider.enabled = enabled;
                }
            }
        }

        #region Spawning animation
        public void AnimateNewChunk() => _ = StartCoroutine(AnimateChunk(chunks[chunks.Count - 1]));

        private IEnumerator AnimateChunk(Transform chunk)
        {
            Transform ground = chunk.GetChild(0), obstacles = chunk.GetChild(1);

            // Hiding and moving station and checkpoint
            var extras = new List<Transform>();
            var extraStartPos = new List<Vector3>();
            if (obstacles.childCount - ground.childCount == 2) extras.Add(obstacles.GetChild(obstacles.childCount - 2));
            extras.Add(obstacles.GetChild(obstacles.childCount - 1));
            foreach (var obj in extras)
            {
                extraStartPos.Add(obj.localPosition);
                obj.localPosition -= spawnDelta;

                obj.GetChild(0).gameObject.SetActive(false);
                foreach (var tile in obj.GetComponentsInChildren<Tile>()) tile.SetVisible(false);
            }

            // Moving and hiding each row
            for (int i = 0; i < ground.childCount; i++)
            {
                ground.GetChild(i).localPosition -= spawnDelta;
                obstacles.GetChild(i).localPosition -= spawnDelta;

                ground.GetChild(i).gameObject.SetActive(false);
                // Hides obstacle graphics without affecting colliders
                foreach (var tile in obstacles.GetChild(i).GetComponentsInChildren<Tile>()) tile.SetVisible(false);
            }

            // Turning on and sliding each row into place
            for (int i = 0; i < ground.childCount; i++)
            {
                Transform groundRow = ground.GetChild(i), obstacleRow = obstacles.GetChild(i);
                groundRow.gameObject.SetActive(true);
                // Shows obstacle graphics without affecting colliders
                foreach (var tile in obstacleRow.GetComponentsInChildren<Tile>()) tile.SetVisible(true);

                _ = StartCoroutine(AnimateSpawnSlide(groundRow, Vector3.zero));
                _ = StartCoroutine(AnimateSpawnSlide(obstacleRow, Vector3.zero));

                yield return new WaitForSeconds(spawnTimeOffset);
            }

            // Turning on and sliding station and checkpoint into place
            foreach (var obj in extras)
            {
                obj.GetChild(0).gameObject.SetActive(true);
                foreach (var tile in obj.GetComponentsInChildren<Tile>()) tile.SetVisible(true);
            }
            _ = StartCoroutine(AnimateSpawnSlide(extras[0], extraStartPos[0]));
            if (extras.Count > 1) _ = StartCoroutine(AnimateSpawnSlide(extras[1], extraStartPos[1]));
        }

        private IEnumerator AnimateSpawnSlide(Transform t, Vector3 destination)
        {
            while (t.localPosition != destination)
            {
                t.localPosition = Vector3.MoveTowards(t.localPosition, destination, spawnSpeed * Time.deltaTime);

                yield return null;
            }
        }
        #endregion

        void OnValidate()
        {
            if (stationSize[0] < 1) stationSize[0] = 1;
            if (stationSize[1] < 1) stationSize[1] = 1;
        }

        void OnDestroy()
        {
            if (GameManager.instance == null) return;

            GameManager.instance.OnCheckpoint -= AddChunk;
            GameManager.instance.OnCheckpoint -= DisableObstacles;
            GameManager.instance.OnEndCheckpoint -= EnableObstacles;
            GameManager.instance.OnEndCheckpoint -= AnimateNewChunk;
            foreach (var p in FindObjectsOfType<PlayerController>()) p.OnPlacePickup -= PlacePickup;
        }
    }
}