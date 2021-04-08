using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Terrain;

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
        private Tile heldObject;
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
                    if (heldObject == null)
                    {
                        if (TryPickup(hitData.transform.GetComponent<Tile>())) hit = false;
                    }
                    else UseItemOn(hitData.transform.GetComponent<Tile>(), hitData);
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

            if (input != Vector3.zero)
            {
                rb.MovePosition(transform.position + moveSpeed * input * Time.deltaTime);
                transform.forward = input;
            }
        }

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
        private bool TryPickup(Tile tile)
        {
            if (tile is IPickupable pickup)
            {
                bool isPickup = pickup is StackTile;

                heldObject = pickup.TryPickUp(isPickup ? pickupHolder : toolHolder, strength);
                if (heldObject != null) OnPickUp?.Invoke(isPickup);

                return heldObject != null;
            }

            return false;
        }

        private bool TryDrop()
        {
            if (heldObject == null) return false;

            Vector3Int coords = Vector3Int.RoundToInt(transform.position + Vector3.up + transform.forward);
            map.PlaceTile(heldObject, coords);
            heldObject.OnDrop(coords);
            OnDrop?.Invoke(heldObject is StackTile);
            heldObject = null;

            return true;
        }

        private void UseItemOn(Tile tile, RaycastHit data)
        {
            if (!isSwinging && heldObject is Tool tool)
            {
                // Use Tool
                if (tool.InteractWith(tile, data)) StartCoroutine(SwingTool());
            }
            else if (heldObject is StackTile pickup)
            {
                // Stack
                if (tile is StackTile stack && pickup.TryStackOn(stack))
                {
                    OnDrop?.Invoke(true);
                    heldObject = null;
                }
                else if (pickup.Bridge != null && tile.Liquid != null)
                {
                    // Build Bridge
                    TryDrop();
                    pickup.BuildBridge(tile);
                }
            }
        }
        #endregion

        #region Arm Movement
        private void RaiseArms(bool both)
        {
            TryStopTurnArmRoutines();
            currentArmTurns.Add(StartCoroutine(TurnArm(armR, -90, armTurnSpeed)));
            if (both) currentArmTurns.Add(StartCoroutine(TurnArm(armL, -90, armTurnSpeed)));
        }

        private void LowerArms(bool both)
        {
            TryStopTurnArmRoutines();
            currentArmTurns.Add(StartCoroutine(TurnArm(armR, 0, armTurnSpeed)));
            if (both) currentArmTurns.Add(StartCoroutine(TurnArm(armL, 0, armTurnSpeed)));
        }

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