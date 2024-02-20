using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Managers.Audio;

namespace Overrailed.Terrain.Tools
{
    public class Bucket : Tool
    {
        public override event System.Action OnPickUp;
        public override event System.Action OnDrop;

        [Space]
        [SerializeField] private Transform liquid;

        public bool IsFull
        {
            get => liquid.gameObject.activeSelf;
            set => liquid.gameObject.SetActive(value);
        }

        new protected virtual void Start()
        {
            liquid.gameObject.SetActive(IsFull);
        }

        public void Refill()
        {
            _ = StartCoroutine(AudioManager.PlaySound(InteractSound, transform.position));
            IsFull = true;
        }
    }
}