using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Managers;

namespace Overrailed.Terrain.Tools
{
    public class Bucket : Tool
    {
        [Space]
        [SerializeField] private Transform liquid;

        public bool IsFull
        {
            get => liquid.gameObject.activeSelf;
            set => liquid.gameObject.SetActive(value);
        }

        override protected void Start()
        {
            liquid.gameObject.SetActive(IsFull);
        }

        public void Refill()
        {
            AudioManager.instance.PlaySound(InteractSound, transform.position);
            IsFull = true;
        }
    }
}