using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Terrain.Tiles;

namespace Uncooked.Train
{
    public class CraftCar : TrainCar
    {
        [Space]
        [SerializeField] private HolderCar craftResultHolder;
        [SerializeField] private StackTile craftResultPrefab;
        [SerializeField] private CraftPoint[] craftPoints;
        [SerializeField] [Min(0.05f)] private float craftSpeed = 0.25f;

        private bool isCrafting;

        protected bool CanCraft
        {
            get
            {
                if (isCrafting) return false;
                foreach (CraftPoint cp in craftPoints) if (!cp.CanCraft) return false;
                return true;
            }
        }

        protected IEnumerator Craft()
        {
            isCrafting = true;

            // Variables for crafting animation
            var craftResult = Instantiate(craftResultPrefab);
            var craftMeshes = craftResult.GetComponentsInChildren<MeshRenderer>();
            var ingredientMeshes = new List<MeshRenderer[]>();
            float percent = 0;

            // Disable craft's hitboxes
            craftResult.GetComponent<BoxCollider>().enabled = false;
            if (craftResultHolder.SpawnPoint.childCount == 1)
                craftResultHolder.SpawnPoint.GetChild(0).GetComponent<BoxCollider>().enabled = false;

            // Parent craftResult to stack if there is one, otherwise parent it to craft spawnpoint
            if (craftResultHolder.SpawnPoint.childCount == 0) ParentAToB(craftResult.transform, craftResultHolder.SpawnPoint);
            else craftResult.TryStackOn(craftResultHolder.SpawnPoint.GetChild(0).GetComponent<StackTile>());

            // Get meshes to be animated
            foreach (var cp in craftPoints) ingredientMeshes.Add(cp.stackTop.GetComponentsInChildren<MeshRenderer>());
            foreach (var mesh in craftMeshes) mesh.enabled = false;

            // Animate crafting
            while (percent < 1)
            {
                percent += craftSpeed * tier * Time.deltaTime;

                float onCount;
                foreach (var renderers in ingredientMeshes)
                {
                    onCount = percent * renderers.Length;

                    for (int i = 0; i < onCount - 1; i++) renderers[i].enabled = false;
                }

                onCount = percent * craftMeshes.Length;
                for (int i = 0; i < onCount - 1; i++) craftMeshes[i].enabled = true;

                yield return null;
            }

            // Destroy top object of craftoint stacks
            foreach (var cp in craftPoints)
            {
                var newStackTop = cp.stackTop.PrevInStack;
                Destroy(cp.stackTop.gameObject);
                cp.stackTop = newStackTop;
            }

            // Re-enable hitbox for HolderCar.CanPickup
            craftResultHolder.SpawnPoint.GetChild(0).GetComponent<BoxCollider>().enabled = true;
            isCrafting = false;

            // See if another can be crafted
            yield return null; // Required for destroy cleanup
            if (CanCraft) StartCoroutine(Craft());
        }

        public override bool TryInteractUsing(IPickupable item, RaycastHit hitInfo)
        {
            if (item is StackTile stack) return TryAddItem(stack);
            else return base.TryInteractUsing(item, hitInfo);
        }

        private bool TryAddItem(StackTile stack)
        {
            // Find point for stack to add to
            CraftPoint craftPoint = null;
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

            // Stack point's previous stack on given stack
            if (craftPoint.Transform.childCount == 2)
                craftPoint.Transform.GetChild(0).GetComponent<StackTile>().TryStackOn(stack);
            else craftPoint.stackTop = stack.GetStackTop();

            // Try start crafting
            if (CanCraft) StartCoroutine(Craft());

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

        [System.Serializable]
        private class CraftPoint
        {
            [SerializeField] private Transform transform;
            [SerializeField] private StackTile.Type stackType;

            [HideInInspector] public StackTile stackTop;

            public Transform Transform => transform;
            public StackTile.Type StackType => stackType;
            public bool CanCraft => transform.childCount == 1;
        }
    }
}