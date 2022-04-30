using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Managers;
using Overrailed.Terrain.Tiles;

namespace Overrailed.Train
{
    public class CraftCar : TrainCar
    {
        [Space]
        [SerializeField] private HolderCar craftResultHolder;
        [SerializeField] private StackTile craftResultPrefab;
        [SerializeField] private StackPoint[] craftPoints;

        private bool isCrafting;

        protected bool CanCraft
        {
            get
            {
                if (isCrafting) return false;
                foreach (StackPoint cp in craftPoints) if (!cp.IsHolding) return false;
                return craftResultHolder.HasSpace;
            }
        }
        public override bool IsWarning => false;

        override protected void Start()
        {
            if (craftResultHolder) AddToHolderEvents();

            base.Start();
        }

        #region Car Upgrading
        protected override bool TryUpgradeCar(TrainCar newCar)
        {
            if (base.TryUpgradeCar(newCar))
            {
                ((CraftCar)newCar).craftResultHolder = craftResultHolder;
                craftResultHolder.OnTaken += ((CraftCar)newCar).ProductTaken;
                craftResultHolder.OnUpgrade += ((CraftCar)newCar).UpdateHolder;
                return true;
            }
            else return false;
        }
        private void UpdateHolder(HolderCar newHolder) 
        { 
            craftResultHolder = newHolder;
            AddToHolderEvents();
        }
        private void AddToHolderEvents()
        {
            craftResultHolder.OnTaken += ProductTaken;
            craftResultHolder.OnUpgrade += UpdateHolder;
        }
        #endregion

        private void ProductTaken()
        {
            if (CanCraft) _ = StartCoroutine(TryCraft());
        }

        protected IEnumerator TryCraft()
        {
            while (CanCraft)
            {
                yield return StartCoroutine(Craft());
                yield return new WaitUntil(() => GameManager.IsPlaying());
            }
        }

        protected IEnumerator Craft()
        {
            isCrafting = true;
            craftResultHolder.AddPartTile();

            // Variables for crafting animation
            var product = Instantiate(craftResultPrefab);
            var productMeshes = product.GetComponentsInChildren<MeshRenderer>();
            var ingredientMeshes = new List<MeshRenderer[]>();
            float percent = 0;

            // Disable craft's hitboxes
            product.GetComponent<BoxCollider>().enabled = false;
            GameManager.MoveToLayer(product.transform, LayerMask.NameToLayer("Train"));

            // Parent craftResult to stack if there is one, otherwise parent it to craft spawnpoint
            if (craftResultHolder.SpawnPoint.childCount == 0) ParentAToB(product.transform, craftResultHolder.SpawnPoint);
            else product.TryStackOn(craftResultHolder.SpawnPoint.GetChild(0).GetComponent<StackTile>());

            // Get meshes to be animated
            foreach (var cp in craftPoints) ingredientMeshes.Add(cp.stackTop.GetComponentsInChildren<MeshRenderer>());
            foreach (var mesh in productMeshes) mesh.enabled = false;

            // Instantly completes recipe
            if (GameManager.instance == null || GameManager.IsEditing())
            {
                foreach (var mesh in productMeshes) mesh.enabled = true;
                percent = 1;
            }

            // Animate crafting
            while (percent < 1)
            {
                float oldPercent = percent;
                percent += (0.2f + 0.05f * tier) * Time.deltaTime;

                // Disables ingredient meshes
                int onCount;
                foreach (var renderers in ingredientMeshes)
                {
                    onCount = (int)(percent * renderers.Length);

                    if (onCount - (int)(oldPercent * renderers.Length) == 1) renderers[onCount - 1].enabled = false;
                }

                // Enables product meshes
                onCount = (int)(percent * productMeshes.Length);
                if (onCount - (int)(oldPercent * productMeshes.Length) == 1) productMeshes[onCount - 1].enabled = true;

                yield return null;
            }

            // Destroy top object of craft point stacks
            foreach (var cp in craftPoints)
            {
                var newStackTop = cp.stackTop.PrevInStack;
                Destroy(cp.stackTop.gameObject);
                cp.stackTop = newStackTop;
            }

            craftResultHolder.AddPartTile();
            isCrafting = false;

            yield return null; // Required for destroy cleanup
        }

        public override Interaction TryInteractUsing(IPickupable item)
        {
            Interaction interaction;
            if (item is StackTile stack) interaction = TryAddItem(stack) ? Interaction.Used : Interaction.None;
            else interaction = base.TryInteractUsing(item);

            if (interaction == Interaction.Used) InvokeOnInteract();
            return interaction;
        }

        private bool TryAddItem(StackTile stack)
        {
            // Find point for stack to add to
            StackPoint craftPoint = null;
            foreach (var cp in craftPoints)
            {
                if (stack.StackType == cp.StackType)
                {
                    craftPoint = cp;
                    break;
                }
            }

            if (craftPoint == null) return false;

            // Add stack to point
            ParentAToB(stack.transform, craftPoint.Transform);
            GameManager.MoveToLayer(stack.transform, LayerMask.NameToLayer("Train"));

            // Stack point's previous stack on given stack. Must be added beneath for when new tiles are added during craft, the top one is always the one being used
            if (craftPoint.Transform.childCount == 2) craftPoint.Transform.GetChild(0).GetComponent<StackTile>().TryStackOn(stack);
            else craftPoint.stackTop = stack.GetStackTop();

            _ = StartCoroutine(TryCraft());

            return true;
        }

        /// <summary>
        /// Makes a a child of b and sets its local position and rotation to zero
        /// </summary>
        /// <param name="a">Child Transform</param>
        /// <param name="b">Parent Transform</param>
        private void ParentAToB(Transform a, Transform b)
        {
            a.parent = b;
            a.localPosition = Vector3.zero;
            a.localRotation = Quaternion.identity;
        }

        private void OnDestroy()
        {
            if (craftResultHolder)
            {
                craftResultHolder.OnTaken -= ProductTaken;
                craftResultHolder.OnUpgrade -= UpdateHolder;
            }
        }
    }
}