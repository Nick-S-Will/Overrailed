using System.Collections;
using UnityEngine;

using Overrailed.Managers;
using Overrailed.Managers.Audio;
using Overrailed.Terrain.Tools;
using Overrailed.Terrain.Tiles;

namespace Overrailed.Train
{
    [RequireComponent(typeof(Rigidbody))]
    public abstract class TrainCar : Tile, IPickupable, IInteractable
    {
        public event System.Action OnDeath;
        public virtual event System.Action OnInteract;
        public static event System.Action<TrainCar> OnWarning;
        protected event System.Action OnStartDriving;

        [SerializeField] private Locomotive leaderLocomotive;
        [Space]
        [SerializeField] private ParticleSystem burningParticlePrefab;
        [SerializeField] private ParticleSystem breakParticlePrefab, splashParticlePrefab;
        [Space]
        [SerializeField] private AudioClip explosionSound;
        [SerializeField] private AudioClip igniteSound;
        [Space]
        [SerializeField] private Transform burnPoint;
        [SerializeField] private RailTile startRail;
        [SerializeField] protected int tier = 1;
        [SerializeField] protected bool isPermeable;

        protected ParticleSystem burningParticles;
        private Rigidbody rb;
        protected RailTile currentRail;
        private int pathIndex, pathDir;

        public Locomotive LeaderLocomotive => leaderLocomotive;
        public int Tier => tier;
        public bool HasRail => currentRail;
        public override bool IsTwoHanded => true;
        public abstract bool IsWarning { get; }

        override protected void Start()
        {
            if (startRail) _ = TrySetRail(startRail, false);
            if (leaderLocomotive) leaderLocomotive.OnStartTrain += StartDriving;
            rb = GetComponent<Rigidbody>();

            base.Start();
        }

        public void StartDriving() => _ = StartCoroutine(Drive());

        private IEnumerator Drive()
        {
            OnStartDriving?.Invoke();

            // Sets initial start values
            var target = currentRail.Path.GetChild(pathIndex);
            var startForward = target.forward;
            float startDst = (target.position - transform.position).magnitude;

            while (currentRail)
            {
                while (transform.position == target.position)
                {
                    pathIndex += pathDir;

                    // At middle of final checkpoint rail
                    if (currentRail.IsFinalCheckpoint && pathIndex == currentRail.Path.childCount / 2 + 1)
                    {
                        if (Manager.instance is GameManager gm) gm.ReachCheckpoint();
                        else if (Manager.instance is TutorialManager tm) tm.ReachCheckpoint();
                        else Debug.LogError("No GameManager or TutorialManager Found");
                    }
                    // Reached end of rail
                    else if (pathIndex < 0 || currentRail.Path.childCount <= pathIndex)
                    {
                        if (currentRail.NextRail) UpdateRail(currentRail.NextRail);
                        else
                        {
                            Die();
                            yield break;
                        }
                    }

                    // Sets new start values
                    startForward = pathDir * target.forward;
                    target = currentRail.Path.GetChild(pathIndex);
                    startDst = (target.position - transform.position).magnitude;
                }

                transform.position = Vector3.MoveTowards(transform.position, target.position, leaderLocomotive.TrainSpeed * Time.deltaTime);
                rb.MovePosition(transform.position);
                float currentDst = (target.position - transform.position).magnitude;
                transform.forward = Vector3.Lerp(startForward, pathDir * target.forward, 1 - (currentDst / startDst));

                yield return null;
                if (!Manager.IsPlaying())
                {
                    yield return new WaitUntil(() => Manager.IsPlaying() && leaderLocomotive.IsDriving);
                    OnStartDriving?.Invoke();
                }
            }
        }

        #region IPickupable and IInteractable implementations
        /// <summary>
        /// Picks up this car if in edit mode
        /// </summary>
        /// <returns>This train car if in edit mode, otherwise null</returns>
        public override IPickupable TryPickUp(Transform parent, int amount)
        {
            if (GameManager.CurrentState != GameState.Edit || this is Locomotive || currentRail) return null;

            GetComponent<BoxCollider>().enabled = false;
            transform.parent = parent;
            transform.localPosition = Vector3.up;
            transform.localRotation = Quaternion.identity;

            AudioManager.PlaySound(PickupAudio, transform.position);

            return this;
        }

        public override bool OnTryDrop() => false;

        public override void Drop(Vector3Int position) => AudioManager.PlaySound(dropSound, transform.position);

        public virtual Interaction TryInteractUsing(IPickupable item)
        {
            if (Manager.IsEditing() && item is TrainCar car) return TryUpgradeCar(car) ? Interaction.Used : Interaction.None;
            else if (item is Bucket bucket) return TryExtinguish(bucket) ? Interaction.Interacted : Interaction.None;
            else return Interaction.None;
        }
        protected void InvokeOnInteract() => OnInteract?.Invoke();
        #endregion

        protected virtual bool TryUpgradeCar(TrainCar newCar)
        {
            if (newCar.GetType() != GetType() || newCar.tier <= tier) return false;

            if (newCar.TrySetRail(currentRail, false))
            {
                newCar.leaderLocomotive = leaderLocomotive;
                newCar.StartDriving();

                AudioManager.PlaySound(dropSound, transform.position);
            }

            Die();
            return true;
        }

        public virtual async void Ignite()
        {
            burningParticles = Instantiate(burningParticlePrefab, burnPoint);
            AudioManager.PlaySound(igniteSound, transform.position);
            Manager.OnPause += burningParticles.Pause;
            Manager.OnResume += burningParticles.Play;

            while (burningParticles)
            {
                await Manager.Delay(5);

                if (!burningParticles) return;

                var carF = TryGetAdjacentCar(transform.position, transform.forward);
                var carB = TryGetAdjacentCar(transform.position, -transform.forward);

                if (carF && !carF.burningParticles && carF.currentRail) carF.Ignite();
                if (carB && !carB.burningParticles && carB.currentRail) carB.Ignite();
            }

            Manager.OnPause -= burningParticles.Pause;
            Manager.OnResume -= burningParticles.Play;
        }
        
        /// <summary>
        /// Puts out the fire on this car if buck has water
        /// </summary>
        /// <param name="bucket">Bucket used to put out the fire</param>
        /// <returns>True if fire was put out, otherwise null</returns>
        private bool TryExtinguish(Bucket bucket)
        {
            if (burningParticles && bucket.IsFull)
            {
                Destroy(burningParticles.gameObject, burningParticles.main.duration);
                var emissionSettings = burningParticles.emission;
                emissionSettings.enabled = false;
                burningParticles = null;
                bucket.IsFull = false;

                return true;
            }
            else return false;
        }

        // TODO: Clean up how these work
        /// <summary>
        /// Places this train car on given rail tile
        /// </summary>
        /// <param name="rail">Rail to be set</param>
        /// <param name="connectCheck">If placing requires checking for a train car ahead</param>
        /// <returns>True if train car is successfully placed</returns>
        public bool TrySetRail(RailTile rail, bool connectCheck)
        {
            UpdateRail(rail);
            pathIndex = rail.Path.childCount / 2;

            var pos = rail.Path.GetChild(pathIndex).position;
            var dir = pathDir * rail.Path.GetChild(pathIndex).forward;
            if (connectCheck && !TryGetAdjacentCar(pos, rail.OutDirection)) return false;

            transform.position = pos;
            transform.forward = dir;

            GetComponent<BoxCollider>().enabled = !isPermeable;

            return true;
        }

        /// <summary>
        /// Updates <paramref name="newRail"/>'s last passenger variable, then sets train car's pathIndex and pathDir
        /// </summary>
        /// <param name="newRail">The RailTile the </param>
        private void UpdateRail(RailTile newRail)
        {
            if (currentRail) currentRail.Disembark(this);
            newRail.Embark(this);
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

            transform.parent = newRail.transform;
        }

        /// <summary>
        /// Looks for TrainCar in the given direction from the given point
        /// </summary>
        /// <param name="point">Point from where the raycast is sent</param>
        /// <param name="direction">Direction where the raycast is sent to</param>
        /// <returns>TrainCar in the given direction from the given point if there is one, otherwise null</returns>
        private TrainCar TryGetAdjacentCar(Vector3 point, Vector3 direction)
        {
            var mask = LayerMask.GetMask("Train");

            if (Physics.Raycast(point, direction, out RaycastHit hitInfo, 1, mask)) return hitInfo.transform.GetComponent<TrainCar>();
            else return null;
        }

        protected void StartWarning() => OnWarning?.Invoke(this);
        
        protected virtual void Die()
        {
            BreakIntoParticles(breakParticlePrefab, MeshColorGradient, transform.position);
            AudioManager.PlaySound(explosionSound, transform.position);

            var waterColliders = Physics.OverlapBox(transform.position, 0.1f * Vector3.one, Quaternion.identity, LayerMask.GetMask("Water"));
            if (waterColliders.Length > 0) Destroy(Instantiate(splashParticlePrefab, waterColliders[0].transform.position, Quaternion.identity), splashParticlePrefab.main.startLifetime.constant);

            if (leaderLocomotive) leaderLocomotive.OnStartTrain -= StartDriving;
            Destroy(gameObject);

            OnDeath?.Invoke();
        }

        [System.Serializable]
        public class StackPoint
        {
            [SerializeField] private Transform transform;
            [SerializeField] private string stackType;

            [HideInInspector] public StackTile stackTop;

            public Transform Transform => transform;
            public string StackType => stackType;
            /// <summary>
            /// True if the crafting point has at least one of its stack tiles
            /// </summary>
            public bool IsHolding => transform.childCount == 1;
        }
    }
}