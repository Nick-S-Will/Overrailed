using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Terrain.Tiles;

namespace Uncooked.Terrain.Tools
{
    public abstract class Tool : Tile, IPickupable
    {
        public event System.Action<Tool> OnPickup, OnDropTool;

        [SerializeField] private Transform handOffset;
        [SerializeField] private int tier = 1;

        public int Tier => tier;
        public override bool IsTwoHanded() => false;
        public override bool CanPickUp => true;

        /// <summary>
        /// Picks up this Tool
        /// </summary>
        /// <param name="parent">Transform this will be parented to</param>
        /// <returns>Tool that was picked up</returns>
        public override IPickupable TryPickUp(Transform parent, int amount)
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

        public override bool OnTryDrop() => true;

        public override void Drop(Vector3Int position) => OnDropTool?.Invoke(this);
    }
}