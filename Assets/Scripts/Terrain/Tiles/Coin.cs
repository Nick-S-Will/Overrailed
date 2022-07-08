using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Overrailed.Managers;
using Overrailed.UI.Shop;

namespace Overrailed.Terrain.Tiles
{
    public class Coin : Tile
    {
        [Space]
        [SerializeField] private string collisionTag = "Player";
        [SerializeField] private float bobHeight = 0.15f, bobSpeed = 1;
        [SerializeField] private int spinCycleInterval = 3;

        private Vector3 startLocalPosition;

        protected override void Start()
        {
            base.Start();

            startLocalPosition = MeshParent.localPosition;
            AnimateCoin();
        }

        private void Collect()
        {
            var shop = FindObjectOfType<TrainStoreManager>();
            if (shop) shop.Coins += 1;
            else Debug.LogWarning($"{name} couldn't find shop");

            BreakIntoParticles(transform.position);
            Destroy(gameObject);
        }

        /// <summary>
        /// Moves <see cref="pointer"/> up and down over <see cref="pointerTarget"/> and spins it every <see cref="spinCycleInterval"/> cycle
        /// </summary>
        private async void AnimateCoin()
        {
            float time = 0;

            MeshParent.gameObject.SetActive(true);
            while (MeshParent && Application.isPlaying)
            {
                Vector3 bobOffset = bobHeight * Mathf.Sin(bobSpeed * 2 * time * Mathf.PI) * Vector3.up;
                float cycleDuration = 1 / bobSpeed;
                float cycleProgress = time / cycleDuration;
                int cycleCount = Mathf.FloorToInt(cycleProgress);
                bool nthCycle = cycleCount % spinCycleInterval == 0;

                MeshParent.localPosition = startLocalPosition + bobOffset;
                MeshParent.localRotation = nthCycle ? Quaternion.Euler(0, Mathf.Lerp(0, 360, cycleProgress - cycleCount), 0) : Quaternion.identity;

                await Manager.Pause;
                await Task.Yield();
                time += Time.deltaTime;
            }

            if (MeshParent && Application.isPlaying)
            {
                MeshParent.position = startLocalPosition;
                MeshParent.localRotation = Quaternion.identity;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(collisionTag)) Collect();
        }
    }
}