using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Overrailed.Terrain.Tiles
{
    public abstract class PickupTile : Tile, IPickupable
    {
        public abstract event System.Action OnPickUp, OnDrop;

        [Space]
        [SerializeField] private AudioClip pickupAudio;
        [SerializeField] private AudioClip dropAudio;

        public AudioClip PickupAudio => pickupAudio;
        public AudioClip DropAudio => dropAudio;
        public virtual bool CanPickUp => true;
        public virtual bool IsTwoHanded => true;

        protected override void Start() => base.Start();

        public virtual IPickupable TryPickUp(Transform parent, int amount) => null;

        public virtual bool OnTryDrop(Vector3Int position) => true;

        public virtual void Drop(Vector3Int position) { }
    }
}