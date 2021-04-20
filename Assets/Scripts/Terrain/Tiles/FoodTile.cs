using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncooked.Terrain.Tiles
{
    public class FoodTile : StackTile
    {
        [Space]
        [SerializeField] private StackTile dishPrefab;
    }
}