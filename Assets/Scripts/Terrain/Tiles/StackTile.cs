using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncooked.Terrain.Tiles
{
    public class StackTile : Tile, IPickupable, IInteractable
    {
        [SerializeField] private Tile bridge;
        [Tooltip("Must have same exact same string to stack with")]
        [SerializeField] private string stackType;
        [SerializeField] private float tileHeight;
        [Min(1)] [SerializeField] private int startStackHeight = 1;

        private StackTile nextInStack, prevInStack;

        public StackTile NextInStack => nextInStack;
        public StackTile PrevInStack => prevInStack;
        public string StackType => stackType;
        public float TileHeight => tileHeight;
        public bool IsTwoHanded() => true;

        override protected void Start()
        {
            if (startStackHeight > 1) SelfStack();
        }

        public Tile Bridge => bridge;

        /// <summary>
        /// Counts the size of the stack with this as the base
        /// </summary>
        /// <returns>Number of tiles stacked on this plus this</returns>
        public int GetStackCount()
        {
            StackTile top = this;
            int count = 1;
            while (top.nextInStack)
            {
                count++;
                top = top.nextInStack;
            }
            return count;
        }

        public StackTile GetStackTop()
        {
            var top = this;
            while (top.nextInStack) top = top.nextInStack;
            
            return top;
        }

        public virtual bool TryInteractUsing(IPickupable item, RaycastHit hitInfo)
        {
            if (item is StackTile stack) return stack.TryStackOn(this);
            else return false;
        }

        /// <summary>
        /// Picks up, up to given amount of StackTiles from this
        /// </summary>
        /// <param name="parent">Transform this will be parented to</param>
        /// <param name="amount">Max amount of tiles to be picked up from the stack</param>
        /// <returns>Bottom StackTile of the stack to be picked up</returns>
        public virtual IPickupable TryPickUp(Transform parent, int amount)
        {
            StackTile toPickUp = this;
            int stackSize = GetStackCount();

            if (amount < stackSize)
            {
                // If stack is left at 1, base becomes trigger
                if (stackSize - amount == 1) GetComponent<BoxCollider>().isTrigger = true;

                // Get bottom tile to pick up
                for (int i = 0; i < stackSize - amount; i++) toPickUp = toPickUp.nextInStack;
                
                // Disconnect toPickup from linked list
                toPickUp.prevInStack.nextInStack = null;
                toPickUp.prevInStack = null;
            }

            // Moving bottom of pickup stack to hands
            toPickUp.GetComponent<BoxCollider>().enabled = false;
            toPickUp.transform.parent = parent;
            toPickUp.transform.localPosition = Vector3.up;
            toPickUp.transform.localRotation = Quaternion.identity;

            return toPickUp;
        }

        public virtual bool OnTryDrop() => true;

        public virtual void Drop(Vector3Int position) { }

        /// <summary>
        /// Places this on stackBase if they have the same stackType
        /// </summary>
        /// <param name="stackBase">Base of the stack for this to be placed on</param>
        /// <returns>True if stack is successful</returns>
        public virtual bool TryStackOn(StackTile stackBase)
        {
            if (stackType != stackBase.stackType) return false;
            if (transform == stackBase.transform) throw new System.Exception("Potentially Recursive Hierarchy");

            if (!stackBase.prevInStack) stackBase.GetComponent<BoxCollider>().isTrigger = false;

            // Get top of stack
            StackTile top = stackBase;
            while (top.nextInStack) top = top.nextInStack;

            // Place and orient this on stack
            prevInStack = top;
            top.nextInStack = this;
            // stackIndex = (top.stackIndex + 1);
            transform.parent = top.transform;
            transform.localPosition = top.tileHeight * Vector3.up;
            transform.rotation = stackBase.transform.parent.rotation;
            transform.localRotation = Quaternion.Euler(0, Random.Range(-5, 5f), 0);

            return true;
        }

        /// <summary>
        /// Instantiates a clone of this, then stacks the clone on this
        /// </summary>
        private void SelfStack()
        {
            var newTile = Instantiate(this);
            newTile.GetComponent<BoxCollider>().enabled = false;

            newTile.startStackHeight = startStackHeight - 1;
            newTile.TryStackOn(this);
        }

        /// <summary>
        /// Instantiates and places bridge, then destroys gameObject
        /// </summary>
        /// <param name="liquid">Liquid Tile in which bridge is to be placed</param>
        public void BuildBridge(LiquidTile liquid)
        {
            Instantiate(bridge, liquid.transform.position, liquid.transform.rotation, liquid.transform);
            Destroy(gameObject);

            liquid.GetComponent<BoxCollider>().enabled = false;
        }
    }
}