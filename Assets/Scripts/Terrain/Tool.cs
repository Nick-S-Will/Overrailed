using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unrailed.Terrain
{
    public abstract class Tool : Tile, IPickupable
    {
        public Transform handOffset;
        public int tier = 1;

        public void PickUp(Transform parent)
        {
            GetComponent<BoxCollider>().enabled = false;

            parent.localPosition = handOffset.localPosition;
            parent.localRotation = handOffset.localRotation;
            transform.parent = parent;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        public abstract bool InteractWith(Tile tile, Vector3 point);
    }
}