using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Managers;
using Uncooked.Train;

namespace Uncooked.UI
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class TrainCarStore : MonoBehaviour
    {
        [SerializeField] private TrainCarHolder holderPrefab;
        [SerializeField] private SellPoint[] carTypes;

        private float panelWidth;

        private static int coins;

        public static int Coins
        {
            get { return coins; }
            set
            {
                HUDManager.instance.UpdateCoinsText(value.ToString());
                coins = value;
            }
        }

        void Start()
        {
            panelWidth = GetComponent<SpriteRenderer>().size.x;
            float panelInterval = panelWidth / (carTypes.Length + 1);
            Coins = 10;

            // Spawns in holders
            for (int i = 0; i < carTypes.Length; i++)
            {
                carTypes[i].holder = Instantiate(holderPrefab, transform);

                float xOffset = (i + 1) * panelInterval - panelWidth / 2f;
                carTypes[i].holder.transform.position = transform.position + xOffset * Vector3.right;
                carTypes[i].holder.transform.rotation = Quaternion.identity;

                carTypes[i].holder.gameObject.SetActive(GameManager.instance.CurrentState == GameState.Edit);
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
            foreach (var type in carTypes) type.holder.gameObject.SetActive(GameManager.instance.CurrentState == GameState.Edit);
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

            public TrainCar[] TierPrefabs => tierPrefabs;

            public bool TrySetNextCar()
            {
                if (nextIndex == tierPrefabs.Length - 1) return false;

                var car = Instantiate(tierPrefabs[nextIndex]);
                car.GetComponent<BoxCollider>().enabled = false;

                nextIndex++;
                return holder.TryPlaceCar(car);
            }
        }
    }
}