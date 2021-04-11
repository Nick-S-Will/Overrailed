using System.Collections;
using System.Collections.Generic;
using Uncooked.Terrain;
using UnityEngine;

using Uncooked.Managers;

namespace Uncooked.Train
{
    public abstract class TrainCar : LiquidTile, IPickupable
    {
        [Space]
        [SerializeField] private RailTile startRail;
        [SerializeField] protected int tier = 1;
        [SerializeField] protected bool isPermeable;

        protected RailTile currentRail;
        private int pathIndex, pathDir;
        protected bool isBurning;

        public bool IsTwoHanded() => true;

        protected override void Start()
        {
            if (startRail != null) SetRail(startRail);

            base.Start();
        }

        private IEnumerator Drive()
        {
            var target = currentRail.Path.GetChild(pathIndex);
            var startForward = transform.forward;
            float startDst = (target.position - transform.position).magnitude;

            while (currentRail != null)
            {
                while (transform.position == target.position)
                {
                    pathIndex += pathDir;
                    if (pathIndex < 0 || currentRail.Path.childCount <= pathIndex)
                    {
                        var nextRail = currentRail.TryGetNextRail();
                        if (nextRail == null)
                        {
                            Debug.Log("Train Died => TODO: Make train death animation");
                            yield break;
                        }
                        else UpdateRail(nextRail);
                    }
                    target = currentRail.Path.GetChild(pathIndex);
                    startForward = transform.forward;
                    startDst = (target.position - transform.position).magnitude;
                }

                transform.position = Vector3.MoveTowards(transform.position, target.position, GameManager.instance.TrainSpeed * Time.deltaTime);
                float currentDst = (target.position - transform.position).magnitude;
                transform.forward = Vector3.Lerp(startForward, pathDir * target.forward, 1 - (currentDst / startDst));

                yield return null;
                yield return new WaitWhile(() => GameManager.instance.IsEditing);
            }
        }

        /// <summary>
        /// Picks up this Wagon
        /// </summary>
        /// <returns></returns>
        public virtual IPickupable TryPickUp(Transform parent, int amount)
        {
            if (!GameManager.instance.IsEditing) return null;

            GetComponent<BoxCollider>().enabled = false;
            transform.parent = parent;
            transform.localPosition = Vector3.up;
            transform.localRotation = Quaternion.identity;

            return this;
        }

        public virtual void OnDrop(Vector3Int position) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bucket"></param>
        /// <returns></returns>
        private bool TryExtinguish(Bucket bucket)
        {
            if (isBurning && bucket.IsFull)
            {
                bucket.ToggleLiquid();
                isBurning = false; // <= gonna be more complex later
                return true;
            }
            else return false;
        }

        public override bool TryInteractUsing(IPickupable item, RaycastHit hitInfo)
        {
            if (item is Bucket bucket) return TryExtinguish(bucket);
            else return false;
        }

        /// <summary>
        /// Places this Wagon on given RailTile
        /// </summary>
        /// <param name="rail"></param>
        public void SetRail(RailTile rail) // TODO: Make check if has a wagon right in front
        {
            UpdateRail(rail);
            pathIndex = rail.Path.childCount / 2;

            transform.parent = rail.transform;
            transform.position = rail.Path.GetChild(pathIndex).position;
            transform.forward = pathDir * rail.Path.GetChild(pathIndex).forward;

            GetComponent<BoxCollider>().enabled = !isPermeable;

            if (currentRail != null) StartCoroutine(Drive());
        }

        /// <summary>
        /// Updates previous rail and newRail's wagonCount variables, then sets wagon's pathIndex and pathDir
        /// </summary>
        /// <param name="newRail">The RailTile the </param>
        private void UpdateRail(RailTile newRail)
        {
            newRail.AddWagon();

            currentRail = newRail;

            if (newRail.IsStraight)
            {
                pathIndex = 0;
                pathDir = 1;
            }
            else
            {
                bool turnsRight = RailTile.BentRailToRight(newRail);

                pathIndex = turnsRight ? newRail.Path.childCount - 1 : 0;
                pathDir = turnsRight ? -1 : 1;
            }
        }
    }
}