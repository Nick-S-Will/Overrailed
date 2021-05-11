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

        void Start()
        {
            holders = new TrainCarHolder[carTypes.Length];
            panelWidth = GetComponent<SpriteRenderer>().size.x;

            for (int i = 0; i < carTypes.Length; i++)
            {
                holders[i] = Instantiate(holderPrefab, transform);
                var car = Instantiate(carTypes[i].tierPrefabs[0]);

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
            foreach (var h in holders) h.gameObject.SetActive(GameManager.instance.IsEditing);
        }

        [System.Serializable]
        private class SellPoint
        {
            public TrainCar[] tierPrefabs;
        }
    }
}