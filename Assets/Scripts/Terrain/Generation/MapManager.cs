using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Uncooked.Managers;
using Uncooked.Terrain.Tiles;

namespace Uncooked.Terrain.Generation
{
    public class MapManager : MonoBehaviour
    {
        [Header("Generation")] [SerializeField] private int seed = 0;
        [SerializeField] [Min(1)] private float noiseScale = 10;
        [SerializeField] private Vector2 noiseOffset;
        [Range(1, 1000)] [SerializeField] private int mapLength = 25, mapWidth = 19;

        [Header("Regions")] [SerializeField] private Biome groundBiome = Biome.Base;
        [SerializeField] private Biome obstacleBiome = Biome.Base;

        private System.Random rng;
        [Header("Parents")] [SerializeField] private Transform floorParent;
        [SerializeField] private Transform obstacleParent, railParent;

        [Header("Station")]
        [SerializeField] private GameObject stationPrefab;
        [SerializeField] private Tile stationBridge;
        [SerializeField] private Vector2Int stationSize = 5 * Vector2Int.one;

        private Vector3Int stationPos;

        public Vector3 StartPoint => stationPos;

        public void Start()
        {
            GameManager.instance.OnCheckpoint += HideObstacles;
            GameManager.instance.OnEndCheckpoint += ShowObstacles;

            HUDManager.instance.UpdateSeedText(seed.ToString());
        }

        /// <summary>
        /// Generates map floor and obstacles based on based on Generation variables
        /// </summary>
        public void GenerateMap()
        {
            if (!stationPrefab) throw new System.Exception("No stationPrefab given");

            System.Action<Object> destroyType = DestroyImmediate;
            if (Application.isPlaying) destroyType = Destroy;
            foreach (Transform t in transform.Cast<Transform>().ToList()) destroyType(t.gameObject);

            rng = new System.Random(seed);
            if (Application.isPlaying) HUDManager.instance.UpdateSeedText(seed.ToString());
            var heightMap = GenerateHeightMap();

            // Station info
            int stationHalfX = stationSize.x / 2, stationHalfZ = stationSize.y / 2;
            stationPos = new Vector3Int(stationHalfX, 0, rng.Next(stationHalfZ, mapWidth - stationHalfZ));
            BoundsInt stationBounds = new BoundsInt(stationPos - new Vector3Int(stationHalfX, 0, stationHalfZ), new Vector3Int(stationSize.x, 3, stationSize.y));

            #region Map Floor
            floorParent = new GameObject("Ground").transform;
            floorParent.parent = transform;

            for (int x = 0; x < mapLength; x++)
            {
                var rowParent = new GameObject("Row " + x).transform;
                rowParent.parent = floorParent;

                for (int z = 0; z < mapWidth; z++)
                {
                    // Gets tile type based on height map
                    var tile = groundBiome.GetTile(heightMap[x, z]);
                    var obj = Instantiate(tile, new Vector3(x, transform.position.y, z), Quaternion.identity, rowParent);
                    obj.name = obj.name.Substring(0, obj.name.Length - 7) + " " + z;

                    // Adds bridge to liquid tiles under the station
                    if (obj is LiquidTile liquid && stationBounds.Contains(new Vector3Int(x, (int)transform.position.y, z)))
                    {
                        Instantiate(stationBridge, liquid.transform.position, liquid.transform.rotation, liquid.transform.parent);
                        liquid.GetComponent<BoxCollider>().enabled = false;
                    }
                }
            }

            var groundCollider = new GameObject("Ground Collider").transform;
            groundCollider.parent = transform;
            groundCollider.position = new Vector3(mapLength / 2f - 0.5f, transform.position.y -0.5f, mapWidth / 2f - 0.5f);
            groundCollider.gameObject.AddComponent<BoxCollider>().size = new Vector3(mapLength, 1, mapWidth);
            groundCollider.gameObject.layer = LayerMask.NameToLayer("Ground");
            #endregion

            #region Map Obstacles
            obstacleParent = new GameObject("Obstacles").transform;
            obstacleParent.parent = transform;

            // Station placement
            Instantiate(stationPrefab, stationPos, Quaternion.identity, obstacleParent);

            for (int x = 0; x < mapLength; x++)
            {
                var rowParent = new GameObject("Row " + x).transform;
                rowParent.parent = obstacleParent;

                for (int z = 0; z < mapWidth; z++)
                {
                    if (stationBounds.Contains(new Vector3Int(x, (int)transform.position.y + 1, z))) continue;

                    // Gets tile type based on height map
                    Tile tile = obstacleBiome.GetTile(heightMap[x, z]);
                    if (tile == null) continue;

                    var obj = Instantiate(tile, new Vector3(x, transform.position.y + 1, z), Quaternion.Euler(0, rng.Next(3) * 90, 0), rowParent);
                }

                if (rowParent.childCount == 0) DestroyImmediate(rowParent.gameObject);
            }
            #endregion
        }

        /// <summary>
        /// Generates 2D perlin noise based on generation variables
        /// </summary>
        /// <param name="noiseScale"></param>
        /// <returns></returns>
        private float[,] GenerateHeightMap()
        {
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

        /// <summary>
        /// Places given Tile at coords, sets obstacleParent as its parent, and enables its BoxCollider
        /// </summary>
        public void PlacePickup(IPickupable pickup, Vector3Int coords)
        {
            Transform obj = (pickup as Tile).transform;

            obj.parent = pickup is RailTile ? railParent : obstacleParent;
            obj.position = coords;
            obj.localRotation = Quaternion.identity;

            obj.GetComponent<BoxCollider>().enabled = true;
            pickup.Drop(coords);
        }

        private void HideObstacles() => SetObstacles(false);
        private void ShowObstacles() => SetObstacles(true);

        private void SetObstacles(bool enabled)
        {
            obstacleParent.gameObject.SetActive(enabled);
            floorParent.gameObject.SetActive(enabled);
        }

        void OnValidate()
        {
            if (stationSize[0] < 1) stationSize[0] = 1;
            if (stationSize[1] < 1) stationSize[1] = 1;
        }
    }
}