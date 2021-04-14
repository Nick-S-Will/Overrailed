using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncooked.Terrain
{
    public class StackTile : Tile, IPickupable, IInteractable
    {
        public enum Type { Wood, Rock, Rail }

        [SerializeField] private Tile bridge;
        [SerializeField] private Type stackType;
        [SerializeField] private float tileHeight;
        [Min(1)] [SerializeField] private int startStackHeight = 1;

        private StackTile nextInStack, prevInStack;

        public StackTile NextInStack => nextInStack;
        public StackTile PrevInStack => prevInStack;
        public Type StackType => stackType;
        public float TileHeight => tileHeight;
        public bool IsTwoHanded() => true;

        protected virtual void Start()
        {
            if (startStackHeight > 1) SelfStack();
        }

        public Tile Bridge => bridge;
        public int stackIndex { get; private set; } = 0;

        public int GetStackCount()
        {
            StackTile top = this;
            int count = 1;
            while (top.nextInStack != null)
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
                if (stackSize - amount == 1) GetComponent<BoxCollider>().isTrigger = true;

                // Get bottom tile to pick up
                for (int i = 0; i < stackSize - amount; i++) toPickUp = toPickUp.nextInStack;
                
                // Disconnect toPickup from stack
                toPickUp.prevInStack.nextInStack = null;
                toPickUp.prevInStack = null;

                // Update toPickup stackIndex variables
                toPickUp.stackIndex = 0;
                StackTile top = toPickUp;
                while (top.nextInStack)
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

        public virtual void OnDrop(Vector3Int position) { }

        /// <summary>
        /// Places this on stackBase if they have the same stackType
        /// </summary>
        /// <param name="stackBase">Base of the stack for this to be placed on</param>
        /// <returns>True if stack is successful</returns>
        public virtual bool TryStackOn(StackTile stackBase)
        {
            if (stackType != stackBase.stackType) return false;
            if (transform == stackBase.transform) throw new System.Exception("Self Parenting");

            stackBase.GetComponent<BoxCollider>().isTrigger = false;

            // Get top of stack
            StackTile top = stackBase;
            while (top.nextInStack) top = top.nextInStack;

            // Place and orient this on stack
            prevInStack = top;
            top.nextInStack = this;
            stackIndex = (top.stackIndex + 1);
            transform.parent = top.transform;
            transform.localPosition = top.tileHeight * Vector3.up;
            transform.localRotation = Quaternion.Euler(0, Random.Range(-10, 10f), 0);

            // Update stackIndex
            top = this;
            while (top.nextInStack)
            {
                top = top.nextInStack;
                top.stackIndex = top.prevInStack.stackIndex + 1;
            }

            return true;
        }

        /// <summary>
        /// Instantiates a clone of this, then stacks the clone on this
        /// </summary>
        private void SelfStack()
        {
            var newTile = Instantiate(this);

            newTile.startStackHeight = startStackHeight - 1;
            newTile.TryStackOn(this);
        }

        /// <summary>
        /// Instantiates and places bridge, then destroys gameObject
        /// </summary>
        /// <param name="liquid">Liquid Tile in which bridge is to be placed</param>
        public void BuildBridge(LiquidTile liquid)
        {
            Instantiate(bridge, liquid.transform.position, liquid.transform.rotation, liquid.transform.parent);
            Destroy(gameObject);

            liquid.GetComponent<BoxCollider>().enabled = false;
        }
    }
}