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
        [SerializeField] private bool IsUnmineable;

        public bool TryInteractUsing(IPickupable item, RaycastHit hitInfo)
        {
            if (IsUnmineable) return false;

            if (item is BreakTool breaker && name.Contains(breaker.BreakTileCode)) TakeHit(breaker.Tier, hitInfo);
            else return false;

            return true;
        }

        /// <summary>
        /// Replaces this with it's lowerTier up to given damage number of times
        /// </summary>
        /// <param name="damage">Max amount of times Tile toSpawn will get its lowerTier</param>
        /// <param name="hit">Info about the Raycast used to find this</param>
        public void TakeHit(int damage, RaycastHit hit)
        {
            Tile toSpawn = this;

            for (int i = 0; i < damage; i++)
            {
                if (toSpawn is BreakableTile t)
                {
                    BreakIntoParticles(breakParticlePrefab, toSpawn.GetMeshColors(toSpawn.transform), hit.transform.position);
                    
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