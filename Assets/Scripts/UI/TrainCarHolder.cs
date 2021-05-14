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

        public IPickupable TryPickUp(Transform parent, int amount) => TryBuyCar(parent);
        
        public bool OnTryDrop() => false;

        public void Drop(Vector3Int position) { }

        public bool TryInteractUsing(IPickupable item, RaycastHit hitInfo)
        {
            if (item is TrainCar car) return TryPlaceCar(car);
            else return false;
        }

        /// <summary>
        /// Tries to pick up heldCar and deduct heldCar.Price from TrainCarStore.coinCount
        /// </summary>
        /// <param name="parent">The transform heldCar will be parented to</param>
        /// <returns>The heldCar if there's enough coins in TrainCarStore.coinCount, otherwise null</returns>
        private TrainCar TryBuyCar(Transform parent)
        {
            if (IsHolding && TrainCarStore.coinCount >= heldCar.Price)
            {
                TrainCarStore.coinCount -= heldCar.Price;
                heldCar.GetComponent<BoxCollider>().enabled = true;
                return (TrainCar)heldCar.TryPickUp(parent, 1);
            }
            else return null;
        }

        /// <summary>
        /// Tries to put given car back on this, and refunds heldCar.Price to TrainCarStore.coinCount
        /// </summary>
        /// <param name="car">The car being put on this</param>
        /// <returns>True if can place car on this, otherwise false</returns>
        private bool TryPlaceCar(TrainCar car)
        {
            if (!IsHolding && (heldCar == null || car == heldCar))
            {
                car.transform.parent = transform;
                car.transform.localPosition = 1.1f * Vector3.up;
                car.transform.localRotation = Quaternion.identity;
                car.GetComponent<BoxCollider>().isTrigger = true;

                if (heldCar == null) heldCar = car;
                else TrainCarStore.coinCount += car.Price;

                return true;
            }
            else return false;
        }
    }
}