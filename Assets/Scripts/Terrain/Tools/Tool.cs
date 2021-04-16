using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Terrain.Tiles;

namespace Uncooked.Terrain.Tools
{
    public abstract class Tool : Tile, IPickupable
    {
        public event System.Action<Tool> OnPickup, OnDrop;

        [SerializeField] private Transform handOffset;
        [SerializeField] private int tier = 1;

        public int Tier => tier;
        public bool IsTwoHanded() => false;

        /// <summary>
        /// Picks up this Tool
        /// </summary>
        /// <param name="parent">Transform this will be parented to</param>
        /// <returns>Tool that was picked up</returns>
        public IPickupable TryPickUp(Transform parent, int amount)
        {
            GetComponent<BoxCollider>().enabled = false;

            parent.localPosition = handOffset.localPosition;
            parent.localRotation = handOffset.localRotation;
            transform.parent = parent;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            OnPickup?.Invoke(this);
            return this;
        }

        /// <summary>
        /// Used in PlayerController.TryDrop() to do custom action, and can choose if the Tool is held or dropped
        /// </summary>
        /// <returns>True if implementation wants Tool to continue being held, otherwise false</returns>
        public virtual bool OnTryDrop() { return false; }

        public virtual void Drop(Vector3Int position) => OnDrop?.Invoke(this);
    }
}