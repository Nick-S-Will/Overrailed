using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Terrain.Tools;

namespace Uncooked.Terrain.Tiles
{
    public class BreakableTile : Tile, IDamageable, IInteractable
    {
        [SerializeField] private Tile lowerTier;
        [SerializeField] private ParticleSystem breakParticlePrefab;

        protected override void Start() => base.Start();
        
        public Interaction TryInteractUsing(IPickupable item)
        {
            if (item is BreakTool breaker && name.Contains(breaker.BreakTileCode)) TakeHit(breaker.Tier);
            else return Interaction.None;

            return Interaction.Interacted;
        }

        /// <summary>
        /// Replaces this with it's lowerTier up to given damage number of times
        /// </summary>
        /// <param name="damage">Max amount of times Tile toSpawn will get its lowerTier</param>
        /// <param name="hit">Info about the Raycast used to find this</param>
        public void TakeHit(int damage)
        {
            Tile toSpawn = this;

            for (int i = 0; i < damage; i++)
            {
                if (toSpawn is BreakableTile t)
                {
                    BreakIntoParticles(breakParticlePrefab, toSpawn.MeshColorGradient, transform.position);
                    
                    toSpawn = t.lowerTier;
                }
                else break;
            }

            if (toSpawn == this) return;

            Instantiate(toSpawn, transform.position, transform.rotation, transform.parent);
            Destroy(gameObject);
        }
    }
}