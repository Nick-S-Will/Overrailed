using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Overrailed.Managers;
using Overrailed.Managers.Audio;
using Overrailed.Train;

namespace Overrailed.Terrain.Tiles
{
    public class RailTile : StackTile, IInteractable
    {
        [Space]
        [SerializeField] private GameObject straightMesh;
        [SerializeField] private GameObject bentMesh;
        [Tooltip("Gameobject that is enabled when this.IsPowered")]
        [SerializeField] private GameObject straightPower, bentPower;
        [Space]
        [Tooltip("Must have odd number of children")] [SerializeField] private Transform straightPathParent;
        [Tooltip("Must have odd number of children")] [SerializeField] private Transform bentPathParent;
        [SerializeField] private AudioClip connectSound;
        [Space]
        [SerializeField] protected bool startsPowered;
        [SerializeField] private bool isCheckpoint, showPath;

        private bool hasBeenRidden;

        private static readonly Vector3[] XZDirections = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };

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
            if (startsPowered) Utils.MoveToLayer(transform, LayerMask.NameToLayer("Rail"));
        }

        protected override void Start()
        {
            if (startsPowered || isCheckpoint)
            {
                InDirection = Vector3Int.RoundToInt(straightMesh.transform.forward);
                OutDirection = InDirection;

                UpdateConnections();

                if (startsPowered) straightPower.SetActive(true);
                else if (Manager.instance is GameManager gm) gm.OnEndCheckpoint += ConvertToNonCheckpoint;
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
                if (Manager.instance is GameManager gm) gm.OnEndCheckpoint -= ConvertToNonCheckpoint;
            }
        }

        private void UpdateConnections()
        {
            RaycastHit info;
            if (Physics.Raycast(transform.position, OutDirection, out info, 1, LayerMask.GetMask("Default", "Rail"))) NextRail = info.collider.GetComponent<RailTile>();
            if (Physics.Raycast(transform.position, -InDirection, out info, 1, LayerMask.GetMask("Default", "Rail"))) PrevRail = info.collider.GetComponent<RailTile>();
        }
        #endregion

        #region Interface Overrides
        public override Interaction TryInteractUsing(IPickupable item)
        {
            if (Passenger) return Passenger.TryInteractUsing(item);
            else if (item is TrainCar car) return car.TrySetRail(this, true) ? Interaction.Used : Interaction.None;
            else if (item is RailTile rail)
            {
                if (IsPowered) return Interaction.Interacted; // To prevent swapping of rails
                else return rail.TryStackOn(this) ? Interaction.Used : Interaction.None;
            }
            else return Interaction.None;
        }

        /// <summary>
        /// Checks if can be picked up, then unpowers it, then returns the base method
        /// </summary>
        /// <returns>The tile to be picked up, if it can be</returns>
        public override IPickupable TryPickUp(Transform parent, int amount = 1)
        {
            if (Passenger) return Passenger.TryPickUp(parent, amount);
            else if (!CanPickUp) return null;
            else if (IsPowered) SetState(Vector3Int.zero, Vector3Int.zero, null);

            return base.TryPickUp(parent, amount);
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

        public override bool OnTryDrop(Vector3Int coords)
        {
            if (NextInStack == null) return true;

            if (TryGetConnectableRailAt(coords))
            {
                MapManager.FindMap(coords).ForcePlacePickup(TryPickUp(null), coords);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Places rail at <paramref name="coords"/> and updates connectivity
        /// </summary>
        public override void Drop(Vector3Int coords)
        {
            // Doesn't connect to tracks if in a stack
            if (NextInStack)
            {
                AudioManager.PlaySound(DropAudio, coords);
                return;
            }

            var connectableRail = TryGetConnectableRail();
            if (connectableRail) ConnectToRail(connectableRail);
            else AudioManager.PlaySound(DropAudio, transform.position);
        }
        #endregion

        private void ConnectToRail(RailTile connectableRail)
        {
            Vector3Int dirToThis = Coords - Vector3Int.FloorToInt(connectableRail.transform.position);

            if (connectableRail.OutDirection != dirToThis) connectableRail.SetState(connectableRail.InDirection, dirToThis, this);
            else connectableRail.NextRail = this;

            PrevRail = connectableRail;
            SetState(dirToThis, dirToThis, null);

            AudioManager.PlaySound(connectSound, transform.position);
        }

        #region Find Rails
        private RailTile TryGetConnectableRail() => TryGetConnectableRailAt(Coords);
        private static RailTile TryGetConnectableRailAt(Vector3Int coords)
        {
            // Looks for adjacent powered rails to connect to
            foreach (var dir in XZDirections)
            {
                var rail = TryGetRailAdjacentTo(coords, dir, true);
                // rail found, has a connection available, isn't the final checkpoint, has no passenger or rail already pointing at this
                if (rail && rail.NextRail == null && !rail.IsFinalCheckpoint && (rail.Passenger == null || rail.OutDirection == (coords - rail.Coords)))
                {
                    return rail;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets adjacent rail in the given direction from this transform if <see cref="IsPowered"/> is equal to <paramref name="isPowered"/>
        /// </summary>
        /// <param name="isPowered">True to search only for powered rails, false to search only for normal rails</param>
        /// <returns>Adjacent RailTile if there is one that matches criteria, otherwise null</returns>
        private RailTile TryGetAdjacentRail(Vector3 direction, bool isPowered) => TryGetRailAdjacentTo(transform.position, direction, isPowered);

        /// <summary>
        /// Gets adjacent rail in the given direction from <paramref name="position"/> if <see cref="IsPowered"/> is equal to <paramref name="isPowered"/>
        /// </summary>
        /// <param name="isPowered">True to search only for powered rails, false to search only for normal rails</param>
        /// <returns>Adjacent RailTile if there is one that matches criteria, otherwise null</returns>
        private static RailTile TryGetRailAdjacentTo(Vector3 position, Vector3 direction, bool isPowered) => TryFindRail(position, direction, isPowered ? LayerMask.GetMask("Rail") : LayerMask.GetMask("Default"));

        /// <summary>
        /// Used by the <see cref="TryGetAdjacentRail"/> methods, which are more straightforward
        /// </summary>
        /// <param name="layerMask">Physics layer mask used to find rail</param>
        /// <returns>Adjacent RailTile if there is one that matches criteria, otherwise null</returns>
        private static RailTile TryFindRail(Vector3 position, Vector3 direction, int layerMask)
        {
            if (Physics.Raycast(position, direction, out RaycastHit hitInfo, 1, layerMask)) return hitInfo.transform.GetComponent<RailTile>();
            return null;
        }
        #endregion

        #region Update State
        // Prevents StackOverflow when rails connect consecutively
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

                transform.forward = isStraight ? outDir : ForwardFor(inDir, outDir);

                if (newConnection) NextRail = newConnection;
                // Tries to connect this to proceeding rail
                else
                {
                    RailTile nextRail = null;
                    // Iterates the local left, forward, then right to find an unpowered rail to connect to
                    Vector3Int dir = Vector3Int.RoundToInt(Quaternion.AngleAxis(-90, Vector3.up) * outDir);
                    for (int i = 0; i < 3; i++)
                    {
                        // Tries to get rail in currently iterated direction
                        nextRail = TryGetAdjacentRail(dir, false);
                        if (nextRail && nextRail.NextInStack == null && (!isCheckpoint ^ nextRail.IsFinalCheckpoint)) break;

                        dir = Vector3Int.RoundToInt(Quaternion.AngleAxis(90, Vector3.up) * dir);
                    }

                    if (nextRail)
                    {
                        SetState(inDir, dir, nextRail);
                        nextRail.PrevRail = this;

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
                                        if (Manager.instance is GameManager) locomotive.SpeedUp();
                                        else if (Manager.instance is TutorialManager) locomotive.StartTrain();
                                        break;
                                    }
                                } while (prev);
                            }
                        }
                        else nextRail.DelaySetState(dir, dir, null);

                        return;
                    }
                }
            }

            InDirection = inDir;
            OutDirection = outDir;

            Utils.MoveToLayer(transform, PrevRail ? LayerMask.NameToLayer("Rail") : LayerMask.NameToLayer("Default"));
        }
        #endregion

        /// <summary>
        /// Determines if this rail turns right or not
        /// </summary>
        /// <returns>False if this rail turns left or is straight, otherwise true</returns>
        public bool TurnsRight() => Mathf.RoundToInt(Vector3.Cross(InDirection, OutDirection).y) == 1;
        
        /// <summary>
        /// Converts in and out directions to local forward for bent rails
        /// </summary>
        private static Vector3Int ForwardFor(Vector3Int inDir, Vector3Int outDir)
        {
            bool turnsRight = Mathf.RoundToInt(Vector3.Cross(inDir, outDir).y) == 1;

            if (turnsRight) return outDir;
            else return -inDir;
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