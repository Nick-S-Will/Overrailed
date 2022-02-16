using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Managers;

namespace Uncooked.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : CharacterControls
    {
        [Header("Controls")]
        [SerializeField] private KeyCode forwardKey = KeyCode.W;
        [SerializeField] private KeyCode backKey = KeyCode.S, leftKey = KeyCode.A, rightKey = KeyCode.D, dashKey = KeyCode.LeftShift;

        public static List<PlayerController> players = new List<PlayerController>();

        protected override void Start()
        {
            players.Add(this);
            GameManager.instance.OnCheckpoint += ForceDrop;

            base.Start();
        }

        void Update()
        {
            HandleMovement(Input.GetKey(leftKey), Input.GetKey(rightKey), Input.GetKey(forwardKey), Input.GetKey(backKey), Input.GetKey(dashKey));

            // Interact Input
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1)) TryInteract(Input.GetMouseButton(1));

            // Tile highlighting
            var tile = map.GetTileAt(LookPoint);
            if (tile == null) tile = map.GetTileAt(LookPoint + Vector3Int.down);
            map.TryHighlightTile(tile);
        }

        public static void EnableControls() => SetControls(true);
        public static void DisableControls() => SetControls(false);
        private static void SetControls(bool enabled)
        {
            foreach (var player in players)
            {
                player.StopMovement();
                player.enabled = false;
            }
        }

        public static float MinDistanceToPlayer(Vector3 point)
        {
            float minDst = float.MaxValue;

            foreach (var player in players)
            {
                float dst = Vector3.Distance(point, player.transform.position);
                if (dst < minDst) minDst = dst; 
            }

            return minDst;
        }

        private void OnDestroy()
        {
            if (GameManager.instance) GameManager.instance.OnCheckpoint -= ForceDrop;

            players.Remove(this);
        }
    }
}