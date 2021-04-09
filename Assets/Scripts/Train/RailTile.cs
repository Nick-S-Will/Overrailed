using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Terrain;

namespace Uncooked.Train
{
    public class RailTile : StackTile
    {
        [SerializeField] private GameObject straightMesh, bentMesh;
        [Tooltip("Gameobject that enables/disables based on the rail's IsPowered")]
        [SerializeField] private GameObject straightPower, bentPower;
        [Space]
        [Tooltip("Must have odd number of points")] [SerializeField] private Transform straightPathParent;
        [Tooltip("Must have odd number of points")] [SerializeField] private Transform bentPathParent;
        [Space]
        [SerializeField] private bool startsPowered;

        private Vector3Int inDirection = Vector3Int.zero, outDirection = Vector3Int.zero;
        private int connectCount, wagonCount;

        public Transform Path => straightMesh.gameObject.activeSelf ? straightPathParent : bentPathParent;
        public bool IsPowered => inDirection != Vector3Int.zero;
        public bool IsStraight => straightMesh.activeSelf;

        protected override void Start()
        {
            if (startsPowered)
            {
                straightPower.SetActive(true);
                inDirection = Vector3Int.RoundToInt(straightMesh.transform.forward);
                outDirection = Vector3Int.RoundToInt(straightMesh.transform.forward);
                connectCount = Physics.Raycast(transform.position, outDirection, 1, LayerMask.GetMask("Default")) ? 2 : 1;
            }

            base.Start();
        }

        /// <summary>
        /// Checks if can be picked up, then unpowers it, then returns the base method
        /// </summary>
        /// <returns>The tile to be picked up, if it can be</returns>
        public override IPickupable TryPickUp(Transform parent, int amount)
        {
            if (startsPowered) return null;
            if (IsPowered) SetState(Vector3Int.zero, Vector3Int.zero, 0);

            return base.TryPickUp(parent, amount);
        }

        /// <summary>
        /// Places this on stackBase if stackBase IsPowered is false
        /// </summary>
        /// <returns>True if stack is successful</returns>
        public override bool TryStackOn(StackTile stackBase)
        {
            if (stackBase is RailTile rail && rail.IsPowered) return false;
            return base.TryStackOn(stackBase);
        }

        /// <summary>
        /// Updates rail connectivity if necessary
        /// </summary>
        public override void OnDrop(Vector3Int coords)
        {
            List<RailTile> poweredRails = new List<RailTile>();
            Vector3 dir = Vector3.forward;

            for (int i = 0; i < 4; i++)
            {
                var rail = TryGetAdjacentRail(dir, true);
                if (rail != null && Vector3.Dot(rail.inDirection, coords - rail.transform.position) >= 0)
                    poweredRails.Add(rail);

                dir = Quaternion.AngleAxis(90, Vector3.up) * dir;
            }

            if (poweredRails.Count > 0)
            {
                RailTile inRail = null;
                foreach (RailTile r in poweredRails)
                {
                    if (r.connectCount < 2)
                    {
                        if (inRail == null)
                        {
                            inRail = r;
                            break;
                        }
                    }
                }
                if (inRail == null) return;

                Vector3Int dirToThis = coords - Vector3Int.FloorToInt(inRail.transform.position);

                if (inRail.outDirection != dirToThis) inRail.SetState(inRail.inDirection, dirToThis, 2);
                SetState(dirToThis, dirToThis, 1);
            }
        }

        /// <summary>
        /// Gets next powered rail in the track after this
        /// </summary>
        /// <returns>Next RailTile if there is one, otherwise null</returns>
        public RailTile TryGetNextRail() => TryGetAdjacentRail(outDirection, true);

        /// <summary>
        /// Gets adjacent powered rail in the given direction from this transform
        /// </summary>
        /// <param name="isPowered">If the search require rail to be true or not</param>
        /// <returns>Adjacent RailTile if there is one, otherwise null</returns>
        private RailTile TryGetAdjacentRail(Vector3 direction, bool isPowered)
        {
            RaycastHit hitInfo;
            var mask = LayerMask.GetMask("Default");
            if (Physics.Raycast(transform.position, direction, out hitInfo, 1, mask))
            {
                var rail = hitInfo.transform.GetComponent<RailTile>();
                if (rail != null && !(isPowered ^ rail.IsPowered)) return rail;
            }

            return null;
        }

        /// <summary>
        /// Sets rail's connections
        /// </summary>
        /// <param name="inDir">Direction to this rail from the previous one</param>
        /// <param name="outDir">Direction from this rail to the next one</param>
        /// <param name="connectionCount">The amount of rails this is connecting to [0, 2]</param>
        private void SetState(Vector3Int inDir, Vector3Int outDir, int connectionCount)
        {
            connectCount = connectionCount;

            if (inDir != Vector3Int.zero)
            {
                bool isStraight = inDir - outDir == Vector3Int.zero;

                straightMesh.gameObject.SetActive(isStraight);
                bentMesh.gameObject.SetActive(!isStraight);
                straightPower.SetActive(isStraight);
                bentPower.SetActive(!isStraight);

                if (isStraight) transform.forward = outDir;
                else transform.forward = InOutToForward(inDir, outDir);

                var nextRail = TryGetAdjacentRail(outDir, false);
                if (nextRail != null)
                {
                    nextRail.SetState(outDir, outDir, 1);
                    connectCount++;
                }
            }
            else
            {
                straightMesh.gameObject.SetActive(true);
                bentMesh.gameObject.SetActive(false);
                straightPower.SetActive(false);
                bentPower.SetActive(false);

                transform.forward = Vector3.forward;

                var nextRail = TryGetAdjacentRail(outDirection, true);
                if (nextRail != null) nextRail.SetState(Vector3Int.zero, nextRail.outDirection, 0);
                var prevRail = TryGetAdjacentRail(-inDirection, true);
                if (prevRail != null) prevRail.connectCount = 1;
            }

            inDirection = inDir;
            outDirection = outDir;
        }

        /// <summary>
        /// Jank way to determines which way the given bent rail turn
        /// </summary>
        /// <returns>True if rail turns right, otherwise false</returns>
        public static bool BentRailToRight(RailTile rail)
        {
            if (rail.straightMesh.activeSelf) Debug.LogError("Given Rail not bent");

            // No Vector3Int.forward or back in this version wtf?
            var forward = new Vector3Int(0, 0, 1);

            if ((rail.inDirection == forward && rail.outDirection == Vector3Int.right) ||
                (rail.inDirection == Vector3Int.right && rail.outDirection == -forward) ||
                (rail.inDirection == Vector3Int.left && rail.outDirection == forward) ||
                (rail.inDirection == -forward && rail.outDirection == Vector3Int.left)) return true;
            else return false;
        }

        // Probably unnecessary
        public static Vector3Int BentRailToForward(RailTile rail) => InOutToForward(rail.inDirection, rail.outDirection);

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
    }
}