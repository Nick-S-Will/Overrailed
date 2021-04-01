using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unrailed.Terrain
{
    public abstract class Tool : Tile, IPickupable
    {
        public int tier;

        public void PickUp(Transform parent)
        {
            GetComponent<BoxCollider>().enabled = false;

            transform.parent = parent;
            transform.localPosition = Vector3.up;
            transform.localRotation = Quaternion.identity;
        }

        public abstract void InteractWith(Tile tile);
    }
}