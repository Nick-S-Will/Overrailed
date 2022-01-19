using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Managers;
using Uncooked.Train;

namespace Uncooked.Terrain.Tiles
{
    public class RailTile : StackTile, IInteractable
    {
        [SerializeField] private GameObject straightMesh, bentMesh;
        [Tooltip("Gameobject that is enabled when this.IsPowered")]
        [SerializeField] private GameObject straightPower, bentPower;
        [Space]
        [Tooltip("Must have odd number of children")] [SerializeField] private Transform straightPathParent;
        [Tooltip("Must have odd number of children")] [SerializeField] private Transform bentPathParent;
        [Space]
        [SerializeField] protected bool startsPowered;
        [SerializeField] private bool isCheckpoint, showPath;

        private TrainCar lastPassenger;
        private Vector3Int inDirection = Vector3Int.zero, outDirection = Vector3Int.zero;
        private int connectionCount;

        public Transform Path => IsStraight ? straightPathParent : bentPathParent;
        public Vector3Int InDirection => inDirection;
        public Vector3Int OutDirection => outDirection;
        public bool IsStraight => straightMesh.activeSelf;
        public bool IsPowered => connectionCount > 0;
        public bool IsCheckpoint => isCheckpoint;
        public bool IsFinalCheckpoint => isCheckpoint && TryGetAdjacentRail(outDirection) == null;

        private void Awake()
        {
            // Must use method for edit cam to see tracks
            if (startsPowered) GameManager.MoveToLayer(transform, LayerMask.NameToLayer("Rail"));
        }

        protected override void Start()
        {
            if (startsPowered) straightPower.SetActive(true);

            if (startsPowered || isCheckpoint)
            {
                inDirection = Vector3Int.RoundToInt(straightMesh.transform.forward);
                outDirection = Vector3Int.RoundToInt(straightMesh.transform.forward);

                if (startsPowered) connectionCount = Physics.Raycast(transform.position, outDirection, 1, LayerMask.GetMask("Rail")) ? 2 : 1;
                else UpdateConnectionCount();
            }

            if (isCheckpoint) GameManager.instance.OnCheckpoint += ReachCheckpoint;
            base.Start();
        }

        public void NewPassenger(TrainCar newCar) => lastPassenger = newCar;

        private void ReachCheckpoint()
        {
            if (lastPassenger)
            {
                isCheckpoint = false;
                UpdateConnectionCount();
                GameManager.instance.OnEndCheckpoint -= ReachCheckpoint;
            }
        }

        private void UpdateConnectionCount()
        {
            int count = 0;
            if (Physics.Raycast(transform.position, outDirection, 1, LayerMask.GetMask("Default", "Rail"))) count++;
            if (Physics.Raycast(transform.position, -inDirection, 1, LayerMask.GetMask("Default", "Rail"))) count++;
            connectionCount = count;
        }

        #region IInteractable Overrides
        /// <summary>
        /// Checks if can be picked up, then unpowers it, then returns the base method
        /// </summary>
        /// <returns>The tile to be picked up, if it can be</returns>
        public override IPickupable TryPickUp(Transform parent, int amount)
        {
            if (lastPassenger) return lastPassenger.TryPickUp(parent, amount);
            else if (startsPowered || isCheckpoint || GameManager.instance.TrainIsSpeeding) return null;
            else if (IsPowered) SetState(Vector3Int.zero, Vector3Int.zero, 0, true);

            return base.TryPickUp(parent, amount);
        }

        public override bool TryInteractUsing(IPickupable item, RaycastHit hitInfo)
        {
            if (lastPassenger && lastPassenger.TryInteractUsing(item, hitInfo)) return true;
            else if (item is TrainCar car) return car.TrySetRail(this, true);
            else return base.TryInteractUsing(item, hitInfo);
        }

        /// <summary>
        /// Places this on stackBase if stackBase IsPowered is false
        /// </summary>
        /// <returns>True if stack is successful</returns>
        public override bool TryStackOn(StackTile stackBase)
        {
            if (stackBase is RailTile rail && rail.IsPowered) return false;
            else return base.TryStackOn(stackBase);
        }

        /// <summary>
        /// Updates rail connectivity
        /// </summary>
        public override void Drop(Vector3Int coords)
        {
            // Doesn't connect to tracks if in a stack
            if (NextInStack) return;

            RailTile connectableRail = null;
            Vector3 dir = Vector3.forward;

            // Looks for adjacent powered rail to connect to
            for (int i = 0; i < 4; i++)
            {
                var rail = TryGetAdjacentRail(dir, true);
                if (rail && rail.connectionCount < 2)
                {
                    connectableRail = rail;
                    break;
                }

                dir = Quaternion.AngleAxis(90, Vector3.up) * dir;
            }

            // Updates affected rails' states
            if (connectableRail)
            {
                Vector3Int dirToThis = coords - Vector3Int.FloorToInt(connectableRail.transform.position);

                if (connectableRail.outDirection != dirToThis) connectableRail.SetState(connectableRail.inDirection, dirToThis, 2, false);
                else connectableRail.connectionCount++;

                SetState(dirToThis, dirToThis, 1, true);
            }
        }
        #endregion

        #region Find Rails
        /// <summary>
        /// Gets next powered rail in the track after this
        /// </summary>
        /// <returns>Next RailTile in the track if there is a powered one, otherwise null</returns>
        public RailTile TryGetNextPoweredRail() => TryGetAdjacentRail(outDirection, true);

        /// <summary>
        /// Gets adjacent rail in the given direction from this transform
        /// </summary>
        /// <returns>Adjacent RailTile if there is one, otherwise null</returns>
        private RailTile TryGetAdjacentRail(Vector3 direction) => TryFindRail(direction, LayerMask.GetMask("Default", "Rail"));

        /// <summary>
        /// Gets adjacent rail in the given direction from this transform if it's IsPowered (property) is equal to isPowered (parameter)
        /// </summary>
        /// <param name="isPowered">True to search only for powered rails, false to search only for normal rails</param>
        /// <returns>Adjacent RailTile if there is one that matches criteria, otherwise null</returns>
        private RailTile TryGetAdjacentRail(Vector3 direction, bool isPowered) => TryFindRail(direction, isPowered ? LayerMask.GetMask("Rail") : LayerMask.GetMask("Default"));

        /// <summary>
        /// Used by the <see cref="TryGetAdjacentRail"/> methods, which are more straightforward
        /// </summary>
        /// <param name="layerMask">Physics layer mask used to find rail</param>
        /// <returns>Adjacent RailTile if there is one that matches criteria, otherwise null</returns>
        private RailTile TryFindRail(Vector3 direction, int layerMask)
        {
            if (Physics.Raycast(transform.position, direction, out RaycastHit hitInfo, 1, layerMask)) return hitInfo.transform.GetComponent<RailTile>();
            return null;
        }
        #endregion

        #region Update State
        // For preventing StackOverflow when rails connect consecutively
        /// <summary>
        /// Waits a frame, then does SetState
        /// </summary>
        /// <param name="inDir">Direction to this rail from the previous one</param>
        /// <param name="outDir">Direction from this rail to the next one</param>
        /// <param name="connectionCount">The amount of rails this is connecting to [0, 2]</param>
        /// <param name="tryExtend">True to check and update proceeding rails</param>
        private IEnumerator DelaySetState(Vector3Int inDir, Vector3Int outDir, int connectionCount, bool tryExtend)
        {
            yield return null;

            SetState(inDir, outDir, connectionCount, tryExtend);
        }

        /// <summary>
        /// Sets rail's connections
        /// </summary>
        /// <param name="inDir">Direction to this rail from the previous one</param>
        /// <param name="outDir">Direction from this rail to the next one</param>
        /// <param name="connectCount">The amount of rails this is connecting to [0, 2]</param>
        /// <param name="tryExtend">True to check and update proceeding rails</param>
        private void SetState(Vector3Int inDir, Vector3Int outDir, int connectCount, bool tryExtend)
        {
            // Power Off
            if (inDir == Vector3Int.zero || outDir == Vector3Int.zero)
            {
                straightMesh.gameObject.SetActive(true);
                bentMesh.gameObject.SetActive(false);
                straightPower.SetActive(false);
                bentPower.SetActive(false);

                if (!isCheckpoint) transform.forward = Vector3.forward;

                // Tries to disconnect this and proceeding rails from track
                if (tryExtend)
                {
                    var nextRail = TryGetAdjacentRail(outDirection, true);
                    if (nextRail) _ = StartCoroutine(nextRail.DelaySetState(Vector3Int.zero, Vector3Int.zero, 0, true));
                    var prevRail = TryGetAdjacentRail(-inDirection, true);
                    if (prevRail) prevRail.connectionCount = 1;
                }
            }
            // Power On
            else
            {
                bool isStraight = inDir - outDir == Vector3Int.zero;

                straightMesh.gameObject.SetActive(isStraight);
                bentMesh.gameObject.SetActive(!isStraight);
                straightPower.SetActive(isStraight);
                bentPower.SetActive(!isStraight);

                transform.forward = isStraight ? outDir : InOutToForward(inDir, outDir);

                // Tries to connect this to proceeding rail
                if (tryExtend)
                {
                    // Iterates left, forward, then right to find rail to connect to
                    Vector3Int dir = Vector3Int.RoundToInt(Quaternion.AngleAxis(-90, Vector3.up) * outDir);
                    for (int i = 0; i < 3; i++)
                    {
                        // Tries to get rail in currently iterated direction
                        var nextRail = TryGetAdjacentRail(dir, false);
                        if (nextRail && nextRail.GetStackCount() == 1)
                        {
                            // Checkpoint rails
                            if (nextRail.isCheckpoint && nextRail.connectionCount < 2 || (isCheckpoint && nextRail.isCheckpoint))
                            {
                                _ = StartCoroutine(nextRail.DelaySetState(dir, nextRail.outDirection, 2, true));
                                GameManager.instance.SpeedUp();

                                SetState(inDir, dir, 2, false);
                                return;
                            }
                            // Other rails
                            else if (nextRail.connectionCount < 2)
                            {
                                _ = StartCoroutine(nextRail.DelaySetState(dir, dir, 1, true));

                                SetState(inDir, dir, 2, false);
                                return;
                            }
                        }

                        dir = Vector3Int.RoundToInt(Quaternion.AngleAxis(90, Vector3.up) * dir);
                    }
                }
            }

            inDirection = inDir;
            outDirection = outDir;
            connectionCount = connectCount;

            GameManager.MoveToLayer(transform, connectionCount > 0 ? LayerMask.NameToLayer("Rail") : LayerMask.NameToLayer("Default"));
        }
        #endregion

        /// <summary>
        /// Calculates the linear distance of the path
        /// </summary>
        /// <returns>The linear distance of the path</returns>
        public float GetPathLength()
        {
            float length = 0;
            for (int i = 0; i < Path.childCount - 1; i++) length += Vector3.Distance(Path.GetChild(i).position, Path.GetChild(i + 1).position);

            return length;
        }

        /// <summary>
        /// Jank way to determines which way the given bent rail turn
        /// </summary>
        /// <returns>True if rail turns right, otherwise false</returns>
        public static bool BentRailToRight(RailTile rail)
        {
            if (!rail.bentMesh.activeSelf) Debug.LogError("Given Rail not bent");

            if ((rail.inDirection == Vector3Int.forward && rail.outDirection == Vector3Int.right) ||
                (rail.inDirection == Vector3Int.right && rail.outDirection == Vector3Int.back) ||
                (rail.inDirection == Vector3Int.left && rail.outDirection == Vector3Int.forward) ||
                (rail.inDirection == Vector3Int.back && rail.outDirection == Vector3Int.left)) return true;
            else return false;
        }

        /// <summary>
        /// Jank way to convert in and out directions to local forward for bent rails
        /// </summary>
        /// <returns>The forward direction for the bent rail with these in and out directions</returns>
        private static Vector3Int InOutToForward(Vector3Int inDir, Vector3Int outDir)
        {
            if ((inDir == Vector3Int.left && outDir == Vector3Int.back) ||          // (-1, -1)
                (inDir == Vector3Int.forward && outDir == Vector3Int.right))        // (1, 1)
                return Vector3Int.right;
            else if ((inDir == Vector3Int.right && outDir == Vector3Int.back) ||    // (1, -1)
                     (inDir == Vector3Int.forward && outDir == Vector3Int.left))    // (-1, 1)
                return Vector3Int.back;
            else if ((inDir == Vector3Int.right && outDir == Vector3Int.forward) || // (1, 1)
                     (inDir == Vector3Int.back && outDir == Vector3Int.left))       // (-1, -1)
                return Vector3Int.left;
            else return Vector3Int.forward;
        }

        private void OnDrawGizmos()
        {
            if (showPath)
            {
                var pathParent = straightPathParent;
                if (bentMesh.activeSelf) pathParent = bentPathParent;

                foreach (Transform t in pathParent) Gizmos.DrawSphere(t.position, 0.05f);
                for (int i = 0; i < pathParent.childCount - 1; i++) Gizmos.DrawLine(pathParent.GetChild(i).position, pathParent.GetChild(i + 1).position);
            }
        }
    }
}