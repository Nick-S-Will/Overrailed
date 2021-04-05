using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncooked.Terrain
{
    public class Bucket : Tool
    {
        private bool isFull;

        protected override void Start()
        {
            liquid.gameObject.SetActive(isFull);
            if (liquid != null) StartCoroutine(AnimateLiquid());
        }

        public override bool InteractWith(Tile tile, RaycastHit hit)
        {
            print(tile.name);
            if (tile.liquid != null)
            {
                isFull = !isFull;
                liquid.gameObject.SetActive(isFull);
                return true;
            }

            return false;
        }
    }
}