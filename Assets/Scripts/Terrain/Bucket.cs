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

        /// <summary>
        /// Tries to pick up or drop liquid
        /// </summary>
        /// <param name="obj">Tile to be interacted with</param>
        /// <param name="hit">Info about the Raycast used to find this</param>
        /// <returns>True if </returns>
        public override bool InteractWith(IInteractable interactable, RaycastHit hit)
        {
            if (interactable is Tile tile && tile.Liquid != null)
            {
                isFull = !isFull;
                liquid.gameObject.SetActive(isFull);
                return true;
            }

            return false;
        }
    }
}