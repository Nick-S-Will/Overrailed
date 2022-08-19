using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Managers;
using Overrailed.Terrain.Tiles;

namespace Overrailed.Train
{
    public class HolderCar : TrainCar
    {
        /// <summary>
        /// Invoked when a player picks up a tile from the holder
        /// </summary>
        public event Action OnTaken;
        public event Action<TrainCar> OnUpgrade;
        public override event Action OnPickUp;
        public override event Action OnDrop;

        [Space]
        [SerializeField] private Transform holderSpawnPoint;

        public Transform SpawnPoint => holderSpawnPoint;

        /// <summary>
        /// Adds 0.5 to the holder car's count of tiles which represent the beginning and end of a craft
        /// </summary>
        public void AddPartTile() => holdCount += 0.5f;

        public float holdCount { get; private set; }

        public bool HasSpace => Mathf.CeilToInt(holdCount) < 2 + 2 * tier;
        public override bool IsWarning => false;

        public override IPickupable TryPickUp(Transform parent, int amount)
        {
            if (Manager.IsEditing()) return base.TryPickUp(parent, amount);
            else return TryPickupCraftedTile(parent, amount);
        }

        private StackTile TryPickupCraftedTile(Transform parent, int amount)
        {
            if (holdCount < 1) return null;

            // Gets whole stack
            var holdersContent = (StackTile)holderSpawnPoint.GetChild(0).GetComponent<StackTile>().TryPickUp(parent, int.MaxValue);

            // Puts back extra if there's too much to pick up
            if (amount < holdCount || holdCount % 1 != 0)
            {
                int extraCount = Mathf.Max(Mathf.CeilToInt(holdCount) - amount, 1);

                var extraStack = (StackTile)holdersContent.TryPickUp(holderSpawnPoint, extraCount);
                extraStack.transform.localPosition = Vector3.zero;
            }

            Utils.MoveToLayer(holdersContent.transform, LayerMask.NameToLayer("Default"));
            holdCount = Mathf.Max(holdCount - amount, holdCount % 1);

            OnTaken?.Invoke();
            OnPickUp?.Invoke();
            return holdersContent;
        }

        protected override bool TryUpgradeCar(TrainCar newCar)
        {
            if (base.TryUpgradeCar(newCar))
            {
                OnUpgrade?.Invoke(newCar);
                return true;
            }
            else return false;
        }
    }
}