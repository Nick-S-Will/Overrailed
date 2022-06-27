using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Managers.Audio;

namespace Overrailed.Terrain.Tiles
{
    [RequireComponent(typeof(BoxCollider))]
    public class StackTile : Tile, IPickupable, IInteractable
    {
        [SerializeField] private Tile bridge;
        [SerializeField] private AudioClip bridgeBuildAudio;
        [Tooltip("Must have same exact same string to stack with")]
        [SerializeField] private string stackType;
        [SerializeField] private float tileHeight;
        [Min(1)] [SerializeField] private int startStackHeight = 1;

        private StackTile nextInStack, prevInStack;
        private BoxCollider boxCollider;

        public StackTile NextInStack => nextInStack;
        public StackTile PrevInStack => prevInStack;
        public string StackType => stackType;
        public float TileHeight => tileHeight;
        public override bool CanPickUp => true;
        public override bool IsTwoHanded => true;

        override protected void Start()
        {
            boxCollider = GetComponent<BoxCollider>();
            if (startStackHeight > 1) SelfStack();
        }

        public bool HasBridge => bridge;

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

        public virtual Interaction TryInteractUsing(IPickupable item)
        {
            if (item is StackTile stack) return stack.TryStackOn(this) ? Interaction.Used : Interaction.None;
            else return Interaction.None;
        }

        /// <summary>
        /// Picks up, up to given amount of StackTiles from this
        /// </summary>
        /// <param name="parent">Transform this will be parented to</param>
        /// <param name="amount">Max amount of tiles to be picked up from the stack</param>
        /// <returns>Bottom StackTile of the stack to be picked up</returns>
        public override IPickupable TryPickUp(Transform parent, int amount = 1)
        {
            if (!CanPickUp) return null;

            StackTile toPickUp = this;
            int stackSize = GetStackCount();

            if (amount < stackSize)
            {
                // If stack is left at 1, base becomes trigger
                if (stackSize - amount == 1) boxCollider.isTrigger = true;

                // Get bottom tile to pick up
                for (int i = 0; i < stackSize - amount; i++) toPickUp = toPickUp.nextInStack;

                // Disconnect toPickup from linked list
                toPickUp.prevInStack.nextInStack = null;
                toPickUp.prevInStack = null;
            }

            // Moving bottom of pickup stack to hands
            toPickUp.boxCollider.enabled = false;
            toPickUp.transform.parent = parent;
            toPickUp.transform.localPosition = Vector3.up;
            toPickUp.transform.localRotation = Quaternion.identity;

            AudioManager.PlaySound(PickupAudio, transform.position);
            InvokeOnPickUp();

            return toPickUp;
        }

        public override bool OnTryDrop(Vector3Int position) => true;

        public override void Drop(Vector3Int position)
        {
            AudioManager.PlaySound(DropAudio, position);
            InvokeOnDrop();

            boxCollider.enabled = true;
            boxCollider.isTrigger = nextInStack == null;
        }
        
        /// <summary>
        /// Places this on stackBase if they have the same stackType
        /// </summary>
        /// <param name="stackBase">Base of the stack for this to be placed on</param>
        /// <returns>True if stack is successful</returns>
        public virtual bool TryStackOn(StackTile stackBase)
        {
            if (stackType != stackBase.stackType) return false;

            GetComponent<BoxCollider>().enabled = false;
            if (!stackBase.prevInStack) stackBase.boxCollider.isTrigger = false;

            // Get top of stack
            StackTile top = stackBase.GetStackTop();

            // Place and orient this on stack
            prevInStack = top;
            top.nextInStack = this;
            transform.parent = top.transform;
            transform.localPosition = top.tileHeight * Vector3.up;
            transform.rotation = stackBase.transform.parent.rotation;
            transform.localRotation = Quaternion.Euler(0, Random.Range(-5, 5f), 0);

            AudioManager.PlaySound(DropAudio, transform.position);

            return true;
        }

        /// <summary>
        /// Instantiates a clone of this, then stacks the clone on this
        /// </summary>
        private void SelfStack()
        {
            var newTile = Instantiate(this);
            newTile.name = newTile.name.Substring(0, newTile.name.Length - 7);

            newTile.startStackHeight = startStackHeight - 1;
            newTile.TryStackOn(this);
        }

        /// <summary>
        /// Instantiates and places bridge, then destroys gameObject
        /// </summary>
        /// <param name="liquid">Liquid Tile in which bridge is to be placed</param>
        public void BuildBridge(LiquidTile liquid)
        {
            _ = Instantiate(bridge, liquid.transform.position, liquid.transform.rotation, liquid.transform);
            Destroy(GetStackTop().gameObject);

            AudioManager.PlaySound(bridgeBuildAudio, liquid.transform.position);

            Destroy(liquid.GetComponent<BoxCollider>());
        }
    }
}