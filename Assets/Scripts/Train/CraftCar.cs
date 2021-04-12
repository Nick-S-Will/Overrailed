using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Terrain;

namespace Uncooked.Train
{
    public class CraftCar : TrainCar
    {
        [Space]
        [SerializeField] private HolderCar craftResultHolder;
        [SerializeField] private StackTile craftResultPrefab;
        [SerializeField] private CraftPoint[] craftPoints;

        private bool isCrafting;

        private IEnumerator Craft()
        {
            isCrafting = true;

            var craftResult = Instantiate(craftResultPrefab);
            var ingredientMeshes = new List<MeshRenderer[]>();
            var craftMeshes = craftResult.GetComponentsInChildren<MeshRenderer>();
            float percent = 0;

            var craftHitbox = craftResult.GetComponent<BoxCollider>();
            craftHitbox.enabled = false;

            if (craftResultHolder.SpawnPoint.childCount == 0)
            {
                craftResult.transform.parent = craftResultHolder.SpawnPoint;
                craftResult.transform.localPosition = Vector3.zero;
                craftResult.transform.localRotation = Quaternion.identity;
            }
            else craftResult.TryStackOn(craftResultHolder.SpawnPoint.GetChild(0).GetComponent<StackTile>());

            foreach (var cp in craftPoints) ingredientMeshes.Add(cp.stackTop.GetComponentsInChildren<MeshRenderer>());
            foreach (var mesh in craftMeshes) mesh.enabled = false;

            while (percent < 1)
            {
                percent += 0.3f * tier * Time.deltaTime;

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

            foreach (var cp in craftPoints)
            {
                Destroy(cp.stackTop.gameObject);
                cp.stackTop = null;
                cp.stackCount--;
            }

            craftHitbox.enabled = true;
            isCrafting = false;
        }

        public override bool TryInteractUsing(IPickupable item, RaycastHit hitInfo)
        {
            if (item is StackTile stack) return TryAddItem(stack);
            else return base.TryInteractUsing(item, hitInfo);
        }

        private bool TryAddItem(StackTile stack)
        {
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

            if (craftPoint.Transform.childCount == 0)
            {
                stack.transform.parent = craftPoint.Transform;
                stack.transform.localPosition = Vector3.up;
                stack.transform.localRotation = Quaternion.identity;
            }
            else stack.TryStackOn(craftPoint.Transform.GetChild(0).GetComponent<StackTile>());

            craftPoint.stackTop = stack.GetStackTop();
            craftPoint.stackCount += stack.GetStackCount();

            int craftCount = 0;
            foreach (CraftPoint cp in craftPoints) if (cp.CanCraft) craftCount++;
            if (craftCount == craftPoints.Length && !isCrafting) StartCoroutine(Craft());

            return true;
        }

        [System.Serializable]
        private class CraftPoint
        {
            [SerializeField] private Transform transform;
            [SerializeField] private StackTile.Type stackType;

            [HideInInspector] public StackTile stackTop;
            [HideInInspector] public int stackCount;

            public Transform Transform => transform;
            public StackTile.Type StackType => stackType;
            public bool CanCraft => transform.childCount == 1;
        }
    }
}