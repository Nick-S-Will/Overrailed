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
            if (Manager.instance is GameManager gm)
            {
                gm.OnCheckpoint += ReachCheckpoint;
                gm.OnEndCheckpoint += EndCheckpoint;
            }
            else Destroy(gameObject);
        }

        protected virtual void ReachCheckpoint()
        {
            gameObject.SetActive(false);
        }

        protected virtual void EndCheckpoint() { }

        protected virtual void OnDestroy()
        {
            if (Manager.instance is GameManager gm)
            {
                gm.OnCheckpoint -= ReachCheckpoint;
                gm.OnEndCheckpoint -= EndCheckpoint;
            }
        }
    }
}
