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

        private Vector3Int inDirection = Vector3Int.zero, outDirection = Vector3Int.zero;
        private int connectCount, wagonCount;

        public Transform Path => straightMesh.gameObject.activeSelf ? straightPathParent : bentPathParent;
        public bool IsStraight => straightMesh.activeSelf;
        public bool IsPowered => connectCount > 0;
        public bool IsCheckpoint => isCheckpoint;
        public bool IsFinalCheckpoint => isCheckpoint && TryGetNextRail() == null;

        private void Awake()
        {
            if (startsPowered) GameManager.MoveToLayer(transform, LayerMask.NameToLayer("Rail"));
        }

        protected override void Start()
        {
            if (startsPowered) straightPower.SetActive(true);

            if (startsPowered || isCheckpoint)
            {
                inDirection = Vector3Int.RoundToInt(straightMesh.transform.forward);
                outDirection = Vector3Int.RoundToInt(straightMesh.transform.forward);

                if (startsPowered) connectCount = Physics.Raycast(transform.position, outDirection, 1, LayerMask.GetMask("Rail")) ? 2 : 1;
                else
                {
                    int count = 0;
                    if (Physics.Raycast(transform.position, outDirection, 1, LayerMask.GetMask("Rail"))) count++;
                    if (Physics.Raycast(transform.position, -outDirection, 1, LayerMask.GetMask("Rail"))) count++;
                    connectCount = count;
                }
            }

            if (isCheckpoint) GameManager.instance.OnEndCheckpoint += EndCheckpoint;
            base.Start();
        }

        public void AddWagon() => wagonCount++;
        
        private void EndCheckpoint()
        {
            if (wagonCount > 0)
            {
                isCheckpoint = false;
                GameManager.instance.OnEndCheckpoint -= EndCheckpoint;
            }
        }

        /// <summary>
        /// Checks if can be picked up, then unpowers it, then returns the base method
        /// </summary>
        /// <returns>The tile to be picked up, if it can be</returns>
        public override IPickupable TryPickUp(Transform parent, int amount)
        {
            if (startsPowered || isCheckpoint || wagonCount > 0) return null;
            if (IsPowered) SetState(Vector3Int.zero, Vector3Int.zero, 0);

            return base.TryPickUp(parent, amount);
        }

        public override bool TryInteractUsing(IPickupable item, RaycastHit hitInfo)
        {
            if (item is TrainCar wagon) wagon.SetRail(this, true);
            else return base.TryInteractUsing(item, hitInfo);

            return true;
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
        /// Updates rail connectivity if necessary
        /// </summary>
        public override void Drop(Vector3Int coords)
        {
            if (NextInStack) return;

            List<RailTile> connectableRails = new List<RailTile>();
            Vector3 dir = Vector3.forward;

            for (int i = 0; i < 4; i++)
            {
                var rail = TryGetAdjacentRail(dir, true);
                if (rail && rail.connectCount < 2)
                {
                    connectableRails.Add(rail);
                    break;
                }

                dir = Quaternion.AngleAxis(90, Vector3.up) * dir;
            }

            if (connectableRails.Count == 1)
            {
                RailTile inRail = connectableRails[0];
                Vector3Int dirToThis = coords - Vector3Int.FloorToInt(inRail.transform.position);

                if (inRail.outDirection != dirToThis) inRail.SetState(inRail.inDirection, dirToThis, 2);
                else inRail.connectCount++;
                SetState(dirToThis, dirToThis, 1);
            }
        }

        /// <summary>
        /// Gets next powered rail in the track after this
        /// </summary>
        /// <returns>Next RailTile in the track if there is a powered one, otherwise null</returns>
        public RailTile TryGetNextRail() => TryGetAdjacentRail(outDirection, true);

        /// <summary>
        /// Gets adjacent rail in the given direction from this transform
        /// </summary>
        /// <param name="isPowered">If the search require rail to be powered or not</param>
        /// <returns>Adjacent RailTile if there is one, otherwise null</returns>
        private RailTile TryGetAdjacentRail(Vector3 direction, bool isPowered)
        {
            RaycastHit hitInfo;
            var mask = isPowered ? LayerMask.GetMask("Rail") : LayerMask.GetMask("Default");

            if (Physics.Raycast(transform.position, direction, out hitInfo, 1, mask))
            {
                return hitInfo.transform.GetComponent<RailTile>();
            }

            return null;
        }

        // For preventing StackOverflow when rails connect multiple in a row
        /// <summary>
        /// Waits a frame, then does SetState
        /// </summary>
        /// <param name="inDir">Direction to this rail from the previous one</param>
        /// <param name="outDir">Direction from this rail to the next one</param>
        /// <param name="connectionCount">The amount of rails this is connecting to [0, 2]</param>
        private IEnumerator DelaySetState(Vector3Int inDir, Vector3Int outDir, int connectionCount)
        {
            yield return null;

            SetState(inDir, outDir, connectionCount);
        }

        /// <summary>
        /// Sets rail's connections
        /// </summary>
        /// <param name="inDir">Direction to this rail from the previous one</param>
        /// <param name="outDir">Direction from this rail to the next one</param>
        /// <param name="connectionCount">The amount of rails this is connecting to [0, 2]</param>
        private void SetState(Vector3Int inDir, Vector3Int outDir, int connectionCount)
        {
            // Placing
            if (inDir != Vector3Int.zero)
            {
                bool isStraight = inDir - outDir == Vector3Int.zero;

                straightMesh.gameObject.SetActive(isStraight);
                bentMesh.gameObject.SetActive(!isStraight);
                straightPower.SetActive(isStraight);
                bentPower.SetActive(!isStraight);

                transform.forward = isStraight ? outDir : InOutToForward(inDir, outDir);

                // Tries to connect this to proceeding rails
                if (connectionCount == 1)
                {
                    var nextRail = TryGetAdjacentRail(outDir, false);
                    if (nextRail && nextRail.GetStackCount() == 1)
                    {
                        if (nextRail.isCheckpoint)
                        {
                            StartCoroutine(nextRail.DelaySetState(nextRail.inDirection, nextRail.outDirection, 1));
                            GameManager.instance.SpeedToCheckpoint();
                        }
                        else StartCoroutine(nextRail.DelaySetState(outDir, outDir, 1));
                        connectionCount++;
                    }
                }
            }
            // Pick up
            else
            {
                if (isCheckpoint) return;

                straightMesh.gameObject.SetActive(true);
                bentMesh.gameObject.SetActive(false);
                straightPower.SetActive(false);
                bentPower.SetActive(false);

                transform.forward = Vector3.forward;

                // Tries to disconnect this and proceeding rails from track
                var nextRail = TryGetAdjacentRail(outDirection, true);
                if (nextRail) StartCoroutine(nextRail.DelaySetState(Vector3Int.zero, Vector3Int.zero, 0));
                var prevRail = TryGetAdjacentRail(-inDirection, true);
                if (prevRail) prevRail.connectCount = 1;
            }

            inDirection = inDir;
            outDirection = outDir;
            connectCount = connectionCount;

            GameManager.MoveToLayer(transform, connectCount > 0 ? LayerMask.NameToLayer("Rail") : LayerMask.NameToLayer("Default"));
        }

        /// <summary>
        /// Jank way to determines which way the given bent rail turn
        /// </summary>
        /// <returns>True if rail turns right, otherwise false</returns>
        public static bool BentRailToRight(RailTile rail)
        {
            if (!rail.bentMesh.activeSelf) Debug.LogError("Given Rail not bent");

            // No Vector3Int.forward or back in this version wtf?
            var forward = new Vector3Int(0, 0, 1);

            if ((rail.inDirection == forward && rail.outDirection == Vector3Int.right) ||
                (rail.inDirection == Vector3Int.right && rail.outDirection == -forward) ||
                (rail.inDirection == Vector3Int.left && rail.outDirection == forward) ||
                (rail.inDirection == -forward && rail.outDirection == Vector3Int.left)) return true;
            else return false;
        }

        /// <summary>
        /// Jank way to convert in and out directions to local forward for bent rails
        /// </summary>
        /// <returns>The forward direction for the bent rail with these in and out directions</returns>
        private static Vector3Int InOutToForward(Vector3Int inDir, Vector3Int outDir)
        {
            // No Vector3Int.forward or back in this version wtf?
            var forward = new Vector3Int(0, 0, 1);

            if ((inDir == Vector3Int.left && outDir == -forward) ||       // (-1, -1)
                (inDir == forward && outDir == Vector3Int.right))         // (1, 1)
                forward = Vector3Int.right;
            else if ((inDir == Vector3Int.right && outDir == -forward) || // (1, -1)
                     (inDir == forward && outDir == Vector3Int.left))     // (-1, 1)
                forward = -forward;
            else if ((inDir == Vector3Int.right && outDir == forward) ||  // (1, 1)
                     (inDir == -forward && outDir == Vector3Int.left))    // (-1, -1)
                forward = Vector3Int.left;

            return forward;
        }

        private void OnDrawGizmos()
        {
            if (showPath)
            {
                if (straightMesh.activeSelf) foreach (Transform t in straightPathParent) Gizmos.DrawSphere(t.position, 0.05f);
                if (bentMesh.activeSelf) foreach (Transform t in bentPathParent) Gizmos.DrawSphere(t.position, 0.05f);
            }
        }
    }
}