using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncooked.Terrain
{
    public abstract class Tool : Tile, IPickupable
    {
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

            return this;
        }

        public virtual void OnDrop(Vector3Int position) { }

        /// <summary>
        /// Custom way child Tool classes interact with given Tile
        /// </summary>
        /// <param name="tile">Tile to be interacted with</param>
        /// <param name="hit">Info about the Raycast used to find this</param>
        /// <returns>True if an interaction happened</returns>
        public abstract bool InteractWith(IInteractable interactable, RaycastHit hit);
    }
}