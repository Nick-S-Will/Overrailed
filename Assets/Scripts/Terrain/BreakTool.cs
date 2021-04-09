using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncooked.Terrain
{
    public class BreakTool : Tool
    {
        [Tooltip("String all BreakableTile's you want this tool to break have in their names")] public string breakTileCode = "Tree";

        /// <summary>
        /// Tries to break given Tile
        /// </summary>
        /// <param name="interactable">Tile to be interacted with</param>
        /// <param name="hit">Info about the Raycast used to find this</param>
        /// <returns>True if tile is BreakTile and it takes a hit</returns>
        public override bool InteractWith(IInteractable interactable, RaycastHit hit)
        {
            if (interactable is BreakableTile breakT)
            {
                if (breakT.name.Contains(breakTileCode))
                {
                    breakT.TakeHit(Tier, hit);
                    return true;
                }
            }

            return false;
        }
    }
}