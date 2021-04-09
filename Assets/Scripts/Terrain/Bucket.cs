using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncooked.Terrain
{
    public class Bucket : Tool
    {
        [Space]
        [SerializeField] private Transform liquid;

        private bool isFull;

        public bool IsFull => isFull;

        protected virtual void Start()
        {
            liquid.gameObject.SetActive(isFull);
        }

        public void ToggleLiquid()
        {
            isFull = !isFull;
            liquid.gameObject.SetActive(isFull);
        }
    }
}