using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncooked.Terrain.Tools
{
    public class BreakTool : Tool
    {
        [Tooltip("String all BreakableTile's you want this tool to break have in their names")]
        [SerializeField] private string breakTileCode = "Tree";

        public string BreakTileCode => breakTileCode;
    }
}