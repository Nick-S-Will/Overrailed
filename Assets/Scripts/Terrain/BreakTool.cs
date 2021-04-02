using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unrailed.Terrain
{
    public class BreakTool : Tool
    {
        [Tooltip("String all BreakableTile's you want this tool to break have in their names")] public string breakTileCode = "Tree";

        public override bool InteractWith(Tile tile, RaycastHit hit)
        {
            if (tile is BreakableTile breakT)
            {
                if (breakT.name.Contains(breakTileCode))
                {
                    breakT.TakeHit(tier, hit);
                    return true;
                }
            }

            return false;
        }
    }
}