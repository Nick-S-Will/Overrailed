using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Overrailed.Terrain.Tiles
{
    public class FoodTile : StackTile
    {
        [Space]
        [SerializeField] private StackTile dishPrefab;

        public override bool CanPickUp => true;
    }
}