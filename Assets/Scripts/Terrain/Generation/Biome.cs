using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Terrain.Tiles;

namespace Overrailed.Terrain.Generation
{
    [CreateAssetMenu(fileName = "New Biome", menuName = "Biome")]
    public class Biome : ScriptableObject
    {
        [SerializeField] private Tile liquid;
        [SerializeField] [Range(0, 1)] private float liquidMaxHeight = 0.25f;
        [Space]
        [SerializeField] private List<Region> regions = new List<Region>( new Region[] { new Region(), new Region() } );

        public Tile LiquidTile => liquid;
        public float MinObstaclePercentage => liquidMaxHeight + 0.05f;
        public Region[] Regions => regions.ToArray();
    }
}