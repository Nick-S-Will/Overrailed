using System.Collections;
using UnityEngine;

using Overrailed.Managers;
using Overrailed.Terrain.Tools;

namespace Overrailed.Terrain.Tiles
{
    public class LiquidTile : Tile, IInteractable
    {
        public System.Action OnBridge, OnRefill;

        [SerializeField] protected Transform liquid, surfacePoint;
        [SerializeField] private float waveHeight = 0.2f;

        public Transform Liquid => liquid;
        public Transform SurfacePoint => surfacePoint;

        override protected void Start()
        {
            if (liquid) _ = StartCoroutine(AnimateLiquid());
        }

        /// <summary>
        /// Animates waves in liquid by scaling its local y in [1 - waveHeight, 1]
        /// </summary>
        protected IEnumerator AnimateLiquid()
        {
            var seconds = 0.2f * transform.position.x + 0.3f * transform.position.z;
            yield return Manager.Delay(seconds);
            float time = 0;

            while (this && liquid)
            {
                if (liquid.gameObject.activeSelf)
                {
                    liquid.localScale = new Vector3(1, Mathf.Lerp(1, 1 - waveHeight, Mathf.PingPong(time, 1)), 1);
                    time += Time.deltaTime;
                }
                yield return null;
                yield return Manager.PauseRoutine;
            }
        }

        public virtual Interaction TryInteractUsing(IPickupable item)
        {
            if (item is StackTile stack && stack.HasBridge)
            {
                stack.BuildBridge(this);
                OnBridge?.Invoke();
                return stack.NextInStack == null ? Interaction.Used : Interaction.Interacted;
            }
            else if (item is Bucket bucket)
            {
                bucket.Refill();
                OnRefill?.Invoke();
            }
            else if (item is Rod rod) rod.UseOn(this);
            else return Interaction.None;

            return Interaction.Interacted;
        }
    }
}