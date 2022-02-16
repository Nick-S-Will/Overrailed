using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        private bool hasBeenRidden;

        private static readonly Vector3[] mainDirections = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };

        public Transform Path => IsStraight ? straightPathParent : bentPathParent;
        public TrainCar Passenger { get; private set; }
        public RailTile PrevRail { get; private set; }
        public RailTile NextRail { get; private set; }
        public Vector3Int InDirection { get; private set; } = Vector3Int.zero;
        public Vector3Int OutDirection { get; private set; } = Vector3Int.zero;
        public override bool CanPickUp => !(startsPowered || hasBeenRidden || isCheckpoint);
        public bool IsStraight => straightMesh.activeSelf;
        public bool IsPowered => PrevRail || NextRail;
        public bool IsFinalCheckpoint => isCheckpoint && NextRail == null;

        private void Awake()
        {
            if (startsPowered) GameManager.MoveToLayer(transform, LayerMask.NameToLayer("Rail"));
        }

        protected override void Start()
        {
            if (startsPowered || isCheckpoint)
            {
                InDirection = Vector3Int.RoundToInt(straightMesh.transform.forward);
                OutDirection = InDirection;

                UpdateConnections();

                if (startsPowered) straightPower.SetActive(true);
                else GameManager.instance.OnEndCheckpoint += ConvertToNonCheckpoint;
            }

            base.Start();
        }

        #region Train Interactions
        public void Embark(TrainCar newCar)
        {
            Passenger = newCar;
            hasBeenRidden = true;
        }
        public void Disembark(TrainCar car)
        {
            if (Passenger == car) Passenger = null;
        }
        #endregion

        #region Rail Interactions
        private void ConvertToNonCheckpoint()
        {
            if (Passenger)
            {
                isCheckpoint = false;
                UpdateConnections();
                GameManager.instance.OnEndCheckpoint -= ConvertToNonCheckpoint;
            }
        }

        private void UpdateConnections()
        {
            RaycastHit info;
            if (Physics.Raycast(transform.position, OutDirection, out info, 1, LayerMask.GetMask("Default", "Rail"))) NextRail = info.collider.GetComponent<RailTile>();
            if (Physics.Raycast(transform.position, -InDirection, out info, 1, LayerMask.GetMask("Default", "Rail"))) PrevRail = info.collider.GetComponent<RailTile>();
        }
        #endregion

        #region IInteractable Overrides
        /// <summary>
        /// Checks if can be picked up, then unpowers it, then returns the base method
        /// </summary>
        /// <returns>The tile to be picked up, if it can be</returns>
        public override IPickupable TryPickUp(Transform parent, int amount)
        {
            if (Passenger) return Passenger.TryPickUp(parent, amount);
            else if (!CanPickUp) return null;
            else if (IsPowered) SetState(Vector3Int.zero, Vector3Int.zero, null);

            return base.TryPickUp(parent, amount);
        }

        public override bool TryInteractUsing(IPickupable item)
        {
            if (Passenger && Passenger.TryInteractUsing(item)) return true;
            else if (item is TrainCar car) return car.TrySetRail(this, true);
            else if (item is RailTile rail && !IsPowered) return rail.TryStackOn(this);
            else return false;
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
            // Looks for adjacent powered rails to connect to
            foreach (var dir in mainDirections)
            {
                var rail = TryGetAdjacentRail(dir, true);
                // rail found, has a connection available, has no passenger or rail already pointing at this
                if (rail && rail.NextRail == null && (rail.Passenger == null || rail.OutDirection == (transform.position - rail.transform.position)))
                {
                    connectableRail = rail;
                    break;
                }
            }

            // Updates affected rails' states
            if (connectableRail)
            {
                Vector3Int dirToThis = coords - Vector3Int.FloorToInt(connectableRail.transform.position);

                if (connectableRail.OutDirection != dirToThis) connectableRail.SetState(connectableRail.InDirection, dirToThis, this);
                else connectableRail.NextRail = this;

                PrevRail = connectableRail;
                SetState(dirToThis, dirToThis, null);
            }
        }
        #endregion

        #region Find Rails
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
        private async void DelaySetState(Vector3Int inDir, Vector3Int outDir, RailTile newConnection)
        {
            await Task.Yield();

            SetState(inDir, outDir, newConnection);
        }

        /// <summary>
        /// Sets rail's connections
        /// </summary>
        /// <param name="inDir">Direction to this rail from the previous one</param>
        /// <param name="outDir">Direction from this rail to the next one</param>
        private void SetState(Vector3Int inDir, Vector3Int outDir, RailTile newConnection)
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
                if (newConnection == null)
                {
                    if (NextRail)
                    {
                        NextRail.PrevRail = null;
                        NextRail.DelaySetState(Vector3Int.zero, Vector3Int.zero, null);
                        NextRail = null;
                    }
                    if (PrevRail)
                    {
                        PrevRail.NextRail = null;
                        PrevRail = null;
                    }
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

                if (newConnection) NextRail = newConnection;
                // Tries to connect this to proceeding rail
                else
                {
                    // Iterates the local left, forward, then right to find an unpowered rail to connect to
                    Vector3Int dir = Vector3Int.RoundToInt(Quaternion.AngleAxis(-90, Vector3.up) * outDir);
                    for (int i = 0; i < 3; i++)
                    {
                        // Tries to get rail in currently iterated direction
                        var nextRail = TryGetAdjacentRail(dir, false);
                        if (nextRail && nextRail.NextInStack == null)
                        {
                            SetState(inDir, dir, nextRail);
                            nextRail.PrevRail = this;

                            // Checkpoint rails
                            if (nextRail.isCheckpoint)
                            {
                                nextRail.DelaySetState(dir, nextRail.OutDirection, null);

                                if (!isCheckpoint)
                                {
                                    // Speeds train to checkpoint
                                    var prev = this;
                                    do
                                    {
                                        prev = prev.PrevRail;
                                        prev.hasBeenRidden = true;
                                        if (prev.Passenger && prev.Passenger is Locomotive locomotive)
                                        {
                                            locomotive.SpeedUp();
                                            break;
                                        }
                                    } while (prev);
                                }
                            }
                            // Other rails
                            else nextRail.DelaySetState(dir, dir, null);

                            return;
                        }

                        dir = Vector3Int.RoundToInt(Quaternion.AngleAxis(90, Vector3.up) * dir);
                    }
                }
            }

            InDirection = inDir;
            OutDirection = outDir;

            GameManager.MoveToLayer(transform, PrevRail ? LayerMask.NameToLayer("Rail") : LayerMask.NameToLayer("Default"));
        }
        #endregion

        /// <summary>
        /// Jank way to determines which way the given bent rail turn
        /// </summary>
        /// <returns>True if rail turns right, otherwise false</returns>
        public static bool BentRailToRight(RailTile rail)
        {
            if (!rail.bentMesh.activeSelf) Debug.LogError("Given Rail not bent");

            if ((rail.InDirection == Vector3Int.forward && rail.OutDirection == Vector3Int.right) ||
                (rail.InDirection == Vector3Int.right && rail.OutDirection == Vector3Int.back) ||
                (rail.InDirection == Vector3Int.left && rail.OutDirection == Vector3Int.forward) ||
                (rail.InDirection == Vector3Int.back && rail.OutDirection == Vector3Int.left)) return true;
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