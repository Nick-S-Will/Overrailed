using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Managers;
using Overrailed.Terrain.Generation;
using Overrailed.Terrain.Tools;
using Overrailed.Terrain.Tiles;

namespace Overrailed.Mob
{
    [RequireComponent(typeof(CharacterController))]
    public abstract class HumanoidControls : MonoBehaviour
    {
        #region Inspector Variables
        [Header("Walk")]
        [SerializeField] private float moveSpeed = 5;
        [Tooltip("Radians per second of the player's turn")]
        [SerializeField] private float turnSpeed = 15;
        [SerializeField] private float legSwingCoefficient = 0.1f, legRaiseAngle = 45;

        [Header("Dash")]
        [SerializeField] [Range(1, 10)] private float dashSpeedMultiplier = 2;
        [SerializeField] [Range(0, 1)] private float dashDuration = 1;

        [Header("Arms")]
        [SerializeField] private float armTurnSpeed = 180;
        [SerializeField] private float armSwingSpeed = 360;

        [Header("Interact")]
        [SerializeField] private float interactInterval = 0.5f;

        [Header("Transforms")] [SerializeField] private Transform armL;
        [SerializeField] private Transform armR, legL, legR, calfL, calfR, toolHolder, pickupHolder;
        #endregion

        protected MapManager map;
        private CharacterController controller;
        private List<Coroutine> currentArmTurns = new List<Coroutine>();
        private Coroutine toolSwinging, legSwinging;
        private IPickupable heldItem;
        private Vector3 lastInputDir;
        private float lastDashTime, lastInteractTime;
        /// <summary>
        /// True if player was moving in the previous update, used to start leg swinging
        /// </summary>
        private bool wasMoving;
        /// <summary>
        /// True if player is moving in the current update, used to keep legs swinging
        /// </summary>
        private bool isMoving;

        public Vector3Int LookPoint => Vector3Int.RoundToInt(transform.position + Vector3.up + lastInputDir);
        protected Vector2 InputDir { private get; set; }
        public int Strength { get; private set; } = 2;
        public bool IsHoldingItem => heldItem != null;
        protected bool HoldingDashKey { private get; set; }

        protected virtual void Start()
        {
            transform.parent = null;
            transform.position = new Vector3(transform.position.x, 0, transform.position.z);

            try { map = Physics.OverlapBox(transform.position, 0.1f * Vector3.one, Quaternion.identity, LayerMask.GetMask("Ground"))[0].transform.parent.GetComponent<MapManager>(); }
            catch (IndexOutOfRangeException) 
            {
                enabled = false;
                throw new Exception("Character spawned in without a map beneath it");
            }
            
            controller = GetComponent<CharacterController>();
            lastDashTime = -dashDuration;

            _ = StartCoroutine(HandleMovement());
        }

        #region Movement
        protected IEnumerator HandleMovement()
        {
            while (this)
            {
                var inputDir = new Vector3(InputDir.x, 0, InputDir.y);

                transform.forward = Vector3.RotateTowards(transform.forward, lastInputDir, turnSpeed * Time.fixedDeltaTime, 0);
                if (UpdateMovingStates(inputDir))
                {
                    Vector3 deltaPos;
                    if (inputDir == Vector3.zero) deltaPos = moveSpeed * lastInputDir * Time.fixedDeltaTime;
                    else
                    {
                        deltaPos = moveSpeed * inputDir * Time.fixedDeltaTime;
                        lastInputDir = inputDir;
                    }

                    if (HoldingDashKey) lastDashTime = Time.time;
                    if (Time.time < lastDashTime + dashDuration) deltaPos *= DashMultiplier();

                    // Moves character
                    if (map.PointIsInBounds(transform.position + deltaPos)) controller.Move(deltaPos);
                }

                yield return new WaitForSeconds(Time.fixedDeltaTime);
                yield return new WaitUntil(() => enabled && !GameManager.IsPaused());
            }
        }

        /// <summary>
        /// Updates <see cref="isMoving"/> and <see cref="wasMoving"/>
        /// </summary>
        /// <param name="input">Player XZ plane input</param>
        /// <returns>True if player is moving</returns>
        private bool UpdateMovingStates(Vector3 input)
        {
            // Gets current moving state
            isMoving = input != Vector3.zero || HoldingDashKey || Time.time < lastDashTime + dashDuration;
            if (!isMoving) return wasMoving = false;

            // Start swinging legs if player wasn't moving last update
            if (!wasMoving)
            {
                if (legSwinging != null) StopCoroutine(legSwinging);
                legSwinging = StartCoroutine(SwingLimbs());
            }
            wasMoving = isMoving;

            return true;
        }

        protected float DashMultiplier()
        {
            /// Parabola that goes from <see cref="dashSpeedMultiplier"/> to 1 over <see cref="dashDuration"/> seconds
            return (dashSpeedMultiplier - 1) * (-Mathf.Pow(Mathf.InverseLerp(lastDashTime, lastDashTime + dashDuration, Time.time), 4) + 1) + 1;
        }

        protected void StopMovement()
        {
            wasMoving = false;
            isMoving = false;
        }
        #endregion

        #region Interact
        protected void MainInteract() => TryInteract(TryToPickUpAll, TryDropAll);
        protected void AltInteract() => TryInteract(TryToPickUpSingle, TryDropSingle);
        private void TryInteract(Func<IPickupable, bool> pickup, Func<bool> drop)
        {
            if (Time.time < lastInteractTime + interactInterval) return;
            lastInteractTime = Time.time;

            var collisions = Physics.OverlapBox(transform.position + Vector3.up + lastInputDir, 0.1f * Vector3.one, Quaternion.identity, GameManager.instance.InteractMask);
            if (collisions.Length > 0)
            {
                if (IsHoldingItem) TryUseHeldItemOn(collisions[0].transform.GetComponent<IInteractable>());
                else _ = pickup(collisions[0].transform.GetComponent<IPickupable>());
            }
            else _ = drop();
        }

        private bool TryToPickUpAll(IPickupable pickup) => TryToPickUp(pickup, Strength);
        private bool TryToPickUpSingle(IPickupable pickup) => TryToPickUp(pickup, 1);
        /// <summary>
        /// Tries to pick up given IPickupable
        /// </summary>
        /// <returns>True if given pickup isn't null and is picked up</returns>
        private bool TryToPickUp(IPickupable pickup, int amount)
        {
            if (pickup == null) return false;

            bool bothHands = pickup.IsTwoHanded;
            heldItem = pickup.TryPickUp(bothHands ? pickupHolder : toolHolder, amount);
            if (IsHoldingItem) RaiseArms(bothHands);

            return IsHoldingItem;
        }

        /// <summary>
        /// Uses <see cref="heldItem"/> on the given Tile if not swinging
        /// </summary>
        private void TryUseHeldItemOn(IInteractable interactable)
        {
            if (!IsHoldingItem || toolSwinging != null || interactable == null) return;

            var interaction = interactable.TryInteractUsing(heldItem);
            if (interaction == Interaction.None)
            {
                if (!TrySwapHeldWith(interactable)) _ = TryReplaceWithHeld(interactable as Tile);
            }
            else if (interaction == Interaction.Used)
            {
                LowerArms(heldItem.IsTwoHanded);
                heldItem = null;
            }
            else if (interaction == Interaction.Interacted)
            {
                if (heldItem is Tool) toolSwinging = StartCoroutine(SwingTool());
            }
        }

        /// <summary>
        /// Swaps the <see cref="heldItem"/> and the <paramref name="interactable"/>'s positions
        /// </summary>
        /// <returns>True if they swapped successfully</returns>
        private bool TrySwapHeldWith(IInteractable interactable)
        {
            Tile interactTile = interactable as Tile;
            if (!IsHoldingItem || !interactTile.CanPickUp) return false;

            var stackPos = Vector3Int.RoundToInt(interactTile.transform.position);

            if (!(interactTile is StackTile stackTile) || stackTile.GetStackCount() <= Strength)
            {
                _ = interactTile.TryPickUp(interactTile.IsTwoHanded ? pickupHolder : toolHolder, Strength);
                map.PlacePickup(heldItem, stackPos);
                heldItem = interactTile;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Moves <paramref name="toReplace"/> to the nearest viable position then puts <see cref="heldItem"/> where <paramref name="toReplace"/> was
        /// </summary>
        /// <returns>True if the objects were moved successfully</returns>
        private bool TryReplaceWithHeld(IPickupable toReplace)
        {
            if (!IsHoldingItem || !toReplace.CanPickUp) return false;

            var replacePos = Vector3Int.RoundToInt((toReplace as MonoBehaviour).transform.position);
            _ = map.MovePickup(toReplace);
            map.ForcePlacePickup(heldItem, replacePos);

            LowerArms(heldItem.IsTwoHanded);
            heldItem = null;

            return true;
        }

        /// <summary>
        /// Places <see cref="heldItem"/> on the ground if it's not null
        /// </summary>
        /// <returns>True if <see cref="heldItem"/> was placed</returns>
        private bool TryDropAll() => TryDrop(false);
        /// <summary>
        /// Places <see cref="heldItem"/> on the ground, or a single tile if it's a stack, if <see cref="heldItem"/> isn't null
        /// </summary>
        /// <returns>True if <see cref="heldItem"/>'s entire stack was placed</returns>
        private bool TryDropSingle() => TryDrop(true);
        private bool TryDrop(bool single)
        {
            Vector3Int coords = LookPoint;
            if (!IsHoldingItem || toolSwinging != null || !map.PointIsInPlayBounds(coords) || !heldItem.OnTryDrop()) return false;

            if (single && heldItem is StackTile stack && stack.GetStackCount() > 1)
            {
                map.PlacePickup(stack.TryPickUp(null, 1), coords);
            }
            else
            {
                map.PlacePickup(heldItem, coords);
                LowerArms(heldItem.IsTwoHanded);
                heldItem = null;

                return true;
            }

            return false;
        }

        public void ForceDrop()
        {
            if (!IsHoldingItem) return;

            map.PlacePickup(heldItem, Vector3Int.RoundToInt(transform.position + Vector3.up));
            LowerArms(heldItem.IsTwoHanded);
            heldItem = null;
        }
        #endregion

        #region Limb Movement
        /// <summary>
        /// Points armR to the local forwards
        /// </summary>
        /// <param name="both">Makes armL point forwards too</param>
        private void RaiseArms(bool both)
        {
            _ = StopTurnArmRoutines();

            currentArmTurns.Add(StartCoroutine(TurnArm(armR, -90, armTurnSpeed)));
            if (both) currentArmTurns.Add(StartCoroutine(TurnArm(armL, -90, armTurnSpeed)));
            else currentArmTurns.Add(StartCoroutine(TurnArm(armL, 0, armTurnSpeed)));
        }

        /// <summary>
        /// Points armR or both arms to the local downwards
        /// </summary>
        /// <param name="both">Makes armL point downwards too</param>
        private void LowerArms(bool both)
        {
            _ = StopTurnArmRoutines();
            currentArmTurns.Add(StartCoroutine(TurnArm(armR, 0, armTurnSpeed)));
            if (both) currentArmTurns.Add(StartCoroutine(TurnArm(armL, 0, armTurnSpeed)));
        }

        /// <summary>
        /// Rotates armR down, waits 0.1 seconds, then back up
        /// </summary>
        private IEnumerator SwingTool()
        {
            _ = StopTurnArmRoutines();
            currentArmTurns.Add(StartCoroutine(TurnArm(armR, 0, armSwingSpeed)));
            yield return currentArmTurns[currentArmTurns.Count - 1];
            yield return new WaitForSeconds(0.05f);
            currentArmTurns.Add(StartCoroutine(TurnArm(armR, -90, armSwingSpeed / 2)));
            yield return currentArmTurns[currentArmTurns.Count - 1];

            toolSwinging = null;
        }

        /// <summary>
        /// Animates arm turning around it's local x
        /// </summary>
        /// <param name="arm">Selected arm pivot</param>
        /// <param name="rotation">Final x value on arm.eulerAngles.x</param>
        /// <param name="turnSpeed">Speed of rotation in degrees per second</param>
        private IEnumerator TurnArm(Transform arm, float rotation, float turnSpeed)
        {
            Quaternion from = arm.localRotation, to = from * Quaternion.Euler(rotation - from.eulerAngles.x, 0, 0);
            float animSpeed = turnSpeed / Quaternion.Angle(from, to);
            float percent = 0;

            while (percent < 1)
            {
                yield return null;

                percent += animSpeed * Time.deltaTime;
                arm.localRotation = Quaternion.Lerp(from, to, percent);
            }

            arm.localRotation = to;
        }

        /// <summary>
        /// Cancels all ongoing TurnArm routines
        /// </summary>
        /// <returns>True if there were any TurnArm routines in progress</returns>
        private bool StopTurnArmRoutines()
        {
            if (currentArmTurns.Count == 0) return false;

            foreach (var c in currentArmTurns) StopCoroutine(c);
            currentArmTurns.Clear();

            return true;
        }

        private IEnumerator SwingLimbs()
        {
            float time = 0;

            // Swing legs back and forth
            while (isMoving)
            {
                float angle = legRaiseAngle * Mathf.Sin(time);

                legL.localRotation = Quaternion.Euler(angle, 0, 0);
                legR.localRotation = Quaternion.Euler(-angle, 0, 0);

                calfL.localRotation = Quaternion.Euler(Mathf.Abs(angle), 0, 0); ;
                calfR.localRotation = calfL.localRotation;

                if (!IsHoldingItem)
                {
                    armL.localRotation = legR.localRotation;
                    armR.localRotation = legL.localRotation;
                }

                time += legSwingCoefficient * moveSpeed * legRaiseAngle * Time.deltaTime;
                yield return null;
            }

            // Return to base position
            while (legL.localRotation != Quaternion.identity)
            {
                float angle = Quaternion.Angle(legL.localRotation, Quaternion.identity);
                float maxRadians = moveSpeed * legRaiseAngle * Time.deltaTime;
                legL.localRotation = Quaternion.RotateTowards(legL.localRotation, Quaternion.identity, maxRadians);
                legR.localRotation = Quaternion.RotateTowards(legR.localRotation, Quaternion.identity, maxRadians);

                calfL.localRotation = Quaternion.RotateTowards(calfL.localRotation, Quaternion.identity, maxRadians);
                calfR.localRotation = Quaternion.RotateTowards(calfR.localRotation, Quaternion.identity, maxRadians);

                if (!IsHoldingItem)
                {
                    armL.localRotation = Quaternion.RotateTowards(armL.localRotation, Quaternion.identity, maxRadians);
                    armR.localRotation = Quaternion.RotateTowards(armR.localRotation, Quaternion.identity, maxRadians);
                }

                yield return null;
            }

            legR.localRotation = Quaternion.identity;

            calfL.localRotation = Quaternion.identity;
            calfR.localRotation = Quaternion.identity;

            if (!IsHoldingItem)
            {
                armL.localRotation = Quaternion.identity;
                armR.localRotation = Quaternion.identity;
            }
        }
        #endregion
    }
}