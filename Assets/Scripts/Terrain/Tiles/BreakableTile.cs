﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Managers.Audio;
using Overrailed.Terrain.Tools;

namespace Overrailed.Terrain.Tiles
{
    public class BreakableTile : Tile, IDamageable, IInteractable
    {
        public event System.Action<Tile> OnBreak;

        [Space]
        [SerializeField] private Tile lowerTier;
        [SerializeField] private AudioClip breakAudio;

        protected override void Start() => base.Start();
        
        public Interaction TryInteractUsing(IPickupable item)
        {
            if (item is BreakTool breaker && name.Contains(breaker.BreakTileCode)) TakeHit(breaker);
            else return Interaction.None;

            return Interaction.Interacted;
        }

        /// <summary>
        /// Replaces this with it's lowerTier up to given damage number of times
        /// </summary>
        /// <param name="damage">Max amount of times Tile toSpawn will get its lowerTier</param>
        /// <param name="hit">Info about the Raycast used to find this</param>
        public void TakeHit(Tool tool)
        {
            Tile toSpawn = this;

            for (int i = 0; i < tool.Tier; i++)
            {
                if (toSpawn is BreakableTile t)
                {
                    BreakIntoParticles(transform.position);
                    _ = StartCoroutine(AudioManager.PlaySound(tool.InteractSound, transform.position));
                    _ = StartCoroutine(AudioManager.PlaySound(breakAudio, transform.position));
                    toSpawn = t.lowerTier;
                }
                else break;
            }

            if (toSpawn == this)
            {
                Debug.LogError("Tool is tier < 1");
                return;
            }

            if (toSpawn)
            {
                var tile = Instantiate(toSpawn, transform.position, transform.rotation, transform.parent);
                OnBreak?.Invoke(tile);
            }
            Destroy(gameObject);
        }
    }
}