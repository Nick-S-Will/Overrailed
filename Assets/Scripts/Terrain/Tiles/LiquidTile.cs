using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Terrain.Tools;

namespace Uncooked.Terrain.Tiles
{
    [RequireComponent(typeof(BoxCollider))]
    public class LiquidTile : Tile, IInteractable
    {
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
            }

            liquid.localScale = Vector3.one;
        }

        public virtual bool TryInteractUsing(IPickupable item, RaycastHit hitInfo)
        {
            if (item is StackTile stack && stack.Bridge) stack.BuildBridge(this);
            else if (item is Rod rod) rod.Use(this);
            else if (item is Bucket bucket) bucket.isFull = true;
            else return false;

            return true;
        }
    }
}