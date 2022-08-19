using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Overrailed.Terrain.Tools
{
    public class BreakTool : Tool
    {
        public override event System.Action OnPickUp;
        public override event System.Action OnDrop;

        [Tooltip("String all BreakableTile's you want this tool to break have in their names")]
        [SerializeField] private string breakTileCode = "Tree";

        public string BreakTileCode => breakTileCode;
    }
}