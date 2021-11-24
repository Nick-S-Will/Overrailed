using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Managers;

namespace Uncooked.Terrain.Tiles
{
    public class CheckpointTile : Tile
    {
        void Start()
        {
            GameManager.instance.OnCheckpoint += ReachCheckpoint;
        }

        private void ReachCheckpoint()
        {
            gameObject.SetActive(false);
        }
    }
}
