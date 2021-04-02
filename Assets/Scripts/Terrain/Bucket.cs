using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unrailed.Terrain
{
    public class Bucket : Tool
    {
        private bool isFull;

        protected override void Start()
        {
            if (liquid != null && isFull) StartCoroutine(AnimateLiquid());
        }

        public override bool InteractWith(Tile tile, Vector3 point)
        {
            print(tile.name);
            if (tile.liquid != null)
            {
                isFull = !isFull;
                liquid.localScale = isFull ? Vector3.one : Vector3.zero;
                return true;
            }

            return false;
        }
    }
}