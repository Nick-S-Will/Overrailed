using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Train;
using Uncooked.UI;

namespace Uncooked.Managers
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
                OnCoinsChange(coins.ToString());
            }
        }

        void Start()
        {
            float panelWidth = GetComponent<SpriteRenderer>().size.x;
            float panelInterval = panelWidth / (carTypes.Length + 1);
            Coins = 10;

            // Spawns in holders
            for (int i = 0; i < carTypes.Length; i++)
            {
                carTypes[i].holder = Instantiate(holderPrefab, transform);
                carTypes[i].holder.manager = this;

                float xOffset = (i + 1) * panelInterval - panelWidth / 2f;
                carTypes[i].holder.transform.position = transform.position + xOffset * Vector3.right;
                carTypes[i].holder.transform.rotation = Quaternion.identity;

                carTypes[i].holder.gameObject.SetActive(GameManager.IsEditing());
            }

            GameManager.instance.OnCheckpoint += UpdateHoldersCars;
            GameManager.instance.OnEndCheckpoint += SetHolderVisibility;
        }

        private void UpdateHoldersCars()
        {
            foreach (var type in carTypes) _ = type.TrySetNextCar();

            SetHolderVisibility();
        }

        private void SetHolderVisibility()
        {
            foreach (var type in carTypes) type.holder.gameObject.SetActive(GameManager.IsEditing());
        }

        void OnDestroy()
        {
            if (GameManager.instance)
            {
                GameManager.instance.OnCheckpoint -= UpdateHoldersCars;
                GameManager.instance.OnEndCheckpoint -= SetHolderVisibility;
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
                if (nextIndex == tierPrefabs.Length) return false;

                var car = Instantiate(tierPrefabs[nextIndex]);
                GameManager.MoveToLayer(car.transform, LayerMask.NameToLayer("Edit Mode"));
                car.GetComponent<BoxCollider>().enabled = false;

                nextIndex++;
                return holder.TryPlaceCar(car);
            }
        }
    }
}