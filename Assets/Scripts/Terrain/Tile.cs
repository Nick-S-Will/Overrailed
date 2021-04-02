using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unrailed.Terrain
{
    public class Tile : MonoBehaviour
    {
        public Transform liquid;

        protected virtual void Start()
        {
            if (liquid != null) StartCoroutine(AnimateLiquid());
        }

        protected IEnumerator AnimateLiquid()
        {
            float min = 0.85f;

            yield return new WaitForSeconds(transform.position.x);

            while (liquid)
            {
                liquid.localScale = new Vector3(1, Mathf.Lerp(1, min, Mathf.PingPong(Time.time, 1)), 1);
                yield return null;
            }

            liquid.localScale = Vector3.one;
        }
    }
}