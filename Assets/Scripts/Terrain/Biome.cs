using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unrailed.Terrain
{
    [System.Serializable]
    public class Biome
    {
        private List<TileKey> keys = new List<TileKey>();

        public static Biome Rainbow
        {
            get
            {
                Biome b = new Biome();
                b.AddKey(new TileKey(null, 1));
                return b;
            }
        }

        public Color Evaluate(float percent)
        {
            foreach (var key in keys) if (percent <= key.percent) return key.tile == null ? GetRainbow(percent) : key.Color;
            return Color.white;
        }

        public int KeyCount => keys.Count;
        public TileKey GetKey(int index) => keys[index];
        public Tile GetTile(float percent)
        {
            foreach (var key in keys) if (percent <= key.percent) return key.tile;
            return keys[keys.Count - 1].tile;
        }

        public int AddKey(TileKey newKey)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                if (newKey.percent < keys[i].percent)
                {
                    keys.Insert(i, newKey);
                    return i;
                }
            }
            keys.Add(newKey);
            return keys.Count - 1;
        }
        public int MoveKey(int index, float _percent)
        {
            TileKey key = keys[index];
            keys.RemoveAt(index);
            return AddKey(new TileKey(key.tile, _percent));
        }
        public void RemoveKey(int index)
        {
            keys.RemoveAt(index);
        }
        public void SetTile(int index, Tile newTile) => keys[index] = new TileKey(newTile, keys[index].percent);
        public void SetPercent(int index, float _percent) => keys[index] = new TileKey(keys[index].tile, _percent);

        public Texture2D GetTexture(int width)
        {
            Texture2D texture = new Texture2D(width, 1);
            Color[] colors = new Color[width];

            for (int i = 0; i < width; i++) colors[i] = Evaluate((float)i / (width - 1));
            texture.SetPixels(colors);
            texture.Apply();

            return texture;
        }

        private static Color GetRainbow(float percent)
        {
            Color[] rainbow = new Color[] { Color.red, new Color(1, 0.647f, 0), Color.yellow, Color.green, Color.cyan, Color.blue, Color.magenta };

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
        public struct TileKey
        {
            public Tile tile { get; private set; }
            public float percent { get; private set; }

            public Color Color => color;

            private Color color;

            public TileKey(Tile _tile, float _percent)
            {
                tile = _tile;
                if (tile == null) color = Color.white;
                else color = tile.GetComponentInChildren<Renderer>().sharedMaterial.color;
                percent = _percent;
            }
        }
    }
}