using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Overrailed.Managers;
using Overrailed.Train;

namespace Overrailed.UI.Shop
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class TrainStoreManager : MonoBehaviour
    {
        public System.Action<string> OnCoinsChange;

        [SerializeField] private TrainCarHolder holderPrefab;
        [SerializeField] private SellPoint[] carTypes;

        private int coins;

        public int Coins
        {
            get { return coins; }
            set
            {
                coins = value;
                OnCoinsChange?.Invoke(coins.ToString());
            }
        }

        void Start()
        {
            float panelWidth = GetComponent<SpriteRenderer>().size.x;
            float panelInterval = panelWidth / (carTypes.Length + 1);
            Coins = 3;

            // Spawns in holders
            for (int i = 0; i < carTypes.Length; i++)
            {
                carTypes[i].holder = Instantiate(holderPrefab, transform);
                carTypes[i].holder.shopManager = this;

                float xOffset = (i + 1) * panelInterval - panelWidth / 2f;
                carTypes[i].holder.transform.position = transform.position + xOffset * Vector3.right;
                carTypes[i].holder.transform.rotation = Quaternion.identity;

                carTypes[i].holder.gameObject.SetActive(Manager.IsEditing());
            }

            if (Manager.instance is GameManager gm)
            {
                gm.OnCheckpoint += UpdateHoldersCars;
                gm.OnEndCheckpoint += UpdateHolderVisibility;
            }
        }

        private void UpdateHoldersCars()
        {
            foreach (var type in carTypes) _ = type.TrySetNextCar();

            UpdateHolderVisibility();
        }

        private void UpdateHolderVisibility()
        {
            foreach (var type in carTypes) type.holder.gameObject.SetActive(Manager.IsEditing());
        }

        void OnDestroy()
        {
            if (Manager.instance is GameManager gm)
            {
                gm.OnCheckpoint -= UpdateHoldersCars;
                gm.OnEndCheckpoint -= UpdateHolderVisibility;
            }
        }

        [System.Serializable]
        private class SellPoint
        {
            [SerializeField] private TrainCar[] tierPrefabs;

            [HideInInspector] public TrainCarHolder holder;
            private int nextIndex = 0;

            public bool TrySetNextCar()
            {
                if (nextIndex == tierPrefabs.Length || holder.CanPickUp) return false;

                var car = Instantiate(tierPrefabs[nextIndex]);
                Utils.MoveToLayer(car.transform, LayerMask.NameToLayer("Edit Mode"));
                car.GetComponent<BoxCollider>().enabled = false;

                nextIndex++;
                return holder.TryPlaceCar(car);
            }
        }
    }
}