using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Terrain;

namespace Uncooked.Train
{
    public class HolderCar : TrainCar
    {
        [Space]
        [SerializeField] private Transform holderSpawnPoint;
        public bool canPickup;

        public Transform SpawnPoint => holderSpawnPoint;

        public override IPickupable TryPickUp(Transform parent, int amount)
        {
            if (canPickup) return TryPickupCraft(parent, amount);
            else return base.TryPickUp(parent, amount);
        }

        private StackTile TryPickupCraft(Transform parent, int amount)
        {
            if (holderSpawnPoint.childCount == 0) return null;

            return (StackTile)holderSpawnPoint.GetChild(0).GetComponent<StackTile>().TryPickUp(parent, amount);
        }
    }
}