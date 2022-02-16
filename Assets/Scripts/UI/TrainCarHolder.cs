using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Managers;
using Uncooked.Train;

namespace Uncooked.UI
{
    public class TrainCarHolder : MonoBehaviour, IInteractable, IPickupable
    {
        public TrainStoreManager manager { private get; set; }
        private TrainCar heldCar;

        public bool IsTwoHanded() => heldCar ? heldCar.IsTwoHanded() : false;
        public bool IsHolding => heldCar && heldCar.transform.parent == transform;

        public IPickupable TryPickUp(Transform parent, int amount)
        {
            if (IsHolding)
            {
                var car = heldCar.TryPickUp(parent, 1) as TrainCar;
                if (car && manager.Coins >= car.Tier)
                {
                    GameManager.MoveToLayer(car.transform, LayerMask.NameToLayer("Train"));
                    heldCar.GetComponent<BoxCollider>().enabled = true;
                    manager.Coins -= car.Tier;
                    return car;
                }
            }

            return null;
        }

        public bool OnTryDrop() => false;
        public void Drop(Vector3Int position) { }

        public bool TryPlaceCar(TrainCar car)
        {
            if (IsHolding) return false;
            else
            {
                car.transform.parent = transform;
                car.transform.localPosition = 1.1f * Vector3.up;
                car.transform.localRotation = Quaternion.identity;

                GameManager.MoveToLayer(car.transform, LayerMask.NameToLayer("Edit Mode"));

                heldCar = car;
                return true;
            }
        }

        public bool TryInteractUsing(IPickupable item)
        {
            if (item is TrainCar car)
            {
                if (car == heldCar && TryPlaceCar(car))
                {
                    manager.Coins += car.Tier;
                    return true;
                }
            }
            
            return false;
        }
    }
}