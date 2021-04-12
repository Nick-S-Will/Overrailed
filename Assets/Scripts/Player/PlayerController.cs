using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Terrain;
using Uncooked.Train;

namespace Uncooked.Player
{
    public class PlayerController : MonoBehaviour
    {
        public System.Action<bool> OnPickUp, OnDrop; // True for Pickups false for Tools

        [SerializeField] private float moveSpeed = 5, armTurnSpeed = 180, armSwingSpeed = 360;
        [SerializeField] private int strength = 1;
        [SerializeField] private Terrain.Generation.MapManager map;
        [SerializeField] private LayerMask interactMask;
        [SerializeField] private Color highlightColor = Color.white;

        [Header("Transforms")] [SerializeField] private Transform armL;
        [SerializeField] private Transform armR, toolHolder, pickupHolder;

        private Rigidbody rb;
        private Transform lastHit;
        private List<Coroutine> currentArmTurns = new List<Coroutine>();
        private IPickupable heldItem;
        private bool isSwinging;

        void Start()
        {
            OnPickUp += RaiseArms;
            OnDrop += LowerArms;

            rb = GetComponent<Rigidbody>();
            map = FindObjectOfType<Terrain.Generation.MapManager>();
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
                    if (heldItem == null)
                    {
                        if (TryPickup(hitData.transform.GetComponent<IPickupable>())) hit = false;
                    }
                    else
                    {
                        var interact = hitData.transform.GetComponent<IInteractable>();
                        if (interact != null) UseItemOn(interact, hitData);
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
                    StartCoroutine(HightlightObject(hitData.transform));
                }
            }
            else lastHit = null;
        }

        void FixedUpdate()
        {
            // Movement Input
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

            rb.velocity = moveSpeed * input;

            if (input != Vector3.zero) transform.forward = input;
        }

        /// <summary>
        /// Tints selected's childrens' meshes by highlightColor until new object is selected
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
        private bool TryPickup(IPickupable pickup)
        {
            if (pickup == null) return false;

            bool bothHands = pickup.IsTwoHanded();

            heldItem = pickup.TryPickUp(bothHands ? pickupHolder : toolHolder, strength);
            if (heldItem != null) OnPickUp?.Invoke(bothHands);

            return heldItem != null;
        }

        /// <summary>
        /// Places heldItem on the ground if it's not null
        /// </summary>
        /// <returns>True if heldItem was placed</returns>
        private bool TryDrop()
        {
            if (heldItem == null) return false;

            Vector3Int coords = Vector3Int.RoundToInt(transform.position + Vector3.up + transform.forward);
            map.PlacePickup(heldItem, coords);
            OnDrop?.Invoke(heldItem.IsTwoHanded());
            heldItem = null;

            return true;
        }

        /// <summary>
        /// Uses heldItem on the given Tile
        /// </summary>
        private void UseItemOn(IInteractable interactable, RaycastHit hitInfo)
        {
            // TODO: Fix tool stops working after a few pickups or uses?
            if (heldItem is Tool)
            {
                //print("tool");
                if (!isSwinging && interactable.TryInteractUsing(heldItem, hitInfo)) StartCoroutine(SwingTool());
            }
            else if (interactable.TryInteractUsing(heldItem, hitInfo))
            {
                //print("other");
                OnDrop?.Invoke(true);
                heldItem = null;
            }
        }
        #endregion

        #region Arm Movement
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
        #endregion
    }
}