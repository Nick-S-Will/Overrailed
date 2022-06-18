using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Overrailed.Terrain.Tiles
{
    [SelectionBase]
    public class Tile : MonoBehaviour, IPickupable
    {
        public event System.Action OnPickUp, OnDrop;

        [SerializeField] private Transform meshParent;
        [SerializeField] private bool rotateOnSpawn;
        [Space]
        [SerializeField] private AudioClip pickupAudio;
        [SerializeField] private AudioClip dropAudio;

        public Transform MeshParent => meshParent;
        public Gradient MeshColorGradient { get; private set; }
        public AudioClip PickupAudio => pickupAudio;
        public AudioClip DropAudio => dropAudio;
        public Vector3Int Coords => Vector3Int.RoundToInt(transform.position);
        public virtual bool CanPickUp => false;
        public virtual bool IsTwoHanded => true;
        public bool RotateOnSpawn => rotateOnSpawn;

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
        protected void InvokeOnPickUp() => OnPickUp?.Invoke();
        
        public virtual bool OnTryDrop(Vector3Int position) => true;

        public virtual void Drop(Vector3Int position) { }
        protected void InvokeOnDrop() => OnDrop?.Invoke();

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