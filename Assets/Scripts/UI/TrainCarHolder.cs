using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Train;

namespace Uncooked.UI
{
    public class TrainCarHolder : MonoBehaviour, IInteractable, IPickupable
    {
        private TrainCar heldCar;

        public bool IsTwoHanded() => heldCar ? heldCar.IsTwoHanded() : false;
        public bool IsHolding => heldCar && heldCar.transform.parent == transform;


        public IPickupable TryPickUp(Transform parent, int amount)
        {
            if (IsHolding)
            {
                var car = heldCar.TryPickUp(parent, 1) as TrainCar;
                if (car != null && TrainCarStore.Coins >= car.Tier)
                {
                    heldCar.GetComponent<BoxCollider>().enabled = true;
                    TrainCarStore.Coins -= car.Tier;
                    return car;
                }
            }

            return null;
        }

        public bool OnTryDrop() => false;

        public void Drop(Vector3Int position) { }

        public bool TryInteractUsing(IPickupable item, RaycastHit hitInfo)
        {
            if (item is TrainCar car)
            {
                if (car == heldCar && TryPlaceCar(car))
                {
                    TrainCarStore.Coins += car.Tier;
                    return true;
                }
            }
            
            return false;
        }

        public bool TryPlaceCar(TrainCar car)
        {
            if (IsHolding) return false;
            else
            {
                car.transform.parent = transform;
                car.transform.localPosition = 1.1f * Vector3.up;
                car.transform.localRotation = Quaternion.identity;

                heldCar = car;
                return true;
            }
        }
    }
}