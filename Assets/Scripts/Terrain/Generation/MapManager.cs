using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Uncooked.Terrain.Tiles;

namespace Uncooked.Terrain.Generation
{
    public class MapManager : MonoBehaviour
    {
        [Header("Generation")] [SerializeField] private int seed = 0;
        [SerializeField] private float noiseScale = 10;
        [SerializeField] private Vector2 noiseOffset;
        [Range(1, 1000)] [SerializeField] private int mapLength = 25, mapWidth = 19;

        [Header("Regions")] [SerializeField] private Biome groundBiome = Biome.Base;
        [SerializeField] private Biome obstacleBiome = Biome.Base;

        private System.Random rng;
        [Header("Parents")] [SerializeField] private Transform floorParent;
        [SerializeField] private Transform obstacleParent;

        /// <summary>
        /// Generates map floor and obstacles based on based on Generation variables
        /// </summary>
        public void GenerateMap()
        {
            foreach (Transform t in transform.Cast<Transform>().ToList()) DestroyImmediate(t.gameObject);

            rng = new System.Random(seed);
            var heightMap = GenerateHeightMap(noiseScale);

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
                    Tile tile = groundBiome.GetTile(heightMap[x, z]);
                    var obj = Instantiate(tile, new Vector3(x, transform.position.y, z), Quaternion.identity, rowParent);
                    obj.name = obj.name.Substring(0, obj.name.Length - 7) + " " + z;
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

            for (int x = 0; x < mapLength; x++)
            {
                var rowParent = new GameObject("Row " + x).transform;
                rowParent.parent = obstacleParent;

                for (int z = 0; z < mapWidth; z++)
                {
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
        /// Generates 2D perlin noise based on Generation variables
        /// </summary>
        /// <param name="noiseScale"></param>
        /// <returns></returns>
        private float[,] GenerateHeightMap(float noiseScale)
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

            obj.parent = obstacleParent;
            obj.position = coords;
            obj.localRotation = Quaternion.identity;

            obj.GetComponent<BoxCollider>().enabled = true;
            pickup.Drop(coords);
        }
    }
}