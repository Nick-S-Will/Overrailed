using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncooked.Terrain
{
    public class PickupTile : Tile, IPickupable
    {
        public enum Type { Wood, Rock, Rail }

        public Type pickupType;
        public float tileHeight;
        public int stackIndex { get; private set; } = 0;

        private PickupTile nextInStack, prevInStack;

        private static int GetStackSize(PickupTile bottomTile)
        {
            PickupTile top = bottomTile;
            int count = 1;
            while (top.nextInStack != null)
            {
                count++;
                top = top.nextInStack;
            }
            return count;
        }

        public Tile PickUp(Transform parent, int amount)
        {
            PickupTile toPickUp = this;
            int stackSize = GetStackSize(this);

            if (amount < stackSize)
            {
                // Get bottom tile to pick up
                for (int i = 0; i < stackSize - amount; i++) toPickUp = toPickUp.nextInStack;
                
                // Disconnect toPickup from stack
                toPickUp.prevInStack.nextInStack = null;
                toPickUp.prevInStack = null;

                // Update toPickup stackIndex variables
                toPickUp.stackIndex = 0;
                PickupTile top = toPickUp;
                while (top.nextInStack != null)
                {
                    top = top.nextInStack;
                    top.stackIndex = top.prevInStack.stackIndex + 1;
                }
            }

            // Moving bottom of pickup stack to hands
            toPickUp.GetComponent<BoxCollider>().enabled = false;
            toPickUp.transform.parent = parent;
            toPickUp.transform.localPosition = Vector3.up;
            toPickUp.transform.localRotation = Quaternion.identity;

            return toPickUp;
        }

        public bool TryStackOn(PickupTile stackBase)
        {
            if (pickupType != stackBase.pickupType) return false;

            // Get top of stack
            PickupTile top = stackBase;
            while (top.nextInStack != null) top = top.nextInStack;

            // Place this on stack
            prevInStack = top;
            top.nextInStack = this;
            stackIndex = (top.stackIndex + 1);
            transform.parent = top.transform;
            transform.rotation = top.transform.rotation;
            transform.localPosition = tileHeight * Vector3.up;

            // Update stackIndex
            top = this;
            while (top.nextInStack != null)
            {
                top.nextInStack.stackIndex = top.stackIndex + 1;
                top = top.nextInStack;
            }

            return true;
        }
    }
}