using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Terrain.Tiles;

namespace Uncooked.Train
{
    public class HolderCar : TrainCar
    {
        [Space]
        [SerializeField] private Transform holderSpawnPoint;

        public Transform SpawnPoint => holderSpawnPoint;
        // TODO: Be able to pickup rails while crafting
        public bool CanPickup => holderSpawnPoint.childCount == 1 && holderSpawnPoint.GetChild(0).GetComponent<BoxCollider>().enabled;

        public override IPickupable TryPickUp(Transform parent, int amount)
        {
            if (CanPickup) return TryPickupCraft(parent, amount);
            else return base.TryPickUp(parent, amount);
        }

        private StackTile TryPickupCraft(Transform parent, int amount)
        {
            if (holderSpawnPoint.childCount == 0) return null;

            return (StackTile)holderSpawnPoint.GetChild(0).GetComponent<StackTile>().TryPickUp(parent, amount);
        }
    }
}