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

        [SerializeField] private float moveSpeed = 5, legTurnMultiplier = 0.1f, legRaiseAngle = 45;
        [Space]
        [SerializeField] private float armTurnSpeed = 180;
        [SerializeField] private float armSwingSpeed = 360;
        [Space]
        [SerializeField] private float interactInterval = 0.5f;
        [SerializeField] private int strength = 1;
        [SerializeField] private LayerMask interactMask;

        [Header("Transforms")] [SerializeField] private Transform armL;
        [SerializeField] private Transform armR, legL, legR, toolHolder, pickupHolder;

        private CharacterController controller;
        private List<Coroutine> currentArmTurns = new List<Coroutine>();
        private Coroutine legSwinging;
        private IPickupable heldItem;
        private float lastInteractTime;
        private bool isSwinging, wasMoving, isMoving;

        public Vector3Int LookPoint => Vector3Int.RoundToInt(transform.position + transform.forward + Vector3.up);
        public int InteractMask => interactMask;

        void Start()
        {
            controller = GetComponent<CharacterController>();
        }

        void Update()
        {
            // Interact Input
            if (Input.GetMouseButton(0) && Time.time >= lastInteractTime + interactInterval) TryInteract();
        }

        void FixedUpdate()
        {
            // Movement Input
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

            // Updates moving states
            isMoving = input != Vector3.zero;
            if (!isMoving)
            {
                wasMoving = false;
                return;
            }

            // Start swinging legs if player wasn't moving last update
            if (!wasMoving)
            {
                if (legSwinging != null) StopCoroutine(legSwinging);
                legSwinging = StartCoroutine(SwingLegs());
            }
            wasMoving = isMoving;

            transform.forward = input;

            // Moves player
            var deltaPos = moveSpeed * input * Time.fixedDeltaTime;
            if (PointIsInBounds(transform.position + deltaPos))  controller.Move(deltaPos);
        }

        public static bool PointIsInBounds(Vector3 point)
        {
            var mask = LayerMask.GetMask("Ground");
            return Physics.Raycast(point + Vector3.up, Vector3.down, 3, mask);
        }

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
            if (heldItem == null || isSwinging || !PointIsInBounds(coords) || !heldItem.OnTryDrop()) return false;

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
            if (isSwinging) return;

            if (interactable.TryInteractUsing(heldItem, hitInfo))
            {
                if (heldItem is Tool) _ = StartCoroutine(SwingTool());
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
            isSwinging = true;

            currentArmTurns.Add(StartCoroutine(TurnArm(armR, 0, armSwingSpeed)));
            yield return currentArmTurns[currentArmTurns.Count - 1];
            yield return new WaitForSeconds(0.1f);
            currentArmTurns.Add(StartCoroutine(TurnArm(armR, -90, armTurnSpeed)));
            yield return currentArmTurns[currentArmTurns.Count - 1];

            isSwinging = false;
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

                time += legTurnMultiplier * moveSpeed * legRaiseAngle;
                yield return new WaitForFixedUpdate();
            }

            // Return to base position
            while (legL.localRotation != Quaternion.identity || legR.localRotation != Quaternion.identity)
            {
                float angle = Quaternion.Angle(legL.localRotation, Quaternion.identity);
                float maxRadians = 2 * moveSpeed * Mathf.Sqrt(angle) * Time.deltaTime;
                legL.localRotation = Quaternion.RotateTowards(legL.localRotation, Quaternion.identity, maxRadians);
                legR.localRotation = Quaternion.RotateTowards(legR.localRotation, Quaternion.identity, maxRadians);

                yield return null;
            }
        }
        #endregion
    }
}