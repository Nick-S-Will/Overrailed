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

        private TrainCarHolder[] holders;
        private float panelWidth;

        private static int coins;

        public static int coinCount
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
            holders = new TrainCarHolder[carTypes.Length];
            panelWidth = GetComponent<SpriteRenderer>().size.x;
            coinCount = 1;

            for (int i = 0; i < carTypes.Length; i++)
            {
                holders[i] = Instantiate(holderPrefab, transform);
                var car = Instantiate(carTypes[i].CurrentTierPrefab);

                float panelInterval = panelWidth / (carTypes.Length + 1);
                float xOffset = (i + 1) * panelInterval - panelWidth / 2f;
                holders[i].transform.position = transform.position + xOffset * Vector3.right;
                holders[i].transform.rotation = Quaternion.identity;

                _ = holders[i].TryInteractUsing(car, new RaycastHit());
                holders[i].gameObject.SetActive(GameManager.instance.IsEditing);

                car.GetComponent<BoxCollider>().enabled = false;
            }

            GameManager.instance.OnCheckpoint += SetHolderVisibility;
            GameManager.instance.OnEndCheckpoint += SetHolderVisibility;
        }

        private void SetHolderVisibility()
        {
            bool isVisible = GameManager.instance.IsEditing;
            foreach (var h in holders) h.gameObject.SetActive(isVisible);
        }

        [System.Serializable]
        private class SellPoint
        {
            [SerializeField] private TrainCar[] tierPrefabs;
            
            private int index = 0;

            public TrainCar CurrentTierPrefab => tierPrefabs[index];

            public void ToNextTier() => index++;
        }
    }
}