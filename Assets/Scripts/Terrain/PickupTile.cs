using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unrailed.Terrain
{
    public class PickupTile : Tile, IPickupable, IStackable
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

        public bool TryStackOn(IStackable stack)
        {
            return false;
            // TODO: Implement this
        }
    }
}