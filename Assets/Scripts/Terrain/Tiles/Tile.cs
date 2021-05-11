using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncooked.Terrain.Tiles
{
    [RequireComponent(typeof(BoxCollider))]
    public class Tile : MonoBehaviour
    {
        public Gradient GetMeshColors(Transform meshParent)
        {
            var mats = new List<Material>();
            var gradientPins = new List<GradientColorKey>();

            foreach (Transform t in meshParent)
            {
                var renderer = t.GetComponent<MeshRenderer>();
                if (renderer)
                {
                    mats.Add(renderer.material);
                    if (mats.Count == 8) break;
                }
            }
            for (int i = 0; i < mats.Count; i++)
            {
                gradientPins.Add(new GradientColorKey(mats[i].color, (i + 1f) / mats.Count));
            }

            var meshColors = new Gradient();
            meshColors.mode = GradientMode.Fixed;
            meshColors.colorKeys = gradientPins.ToArray();

            return meshColors;
        }

        protected void BreakIntoParticles(ParticleSystem prefab, Gradient meshColors, Vector3 position)
        {
            var particles = Instantiate(prefab, position, prefab.transform.rotation);
            Destroy(particles.gameObject, prefab.main.startLifetime.constant);

            var settings = particles.main;
            var colors = new ParticleSystem.MinMaxGradient(meshColors);
            colors.mode = ParticleSystemGradientMode.RandomColor;
            settings.startColor = colors;
        }
    }
}