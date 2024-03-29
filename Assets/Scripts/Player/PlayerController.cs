﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Managers;
using Overrailed.Managers.Audio;
using Overrailed.Mob;

namespace Overrailed.Player
{
    [SelectionBase]
    public class PlayerController : HumanoidController
    {
        private PlayerInput playerInput;

        protected static List<PlayerController> players = new List<PlayerController>();

        private void Awake()
        {
            playerInput = new PlayerInput();

            playerInput.Movement.Walk.performed += ctx => InputDir = ctx.ReadValue<Vector2>();
            playerInput.Movement.Walk.canceled += _ => InputDir = Vector2.zero;
            playerInput.Movement.Dash.started += _ => StartCoroutine(AudioManager.PlaySound(dashSound, transform.position));
            playerInput.Movement.Dash.started += _ => LastDashDownTime = Time.time;

            playerInput.Interaction.InteractMain.performed += _ => InteractAll();
            playerInput.Interaction.InteractAlt.performed += _ => InteractSingle();

            players.Add(this);
        }

        protected override void Start()
        {
            base.Start();

            if (Manager.instance is GameManager gm)
            {
                gm.OnCheckpoint += ForceDrop;
                gm.OnEndCheckpoint += DisableControls;
                Map.OnFinishAnimateChunk += EnableControls;

                gm.OnGameEnd += DisableControls;
                gm.OnGameEnd += ForceDrop;
            }
            else if (Manager.instance is TutorialManager tm)
            {
                tm.OnShowInfo += DisableControls;
                tm.OnCloseInfo += EnableControls;
            }
            else if (!Manager.Exists) Debug.LogError("No Manager Found");

            if (Map.HighlightEnabled) _ = StartCoroutine(TileHighlighting());
        }

        private IEnumerator TileHighlighting()
        {
            while (this && Map)
            {
                _ = InteractRaycast(out RaycastHit hitInfo);
                var tile = hitInfo.transform;
                if (tile == null) tile = Map.GetTileAt(LookPoint + Vector3Int.down);
                Map.TryHighlightTile(tile);

                yield return null;
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

        public static void SetAllControls(bool enabled)
        {
            if (enabled) foreach (var player in players) player.EnableControls();
            else foreach (var player in players) player.DisableControls();
        }

        private void SetControls(Action setEnable, Action moveHandling)
        {
            if (playerInput != null)
            {
                setEnable();
                moveHandling();
            }
        }

        public void EnableControls()
        {
            if (enabled)
            {
                if (movementHandling == null) SetControls(() => playerInput.Enable(), () => HandleCharacter());
            }
            else enabled = true;
        }
        public void DisableControls()
        {
            if (enabled) enabled = false;
            else if (movementHandling != null) SetControls(() => playerInput.Disable(), () => StopMovement());
        }

        private void OnEnable() => EnableControls();
        private void OnDisable() => DisableControls();

        private void OnDestroy()
        {
            if (Manager.instance is GameManager gm)
            {
                gm.OnCheckpoint -= ForceDrop;
                gm.OnGameEnd -= playerInput.Disable;
                gm.OnGameEnd -= ForceDrop;
            }
            else if (Manager.instance is TutorialManager tm)
            {
                tm.OnShowInfo -= DisableControls;
                tm.OnCloseInfo -= EnableControls;
            }

            if (playerInput != null) playerInput.Disable();
            players.Remove(this);
        }
    }
}