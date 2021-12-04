using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Terrain.Tools;
using Uncooked.Terrain.Tiles;

namespace Uncooked.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public event System.Action<IPickupable, Vector3Int> OnPlacePickup;

        #region Inspector Variables
        [Header("Walk")]
        [SerializeField] private float moveSpeed = 5;
        [SerializeField] private float legSwingCoefficient = 0.1f, legRaiseAngle = 45;

        [Header("Dash")]
        [SerializeField] [Range(1, 10)] private float dashSpeedMultiplier = 2;
        [SerializeField] [Range(0, 1)] private float dashDuration = 1;

        [Header("Arms")]
        [SerializeField] private float armTurnSpeed = 180;
        [SerializeField] private float armSwingSpeed = 360;

        [Header("Interact")]
        [SerializeField] private float interactInterval = 0.5f;
        [SerializeField] private int strength = 1;
        [SerializeField] private LayerMask interactMask;

        [Header("Transforms")] [SerializeField] private Transform armL;
        [SerializeField] private Transform armR, legL, legR, calfL, calfR, toolHolder, pickupHolder;
        #endregion

        private CharacterController controller;
        private List<Coroutine> currentArmTurns = new List<Coroutine>();
        private Coroutine toolSwinging, legSwinging;
        private IPickupable heldItem;
        private float lastDashTime, lastInteractTime;
        /// <summary>
        /// True if player was moving in the previous update, used to start leg swinging
        /// </summary>
        private bool wasMoving;
        /// <summary>
        /// True if player is moving in the current update, used to keep legs swinging
        /// </summary>
        private bool isMoving;

        public Vector3Int LookPoint => Vector3Int.RoundToInt(transform.position + transform.forward + Vector3.up);
        public int InteractMask => interactMask;

        void Start()
        {
            controller = GetComponent<CharacterController>();

            lastDashTime = -dashDuration;
        }

        void Update()
        {
            HandleMovement();

            // Interact Input
            if (Input.GetMouseButton(0) && Time.time >= lastInteractTime + interactInterval) TryInteract();
        }

        public static bool PointIsInBounds(Vector3 point)
        {
            var mask = LayerMask.GetMask("Ground");
            return Physics.Raycast(point + Vector3.up, Vector3.down, 3, mask);
        }

        #region Movement
        private void HandleMovement()
        {
            // Movement Input
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

            if (!UpdateMovingStates(input)) return;

            if (input != Vector3.zero) transform.forward = input;
            var deltaPos = moveSpeed * transform.forward * Time.deltaTime;
            
            /// Updates <see cref="lastDashTime"/> if dash key was pressed
            if (Input.GetKeyDown(KeyCode.LeftShift)) lastDashTime = Time.time;
            /// Multiplies <see cref="deltaPos"/> if dashing
            if (Time.time < lastDashTime + dashDuration) deltaPos *= DashMultiplier();
            
            // Moves player
            if (PointIsInBounds(transform.position + deltaPos)) controller.Move(deltaPos);
        }

        /// <summary>
        /// Updates <see cref="isMoving"/> and <see cref="wasMoving"/>
        /// </summary>
        /// <param name="input">Player XZ plane input</param>
        /// <returns>True if player is moving</returns>
        private bool UpdateMovingStates(Vector3 input)
        {
            // Gets current moving state
            isMoving = input != Vector3.zero || Input.GetKeyDown(KeyCode.LeftShift) || Time.time < lastDashTime + dashDuration;
            if (!isMoving) return wasMoving = false;
            
            // Start swinging legs if player wasn't moving last update
            if (!wasMoving)
            {
                if (legSwinging != null) StopCoroutine(legSwinging);
                legSwinging = StartCoroutine(SwingLegs());
            }
            wasMoving = isMoving;

            return true;
        }

        private float DashMultiplier()
        {
            /// Parabola that goes from <see cref="dashSpeedMultiplier"/> to 1 over the <see cref="dashDuration"/> seconds
            return (dashSpeedMultiplier - 1) * (-Mathf.Pow(Mathf.InverseLerp(lastDashTime, lastDashTime + dashDuration, Time.time), 4) + 1) + 1;
        }
        #endregion

        #region Interact
        private void TryInteract()
        {
            lastInteractTime = Time.time;

            if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out RaycastHit hitData, 1, interactMask))
            {
                if (heldItem == null) TryToPickUp(hitData.transform.GetComponent<IPickupable>());
                else TryUseHeldItemOn(hitData.transform.GetComponent<IInteractable>(), hitData);
            }
            else TryDrop();
        }

        /// <summary>
        /// Tries to pick up given IPickupable
        /// </summary>
        /// <returns>True if given item is picked up</returns>
        private bool TryToPickUp(IPickupable pickup)
        {
            if (pickup == null) return false;

            bool bothHands = pickup.IsTwoHanded();
            heldItem = pickup.TryPickUp(bothHands ? pickupHolder : toolHolder, strength);
            if (heldItem != null) RaiseArms(bothHands);

            return heldItem != null;
        }

        /// <summary>
        /// Places heldItem on the ground if it's not null
        /// </summary>
        /// <returns>True if heldItem was placed</returns>
        private bool TryDrop()
        {
            Vector3Int coords = Vector3Int.RoundToInt(transform.position + Vector3.up + transform.forward);
            if (heldItem == null || toolSwinging != null || !PointIsInBounds(coords) || !heldItem.OnTryDrop()) return false;

            (heldItem as Tile).transform.parent = null;
            OnPlacePickup?.Invoke(heldItem, coords);
            LowerArms(heldItem.IsTwoHanded());
            heldItem = null;

            return true;
        }

        /// <summary>
        /// Uses heldItem on the given Tile if not swinging
        /// </summary>
        private void TryUseHeldItemOn(IInteractable interactable, RaycastHit hitInfo)
        {
            if (toolSwinging != null) return;

            if (interactable.TryInteractUsing(heldItem, hitInfo))
            {
                if (heldItem is Tool) toolSwinging = StartCoroutine(SwingTool());
                else
                {
                    LowerArms(heldItem.IsTwoHanded());
                    heldItem = null;
                }
            }
        }
        #endregion

        #region Limb Movement
        /// <summary>
        /// Points armR to the local forwards
        /// </summary>
        /// <param name="both">Makes armL point forwards too</param>
        private void RaiseArms(bool both)
        {
            _ = TryStopTurnArmRoutines();
            currentArmTurns.Add(StartCoroutine(TurnArm(armR, -90, armTurnSpeed)));
            if (both) currentArmTurns.Add(StartCoroutine(TurnArm(armL, -90, armTurnSpeed)));
        }

        /// <summary>
        /// Points armR to the local downwards
        /// </summary>
        /// <param name="both">Makes armL point downwards too</param>
        private void LowerArms(bool both)
        {
            _ = TryStopTurnArmRoutines();
            currentArmTurns.Add(StartCoroutine(TurnArm(armR, 0, armTurnSpeed)));
            if (both) currentArmTurns.Add(StartCoroutine(TurnArm(armL, 0, armTurnSpeed)));
        }

        /// <summary>
        /// Rotates armR down, waits 0.1 seconds, then back up
        /// </summary>
        private IEnumerator SwingTool()
        {
            currentArmTurns.Add(StartCoroutine(TurnArm(armR, 0, armSwingSpeed)));
            yield return currentArmTurns[currentArmTurns.Count - 1];
            yield return new WaitForSeconds(0.05f);
            currentArmTurns.Add(StartCoroutine(TurnArm(armR, -90, armTurnSpeed)));
            yield return currentArmTurns[currentArmTurns.Count - 1];

            toolSwinging = null;
        }

        /// <summary>
        /// Animates arm turning around it's local x
        /// </summary>
        /// <param name="arm">Selected arm</param>
        /// <param name="rotation">Final x value on arm.eulerAngles.x</param>
        /// <param name="speed">Speed of rotation in degrees per second</param>
        private IEnumerator TurnArm(Transform arm, float rotation, float speed)
        {
            Quaternion from = arm.localRotation, to = from * Quaternion.Euler(rotation - from.eulerAngles.x, 0, 0);
            float animSpeed = speed / Quaternion.Angle(from, to);
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
        private bool TryStopTurnArmRoutines()
        {
            if (currentArmTurns.Count == 0) return false;

            foreach (var c in currentArmTurns) StopCoroutine(c);
            currentArmTurns.Clear();

            return true;
        }

        private IEnumerator SwingLegs()
        {
            float time = 0;

            // Swing legs back and forth
            while (isMoving)
            {
                float angle = legRaiseAngle * Mathf.Sin(time);

                legL.localRotation = Quaternion.Euler(angle, 0, 0);
                legR.localRotation = Quaternion.Euler(-angle, 0, 0);
                if (angle < 0) // Left leg forward
                {
                    calfL.localRotation = Quaternion.Inverse(legL.localRotation);
                    calfR.localRotation = legR.localRotation;
                }
                else if (angle > 0) // Right leg forward
                {
                    calfL.localRotation = legL.localRotation;
                    calfR.localRotation = Quaternion.Inverse(legR.localRotation);
                }

                time += legSwingCoefficient * moveSpeed * legRaiseAngle * Time.deltaTime;
                yield return null;
            }

            // Return to base position
            while (legL.localRotation != Quaternion.identity || legR.localRotation != Quaternion.identity)
            {
                float angle = Quaternion.Angle(legL.localRotation, Quaternion.identity);
                float maxRadians = moveSpeed * legRaiseAngle * Mathf.Sqrt(angle) * Time.deltaTime;
                legL.localRotation = Quaternion.RotateTowards(legL.localRotation, Quaternion.identity, maxRadians);
                legR.localRotation = Quaternion.RotateTowards(legR.localRotation, Quaternion.identity, maxRadians);
                calfL.localRotation = Quaternion.RotateTowards(calfL.localRotation, Quaternion.identity, maxRadians);
                calfR.localRotation = Quaternion.RotateTowards(calfR.localRotation, Quaternion.identity, maxRadians);

                yield return null;
            }
        }
        #endregion
    }
}