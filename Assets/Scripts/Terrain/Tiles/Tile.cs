using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Overrailed.Terrain.Tiles
{
    [SelectionBase]
    public class Tile : MonoBehaviour
    {
        [SerializeField] private Transform meshParent;
        [SerializeField] private ParticleSystem breakParticlePrefab;
        [SerializeField] private bool rotateOnSpawn;

        private Gradient meshColorGradient;

        public Transform MeshParent => meshParent;
        /// <summary>
        /// Rounded <see cref="Transform.position"/>
        /// </summary>
        public Vector3Int Coords => Vector3Int.RoundToInt(transform.position);
        public bool RotateOnSpawn => rotateOnSpawn;

        protected virtual void Start()
        {
            meshColorGradient = GetMeshGradient();
        }

        public void SetVisible(bool isVisible) => meshParent.gameObject.SetActive(isVisible);

        private Gradient GetMeshGradient()
        {
            var mats = new List<Material>();
            var gradientPins = new List<GradientColorKey>();

            foreach (var renderer in meshParent.GetComponentsInChildren<MeshRenderer>())
            {
                mats.Add(renderer.material);
                if (mats.Count == 8) break;
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
        
        protected void BreakIntoParticles(Vector3 position)
        {
            var particles = Instantiate(breakParticlePrefab, position, breakParticlePrefab.transform.rotation);
            Destroy(particles.gameObject, particles.main.startLifetime.constant);

            var settings = particles.main;
            var colors = new ParticleSystem.MinMaxGradient(meshColorGradient);
            colors.mode = ParticleSystemGradientMode.RandomColor;
            settings.startColor = colors;
        }
    }
}