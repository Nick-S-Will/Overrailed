using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncooked.Terrain
{
    public abstract class Tool : Tile, IPickupable
    {
        public Transform handOffset;
        public int tier = 1;

        public Tile PickUp(Transform parent, int amount)
        {
            GetComponent<BoxCollider>().enabled = false;

            parent.localPosition = handOffset.localPosition;
            parent.localRotation = handOffset.localRotation;
            transform.parent = parent;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            return this;
        }

        public abstract bool InteractWith(Tile tile, RaycastHit hit);
    }
}