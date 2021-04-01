using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unrailed.Terrain
{
    public class PickupTile : Tile, IPickupable
    {
        public enum Type { Wood, Rock, Rail }

        public Type pickupType;
        public float tileHeight;

        public void PickUp(Transform parent)
        {
            GetComponent<BoxCollider>().enabled = false;

            transform.parent = parent;
            transform.localPosition = Vector3.up;
        }

        public void TryStackOn(PickupTile tile)
        {
            // TODO: Implement this
        }
    }
}