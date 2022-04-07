using System.Collections.Generic;
using UnityEngine;

using Overrailed.Terrain.Tiles;

namespace Overrailed.Terrain.Generation
{
    [System.Serializable]
    public class Region
    {
        [SerializeField] private List<TileKey> keys = new List<TileKey>();

        private static readonly Color[] rainbow = new Color[] { Color.red, new Color(1, 0.647f, 0), Color.yellow, Color.green, Color.cyan, Color.blue, Color.magenta };
        private static (Color, Color) EmptyEvaluation(float percent) => (GetRainbow(1 - percent), GetRainbow(1 - percent));

        public int KeyCount => keys.Count;

        public Region() => new Region(new TileKey(null, null, 0), new TileKey(null, null, 1));

        public Region(params TileKey[] keys)
        {
            foreach (var key in keys) AddKey(key);
        }


        /// <summary>
        /// Gets the appropriate colors to draw on the slider for this region
        /// </summary>
        /// <param name="percent">Sample point on the slider</param>
        /// <returns>The ground and obstacle colors of the region</returns>
        public (Color, Color) Evaluate(float percent)
        {
            foreach (var key in keys)
            {
                if (percent <= key.Percent)
                {
                    var ground = key.GroundTile ? key.GroundColor : GetRainbow(percent);
                    var obstacle = key.ObstacleTile ? key.ObstacleColor : GetRainbow(percent);
                    return (ground, obstacle);
                }
            }

            return EmptyEvaluation(percent);
        }

        public TileKey GetKey(int index) => keys[index];

        /// <summary>
        /// Gets the tiles at the sample percentage
        /// </summary>
        /// <param name="percent">Sample point on the slider</param>
        /// <returns>The ground and obstacle tiles of the region</returns>
        public (Tile, Tile) GetTiles(float percent)
        {
            foreach (var key in keys) if (percent <= key.Percent) return (key.GroundTile, key.ObstacleTile);

            return (null, null);
        }

        /// <summary>
        /// Adds key to the region's list
        /// </summary>
        /// <returns>index of the new key</returns>
        public int AddKey(TileKey newKey)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                if (newKey.Percent < keys[i].Percent)
                {
                    keys.Insert(i, newKey);
                    return i;
                }
            }
            keys.Add(newKey);
            return keys.Count - 1;
        }
        public int MoveKey(int index, float percent)
        {
            TileKey key = keys[index];
            keys.Remove(key);

            return AddKey(new TileKey(key.GroundTile, key.ObstacleTile, percent));
        }
        public void RemoveKey(int index) => keys.RemoveAt(index);
        
        public void SetTiles(int index, Tile ground, Tile obstacle) => keys[index] = new TileKey(ground, obstacle, keys[index].Percent);
        public void SetPercent(int index, float percent) => keys[index] = new TileKey(keys[index].GroundTile, keys[index].ObstacleTile, percent);

        public Texture2D GetTexture(int width)
        {
            Texture2D texture = new Texture2D(width, 4);
            var colors = new Color[4 * width];

            for (int i = 0; i < width; i++)
            {
                var sample = Evaluate(i / (width - 1f));
                colors[i] = colors[i + width] = sample.Item1;
                colors[i + 2 * width] = colors[i + 3 * width] = sample.Item2;
            }
            texture.SetPixels(colors);
            texture.Apply();

            return texture;
        }

        private static Color GetRainbow(float percent)
        {
            for (int i = 1; i < rainbow.Length; i++)
            {
                if (percent <= (float)i / (rainbow.Length - 1))
                {
                    float lerpT = Mathf.InverseLerp((float)(i - 1) / (rainbow.Length - 1), (float)i / (rainbow.Length - 1), percent);
                    return Color.Lerp(rainbow[i - 1], rainbow[i], lerpT);
                }
            }
            return rainbow[rainbow.Length - 1];
        }

        [System.Serializable]
        public class TileKey
        {
            [SerializeField] private Tile groundTile, obstacleTile;
            [SerializeField] private Color groundColor, obstacleColor;
            [SerializeField] private float percent;

            public Tile GroundTile => groundTile;
            public Tile ObstacleTile => obstacleTile;
            public Color GroundColor => groundColor;
            public Color ObstacleColor => obstacleColor;
            public float Percent => percent;

            public TileKey(Tile _groundTile, Tile _obstacleTile, float _percent)
            {
                groundTile = _groundTile;
                groundColor = groundTile ? groundTile.GetComponentInChildren<Renderer>().sharedMaterial.color : Color.white;

                obstacleTile = _obstacleTile;
                obstacleColor = obstacleTile ? obstacleTile.GetComponentInChildren<Renderer>().sharedMaterial.color : Color.white;

                percent = _percent;
            }
        }
    }
}