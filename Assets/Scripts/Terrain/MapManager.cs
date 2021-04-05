using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unrailed.Terrain
{
    public class MapManager : MonoBehaviour
    {
        [Header("Generation")] public int seed = 0;
        public float noiseScale = 10;
        public Vector2 noiseOffset;
        [Range(1, 1000)] public int mapLength = 5, mapWidth = 5;

        [Header("Regions")] public Biome groundBiome = Biome.Rainbow;
        public Biome obstacleBiome = Biome.Rainbow;

        private System.Random rng;
        private Transform floorParent, obstacleParent;

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
                    var obj = Instantiate(tile, new Vector3(x, 0, z), Quaternion.identity, rowParent);
                    obj.name = obj.name.Substring(0, obj.name.Length - 7) + " " + z;
                }
            }

            var groundCollider = new GameObject("Ground Collider").transform;
            groundCollider.parent = transform;
            groundCollider.position = new Vector3(mapLength / 2f - 0.5f, -0.5f, mapWidth / 2f - 0.5f);
            groundCollider.gameObject.AddComponent<BoxCollider>().size = new Vector3(mapLength, 1, mapWidth);
            #endregion

            #region Map Obstacles
            obstacleParent = new GameObject("Obstacles").transform;
            obstacleParent.parent = transform;

            for (int x = 0; x < mapLength; x++)
            {
                for (int z = 0; z < mapWidth; z++)
                {
                    // Gets tile type based on height map
                    Tile tile = obstacleBiome.GetTile(heightMap[x, z]);
                    if (tile == null) continue;

                    var obj = Instantiate(tile, new Vector3(x, 1, z), Quaternion.Euler(0, rng.Next(3) * 90, 0), obstacleParent);
                }
            }
            #endregion
        }

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

        public void PlaceTile(Tile tile, Vector3 position)
        {
            tile.transform.parent = obstacleParent;
            tile.transform.position = Vector3Int.RoundToInt(position);
            tile.transform.localRotation = Quaternion.identity;

            tile.GetComponent<BoxCollider>().enabled = true;
        }

        [System.Serializable]
        public struct Region
        {
            [Range(0, 1)] public float maxHeight;
            public Tile tile;

            public static Tile GetTile(Region[] regions, float percent)
            {
                foreach (var r in regions) if (percent < r.maxHeight) return r.tile;
                return regions[regions.Length - 1].tile;
            }
        }
    }
}