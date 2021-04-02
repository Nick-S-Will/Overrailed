using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unrailed.Terrain
{
    public class BreakableTile : Tile, IDamageable
    {
        public Tile lowerTier;
        public ParticleSystem breakParticles;

        public void TakeHit(int damage, Vector3 point)
        {
            Tile toSpawn = this;
            Material mat = GetComponent<Renderer>().material;

            for (int i = 0; i < damage; i++)
            {
                if (toSpawn is BreakableTile t)
                {
                    toSpawn = t.lowerTier;

                    var p = Instantiate(breakParticles, point, Quaternion.identity);
                    p.GetComponent<ParticleSystemRenderer>().material = mat;
                }
                else break;
            }

            if (toSpawn == this) return;

            Instantiate(toSpawn, transform.position, transform.rotation, transform.parent);
            Destroy(gameObject);
        }
    }
}