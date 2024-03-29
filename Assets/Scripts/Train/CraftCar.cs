﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Managers;
using Overrailed.Terrain.Tiles;
using System;

namespace Overrailed.Train
{
    public class CraftCar : TrainCar
    {
        public override event Action OnPickUp;
        public override event Action OnDrop;

        [Space]
        [SerializeField] private HolderCar craftProductHolder;
        [SerializeField] private StackTile craftProductPrefab;
        [SerializeField] private StackPoint[] craftPoints;

        private bool isCrafting;

        protected bool CanCraft
        {
            get
            {
                if (isCrafting) return false;
                foreach (StackPoint cp in craftPoints) if (!cp.IsHolding) return false;
                return craftProductHolder.HasSpace;
            }
        }
        public override bool IsWarning => false;

        override protected void Start()
        {
            if (craftProductHolder)
            {
                craftProductHolder.OnTaken += ProductTaken;
                craftProductHolder.OnUpgrade += UpdateHolder;
            }

            base.Start();
        }

        #region Car Upgrading
        protected override bool TryUpgradeCar(TrainCar newCar)
        {
            if (base.TryUpgradeCar(newCar))
            {
                (newCar as CraftCar).UpdateHolder(craftProductHolder);
                return true;
            }
            else return false;
        }
        private void UpdateHolder(TrainCar newHolder)
        {
            craftProductHolder = newHolder as HolderCar;
            craftProductHolder.OnTaken += ProductTaken;
            craftProductHolder.OnUpgrade += UpdateHolder;
        }
        #endregion

        private void ProductTaken() => _ = StartCoroutine(TryCraft());

        protected IEnumerator TryCraft()
        {
            while (CanCraft)
            {
                yield return StartCoroutine(Craft());
                yield return new WaitUntil(() => Manager.IsPlaying());
            }
        }

        protected IEnumerator Craft()
        {
            isCrafting = true;
            craftProductHolder.AddPartTile();

            // Variables for crafting animation
            var product = Instantiate(craftProductPrefab);
            var productMeshes = product.GetComponentsInChildren<MeshRenderer>();
            var ingredientMeshes = new List<MeshRenderer[]>();

            // Disable craft's hitboxes
            product.GetComponent<BoxCollider>().enabled = false;
            Utils.MoveToLayer(product.transform, LayerMask.NameToLayer("Train"));

            // Parent product to stack if there is one, otherwise parent it to craft spawnpoint
            if (craftProductHolder.SpawnPoint.childCount == 0) ParentAToB(product.transform, craftProductHolder.SpawnPoint);
            else product.TryStackOn(craftProductHolder.SpawnPoint.GetChild(0).GetComponent<StackTile>());

            // Get meshes to be animated
            foreach (var cp in craftPoints) ingredientMeshes.Add(cp.stackTop.GetComponentsInChildren<MeshRenderer>());
            foreach (var mesh in productMeshes) mesh.enabled = false;

            // Instantly completes recipe
            float elapsedTime = 0f, craftTime = 1f / (0.15f + 0.05f * tier);
            if (Manager.IsEditing() || Manager.instance is TutorialManager)
            {
                foreach (var mesh in productMeshes) mesh.enabled = true;
                elapsedTime = craftTime;
            }

            // Animate crafting
            while (elapsedTime < craftTime)
            {
                float oldPercent = elapsedTime / craftTime;
                elapsedTime += Time.fixedDeltaTime;
                float percent = elapsedTime / craftTime;

                // Disables ingredient meshes
                int visibleCount;
                foreach (var renderers in ingredientMeshes)
                {
                    visibleCount = (int)(percent * renderers.Length);
                    if (visibleCount - (int)(oldPercent * renderers.Length) == 1) renderers[visibleCount - 1].enabled = false;
                }

                // Enables product meshes
                visibleCount = (int)(percent * productMeshes.Length);
                if (visibleCount - (int)(oldPercent * productMeshes.Length) == 1) productMeshes[visibleCount - 1].enabled = true;

                yield return null;
                yield return Manager.PauseRoutine;
            }

            // Destroy top object of craft point stacks
            foreach (var cp in craftPoints)
            {
                var newStackTop = cp.stackTop.PrevInStack;
                Destroy(cp.stackTop.gameObject);
                cp.stackTop = newStackTop;
            }

            craftProductHolder.AddPartTile();
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
            Utils.MoveToLayer(stack.transform, LayerMask.NameToLayer("Train"));

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
            if (craftProductHolder)
            {
                craftProductHolder.OnTaken -= ProductTaken;
                craftProductHolder.OnUpgrade -= UpdateHolder;
            }
        }
    }
}