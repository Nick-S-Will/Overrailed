using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Managers;

namespace Overrailed.Terrain.Tiles
{
    public class CheckpointTile : Tile
    {
        override protected void Start()
        {
            var gm = Manager.instance as GameManager;
            gm.OnCheckpoint += ReachCheckpoint;
            gm.OnEndCheckpoint += EndCheckpoint;
        }

        protected virtual void ReachCheckpoint()
        {
            gameObject.SetActive(false);
        }

        protected virtual void EndCheckpoint() { }

        protected virtual void OnDestroy()
        {
            if (Manager.Exists)
            {
                var gm = Manager.instance as GameManager;
                gm.OnCheckpoint -= ReachCheckpoint;
                gm.OnEndCheckpoint -= EndCheckpoint;
            }
        }
    }
}
