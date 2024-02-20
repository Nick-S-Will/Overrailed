using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Managers.Audio;
using Overrailed.Terrain.Tiles;

namespace Overrailed.Terrain.Tools
{
    public abstract class Tool : PickupTile, IInteractable
    {
        public event System.Action<Tool> OnPickup, OnDropTool;

        [SerializeField] private AudioClip interactSound;
        [Space]
        [SerializeField] private Transform handOffset;
        [SerializeField] private int tier = 1;

        public AudioClip InteractSound => interactSound;
        public int Tier => tier;
        public override bool IsTwoHanded => false;
        public override bool CanPickUp => true;

        public Interaction TryInteractUsing(IPickupable item) => Interaction.None;

        /// <summary>
        /// Picks up this Tool
        /// </summary>
        /// <param name="parent">Transform this will be parented to</param>
        /// <returns>Tool that was picked up</returns>
        public override IPickupable TryPickUp(Transform parent, int amount)
        {
            GetComponent<BoxCollider>().enabled = false;

            parent.localPosition = handOffset.localPosition;
            parent.localRotation = handOffset.localRotation;
            transform.parent = parent;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            _ = StartCoroutine(AudioManager.PlaySound(PickupAudio, transform.position));

            OnPickup?.Invoke(this);
            return this;
        }

        public override bool OnTryDrop(Vector3Int position) => true;

        public override void Drop(Vector3Int position)
        {
            _ = StartCoroutine(AudioManager.PlaySound(DropAudio, transform.position));

            OnDropTool?.Invoke(this);
        }
    }
}