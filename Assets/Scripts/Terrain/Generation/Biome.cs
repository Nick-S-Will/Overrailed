using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncooked.Terrain.Generation
{
    [CreateAssetMenu(fileName = "New Biome", menuName = "Biome")]
    public class Biome : ScriptableObject
    {
        [SerializeField] private Region groundRegion = Region.Default;
        [SerializeField] private Region treeRegion = Region.Default, stoneRegion = Region.Default;

        public Region GroundRegion => groundRegion;
        public float MinObstaclePercentage => Mathf.Min(groundRegion.GetKey(0).Percent + 0.05f, 1);
        public Region TreeRegion => treeRegion;
        public Region StoneRegion => stoneRegion;
    }
}