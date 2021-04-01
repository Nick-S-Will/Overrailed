using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unrailed.Terrain;

namespace Unrailed.Player
{
    public class PlayerController : MonoBehaviour
    {
        public System.Action<bool> OnPickUp, OnDrop; // True for Pickups false for Tools

        public float moveSpeed = 5, turnSpeed = 360, armTurnTime = 0.5f;
        public LayerMask interactMask;

        [Header("Transforms")] public Transform toolHolder;
        public Transform pickupHolder, armL, armR;

        private Rigidbody rb;
        private MapManager map;
        private Tile heldObject;

        void Start()
        {
            OnPickUp += RaiseArms;
            OnDrop += LowerArms;

            rb = GetComponent<Rigidbody>();
            map = FindObjectOfType<MapManager>();
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out hit, 1, interactMask, QueryTriggerInteraction.Collide))
                {
                    var tile = hit.collider.gameObject.GetComponent<Tile>();

                    if (heldObject == null)
                    {
                        if (tile is IPickupable pickup)
                        {
                            bool isPickup = pickup is PickupTile;

                            pickup.PickUp(isPickup ? pickupHolder : toolHolder);
                            OnPickUp?.Invoke(isPickup);
                            heldObject = tile;
                        }
                    }
                    else
                    {
                        if (heldObject is Tool tool) tool.InteractWith(tile);
                        else if (heldObject is PickupTile pickup && tile is PickupTile stack) pickup.TryStackOn(stack);
                    }
                }
                else if (heldObject != null)
                {
                    map.PlaceTile(heldObject, transform.position + Vector3.up + transform.forward);
                    OnDrop?.Invoke(heldObject is PickupTile);
                    heldObject = null;
                }
            }
        }

        void FixedUpdate()
        {
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

            if (input != Vector3.zero)
            {
                rb.MovePosition(transform.position + moveSpeed * input * Time.deltaTime);

                transform.forward = Vector3.RotateTowards(transform.forward, input, turnSpeed * Mathf.Deg2Rad * Time.deltaTime, 0);
            }
        }

        #region Arm Movement
        private void RaiseArms(bool both)
        {
            StopAllCoroutines();
            StartCoroutine(TurnArm(armR, -90, armTurnTime));
            if (both) StartCoroutine(TurnArm(armL, -90, armTurnTime));
        }

        private void LowerArms(bool both)
        {
            StopAllCoroutines();
            StartCoroutine(TurnArm(armR, 0, armTurnTime));
            if (both) StartCoroutine(TurnArm(armL, 0, armTurnTime));
        }

        /// <summary>
        /// Animates arm turning around it's local x
        /// </summary>
        /// <param name="arm">Selected arm</param>
        /// <param name="rotation">Final x value on eulerAngles.x</param>
        /// <param name="duration">Length in seconds of the animation</param>
        private IEnumerator TurnArm(Transform arm, float rotation, float duration)
        {
            Quaternion from = arm.localRotation, to = from * Quaternion.Euler(rotation - from.eulerAngles.x, 0, 0);
            float percent = 0, speed = 1 / duration;

            while (percent < 1)
            {
                yield return null;

                percent += speed * Time.deltaTime;
                arm.localRotation = Quaternion.Lerp(from, to, percent);
            }

            arm.localRotation = to;
        }
        #endregion
    }
}