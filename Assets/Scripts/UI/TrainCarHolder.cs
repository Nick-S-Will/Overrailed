using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Managers.Audio;
using Overrailed.Train;

namespace Overrailed.UI
{
    public class TrainCarHolder : MonoBehaviour, IInteractable, IPickupable
    {
        [SerializeField] private AudioClip pickupAudio, dropAudio;

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

            var car = heldCar.TryPickUp(parent) as TrainCar;
            if (car && manager.Coins >= car.Tier)
            {
                Utils.MoveToLayer(car.transform, LayerMask.NameToLayer("Train"));
                heldCar.GetComponent<BoxCollider>().enabled = true;
                manager.Coins -= car.Tier;

                AudioManager.PlaySound(PickupAudio, transform.position);

                return car;
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