using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Managers;

namespace Overrailed.Terrain.Tiles
{
    public class ReplaceAtCheckpointTile : CheckpointTile
    {
        [SerializeField] private StackTile replacementTile;

        protected override void Start()
        {
            if (Manager.instance is GameManager gm) gm.OnEndCheckpoint += EndCheckpoint;
            else Destroy(gameObject);
        }

        protected override void EndCheckpoint()
        {
            var map = MapManager.FindMap(transform.position);
            if (map)
            {
                gameObject.SetActive(false);
                map.PlacePickup(Instantiate(replacementTile), Coords);
                Destroy(gameObject);
            }
        }

        protected override void OnDestroy() => base.OnDestroy();
    }
}
