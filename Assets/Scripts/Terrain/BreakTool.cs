using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unrailed.Terrain
{
    public class BreakTool : Tool
    {
        [Tooltip("String all BreakableTile's you want this tool to break have in their name")] public string breakTileCode = "Tree";

        public override bool InteractWith(Tile tile, Vector3 point)
        {
            if (tile is BreakableTile breakT)
            {
                if (breakT.name.Contains(breakTileCode))
                {
                    breakT.TakeHit(tier, point);
                    return true;
                }
            }

            return false;
        }
    }
}