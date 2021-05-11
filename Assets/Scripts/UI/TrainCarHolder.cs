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
                heldCar.GetComponent<BoxCollider>().enabled = true;
                return heldCar.TryPickUp(parent, 1);
            }
            else return null; 
        }

        public bool OnTryDrop() => false;

        public void Drop(Vector3Int position) { }

        public bool TryInteractUsing(IPickupable item, RaycastHit hitInfo)
        {
            if (item is TrainCar car) return TryPlaceCar(car);
            else return false;
        }

        private bool TryPlaceCar(TrainCar car)
        {
            if (!IsHolding)
            {
                car.transform.parent = transform;
                car.transform.localPosition = 1.1f * Vector3.up;
                car.transform.localRotation = Quaternion.identity;

                heldCar = car;
                return true;
            }
            else return false;
        }
    }
}