using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Terrain.Generation;
using Overrailed.Terrain.Tiles;

namespace Overrailed.Terrain.Tiles
{
    public class ReplaceAtCheckpointTile : CheckpointTile
    {
        [SerializeField] private StackTile replacementTile;

        protected override void Start() => base.Start();

        protected override void EndCheckpoint()
        {
            foreach (MapManager map in FindObjectsOfType<MapManager>())
            {
                if (map.PointIsInPlayBounds(transform.position))
                {
                    map.PlacePickup(Instantiate(replacementTile), Vector3Int.RoundToInt(transform.position + Vector3.up));
                    Destroy(gameObject);
                }
            }
        }

        protected override void OnDestroy() => base.OnDestroy();
    }
}
