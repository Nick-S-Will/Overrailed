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
            GameManager.instance.OnEndCheckpoint += EndCheckpoint;
        }

        protected virtual void ReachCheckpoint()
        {
            gameObject.SetActive(false);
        }

        protected virtual void EndCheckpoint() { }

        protected virtual void OnDestroy()
        {
            if (GameManager.instance)
            {
                GameManager.instance.OnCheckpoint -= ReachCheckpoint;
                GameManager.instance.OnEndCheckpoint -= EndCheckpoint;
            }
        }
    }
}
