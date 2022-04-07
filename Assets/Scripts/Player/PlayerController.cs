using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Managers;
using Overrailed.Mob;

namespace Overrailed.Player
{
    public class PlayerController : HumanoidControls
    {
        [Space]
        [SerializeField] private AudioClip dashSound;

        private PlayerInput playerInput;

        protected static List<PlayerController> players = new List<PlayerController>();

        private void Awake()
        {
            playerInput = new PlayerInput();

            playerInput.Movement.Walk.performed += ctx => InputDir = ctx.ReadValue<Vector2>();
            playerInput.Movement.Walk.canceled += ctx => InputDir = Vector2.zero;
            playerInput.Movement.Dash.started += ctx => AudioManager.instance.PlaySound(dashSound, transform.position);
            playerInput.Movement.Dash.started += ctx => HoldingDashKey = true;
            playerInput.Movement.Dash.canceled += ctx => HoldingDashKey = false;

            playerInput.Interaction.InteractMain.performed += ctx => MainInteract();
            playerInput.Interaction.InteractAlt.performed += ctx => AltInteract();

            players.Add(this);
        }

        private void OnEnable() => playerInput.Enable();

        protected override void Start()
        {
            GameManager.instance.OnCheckpoint += ForceDrop;
            GameManager.instance.OnGameEnd += playerInput.Disable;
            GameManager.instance.OnGameEnd += ForceDrop;

            base.Start();
        }

        void Update()
        {
            // Tile highlighting
            var tile = map.GetTileAt(LookPoint);
            if (tile == null) tile = map.GetTileAt(LookPoint + Vector3Int.down);
            map.TryHighlightTile(tile);
        }

        public void EnableControls() => SetControls(true);
        public void DisableControls() => SetControls(false);
        private void SetControls(bool enabled)
        {
            StopMovement();
            this.enabled = false;
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

        private void OnDisable() => playerInput.Disable();

        private void OnDestroy()
        {
            if (GameManager.instance)
            {
                GameManager.instance.OnCheckpoint -= ForceDrop;
                GameManager.instance.OnGameEnd -= playerInput.Disable;
                GameManager.instance.OnGameEnd -= ForceDrop;
            }

            players.Remove(this);
        }
    }
}