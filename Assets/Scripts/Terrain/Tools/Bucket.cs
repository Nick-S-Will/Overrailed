using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncooked.Terrain.Tools
{
    public class Bucket : Tool
    {
        [Space]
        [SerializeField] private Transform liquid;

        public bool isFull
        {
            get => liquid.gameObject.activeSelf;
            set => liquid.gameObject.SetActive(value);
        }

        override protected void Start()
        {
            liquid.gameObject.SetActive(isFull);
        }

        public bool TryUse()
        {
            if (isFull)
            {
                isFull = false;
                return true;
            }
            else return false;
        }
    }
}