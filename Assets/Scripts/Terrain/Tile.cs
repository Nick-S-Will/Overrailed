using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncooked.Terrain
{
    public class Tile : MonoBehaviour
    {
        [SerializeField] protected Transform liquid;
        [SerializeField] private float waveHeight = 0;

        public Transform Liquid => liquid;

        protected virtual void Start()
        {
            if (liquid != null) StartCoroutine(AnimateLiquid());
        }

        public virtual void OnDrop(Vector3Int position) { }

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
    }
}