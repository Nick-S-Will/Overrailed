using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unrailed.Terrain
{
    public class BreakableTile : Tile, IDamageable
    {
        public Tile lowerTier;
        public ParticleSystem breakParticles;

        private Gradient meshColors;

        protected override void Start()
        {
            base.Start();

            var mats = new List<Material>();
            var gradientPins = new List<GradientColorKey>();

            foreach (Transform t in transform)
            {
                var renderer = t.GetComponent<MeshRenderer>();
                if (renderer != null) mats.Add(renderer.material);
            }
            for (int i = 0; i < mats.Count; i++)
            {
                gradientPins.Add(new GradientColorKey(mats[i].color, (i + 1f) / mats.Count));
            }

            meshColors = new Gradient();
            meshColors.mode = GradientMode.Fixed;
            meshColors.colorKeys = gradientPins.ToArray();
        }

        public void TakeHit(int damage, RaycastHit hit)
        {
            Tile toSpawn = this;

            for (int i = 0; i < damage; i++)
            {
                if (toSpawn is BreakableTile t)
                {
                    toSpawn = t.lowerTier;

                    Vector3 particleSpawn;
                    if (toSpawn is BreakableTile) particleSpawn = hit.point;
                    else particleSpawn = hit.collider.transform.position;
                    var p = Instantiate(breakParticles, particleSpawn, breakParticles.transform.rotation);
                    Destroy(p.gameObject, breakParticles.main.startLifetime.constant);

                    var settings = p.main;
                    var colors = new ParticleSystem.MinMaxGradient(meshColors);
                    colors.mode = ParticleSystemGradientMode.RandomColor;
                    settings.startColor = colors;
                }
                else break;
            }

            if (toSpawn == this) return;

            Instantiate(toSpawn, transform.position, transform.rotation, transform.parent);
            Destroy(gameObject);
        }
    }
}