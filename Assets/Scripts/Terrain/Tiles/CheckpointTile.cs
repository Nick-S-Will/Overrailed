using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Managers;

namespace Uncooked.Terrain.Tiles
{
    public class CheckpointTile : Tile
    {
        override protected void Start()
        {
            GameManager.instance.OnCheckpoint += ReachCheckpoint;
        }

        protected virtual void ReachCheckpoint()
        {
            gameObject.SetActive(false);
        }
    }
}
