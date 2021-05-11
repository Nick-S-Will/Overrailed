using System.Collections;
using System.Collections.Generic;
using Uncooked.Terrain;
using UnityEngine;

using Uncooked.Managers;
using Uncooked.Terrain.Tools;
using Uncooked.Terrain.Tiles;

namespace Uncooked.Train
{
    public abstract class TrainCar : Tile, IPickupable, IInteractable
    {
        public event System.Action OnDeath, OnStartDriving, OnPauseDriving;

        [Space]
        [SerializeField] private ParticleSystem burningParticlePrefab;
        [SerializeField] private ParticleSystem breakParticlePrefab;
        [Space]
        [SerializeField] private Transform meshParent;
        [SerializeField] private Transform burnPoint;
        [SerializeField] private RailTile startRail;
        [SerializeField] protected int tier = 1;
        [SerializeField] protected bool isPermeable;

        protected ParticleSystem burningParticles;
        protected RailTile currentRail;
        private int pathIndex, pathDir;

        public bool HasRail => currentRail;
        public bool IsTwoHanded() => true;

        protected virtual void Start()
        {
            if (startRail) _ = TrySetRail(startRail, false);
            OnDeath += Die;
        }

        public void StartDriving() => _ = StartCoroutine(Drive());

        private IEnumerator Drive()
        {
            OnStartDriving?.Invoke();

            var target = currentRail.Path.GetChild(pathIndex);
            var startForward = transform.forward;
            float startDst = (target.position - transform.position).magnitude;

            while (currentRail)
            {
                while (transform.position == target.position)
                {
                    pathIndex += pathDir;
                    if (pathIndex == currentRail.Path.childCount / 2 + 1 && currentRail.IsFinalCheckpoint) 
                        GameManager.instance.ReachCheckpoint();
                    else if (pathIndex < 0 || currentRail.Path.childCount <= pathIndex)
                    {
                        var nextRail = currentRail.TryGetNextPoweredRail();
                        if (nextRail == null)
                        {
                            OnDeath?.Invoke();
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
                if (GameManager.instance.IsPaused)
                {
                    OnPauseDriving?.Invoke();
                    yield return new WaitWhile(() => GameManager.instance.IsPaused);
                    OnStartDriving?.Invoke();
                }
            }
        }

        /// <summary>
        /// Picks up this Wagon
        /// </summary>
        public virtual IPickupable TryPickUp(Transform parent, int amount)
        {
            if (!GameManager.instance.IsEditing) return null;

            GetComponent<BoxCollider>().enabled = false;
            transform.parent = parent;
            transform.localPosition = Vector3.up;
            transform.localRotation = Quaternion.identity;

            return this;
        }

        public virtual bool OnTryDrop() => false;
        
        public virtual void Drop(Vector3Int position) { }

        public virtual bool TryInteractUsing(IPickupable item, RaycastHit hitInfo)
        {
            if (GameManager.instance.IsEditing) return false;
            else if (item is Bucket bucket) return TryExtinguish(bucket);
            else return false;
        }

        /// <summary>
        /// Puts out the fire on this car if buck has water
        /// </summary>
        /// <param name="bucket">Bucket used to put out the fire</param>
        /// <returns>True if fire was put out, otherwise null</returns>
        private bool TryExtinguish(Bucket bucket)
        {
            if (burningParticles && bucket.TryUse())
            {
                Destroy(burningParticles.gameObject, burningParticles.main.duration);
                var emissionSettings = burningParticles.emission;
                emissionSettings.enabled = false;
                burningParticles = null;

                return true;
            }
            else return false;
        }

        public IEnumerator Ignite()
        {
            burningParticles = Instantiate(burningParticlePrefab, burnPoint);

            yield return new WaitForSeconds(4);

            if (!burningParticles) yield break;

            var carF = TryGetAdjacentCar(transform.position, transform.forward);
            var carB = TryGetAdjacentCar(transform.position, -transform.forward);

            if (carF && !carF.burningParticles && carF.currentRail) _ = StartCoroutine(carF.Ignite());
            if (carB && !carB.burningParticles && carB.currentRail) _ = StartCoroutine(carB.Ignite());
        }

        /// <summary>
        /// Places this Wagon on given RailTile
        /// </summary>
        /// <param name="rail">Rail to be set</param>
        /// <param name="connectCheck">If placing requires checking for a TrainCar ahead</param>
        public bool TrySetRail(RailTile rail, bool connectCheck)
        {
            UpdateRail(rail);
            pathIndex = rail.Path.childCount / 2;

            var pos = rail.Path.GetChild(pathIndex).position;
            var dir = pathDir * rail.Path.GetChild(pathIndex).forward;
            if (connectCheck && !TryGetAdjacentCar(pos, dir)) return false;

            transform.position = pos;
            transform.forward = dir;

            GetComponent<BoxCollider>().enabled = !isPermeable;

            return true;
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

        /// <summary>
        /// Gets TrainCar in the given direction from the given point
        /// </summary>
        /// <param name="point">Point from where the raycast is sent</param>
        /// <param name="direction">Direction where the raycast is sent to</param>
        /// <returns>TrainCar in the given direction from the given point if there is one, otherwise null</returns>
        private TrainCar TryGetAdjacentCar(Vector3 point, Vector3 direction)
        {
            RaycastHit hitInfo;
            var mask = LayerMask.GetMask("Train");

            if (Physics.Raycast(point, direction, out hitInfo, 1, mask)) return hitInfo.transform.GetComponent<TrainCar>();
            else return null;
        }

        protected virtual void Die()
        {
            BreakIntoParticles(breakParticlePrefab, GetMeshColors(meshParent), transform.position);

            Destroy(gameObject, Time.deltaTime);
        }

        [System.Serializable]
        public class StackPoint
        {
            [SerializeField] private Transform transform;
            [SerializeField] private string stackType;

            [HideInInspector] public StackTile stackTop;

            public Transform Transform => transform;
            public string StackType => stackType;
            public bool CanCraft => transform.childCount == 1;
        }
    }
}