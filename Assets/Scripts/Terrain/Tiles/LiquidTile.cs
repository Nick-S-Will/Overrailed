using System.Collections;
using System.Collections.Generic;
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
            yield return new WaitForSeconds(0.2f * transform.position.x + 0.3f * transform.position.z);
            float time = 0;

            while (liquid)
            {
                if (liquid.gameObject.activeSelf)
                {
                    liquid.localScale = new Vector3(1, Mathf.Lerp(1, 1 - waveHeight, Mathf.PingPong(time, 1)), 1);
                    time += Time.deltaTime;
                }
                yield return null;
                yield return new WaitUntil(() => GameManager.IsPlaying() || TutorialManager.Exists);
            }

            liquid.localScale = Vector3.one;
        }

        public virtual Interaction TryInteractUsing(IPickupable item)
        {
            if (item is StackTile stack && stack.HasBridge)
            {
                stack.BuildBridge(this);
                OnBridge?.Invoke();
                return stack.GetStackCount() == 1 ? Interaction.Used : Interaction.Interacted;
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