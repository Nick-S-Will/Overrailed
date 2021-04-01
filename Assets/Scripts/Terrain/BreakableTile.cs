using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unrailed.Terrain
{
    public class BreakableTile : Tile, IDamageable
    {
        public Tile lowerTier;

        public void TakeHit(int damage, Vector3 point)
        {
            throw new System.NotImplementedException();
        }
    }
}