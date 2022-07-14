using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

using Overrailed.Managers.Audio;
using Overrailed.Train;

namespace Overrailed.UI.Shop
{
    public class TrainCarHolder : MonoBehaviour, IInteractable, IPickupable
    {
        [SerializeField] private AudioClip pickupAudio, dropAudio;
        [SerializeField] private TextMeshPro priceText;

        public TrainStoreManager manager { private get; set; }
        private TrainCar heldCar;

        public AudioClip PickupAudio => pickupAudio;
        public AudioClip DropAudio => dropAudio;
        public bool IsTwoHanded => heldCar ? heldCar.IsTwoHanded : false;
        /// <summary>
        /// Can pick up car if the holder is holding one
        /// </summary>
        public bool CanPickUp => heldCar && heldCar.transform.parent == transform;

        public IPickupable TryPickUp(Transform parent, int amount)
        {
            if (!CanPickUp) return null;

            if (heldCar && manager.Coins >= heldCar.Tier)
            {
                _ = heldCar.TryPickUp(parent);
                Utils.MoveToLayer(heldCar.transform, LayerMask.NameToLayer("Train"));
                heldCar.GetComponent<BoxCollider>().enabled = true;
                manager.Coins -= heldCar.Tier;
                priceText.text = string.Empty;

                AudioManager.PlaySound(PickupAudio, transform.position);

                return heldCar;
            }
            else return null;
        }

        public bool OnTryDrop(Vector3Int position) => false;
        public void Drop(Vector3Int position) { }

        public bool TryPlaceCar(TrainCar car)
        {
            if (CanPickUp) return false;
            else
            {
                car.transform.parent = transform;
                car.transform.localPosition = 1.1f * Vector3.up;
                car.transform.localRotation = Quaternion.identity;

                Utils.MoveToLayer(car.transform, LayerMask.NameToLayer("Edit Mode"));
                heldCar = car;
                priceText.text = $"{heldCar.Tier}¢";

                return true;
            }
        }

        public Interaction TryInteractUsing(IPickupable item)
        {
            if (item is TrainCar car)
            {
                if (car == heldCar && TryPlaceCar(car))
                {
                    AudioManager.PlaySound(DropAudio, transform.position);
                    manager.Coins += car.Tier;
                    return Interaction.Used;
                }
            }

            return Interaction.None;
        }
    }
}