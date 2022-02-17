using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncooked.Terrain.Generation
{
    [CreateAssetMenu(fileName = "New Biome", menuName = "Biome")]
    public class Biome : ScriptableObject
    {
        [SerializeField] private Region groundRegion = Region.Default;
        [SerializeField] [Range(0, 1)] private float minObstaclePercentage = 0.4f;
        [SerializeField] private Region treeRegion = Region.Default, stoneRegion = Region.Default;

        public Region GroundRegion => groundRegion;
        public float MinObstaclePercentage => minObstaclePercentage;
        public Region TreeRegion => treeRegion;
        public Region StoneRegion => stoneRegion;
    }
}