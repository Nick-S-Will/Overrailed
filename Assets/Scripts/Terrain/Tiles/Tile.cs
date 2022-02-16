using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncooked.Terrain.Tiles
{
    public class Tile : MonoBehaviour, IPickupable
    {
        public Transform meshParent;

        public Gradient MeshColorGradient { get; private set; }
        public virtual bool CanPickUp => false;
        public virtual bool IsTwoHanded() => true;
        public virtual bool OnTryDrop() => true;

        protected virtual void Start()
        {
            MeshColorGradient = GetMeshGradient();
        }

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

        public virtual IPickupable TryPickUp(Transform parent, int amount) => null;

        public virtual void Drop(Vector3Int position) { }

        public void SetVisible(bool isVisible) => meshParent.gameObject.SetActive(isVisible);
        
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