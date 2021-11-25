using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Terrain.Tools;

namespace Uncooked.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public event System.Action<IPickupable, Vector3Int> OnPlacePickup;

        [SerializeField] private float moveSpeed = 5, armTurnSpeed = 180, armSwingSpeed = 360;
        [SerializeField] private int strength = 1;
        [SerializeField] private LayerMask interactMask;
        [SerializeField] private Color highlightColor = Color.white;

        [Header("Transforms")] [SerializeField] private Transform armL;
        [SerializeField] private Transform armR, legL, legR, toolHolder, pickupHolder;

        private CharacterController controller;
        private Transform lastHit;
        private List<Coroutine> currentArmTurns = new List<Coroutine>();
        private Coroutine legSwinging;
        private IPickupable heldItem;
        private bool isSwinging, wasMoving, isMoving;

        void Start()
        {
            controller = GetComponent<CharacterController>();
        }

        void Update()
        {
            RaycastHit hitData;
            bool hit = Physics.Raycast(transform.position + Vector3.up, transform.forward, out hitData, 1, interactMask);

            // Interact Input
            if (Input.GetMouseButtonDown(0))
            {
                if (hit)
                {
                    if (heldItem == null) hit = !TryToPickUp(hitData.transform.GetComponent<IPickupable>());
                    else
                    {
                        var interact = hitData.transform.GetComponent<IInteractable>();
                        if (interact != null) TryUseHeldItemOn(interact, hitData);
                    }
                }
                else TryDrop();
            }

            // Highlight
            if (hit)
            {
                if (hitData.transform != lastHit)
                {
                    lastHit = hitData.transform;
                    _ = StartCoroutine(HightlightObject(hitData.transform));
                }
            }
            else lastHit = null;
        }

        void FixedUpdate()
        {
            // Movement Input
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

            isMoving = input != Vector3.zero;
            if (!isMoving)
            {
                wasMoving = false;
                return;
            }

            // Start swinging legs if player wasn't moving before
            if (!wasMoving)
            {
                if (legSwinging != null) StopCoroutine(legSwinging);
                legSwinging = StartCoroutine(SwingLegs());
            }
            wasMoving = isMoving;

            transform.forward = input;

            var deltaPos = moveSpeed * input * Time.fixedDeltaTime;
            var mask = LayerMask.GetMask("Ground");
            if (Physics.OverlapSphere(transform.position + deltaPos, moveSpeed * Time.fixedDeltaTime, mask).Length > 0) 
                controller.Move(deltaPos);
        }

        /// <summary>
        /// Tints selected's children's meshes by highlightColor until new object is selected
        /// </summary>
        private IEnumerator HightlightObject(Transform selected)
        {
            var renderers = selected.GetComponentsInChildren<MeshRenderer>();
            var originalColors = new Color[renderers.Length];

            // Tint mesh colors
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].material.color;
                renderers[i].material.color = 0.5f * (originalColors[i] + highlightColor);
            }

            yield return new WaitWhile(() => selected == lastHit);

            // Reset colors
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) break;
                else renderers[i].material.color = originalColors[i];
            }
        }

        #region Interact
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
            if (heldItem == null || isSwinging || !heldItem.OnTryDrop()) return false;

            Vector3Int coords = Vector3Int.RoundToInt(transform.position + Vector3.up + transform.forward);
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
                else if (interactable.TryInteractUsing(heldItem, hitInfo))
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
            TryStopTurnArmRoutines();
            currentArmTurns.Add(StartCoroutine(TurnArm(armR, -90, armTurnSpeed)));
            if (both) currentArmTurns.Add(StartCoroutine(TurnArm(armL, -90, armTurnSpeed)));
        }

        /// <summary>
        /// Points armR to the local downwards
        /// </summary>
        /// <param name="both">Makes armL point downwards too</param>
        private void LowerArms(bool both)
        {
            TryStopTurnArmRoutines();
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
            int time = 0;

            // Swing legs back and forth
            while (isMoving)
            {
                float angle = Mathf.PingPong(time, 90) - 45;

                legL.localRotation = Quaternion.Euler(angle, 0, 0);
                legR.localRotation = Quaternion.Euler(-angle, 0, 0);

                time += Mathf.RoundToInt(moveSpeed);
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