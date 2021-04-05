using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Terrain;

namespace Uncooked.Player
{
    public class PlayerController : MonoBehaviour
    {
        public System.Action<bool> OnPickUp, OnDrop; // True for Pickups false for Tools

        [SerializeField] private float moveSpeed = 5, turnSpeed = 420, armTurnSpeed = 180, armSwingSpeed = 360;
        [SerializeField] private int strength = 1;
        [SerializeField] private MapManager map;
        [SerializeField] private LayerMask interactMask;

        [Header("Transforms")] [SerializeField] private Transform armL;
        [SerializeField] private Transform armR, toolHolder, pickupHolder;

        private Rigidbody rb;
        private Tile heldObject;
        private bool isSwinging;

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
                if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out hit, 1, interactMask))
                {
                    var obj = hit.collider.gameObject.GetComponent<Tile>();

                    if (heldObject == null) TryPickup(obj);
                    else UseItem(obj, hit);
                }
                else TryDrop();
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

        #region Interact
        private void TryPickup(Tile tile)
        {
            if (tile is IPickupable pickup)
            {
                bool isPickup = pickup is PickupTile;

                OnPickUp?.Invoke(isPickup);
                heldObject = pickup.PickUp(isPickup ? pickupHolder : toolHolder, strength);
            }
        }

        private void TryDrop()
        {
            if (heldObject == null) return;

            map.PlaceTile(heldObject, transform.position + Vector3.up + transform.forward);
            OnDrop?.Invoke(heldObject is PickupTile);
            heldObject = null;
        }

        private void UseItem(Tile item, RaycastHit data)
        {
            if (!isSwinging && heldObject is Tool tool)
            {
                if (tool.InteractWith(item, data)) StartCoroutine(SwingTool());
            }
            else if (heldObject is PickupTile pickup && item is PickupTile stack)
            {
                if (pickup.TryStackOn(stack))
                {
                    OnDrop?.Invoke(true);
                    heldObject = null;
                }
            }
        }
        #endregion

        #region Arm Movement
        private void RaiseArms(bool both)
        {
            StopAllCoroutines();
            StartCoroutine(TurnArm(armR, -90, armTurnSpeed));
            if (both) StartCoroutine(TurnArm(armL, -90, armTurnSpeed));
        }

        private void LowerArms(bool both)
        {
            StopAllCoroutines();
            StartCoroutine(TurnArm(armR, 0, armTurnSpeed));
            if (both) StartCoroutine(TurnArm(armL, 0, armTurnSpeed));
        }

        private IEnumerator SwingTool()
        {
            isSwinging = true;

            yield return StartCoroutine(TurnArm(armR, 0, armSwingSpeed));
            yield return new WaitForSeconds(0.1f);
            yield return StartCoroutine(TurnArm(armR, -90, armTurnSpeed));

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
        #endregion
    }
}